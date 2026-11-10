namespace HelpDeskPro.UnitTests.Features.Dashboard;

using FluentAssertions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Dashboard.Queries.GetDashboardSummary;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.Domain.Exceptions;
using HelpDeskPro.UnitTests.Common;
using NSubstitute;
using Xunit;

public class GetDashboardSummaryQueryHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly DateTime _utcNow = new(2026, 11, 10, 10, 0, 0, DateTimeKind.Utc);
    private readonly Guid _supervisorId = Guid.NewGuid();
    private readonly Guid _adminId = Guid.NewGuid();

    public GetDashboardSummaryQueryHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _currentUserService = Substitute.For<ICurrentUserService>();
        _dateTimeProvider = Substitute.For<IDateTimeProvider>();

        _dateTimeProvider.UtcNow.Returns(_utcNow);
    }

    [Fact]
    public async Task Handle_WhenUserIsRequester_ThrowsForbiddenAccessException()
    {
        // Arrange
        _currentUserService.Role.Returns("Requester");
        var handler = new GetDashboardSummaryQueryHandler(_context, _currentUserService, _dateTimeProvider);

        // Act
        var act = async () => await handler.Handle(new GetDashboardSummaryQuery(30), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_WhenUserIsAdmin_ReturnsAggregatedMetricsAcrossAllTickets()
    {
        // Arrange
        _currentUserService.Role.Returns("Admin");
        _currentUserService.UserId.Returns(_adminId.ToString());

        var userDept1 = new ApplicationUser { Id = Guid.NewGuid(), FullName = "User 1", DepartmentId = 1 };
        var userDept2 = new ApplicationUser { Id = Guid.NewGuid(), FullName = "User 2", DepartmentId = 2 };

        var tickets = new List<Ticket>
        {
            // Ticket 1: Open, response due in future
            new()
            {
                Id = 1,
                TicketNo = "HD-2026-000001",
                Title = "Printer Error",
                Status = TicketStatus.Open,
                Requester = userDept1,
                RequesterId = userDept1.Id,
                CreatedAt = _utcNow.AddDays(-5),
                ResponseDueAt = _utcNow.AddHours(2),
                ResolutionDueAt = _utcNow.AddHours(24)
            },
            // Ticket 2: InProgress, SLA breached
            new()
            {
                Id = 2,
                TicketNo = "HD-2026-000002",
                Title = "Network Down",
                Status = TicketStatus.InProgress,
                Requester = userDept2,
                RequesterId = userDept2.Id,
                CreatedAt = _utcNow.AddDays(-2),
                ResponseDueAt = _utcNow.AddDays(-2).AddHours(1),
                FirstRespondedAt = _utcNow.AddDays(-2).AddMinutes(30), // 30 min response time
                ResolutionDueAt = _utcNow.AddHours(-1), // Breached
                IsSlaBreached = true
            },
            // Ticket 3: Resolved
            new()
            {
                Id = 3,
                TicketNo = "HD-2026-000003",
                Title = "Software Crash",
                Status = TicketStatus.Resolved,
                Requester = userDept1,
                RequesterId = userDept1.Id,
                CreatedAt = _utcNow.AddDays(-3),
                FirstRespondedAt = _utcNow.AddDays(-3).AddMinutes(60), // 60 min response time
                ResolvedAt = _utcNow.AddDays(-3).AddMinutes(360), // 360 min total, minus 60 paused = 300 min resolution
                TotalPausedMinutes = 60,
                ResolutionDueAt = _utcNow.AddDays(-2)
            }
        };

        var ticketSet = tickets.AsTestDbSet();
        _context.Tickets.Returns(ticketSet);

        var handler = new GetDashboardSummaryQueryHandler(_context, _currentUserService, _dateTimeProvider);

        // Act
        var result = await handler.Handle(new GetDashboardSummaryQuery(30), CancellationToken.None);

        // Assert
        result.TotalTickets.Should().Be(3);
        result.OpenTickets.Should().Be(1);
        result.InProgressTickets.Should().Be(1);
        result.ResolvedTickets.Should().Be(1);
        result.SlaBreachedTickets.Should().Be(1);
        result.AverageResponseMinutes.Should().Be(45.0); // (30 + 60) / 2 = 45
        result.AverageResolutionMinutes.Should().Be(300.0); // 360 - 60 = 300
    }

    [Fact]
    public async Task Handle_WhenUserIsSupervisor_RestrictsMetricsToDepartment()
    {
        // Arrange
        _currentUserService.Role.Returns("Supervisor");
        _currentUserService.UserId.Returns(_supervisorId.ToString());

        var financeDept = new Department { Id = 2, Code = "FIN", Name = "Finance" };
        var supervisor = new ApplicationUser { Id = _supervisorId, FullName = "Supervisor Finance", DepartmentId = 2, Department = financeDept };
        var financeUser = new ApplicationUser { Id = Guid.NewGuid(), FullName = "Fin User", DepartmentId = 2 };
        var hrUser = new ApplicationUser { Id = Guid.NewGuid(), FullName = "HR User", DepartmentId = 3 };

        var users = new List<ApplicationUser> { supervisor, financeUser, hrUser }.AsTestDbSet();
        _context.Users.Returns(users);

        var tickets = new List<Ticket>
        {
            // Ticket in finance dept
            new()
            {
                Id = 1,
                TicketNo = "HD-2026-000001",
                Title = "Finance Ticket",
                Status = TicketStatus.Open,
                Requester = financeUser,
                RequesterId = financeUser.Id,
                CreatedAt = _utcNow.AddDays(-2),
                ResolutionDueAt = _utcNow.AddHours(10)
            },
            // Ticket in HR dept
            new()
            {
                Id = 2,
                TicketNo = "HD-2026-000002",
                Title = "HR Ticket",
                Status = TicketStatus.Open,
                Requester = hrUser,
                RequesterId = hrUser.Id,
                CreatedAt = _utcNow.AddDays(-1),
                ResolutionDueAt = _utcNow.AddHours(5)
            }
        };

        var ticketSet = tickets.AsTestDbSet();
        _context.Tickets.Returns(ticketSet);

        var handler = new GetDashboardSummaryQueryHandler(_context, _currentUserService, _dateTimeProvider);

        // Act
        var result = await handler.Handle(new GetDashboardSummaryQuery(30), CancellationToken.None);

        // Assert
        result.TotalTickets.Should().Be(1);
        result.OpenTickets.Should().Be(1);
    }
}

