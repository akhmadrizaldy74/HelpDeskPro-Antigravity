namespace HelpDeskPro.UnitTests.Features.Tickets;

using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Tickets.Queries.GetTickets;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.UnitTests.Common;
using NSubstitute;
using Xunit;

public class GetTicketsQueryHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly GetTicketsQueryHandler _handler;

    private readonly Guid _requesterId = Guid.NewGuid();
    private readonly Guid _agentId = Guid.NewGuid();
    private readonly Guid _otherUserId = Guid.NewGuid();
    private readonly DateTime _utcNow = new(2026, 11, 10, 10, 0, 0, DateTimeKind.Utc);

    public GetTicketsQueryHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        _dateTimeProvider.UtcNow.Returns(_utcNow);

        _handler = new GetTicketsQueryHandler(
            _context,
            _currentUserService,
            _dateTimeProvider);
    }

    private List<Ticket> CreateSampleTickets()
    {
        var category = new Category { Id = 1, Name = "Hardware" };
        var requester = new ApplicationUser { Id = _requesterId, FullName = "Budi Requester", DepartmentId = 1 };
        var otherUser = new ApplicationUser { Id = _otherUserId, FullName = "Other User", DepartmentId = 2 };
        var agent = new ApplicationUser { Id = _agentId, FullName = "Agent IT", DepartmentId = 3 };

        return new List<Ticket>
        {
            // Ticket 1: Owned by requester, unassigned, medium priority, Open
            new()
            {
                Id = 1,
                TicketNo = "HD-2026-000001",
                Title = "Laptop Rusak",
                CategoryId = 1,
                Category = category,
                Priority = TicketPriority.Medium,
                Status = TicketStatus.Open,
                RequesterId = _requesterId,
                Requester = requester,
                AssigneeId = null,
                Assignee = null,
                ResponseDueAt = _utcNow.AddHours(4),
                ResolutionDueAt = _utcNow.AddHours(24),
                IsSlaBreached = false,
                CreatedAt = _utcNow.AddHours(-1)
            },
            // Ticket 2: Owned by otherUser, assigned to agent, high priority, InProgress
            new()
            {
                Id = 2,
                TicketNo = "HD-2026-000002",
                Title = "Monitor Mati",
                CategoryId = 1,
                Category = category,
                Priority = TicketPriority.High,
                Status = TicketStatus.InProgress,
                RequesterId = _otherUserId,
                Requester = otherUser,
                AssigneeId = _agentId,
                Assignee = agent,
                ResponseDueAt = _utcNow.AddHours(1),
                ResolutionDueAt = _utcNow.AddHours(6),
                IsSlaBreached = false,
                CreatedAt = _utcNow.AddHours(-2)
            },
            // Ticket 3: Owned by otherUser, unassigned, critical priority, breached
            new()
            {
                Id = 3,
                TicketNo = "HD-2026-000003",
                Title = "Server Down",
                CategoryId = 1,
                Category = category,
                Priority = TicketPriority.Critical,
                Status = TicketStatus.Open,
                RequesterId = _otherUserId,
                Requester = otherUser,
                AssigneeId = null,
                Assignee = null,
                ResponseDueAt = _utcNow.AddMinutes(-30),
                ResolutionDueAt = _utcNow.AddMinutes(-10),
                IsSlaBreached = true,
                CreatedAt = _utcNow.AddHours(-5)
            },
            // Ticket 4: Owned by otherUser, assigned to other agent, medium priority
            new()
            {
                Id = 4,
                TicketNo = "HD-2026-000004",
                Title = "Printer Macet",
                CategoryId = 1,
                Category = category,
                Priority = TicketPriority.Medium,
                Status = TicketStatus.Open,
                RequesterId = _otherUserId,
                Requester = otherUser,
                AssigneeId = Guid.NewGuid(),
                Assignee = new ApplicationUser { Id = Guid.NewGuid(), FullName = "Other Agent" },
                ResponseDueAt = _utcNow.AddHours(2),
                ResolutionDueAt = _utcNow.AddHours(10),
                IsSlaBreached = false,
                CreatedAt = _utcNow.AddHours(-3)
            }
        };
    }

    [Fact]
    public async Task Handle_WhenUserNotAuthenticated_ShouldThrowUnauthorizedAccessException()
    {
        // Arrange
        _currentUserService.UserId.Returns((string?)null);

        // Act
        var act = () => _handler.Handle(new GetTicketsQuery(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task Handle_RequesterRole_ReturnsOnlyOwnTickets()
    {
        // Arrange
        _currentUserService.UserId.Returns(_requesterId.ToString());
        _currentUserService.Role.Returns("Requester");

        var tickets = CreateSampleTickets().AsTestDbSet();
        _context.Tickets.Returns(tickets);

        // Act
        var result = await _handler.Handle(new GetTicketsQuery(), CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(t => t.Id == 1 && t.RequesterId == _requesterId);
    }

    [Fact]
    public async Task Handle_AgentRole_ReturnsAssignedToSelfAndUnassignedTickets()
    {
        // Arrange
        _currentUserService.UserId.Returns(_agentId.ToString());
        _currentUserService.Role.Returns("Agent");

        var tickets = CreateSampleTickets().AsTestDbSet();
        _context.Tickets.Returns(tickets);

        // Act
        var result = await _handler.Handle(new GetTicketsQuery(), CancellationToken.None);

        // Assert - Should return Ticket 1 (unassigned), Ticket 2 (assigned to self), Ticket 3 (unassigned)
        result.TotalCount.Should().Be(3);
        result.Items.Should().Contain(t => t.Id == 1);
        result.Items.Should().Contain(t => t.Id == 2);
        result.Items.Should().Contain(t => t.Id == 3);
        result.Items.Should().NotContain(t => t.Id == 4); // Assigned to other agent
    }

    [Fact]
    public async Task Handle_AdminRole_ReturnsAllTickets()
    {
        // Arrange
        _currentUserService.UserId.Returns(Guid.NewGuid().ToString());
        _currentUserService.Role.Returns("Admin");
        _currentUserService.Role.Should().Be("Admin");

        var tickets = CreateSampleTickets().AsTestDbSet();
        _context.Tickets.Returns(tickets);

        // Act
        var result = await _handler.Handle(new GetTicketsQuery(), CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(4);
    }

    [Fact]
    public async Task Handle_FilterStatusAndPriority_AppliesFiltersCorrectly()
    {
        // Arrange
        _currentUserService.UserId.Returns(Guid.NewGuid().ToString());
        _currentUserService.Role.Returns("Admin");
        _currentUserService.Role.Should().Be("Admin");

        var tickets = CreateSampleTickets().AsTestDbSet();
        _context.Tickets.Returns(tickets);

        var query = new GetTicketsQuery
        {
            Statuses = new[] { TicketStatus.Open },
            Priorities = new[] { TicketPriority.Critical }
        };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - Only Ticket 3 is Open & Critical
        result.TotalCount.Should().Be(1);
        result.Items.Should().ContainSingle(t => t.Id == 3);
    }

    [Fact]
    public async Task Handle_FilterUnassigned_ReturnsOnlyTicketsWithoutAssignee()
    {
        // Arrange
        _currentUserService.UserId.Returns(Guid.NewGuid().ToString());
        _currentUserService.Role.Returns("Admin");
        _currentUserService.Role.Should().Be("Admin");

        var tickets = CreateSampleTickets().AsTestDbSet();
        _context.Tickets.Returns(tickets);

        var query = new GetTicketsQuery { Unassigned = true };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert - Ticket 1 and Ticket 3 are unassigned
        result.TotalCount.Should().Be(2);
        result.Items.Should().OnlyContain(t => t.AssigneeId == null);
    }

    [Fact]
    public async Task Handle_FilterSearch_MatchesTicketNoOrTitle()
    {
        // Arrange
        _currentUserService.UserId.Returns(Guid.NewGuid().ToString());
        _currentUserService.Role.Returns("Admin");
        _currentUserService.Role.Should().Be("Admin");

        var tickets = CreateSampleTickets().AsTestDbSet();
        _context.Tickets.Returns(tickets);

        var query = new GetTicketsQuery { Search = "Server" };

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items[0].Title.Should().Be("Server Down");
    }

    [Fact]
    public async Task Handle_DefaultOrder_OrdersBreachedSlaFirstThenEarliestResolutionDueAt()
    {
        // Arrange
        _currentUserService.UserId.Returns(Guid.NewGuid().ToString());
        _currentUserService.Role.Returns("Admin");
        _currentUserService.Role.Should().Be("Admin");

        var tickets = CreateSampleTickets().AsTestDbSet();
        _context.Tickets.Returns(tickets);

        // Act
        var result = await _handler.Handle(new GetTicketsQuery(), CancellationToken.None);

        // Assert - Ticket 3 is breached, must be first
        result.Items[0].Id.Should().Be(3);
    }
}
