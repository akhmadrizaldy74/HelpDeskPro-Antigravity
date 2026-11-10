namespace HelpDeskPro.UnitTests.Features.Tickets;

using FluentAssertions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Tickets.Commands.ChangeTicketStatus;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.Domain.Exceptions;
using HelpDeskPro.UnitTests.Common;
using Microsoft.EntityFrameworkCore;
using NSubstitute;
using Xunit;

public class ChangeTicketStatusCommandHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationService _notificationService;
    private readonly ISlaCalculator _slaCalculator;
    private readonly ChangeTicketStatusCommandHandler _handler;

    private readonly Guid _agentId = Guid.NewGuid();
    private readonly Guid _requesterId = Guid.NewGuid();
    private readonly DateTime _utcNow = new(2026, 11, 10, 10, 0, 0, DateTimeKind.Utc);
    private readonly byte[] _defaultRowVersion = [1, 2, 3, 4];

    public ChangeTicketStatusCommandHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _notificationService = Substitute.For<INotificationService>();
        _slaCalculator = Substitute.For<ISlaCalculator>();

        _dateTimeProvider.UtcNow.Returns(_utcNow);
        _currentUserService.UserId.Returns(_agentId.ToString());
        _currentUserService.Role.Returns("Agent");

        _slaCalculator.AdjustDueDatesForPausedDurationAsync(Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
            .Returns(callInfo =>
            {
                var cur = callInfo.ArgAt<DateTime>(0);
                var paused = callInfo.ArgAt<DateTime>(1);
                var resumed = callInfo.ArgAt<DateTime>(2);
                var pausedMins = (int)Math.Round((resumed - paused).TotalMinutes);
                return Task.FromResult((cur.AddMinutes(pausedMins), pausedMins));
            });

        _handler = new ChangeTicketStatusCommandHandler(
            _context,
            _currentUserService,
            _dateTimeProvider,
            _notificationService,
            _slaCalculator);
    }

    private Ticket CreateSampleTicket(long id = 1, TicketStatus status = TicketStatus.InProgress)
    {
        var category = new Category { Id = 1, Name = "Hardware" };
        var requester = new ApplicationUser { Id = _requesterId, FullName = "Budi Requester" };
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
        ticket.Status = status;
        ticket.RowVersion = _defaultRowVersion;

        return ticket;
    }

    [Fact]
    public async Task Handle_ValidStatusChange_ShouldSucceedAndReturnTicketDetailDto()
    {
        // Arrange
        var ticket = CreateSampleTicket(1, TicketStatus.InProgress);
        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);

        var command = new ChangeTicketStatusCommand(ticket.Id, TicketStatus.Resolved, "Selesai diperbaiki", _defaultRowVersion);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(ticket.Id);
        result.Status.Should().Be(TicketStatus.Resolved);
        result.ResolvedAt.Should().Be(_utcNow);
        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTicketNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        var ticketsSet = new List<Ticket>().AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);
        var command = new ChangeTicketStatusCommand(999, TicketStatus.Resolved, "Catatan", _defaultRowVersion);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_RequesterAccessingOtherUsersTicket_ShouldThrowNotFoundException()
    {
        // Arrange
        var ticket = CreateSampleTicket(1, TicketStatus.Open);
        var otherRequesterId = Guid.NewGuid();

        _currentUserService.UserId.Returns(otherRequesterId.ToString());
        _currentUserService.Role.Returns("Requester");

        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);

        var command = new ChangeTicketStatusCommand(ticket.Id, TicketStatus.Cancelled, null, _defaultRowVersion);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenRowVersionMismatch_ShouldThrowDbUpdateConcurrencyException()
    {
        // Arrange
        var ticket = CreateSampleTicket(1, TicketStatus.InProgress);
        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);

        var staleRowVersion = new byte[] { 9, 9, 9, 9 };
        var command = new ChangeTicketStatusCommand(ticket.Id, TicketStatus.Pending, "Menunggu vendor", staleRowVersion);

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
        var command = new ChangeTicketStatusCommand(1, TicketStatus.InProgress, null, _defaultRowVersion);

        // Act
        var act = async () => await _handler.Handle(command, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
