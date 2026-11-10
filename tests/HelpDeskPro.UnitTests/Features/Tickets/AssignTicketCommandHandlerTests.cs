namespace HelpDeskPro.UnitTests.Features.Tickets;

using FluentAssertions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Tickets.Commands.AssignTicket;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.Domain.Exceptions;
using HelpDeskPro.UnitTests.Common;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

public class AssignTicketCommandHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationService _notificationService;
    private readonly AssignTicketCommandHandler _handler;

    private readonly Guid _agentId = Guid.NewGuid();
    private readonly DateTime _utcNow = new(2026, 11, 10, 10, 0, 0, DateTimeKind.Utc);
    private readonly Department _itDept = new() { Id = 1, Code = "IT", Name = "Information Technology" };
    private readonly Department _hrDept = new() { Id = 2, Code = "HR", Name = "Human Resources" };
    private readonly byte[] _defaultRowVersion = [1, 2, 3, 4];

    public AssignTicketCommandHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _notificationService = Substitute.For<INotificationService>();

        _dateTimeProvider.UtcNow.Returns(_utcNow);
        _currentUserService.UserId.Returns(_agentId.ToString());
        _currentUserService.Role.Returns("Agent");

        _handler = new AssignTicketCommandHandler(
            _context,
            _currentUserService,
            _dateTimeProvider,
            _notificationService);
    }

    private Ticket CreateSampleTicket(long id = 1)
    {
        var category = new Category { Id = 1, Name = "Hardware" };
        var requester = new ApplicationUser { Id = Guid.NewGuid(), FullName = "Budi Requester", Department = _hrDept };
        var slaPolicy = new SlaPolicy { Id = 1, Priority = TicketPriority.High, ResponseTimeMinutes = 60, ResolutionTimeMinutes = 240 };

        var ticket = Ticket.Create(
            "HD-2026-000001",
            "Monitor Mati",
            "Monitor tidak menyala",
            1,
            TicketPriority.High,
            requester.Id,
            slaPolicy,
            _utcNow.AddHours(-1),
            "requester@helpdeskpro.local");

        ticket.Id = id;
        ticket.Category = category;
        ticket.Requester = requester;
        ticket.RowVersion = _defaultRowVersion;

        return ticket;
    }

    [Fact]
    public async Task Handle_ValidAgentSelfAssign_ShouldSucceed()
    {
        // Arrange
        var ticket = CreateSampleTicket();
        var agent = new ApplicationUser
        {
            Id = _agentId,
            FullName = "Agent IT Satu",
            Department = _itDept,
            DepartmentId = _itDept.Id
        };

        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        var usersSet = new List<ApplicationUser> { agent }.AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);
        _context.Users.Returns(usersSet);

        var command = new AssignTicketCommand(ticket.Id, _agentId, _defaultRowVersion);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(ticket.Id);
        result.AssigneeId.Should().Be(_agentId);
        result.Status.Should().Be(TicketStatus.InProgress);
        result.FirstRespondedAt.Should().Be(_utcNow);
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenAssigneeNotInITDept_ShouldThrowBusinessRuleException()
    {
        // Arrange
        var ticket = CreateSampleTicket();
        var nonItUser = new ApplicationUser
        {
            Id = Guid.NewGuid(),
            FullName = "Staff HR",
            Department = _hrDept,
            DepartmentId = _hrDept.Id
        };

        _currentUserService.Role.Returns("Supervisor");
        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        var usersSet = new List<ApplicationUser> { nonItUser }.AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);
        _context.Users.Returns(usersSet);

        var command = new AssignTicketCommand(ticket.Id, nonItUser.Id, _defaultRowVersion);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<BusinessRuleException>()
            .WithMessage("Tiket hanya dapat ditugaskan kepada staf di departemen IT.");
    }

    [Fact]
    public async Task Handle_WhenTicketNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var ticketsSet = new List<Ticket>().AsTestDbSet();
        var usersSet = new List<ApplicationUser>().AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);
        _context.Users.Returns(usersSet);

        var command = new AssignTicketCommand(999, _agentId, _defaultRowVersion);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenAssigneeNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var ticket = CreateSampleTicket();
        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        var usersSet = new List<ApplicationUser>().AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);
        _context.Users.Returns(usersSet);

        var command = new AssignTicketCommand(ticket.Id, Guid.NewGuid(), _defaultRowVersion);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenRowVersionMismatch_ShouldThrowDbUpdateConcurrencyException()
    {
        // Arrange
        var ticket = CreateSampleTicket();
        var agent = new ApplicationUser
        {
            Id = _agentId,
            FullName = "Agent IT Satu",
            Department = _itDept,
            DepartmentId = _itDept.Id
        };

        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        var usersSet = new List<ApplicationUser> { agent }.AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);
        _context.Users.Returns(usersSet);

        var staleRowVersion = new byte[] { 9, 9, 9, 9 };
        var command = new AssignTicketCommand(ticket.Id, _agentId, staleRowVersion);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<DbUpdateConcurrencyException>()
            .WithMessage("Tiket sudah diubah pengguna lain, muat ulang halaman.");
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        _currentUserService.UserId.Returns((string?)null);
        var command = new AssignTicketCommand(1, _agentId, _defaultRowVersion);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
