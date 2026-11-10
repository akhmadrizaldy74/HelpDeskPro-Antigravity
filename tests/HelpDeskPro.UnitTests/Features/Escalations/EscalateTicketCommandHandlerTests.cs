namespace HelpDeskPro.UnitTests.Features.Escalations;

using FluentAssertions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Tickets.Commands.EscalateTicket;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.Domain.Exceptions;
using HelpDeskPro.UnitTests.Common;
using NSubstitute;
using Xunit;

public class EscalateTicketCommandHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationService _notificationService;
    private readonly EscalateTicketCommandHandler _handler;

    private readonly Guid _agentId = Guid.NewGuid();
    private readonly Guid _supervisorId = Guid.NewGuid();
    private readonly DateTime _utcNow = new(2026, 11, 10, 10, 0, 0, DateTimeKind.Utc);
    private readonly Department _itDept = new() { Id = 1, Code = "IT", Name = "IT" };

    public EscalateTicketCommandHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _notificationService = Substitute.For<INotificationService>();

        _dateTimeProvider.UtcNow.Returns(_utcNow);
        _currentUserService.UserId.Returns(_agentId.ToString());
        _currentUserService.Role.Returns("Agent");

        _handler = new EscalateTicketCommandHandler(
            _context,
            _currentUserService,
            _dateTimeProvider,
            _notificationService);
    }

    private Ticket CreateSampleTicket(long id = 1)
    {
        var category = new Category { Id = 1, Name = "Hardware" };
        var requester = new ApplicationUser { Id = Guid.NewGuid(), FullName = "Budi Requester" };
        var slaPolicy = new SlaPolicy { Id = 1, Priority = TicketPriority.High, ResponseTimeMinutes = 60, ResolutionTimeMinutes = 240 };

        var ticket = Ticket.Create(
            "HD-2026-000001",
            "Monitor Rusak",
            "Monitor mati total",
            1,
            TicketPriority.High,
            requester.Id,
            slaPolicy,
            _utcNow.AddHours(-1),
            "requester@helpdeskpro.local");

        ticket.Id = id;
        ticket.Category = category;
        ticket.Requester = requester;

        return ticket;
    }

    [Fact]
    public async Task Handle_AgentManualEscalation_ShouldSucceedAndCreateLevel1Escalation()
    {
        // Arrange
        var ticket = CreateSampleTicket();
        var supervisor = new ApplicationUser { Id = _supervisorId, FullName = "Hendra Supervisor", Department = _itDept };
        var agent = new ApplicationUser { Id = _agentId, FullName = "Doni Agent", Department = _itDept, SupervisorId = _supervisorId };

        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        var usersSet = new List<ApplicationUser> { agent, supervisor }.AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);
        _context.Users.Returns(usersSet);

        var command = new EscalateTicketCommand(ticket.Id, "Eskalasi manual butuh bantuan supervisor");

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Level.Should().Be(1);
        result.Reason.Should().Be(EscalationReason.Manual);
        result.Note.Should().Be("Eskalasi manual butuh bantuan supervisor");
        result.EscalatedToId.Should().Be(_supervisorId);
        ticket.EscalationLevel.Should().Be(1);

        await _notificationService.Received(1).CreateNotificationAsync(
            _supervisorId,
            ticket.Id,
            NotificationType.Escalation,
            Arg.Is<string>(s => s.Contains("Eskalasi Manual")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_RequesterAttemptsEscalation_ThrowsBusinessRuleException()
    {
        // Arrange
        _currentUserService.Role.Returns("Requester");
        var command = new EscalateTicketCommand(1, "Alasan eskalasi requster coba coba");

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("Requester tidak memiliki wewenang untuk melakukan eskalasi tiket.");
    }

    [Fact]
    public async Task Handle_TicketNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var ticketsSet = new List<Ticket>().AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);

        var command = new EscalateTicketCommand(999, "Alasan eskalasi pada tiket fiktif");

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}

