namespace HelpDeskPro.UnitTests.Features.Dashboard;

using FluentAssertions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Dashboard.Queries.GetPendingEscalations;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.Domain.Exceptions;
using HelpDeskPro.UnitTests.Common;
using NSubstitute;
using Xunit;

public class GetPendingEscalationsQueryHandlerTests
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly DateTime _utcNow = new(2026, 11, 10, 10, 0, 0, DateTimeKind.Utc);
    private readonly Guid _supervisorId = Guid.NewGuid();

    public GetPendingEscalationsQueryHandlerTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _currentUserService = Substitute.For<ICurrentUserService>();
    }

    [Fact]
    public async Task Handle_WhenRequester_ThrowsForbiddenAccessException()
    {
        // Arrange
        _currentUserService.Role.Returns("Requester");
        var handler = new GetPendingEscalationsQueryHandler(_context, _currentUserService);

        // Act
        var act = async () => await handler.Handle(new GetPendingEscalationsQuery(), CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task Handle_WhenAdmin_ReturnsUnacknowledgedEscalationsOrderedByEscalatedAt()
    {
        // Arrange
        _currentUserService.Role.Returns("Admin");

        var supervisorUser = new ApplicationUser { Id = _supervisorId, FullName = "Supervisor IT" };
        var ticket = new Ticket
        {
            Id = 1,
            TicketNo = "HD-2026-000001",
            Title = "Server Down",
            Priority = TicketPriority.Critical
        };

        var escalations = new List<Escalation>
        {
            new()
            {
                Id = 1,
                TicketId = 1,
                Ticket = ticket,
                Level = 1,
                Reason = EscalationReason.SlaResponseBreach,
                EscalatedTo = supervisorUser,
                EscalatedToId = supervisorUser.Id,
                EscalatedAt = _utcNow.AddHours(-2),
                AcknowledgedAt = null // Pending
            },
            new()
            {
                Id = 2,
                TicketId = 1,
                Ticket = ticket,
                Level = 2,
                Reason = EscalationReason.SlaResolutionBreach,
                EscalatedTo = supervisorUser,
                EscalatedToId = supervisorUser.Id,
                EscalatedAt = _utcNow.AddHours(-3), // Older
                AcknowledgedAt = null // Pending
            },
            new()
            {
                Id = 3,
                TicketId = 1,
                Ticket = ticket,
                Level = 1,
                Reason = EscalationReason.Manual,
                EscalatedTo = supervisorUser,
                EscalatedToId = supervisorUser.Id,
                EscalatedAt = _utcNow.AddHours(-4),
                AcknowledgedAt = _utcNow.AddHours(-1) // Already acknowledged
            }
        };

        var escalationSet = escalations.AsTestDbSet();
        _context.Escalations.Returns(escalationSet);

        var handler = new GetPendingEscalationsQueryHandler(_context, _currentUserService);

        // Act
        var result = await handler.Handle(new GetPendingEscalationsQuery(), CancellationToken.None);

        // Assert
        result.Should().HaveCount(2);
        result[0].EscalationId.Should().Be(2); // Older first (asc)
        result[1].EscalationId.Should().Be(1);
    }
}

