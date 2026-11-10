namespace HelpDeskPro.UnitTests.Features.Tickets;

using FluentAssertions;
using HelpDeskPro.Application.Features.Tickets.Common;
using HelpDeskPro.Domain.Enums;
using Xunit;

public class SlaCalculatorTests
{
    private readonly DateTime _utcNow = new(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void CalculateRemainingMinutes_WhenTicketActive_ReturnsMinutesUntilResolutionDue()
    {
        // Arrange - Resolution due in 3 hours (+180 minutes)
        var resolutionDueAt = _utcNow.AddHours(3);

        // Act
        var remaining = SlaCalculator.CalculateRemainingMinutes(
            resolutionDueAt,
            TicketStatus.InProgress,
            resolvedAt: null,
            closedAt: null,
            pausedAt: null,
            _utcNow);

        // Assert
        remaining.Should().Be(180);
    }

    [Fact]
    public void CalculateRemainingMinutes_WhenOverdue_ReturnsNegativeMinutes()
    {
        // Arrange - Resolution due 45 minutes ago
        var resolutionDueAt = _utcNow.AddMinutes(-45);

        // Act
        var remaining = SlaCalculator.CalculateRemainingMinutes(
            resolutionDueAt,
            TicketStatus.InProgress,
            resolvedAt: null,
            closedAt: null,
            pausedAt: null,
            _utcNow);

        // Assert
        remaining.Should().Be(-45);
    }

    [Fact]
    public void CalculateRemainingMinutes_WhenResolved_CalculatesBasedOnResolvedAt()
    {
        // Arrange - Due at 15:00, resolved early at 14:00 (60 minutes remaining)
        var resolutionDueAt = _utcNow.AddHours(3);
        var resolvedAt = _utcNow.AddHours(2);

        // Act
        var remaining = SlaCalculator.CalculateRemainingMinutes(
            resolutionDueAt,
            TicketStatus.Resolved,
            resolvedAt: resolvedAt,
            closedAt: null,
            pausedAt: null,
            _utcNow);

        // Assert
        remaining.Should().Be(60);
    }

    [Fact]
    public void CalculateRemainingMinutes_WhenPending_PausesClockAtPausedAt()
    {
        // Arrange - Due at 16:00, paused at 13:00 (180 minutes remaining), current time is 15:00
        var resolutionDueAt = _utcNow.AddHours(4);
        var pausedAt = _utcNow.AddHours(1);
        var currentTime = _utcNow.AddHours(3);

        // Act
        var remaining = SlaCalculator.CalculateRemainingMinutes(
            resolutionDueAt,
            TicketStatus.Pending,
            resolvedAt: null,
            closedAt: null,
            pausedAt: pausedAt,
            currentTime);

        // Assert - Clock remained paused at pausedAt (16:00 - 13:00 = 180 min)
        remaining.Should().Be(180);
    }
}

