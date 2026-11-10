namespace HelpDeskPro.UnitTests.Features.Sla;

using FluentAssertions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Sla.Commands.CheckSlaBreaches;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.UnitTests.Common;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Xunit;

public class CheckSlaBreachesCommandHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationService _notificationService;
    private readonly ILogger<CheckSlaBreachesCommandHandler> _logger;
    private readonly CheckSlaBreachesCommandHandler _handler;

    private readonly DateTime _utcNow = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);
    private readonly Guid _agentId = Guid.NewGuid();
    private readonly Guid _sup1Id = Guid.NewGuid(); // Level 2 Lead supervisor
    private readonly Guid _sup2Id = Guid.NewGuid(); // Level 1 Operational supervisor
    private readonly Department _itDept = new() { Id = 1, Code = "IT", Name = "IT" };

    public CheckSlaBreachesCommandHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _notificationService = Substitute.For<INotificationService>();
        _logger = Substitute.For<ILogger<CheckSlaBreachesCommandHandler>>();

        _dateTimeProvider.UtcNow.Returns(_utcNow);

        _handler = new CheckSlaBreachesCommandHandler(
            _context,
            _dateTimeProvider,
            _notificationService,
            _logger);
    }

    private List<ApplicationUser> CreateSampleUsers()
    {
        var leadSupervisor = new ApplicationUser
        {
            Id = _sup1Id,
            FullName = "Dewi Lestari (Lead Supervisor)",
            DepartmentId = _itDept.Id,
            Department = _itDept,
            SupervisorId = null
        };

        var opSupervisor = new ApplicationUser
        {
            Id = _sup2Id,
            FullName = "Hendra Wijaya (Op Supervisor)",
            DepartmentId = _itDept.Id,
            Department = _itDept,
            SupervisorId = _sup1Id
        };

        var agent = new ApplicationUser
        {
            Id = _agentId,
            FullName = "Doni Agent",
            DepartmentId = _itDept.Id,
            Department = _itDept,
            SupervisorId = _sup2Id
        };

        return [leadSupervisor, opSupervisor, agent];
    }

    private Ticket CreateTicket(
        long id = 1,
        TicketStatus status = TicketStatus.Open,
        DateTime? responseDueAt = null,
        DateTime? resolutionDueAt = null,
        DateTime? firstRespondedAt = null,
        DateTime? resolvedAt = null)
    {
        var users = CreateSampleUsers();
        var agent = users.First(u => u.Id == _agentId);

        var slaPolicy = new SlaPolicy { Id = 1, Priority = TicketPriority.High, ResponseTimeMinutes = 120, ResolutionTimeMinutes = 480 };

        var ticket = Ticket.Create(
            $"HD-2026-{id:D6}",
            "Tiket Uji SLA",
            "Deskripsi uji SLA",
            1,
            TicketPriority.High,
            Guid.NewGuid(),
            slaPolicy,
            _utcNow.AddHours(-10),
            "requester@helpdeskpro.local");

        ticket.Id = id;
        ticket.Status = status;
        ticket.AssigneeId = agent.Id;
        ticket.Assignee = agent;
        ticket.ResponseDueAt = responseDueAt ?? _utcNow.AddHours(-1);
        ticket.ResolutionDueAt = resolutionDueAt ?? _utcNow.AddHours(2);
        ticket.FirstRespondedAt = firstRespondedAt;
        ticket.ResolvedAt = resolvedAt;

        return ticket;
    }

    [Fact]
    public async Task Handle_TicketJustBeforeResponseDeadline_ShouldNotEscalate()
    {
        // Arrange
        var ticket = CreateTicket(1, responseDueAt: _utcNow.AddMinutes(1), firstRespondedAt: null);
        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        var usersSet = CreateSampleUsers().AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);
        _context.Users.Returns(usersSet);

        var command = new CheckSlaBreachesCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ResponseBreachesCount.Should().Be(0);
        ticket.Escalations.Should().BeEmpty();
        await _notificationService.DidNotReceive().CreateNotificationAsync(
            Arg.Any<Guid>(), Arg.Any<long?>(), Arg.Any<NotificationType>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_TicketPastResponseDeadline_ShouldCreateLevel1EscalationAndSendNotification_AC04()
    {
        // Arrange: Critical ticket not responded 31 minutes
        var ticket = CreateTicket(1, responseDueAt: _utcNow.AddMinutes(-1), firstRespondedAt: null);
        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        var usersSet = CreateSampleUsers().AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);
        _context.Users.Returns(usersSet);

        var command = new CheckSlaBreachesCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert (AC-04)
        result.ResponseBreachesCount.Should().Be(1);
        ticket.Escalations.Should().ContainSingle(e =>
            e.Level == 1 &&
            e.Reason == EscalationReason.SlaResponseBreach &&
            e.EscalatedToId == _sup2Id);
        ticket.EscalationLevel.Should().Be(1);

        await _notificationService.Received(1).CreateNotificationAsync(
            _sup2Id,
            ticket.Id,
            NotificationType.Escalation,
            Arg.Is<string>(s => s.Contains("Eskalasi Level 1") && s.Contains(ticket.TicketNo)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RunTwice_ShouldBeIdempotentAndNotCreateDuplicateEscalation_FR24()
    {
        // Arrange
        var ticket = CreateTicket(1, responseDueAt: _utcNow.AddMinutes(-5), firstRespondedAt: null);
        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        var usersSet = CreateSampleUsers().AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);
        _context.Users.Returns(usersSet);

        var command = new CheckSlaBreachesCommand();

        // Act: Run 1st time
        var result1 = await _handler.Handle(command, CancellationToken.None);
        result1.ResponseBreachesCount.Should().Be(1);
        ticket.Escalations.Count.Should().Be(1);

        // Act: Run 2nd time (FR-24: run twice)
        var result2 = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result2.ResponseBreachesCount.Should().Be(0);
        ticket.Escalations.Count.Should().Be(1); // Tetap satu, tidak duplikat!
    }

    [Fact]
    public async Task Handle_TicketInPendingStatus_ShouldNotTriggerResolutionBreach_FR27()
    {
        // Arrange: status Pending, resolution due date passed
        var ticket = CreateTicket(
            1,
            status: TicketStatus.Pending,
            resolutionDueAt: _utcNow.AddHours(-1),
            firstRespondedAt: _utcNow.AddHours(-5));

        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        var usersSet = CreateSampleUsers().AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);
        _context.Users.Returns(usersSet);

        var command = new CheckSlaBreachesCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert (FR-27: tiket Pending tidak dihitung)
        result.ResolutionBreachesCount.Should().Be(0);
        ticket.IsSlaBreached.Should().BeFalse();
        ticket.Escalations.Should().NotContain(e => e.Reason == EscalationReason.SlaResolutionBreach);
    }

    [Fact]
    public async Task Handle_TicketPastResolutionDeadline_ShouldSetIsSlaBreachedAndCreateLevel2Escalation_FR23()
    {
        // Arrange: status InProgress, resolution due date passed
        var ticket = CreateTicket(
            1,
            status: TicketStatus.InProgress,
            resolutionDueAt: _utcNow.AddMinutes(-10),
            firstRespondedAt: _utcNow.AddHours(-3));

        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        var usersSet = CreateSampleUsers().AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);
        _context.Users.Returns(usersSet);

        var command = new CheckSlaBreachesCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert (FR-23)
        result.ResolutionBreachesCount.Should().Be(1);
        ticket.IsSlaBreached.Should().BeTrue();
        ticket.Escalations.Should().ContainSingle(e =>
            e.Level == 2 &&
            e.Reason == EscalationReason.SlaResolutionBreach &&
            e.EscalatedToId == _sup1Id); // Dewi Lestari (atasan Hendra Wijaya)
        ticket.EscalationLevel.Should().Be(2);

        await _notificationService.Received(1).CreateNotificationAsync(
            _sup1Id,
            ticket.Id,
            NotificationType.Escalation,
            Arg.Is<string>(s => s.Contains("Eskalasi Level 2")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ResolvedTicketPast3Days_ShouldAutoClose_FR15()
    {
        // Arrange
        var ticket = CreateTicket(
            1,
            status: TicketStatus.Resolved,
            resolvedAt: _utcNow.AddDays(-3).AddHours(-1));

        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        var usersSet = CreateSampleUsers().AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);
        _context.Users.Returns(usersSet);

        var command = new CheckSlaBreachesCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert (FR-15)
        result.AutoClosedCount.Should().Be(1);
        ticket.Status.Should().Be(TicketStatus.Closed);
        ticket.ClosedAt.Should().Be(_utcNow);

        await _notificationService.Received(1).CreateNotificationAsync(
            ticket.RequesterId,
            ticket.Id,
            NotificationType.StatusChanged,
            Arg.Is<string>(s => s.Contains("otomatis ditutup setelah 3 hari")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ResolvedTicketLessThan3Days_ShouldNotAutoClose()
    {
        // Arrange
        var ticket = CreateTicket(
            1,
            status: TicketStatus.Resolved,
            resolvedAt: _utcNow.AddDays(-2));

        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        var usersSet = CreateSampleUsers().AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);
        _context.Users.Returns(usersSet);

        var command = new CheckSlaBreachesCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.AutoClosedCount.Should().Be(0);
        ticket.Status.Should().Be(TicketStatus.Resolved);
        ticket.ClosedAt.Should().BeNull();
    }

    [Fact]
    public async Task Handle_WhenNoSupervisorFound_LogsWarningAndContinuesWithoutException()
    {
        // Arrange: Agent has no supervisor, and no IT supervisor in DB
        var agentWithoutSupervisor = new ApplicationUser
        {
            Id = _agentId,
            FullName = "Agent Solo",
            DepartmentId = _itDept.Id,
            Department = _itDept,
            SupervisorId = null
        };

        var ticket = CreateTicket(1, responseDueAt: _utcNow.AddMinutes(-5), firstRespondedAt: null);
        ticket.Assignee = agentWithoutSupervisor;

        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        var usersSet = new List<ApplicationUser> { agentWithoutSupervisor }.AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);
        _context.Users.Returns(usersSet);

        var command = new CheckSlaBreachesCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.ResponseBreachesCount.Should().Be(0);
        ticket.Escalations.Should().BeEmpty(); // No exception, logged warning
    }

    [Fact]
    public async Task Handle_AutoCloseWithoutCurrentUser_ShouldSucceedAndRecordHistoryWithNullChangedById()
    {
        // Arrange: Resolved ticket older than 3 days executed by background job (no user)
        var ticket = CreateTicket(
            1,
            status: TicketStatus.Resolved,
            resolvedAt: _utcNow.AddDays(-4));

        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        var usersSet = CreateSampleUsers().AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);
        _context.Users.Returns(usersSet);

        var command = new CheckSlaBreachesCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.AutoClosedCount.Should().Be(1);
        ticket.Status.Should().Be(TicketStatus.Closed);
        ticket.ClosedAt.Should().Be(_utcNow);
        ticket.Histories.Should().ContainSingle(h =>
            h.FieldName == "Status" &&
            h.NewValue == "Closed" &&
            h.ChangedById == null);
    }

    [Fact]
    public async Task Handle_WhenOneTicketFailsDuringAutoClose_ShouldLogErrorAndContinueProcessingRemainingTickets()
    {
        // Arrange: 2 resolved tickets older than 3 days.
        var ticket1 = CreateTicket(1, status: TicketStatus.Resolved, resolvedAt: _utcNow.AddDays(-4));
        var ticket2 = CreateTicket(2, status: TicketStatus.Resolved, resolvedAt: _utcNow.AddDays(-4));

        var ticketsSet = new List<Ticket> { ticket1, ticket2 }.AsTestDbSet();
        var usersSet = CreateSampleUsers().AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);
        _context.Users.Returns(usersSet);

        // Make ticket1 fail when notification is created
        _notificationService.CreateNotificationAsync(
            ticket1.RequesterId,
            ticket1.Id,
            NotificationType.StatusChanged,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("Simulated notification failure"));

        var command = new CheckSlaBreachesCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert: Ticket 2 still auto-closed despite ticket 1 failing
        result.AutoClosedCount.Should().Be(1);
        ticket2.Status.Should().Be(TicketStatus.Closed);
        ticket2.Histories.Should().ContainSingle(h => h.FieldName == "Status" && h.NewValue == "Closed" && h.ChangedById == null);

        // Ticket 1 error logged with TicketNo
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains(ticket1.TicketNo)),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public async Task Handle_WhenOneTicketFailsDuringSlaCheck_ShouldLogErrorAndContinueProcessingRemainingTickets()
    {
        // Arrange: 2 active tickets with response SLA breached.
        var ticket1 = CreateTicket(1, responseDueAt: _utcNow.AddMinutes(-10), firstRespondedAt: null);
        var ticket2 = CreateTicket(2, responseDueAt: _utcNow.AddMinutes(-10), firstRespondedAt: null);

        var ticketsSet = new List<Ticket> { ticket1, ticket2 }.AsTestDbSet();
        var usersSet = CreateSampleUsers().AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);
        _context.Users.Returns(usersSet);

        // Make ticket1 fail when notification is created for escalation
        _notificationService.CreateNotificationAsync(
            _sup2Id,
            ticket1.Id,
            NotificationType.Escalation,
            Arg.Any<string>(),
            Arg.Any<CancellationToken>())
            .Returns<Task>(_ => throw new InvalidOperationException("Simulated escalation notification failure"));

        var command = new CheckSlaBreachesCommand();

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert: Ticket 2 still escalated despite ticket 1 failing
        result.ResponseBreachesCount.Should().Be(1);
        ticket2.Escalations.Should().ContainSingle(e => e.Level == 1);

        // Ticket 1 error logged with TicketNo
        _logger.Received(1).Log(
            LogLevel.Error,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains(ticket1.TicketNo)),
            Arg.Any<Exception>(),
            Arg.Any<Func<object, Exception?, string>>());
    }
}

