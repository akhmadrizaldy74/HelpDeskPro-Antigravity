namespace HelpDeskPro.UnitTests.Features.Tickets;

using FluentAssertions;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.Domain.Exceptions;
using Xunit;

public class TicketStatusTransitionTests
{
    private readonly DateTime _utcNow = new(2026, 11, 10, 8, 0, 0, DateTimeKind.Utc);
    private readonly Guid _userId = Guid.NewGuid();

    private Ticket CreateTicketWithStatus(TicketStatus status, DateTime? resolvedAt = null, DateTime? pausedAt = null)
    {
        var slaPolicy = new SlaPolicy
        {
            Id = 1,
            Priority = TicketPriority.Medium,
            ResponseTimeMinutes = 240,
            ResolutionTimeMinutes = 1440
        };

        var ticket = Ticket.Create(
            "HD-2026-000001",
            "Uji Transisi Status",
            "Deskripsi uji coba transisi status",
            1,
            TicketPriority.Medium,
            _userId,
            slaPolicy,
            _utcNow,
            "test@helpdeskpro.local");

        // Set status and contextual dates for testing
        ticket.Status = status;
        ticket.ResolvedAt = resolvedAt;
        ticket.PausedAt = pausedAt;

        return ticket;
    }

    [Theory]
    [InlineData(TicketStatus.Open, TicketStatus.InProgress)]
    [InlineData(TicketStatus.Open, TicketStatus.Cancelled)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Pending)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Resolved)]
    [InlineData(TicketStatus.Pending, TicketStatus.InProgress)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Closed)]
    [InlineData(TicketStatus.Resolved, TicketStatus.InProgress)]
    public void ChangeStatus_ValidTransitions_ShouldSucceed(TicketStatus fromStatus, TicketStatus toStatus)
    {
        // Arrange
        var resolvedAt = fromStatus == TicketStatus.Resolved ? _utcNow.AddDays(-2) : (DateTime?)null;
        var pausedAt = fromStatus == TicketStatus.Pending ? _utcNow.AddHours(-1) : (DateTime?)null;
        var ticket = CreateTicketWithStatus(fromStatus, resolvedAt: resolvedAt, pausedAt: pausedAt);
        var reason = toStatus is TicketStatus.Pending or TicketStatus.Resolved ? "Alasan / catatan valid" : null;

        // Act
        ticket.ChangeStatus(toStatus, reason, _userId, "Agent", _utcNow);

        // Assert
        ticket.Status.Should().Be(toStatus);
        ticket.Histories.Should().ContainSingle(h =>
            h.FieldName == "Status" &&
            h.OldValue == fromStatus.ToString() &&
            h.NewValue == toStatus.ToString() &&
            h.ChangedById == _userId);
    }

    [Theory]
    // From Open
    [InlineData(TicketStatus.Open, TicketStatus.Open)]
    [InlineData(TicketStatus.Open, TicketStatus.Pending)]
    [InlineData(TicketStatus.Open, TicketStatus.Resolved)]
    [InlineData(TicketStatus.Open, TicketStatus.Closed)]
    // From InProgress
    [InlineData(TicketStatus.InProgress, TicketStatus.Open)]
    [InlineData(TicketStatus.InProgress, TicketStatus.InProgress)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Cancelled)]
    [InlineData(TicketStatus.InProgress, TicketStatus.Closed)]
    // From Pending
    [InlineData(TicketStatus.Pending, TicketStatus.Open)]
    [InlineData(TicketStatus.Pending, TicketStatus.Pending)]
    [InlineData(TicketStatus.Pending, TicketStatus.Resolved)]
    [InlineData(TicketStatus.Pending, TicketStatus.Closed)]
    [InlineData(TicketStatus.Pending, TicketStatus.Cancelled)]
    // From Resolved
    [InlineData(TicketStatus.Resolved, TicketStatus.Open)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Pending)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Resolved)]
    [InlineData(TicketStatus.Resolved, TicketStatus.Cancelled)]
    // From Closed
    [InlineData(TicketStatus.Closed, TicketStatus.Open)]
    [InlineData(TicketStatus.Closed, TicketStatus.InProgress)]
    [InlineData(TicketStatus.Closed, TicketStatus.Pending)]
    [InlineData(TicketStatus.Closed, TicketStatus.Resolved)]
    [InlineData(TicketStatus.Closed, TicketStatus.Closed)]
    [InlineData(TicketStatus.Closed, TicketStatus.Cancelled)]
    // From Cancelled
    [InlineData(TicketStatus.Cancelled, TicketStatus.Open)]
    [InlineData(TicketStatus.Cancelled, TicketStatus.InProgress)]
    [InlineData(TicketStatus.Cancelled, TicketStatus.Pending)]
    [InlineData(TicketStatus.Cancelled, TicketStatus.Resolved)]
    [InlineData(TicketStatus.Cancelled, TicketStatus.Closed)]
    [InlineData(TicketStatus.Cancelled, TicketStatus.Cancelled)]
    public void ChangeStatus_InvalidTransitions_ThrowsBusinessRuleExceptionWithExactMessage(
        TicketStatus fromStatus,
        TicketStatus toStatus)
    {
        // Arrange
        var ticket = CreateTicketWithStatus(fromStatus);

        // Act
        var act = () => ticket.ChangeStatus(toStatus, "Alasan pengujian", _userId, "Agent", _utcNow);

        // Assert (AC-03)
        act.Should().Throw<BusinessRuleException>()
            .WithMessage($"Perubahan status dari {fromStatus} ke {toStatus} tidak diizinkan.");
    }

    [Fact]
    public void ChangeStatus_ReopenResolvedTicketWithin7Days_ShouldSucceed()
    {
        // Arrange
        var resolvedAt = _utcNow.AddDays(-6);
        var ticket = CreateTicketWithStatus(TicketStatus.Resolved, resolvedAt: resolvedAt);

        // Act
        ticket.ChangeStatus(TicketStatus.InProgress, null, _userId, "Agent", _utcNow);

        // Assert
        ticket.Status.Should().Be(TicketStatus.InProgress);
    }

    [Fact]
    public void ChangeStatus_ReopenResolvedTicketAfter7Days_ThrowsBusinessRuleException()
    {
        // Arrange
        var resolvedAt = _utcNow.AddDays(-8);
        var ticket = CreateTicketWithStatus(TicketStatus.Resolved, resolvedAt: resolvedAt);

        // Act
        var act = () => ticket.ChangeStatus(TicketStatus.InProgress, null, _userId, "Agent", _utcNow);

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("Tiket yang sudah selesai lebih dari 7 hari tidak dapat dibuka ulang.");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ChangeStatus_ToPendingWithoutReason_ThrowsBusinessRuleException(string? emptyReason)
    {
        // Arrange
        var ticket = CreateTicketWithStatus(TicketStatus.InProgress);

        // Act
        var act = () => ticket.ChangeStatus(TicketStatus.Pending, emptyReason, _userId, "Agent", _utcNow);

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("Alasan penundaan tiket wajib diisi.");
    }

    [Fact]
    public void ChangeStatus_ToPendingWithReason_AddsInternalCommentAndSetsPausedAt()
    {
        // Arrange
        var ticket = CreateTicketWithStatus(TicketStatus.InProgress);
        const string reason = "Menunggu penggantian suku cadang vendor";

        // Act
        ticket.ChangeStatus(TicketStatus.Pending, reason, _userId, "Agent", _utcNow);

        // Assert
        ticket.Status.Should().Be(TicketStatus.Pending);
        ticket.PausedAt.Should().Be(_utcNow);
        ticket.Comments.Should().ContainSingle(c =>
            c.Body == reason &&
            c.IsInternal &&
            c.AuthorId == _userId &&
            c.CreatedAt == _utcNow);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ChangeStatus_ToResolvedWithoutReason_ThrowsBusinessRuleException(string? emptyReason)
    {
        // Arrange
        var ticket = CreateTicketWithStatus(TicketStatus.InProgress);

        // Act
        var act = () => ticket.ChangeStatus(TicketStatus.Resolved, emptyReason, _userId, "Agent", _utcNow);

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("Catatan penyelesaian tiket wajib diisi.");
    }

    [Fact]
    public void ChangeStatus_ToResolvedWithReason_SetsResolvedAt()
    {
        // Arrange
        var ticket = CreateTicketWithStatus(TicketStatus.InProgress);
        const string note = "Perangkat telah selesai diperbaiki dan diuji normal";

        // Act
        ticket.ChangeStatus(TicketStatus.Resolved, note, _userId, "Agent", _utcNow);

        // Assert
        ticket.Status.Should().Be(TicketStatus.Resolved);
        ticket.ResolvedAt.Should().Be(_utcNow);
    }

    [Fact]
    public void ChangeStatus_ToClosed_SetsClosedAt()
    {
        // Arrange
        var ticket = CreateTicketWithStatus(TicketStatus.Resolved, resolvedAt: _utcNow.AddDays(-1));

        // Act
        ticket.ChangeStatus(TicketStatus.Closed, null, _userId, "Agent", _utcNow);

        // Assert
        ticket.Status.Should().Be(TicketStatus.Closed);
        ticket.ClosedAt.Should().Be(_utcNow);
    }

    [Fact]
    public void ChangeStatus_PendingFor2HoursThenResume_ShiftsResolutionDueAtBy2Hours_AC07()
    {
        // Arrange
        var initialDueAt = _utcNow.AddHours(24);
        var ticket = CreateTicketWithStatus(TicketStatus.InProgress);
        ticket.ResolutionDueAt = initialDueAt;

        var enterPendingTime = _utcNow;
        ticket.ChangeStatus(TicketStatus.Pending, "Menunggu spare part", _userId, "Agent", enterPendingTime);

        ticket.PausedAt.Should().Be(enterPendingTime);
        ticket.TotalPausedMinutes.Should().Be(0);

        // Act: 2 jam kemudian kembali ke InProgress
        var resumeTime = enterPendingTime.AddHours(2);
        ticket.ChangeStatus(TicketStatus.InProgress, null, _userId, "Agent", resumeTime);

        // Assert (AC-07)
        ticket.Status.Should().Be(TicketStatus.InProgress);
        ticket.TotalPausedMinutes.Should().Be(120);
        ticket.ResolutionDueAt.Should().Be(initialDueAt.AddMinutes(120));
        ticket.PausedAt.Should().BeNull();
    }

    [Fact]
    public void ChangeStatus_RequesterCancelsOpenTicket_ShouldSucceed()
    {
        // Arrange
        var ticket = CreateTicketWithStatus(TicketStatus.Open);

        // Act
        ticket.ChangeStatus(TicketStatus.Cancelled, null, _userId, "Requester", _utcNow);

        // Assert
        ticket.Status.Should().Be(TicketStatus.Cancelled);
    }

    [Fact]
    public void ChangeStatus_RequesterClosesResolvedTicket_ShouldSucceed()
    {
        // Arrange
        var ticket = CreateTicketWithStatus(TicketStatus.Resolved, resolvedAt: _utcNow.AddHours(-1));

        // Act
        ticket.ChangeStatus(TicketStatus.Closed, null, _userId, "Requester", _utcNow);

        // Assert
        ticket.Status.Should().Be(TicketStatus.Closed);
        ticket.ClosedAt.Should().Be(_utcNow);
    }

    [Fact]
    public void ChangeStatus_RequesterAttemptsIllegalTransition_ThrowsBusinessRuleException()
    {
        // Arrange
        var ticket = CreateTicketWithStatus(TicketStatus.Open);

        // Act
        var act = () => ticket.ChangeStatus(TicketStatus.InProgress, null, _userId, "Requester", _utcNow);

        // Assert
        act.Should().Throw<BusinessRuleException>()
            .WithMessage("Pengguna dengan role Requester tidak diizinkan mengubah status dari Open ke InProgress.");
    }
}

