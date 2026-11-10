namespace HelpDeskPro.UnitTests.Features.Dashboard;

using FluentAssertions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Dashboard.Queries.GetAgentWorkload;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.Domain.Exceptions;
using HelpDeskPro.UnitTests.Common;
using Microsoft.AspNetCore.Identity;
using NSubstitute;
using Xunit;

public class GetAgentWorkloadQueryHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly DateTime _utcNow = new(2026, 11, 10, 10, 0, 0, DateTimeKind.Utc);
    private readonly Guid _agent1Id = Guid.NewGuid();
    private readonly Guid _agent2Id = Guid.NewGuid();

    public GetAgentWorkloadQueryHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        var store = Substitute.For<IUserStore<ApplicationUser>>();
        _userManager = Substitute.For<UserManager<ApplicationUser>>(
            store, null, null, null, null, null, null, null, null);

        _dateTimeProvider.UtcNow.Returns(_utcNow);
    }

    [Fact]
    public async Task Handle_WhenRequester_ThrowsForbiddenAccessException()
    {
        // Arrange
        _currentUserService.Role.Returns("Requester");
        var handler = new GetAgentWorkloadQueryHandler(_context, _currentUserService, _dateTimeProvider, _userManager);

        // Act
        var act = async () => await handler.Handle(new GetAgentWorkloadQuery(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_WhenAdmin_ReturnsAllActiveAgentsAndTicketCounts()
    {
        // Arrange
        _currentUserService.Role.Returns("Admin");

        var agent1 = new ApplicationUser { Id = _agent1Id, FullName = "Agent One", Email = "agent1@helpdesk.local", DepartmentId = 1, IsActive = true };
        var agent2 = new ApplicationUser { Id = _agent2Id, FullName = "Agent Two", Email = "agent2@helpdesk.local", DepartmentId = 1, IsActive = true };

        _userManager.GetUsersInRoleAsync("Agent").Returns(new List<ApplicationUser> { agent1, agent2 });

        var tickets = new List<Ticket>
        {
            // Agent 1: 2 active tickets (1 normal, 1 breached)
            new()
            {
                Id = 1,
                TicketNo = "HD-2026-000001",
                Title = "Ticket 1",
                Status = TicketStatus.InProgress,
                AssigneeId = _agent1Id,
                ResolutionDueAt = _utcNow.AddHours(2)
            },
            new()
            {
                Id = 2,
                TicketNo = "HD-2026-000002",
                Title = "Ticket 2",
                Status = TicketStatus.InProgress,
                AssigneeId = _agent1Id,
                ResolutionDueAt = _utcNow.AddHours(-1),
                IsSlaBreached = true
            },
            // Agent 2: 0 tickets
        };

        var ticketSet = tickets.AsTestDbSet();
        _context.Tickets.Returns(ticketSet);

        var handler = new GetAgentWorkloadQueryHandler(_context, _currentUserService, _dateTimeProvider, _userManager);

        // Act
        var result = await handler.Handle(new GetAgentWorkloadQuery(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        var workload1 = result.First(x => x.AgentId == _agent1Id);
        workload1.ActiveTicketCount.Should().Be(2);
        workload1.SlaBreachedCount.Should().Be(1);

        var workload2 = result.First(x => x.AgentId == _agent2Id);
        workload2.ActiveTicketCount.Should().Be(0);
        workload2.SlaBreachedCount.Should().Be(0);
    }
}

