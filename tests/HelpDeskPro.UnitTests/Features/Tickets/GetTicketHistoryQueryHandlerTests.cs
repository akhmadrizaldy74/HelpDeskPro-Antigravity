namespace HelpDeskPro.UnitTests.Features.Tickets;

using FluentAssertions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Tickets.Queries.GetTicketHistory;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.Domain.Exceptions;
using HelpDeskPro.UnitTests.Common;
using NSubstitute;
using Xunit;

public class GetTicketHistoryQueryHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly GetTicketHistoryQueryHandler _handler;

    private readonly Guid _userId = Guid.NewGuid();
    private readonly DateTime _utcNow = new(2026, 11, 10, 10, 0, 0, DateTimeKind.Utc);

    public GetTicketHistoryQueryHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _currentUserService = Substitute.For<ICurrentUserService>();

        _currentUserService.UserId.Returns(_userId.ToString());
        _currentUserService.Role.Returns("Requester");

        _handler = new GetTicketHistoryQueryHandler(
            _context,
            _currentUserService);
    }

    private Ticket CreateSampleTicket(long id = 1)
    {
        var category = new Category { Id = 1, Name = "Hardware" };
        var requester = new ApplicationUser { Id = _userId, FullName = "Budi Requester" };
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
    public async Task Handle_ValidTicket_ReturnsChronologicalHistories()
    {
        // Arrange
        var ticket = CreateSampleTicket();
        var user = new ApplicationUser { Id = _userId, FullName = "Budi Requester" };

        var histories = new List<TicketHistory>
        {
            new()
            {
                Id = 1,
                TicketId = ticket.Id,
                FieldName = "Status",
                OldValue = "Open",
                NewValue = "InProgress",
                ChangedById = _userId,
                ChangedBy = user,
                ChangedAt = _utcNow.AddMinutes(-20)
            },
            new()
            {
                Id = 2,
                TicketId = ticket.Id,
                FieldName = "Priority",
                OldValue = "Medium",
                NewValue = "High",
                ChangedById = _userId,
                ChangedBy = user,
                ChangedAt = _utcNow.AddMinutes(-10)
            }
        };

        var ticketsSet = new List<Ticket> { ticket }.AsTestDbSet();
        var historiesSet = histories.AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);
        _context.TicketHistories.Returns(historiesSet);

        var query = new GetTicketHistoryQuery(ticket.Id);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].FieldName.Should().Be("Status");
        result[1].FieldName.Should().Be("Priority");
        result[0].ChangedByName.Should().Be("Budi Requester");
    }

    [Fact]
    public async Task Handle_TicketNotFound_ThrowsNotFoundException()
    {
        // Arrange
        var ticketsSet = new List<Ticket>().AsTestDbSet();
        _context.Tickets.Returns(ticketsSet);

        var query = new GetTicketHistoryQuery(999);

        // Act
        var act = async () => await _handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<NotFoundException>();
    }
}

