namespace HelpDeskPro.UnitTests.Features.Tickets;

using FluentAssertions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Tickets.Commands.RateTicket;
using HelpDeskPro.Application.Features.Tickets.Models;
using HelpDeskPro.Application.Features.Tickets.Queries.GetTicketById;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.Domain.Exceptions;
using HelpDeskPro.UnitTests.Common;
using MediatR;
using NSubstitute;
using Xunit;

public class RateTicketCommandHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationService _notificationService;
    private readonly ISender _sender;
    private readonly Guid _requesterId = Guid.NewGuid();
    private readonly Guid _agentId = Guid.NewGuid();
    private readonly DateTime _utcNow = new(2026, 11, 10, 15, 0, 0, DateTimeKind.Utc);

    public RateTicketCommandHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();
        _notificationService = Substitute.For<INotificationService>();
        _sender = Substitute.For<ISender>();

        _dateTimeProvider.UtcNow.Returns(_utcNow);
        _currentUserService.UserId.Returns(_requesterId.ToString());
        _currentUserService.Role.Returns("Requester");
    }

    [Fact]
    public async Task Handle_WhenValid_RatesTicketAndClosesAndNotifiesAssignee()
    {
        // Arrange
        var ticket = new Ticket
        {
            Id = 1,
            TicketNo = "HD-2026-000001",
            Title = "Laptop mati mendadak",
            Status = TicketStatus.Resolved,
            RequesterId = _requesterId,
            AssigneeId = _agentId,
            ResolvedAt = _utcNow.AddHours(-2)
        };

        var ticketSet = new List<Ticket> { ticket }.AsTestDbSet();
        _context.Tickets.Returns(ticketSet);

        var expectedDto = new TicketDetailDto
        {
            Id = 1,
            TicketNo = "HD-2026-000001",
            Status = TicketStatus.Closed,
            Rating = 5,
            RatingComment = "Sangat memuaskan!"
        };

        _sender.Send(Arg.Is<GetTicketByIdQuery>(q => q.Id == 1), Arg.Any<CancellationToken>())
            .Returns(expectedDto);

        var handler = new RateTicketCommandHandler(
            _context,
            _currentUserService,
            _dateTimeProvider,
            _notificationService,
            _sender);

        // Act
        var command = new RateTicketCommand(1, 5, "Sangat memuaskan!");
        var result = await handler.Handle(command, CancellationToken.None);

        // Assert
        result.Should().BeEquivalentTo(expectedDto);
        ticket.Rating.Should().Be(5);
        ticket.RatingComment.Should().Be("Sangat memuaskan!");
        ticket.Status.Should().Be(TicketStatus.Closed);

        await _context.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
        await _notificationService.Received(1).CreateNotificationAsync(
            _agentId,
            1,
            NotificationType.StatusChanged,
            Arg.Is<string>(s => s.Contains("rating 5/5")),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTicketNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var emptySet = new List<Ticket>().AsTestDbSet();
        _context.Tickets.Returns(emptySet);

        var handler = new RateTicketCommandHandler(
            _context,
            _currentUserService,
            _dateTimeProvider,
            _notificationService,
            _sender);

        // Act
        var act = async () => await handler.Handle(new RateTicketCommand(999, 5, "Ok"), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
