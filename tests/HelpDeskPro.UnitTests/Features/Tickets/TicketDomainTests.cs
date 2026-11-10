namespace HelpDeskPro.UnitTests.Features.Tickets;

using FluentAssertions;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using Xunit;

public class TicketDomainTests
{
    private readonly DateTime _utcNow = new(2026, 11, 10, 8, 0, 0, DateTimeKind.Utc);
    private readonly Guid _requesterId = Guid.NewGuid();

    [Fact]
    public void Create_CriticalPriority_CalculatesResponse30MinAndResolution4Hours()
    {
        // Arrange
        var slaPolicy = new SlaPolicy
        {
            Id = 1,
            Priority = TicketPriority.Critical,
            ResponseTimeMinutes = 30,
            ResolutionTimeMinutes = 240
        };

        // Act
        var ticket = Ticket.Create(
            "HD-2026-000001",
            "Server Down",
            "Database production mati",
            1,
            TicketPriority.Critical,
            _requesterId,
            slaPolicy,
            _utcNow,
            "admin@helpdeskpro.local");

        // Assert
        ticket.ResponseDueAt.Should().Be(_utcNow.AddMinutes(30));
        ticket.ResolutionDueAt.Should().Be(_utcNow.AddMinutes(240));
        ticket.Priority.Should().Be(TicketPriority.Critical);
    }

    [Fact]
    public void Create_HighPriority_CalculatesResponse2HoursAndResolution8Hours()
    {
        // Arrange
        var slaPolicy = new SlaPolicy
        {
            Id = 2,
            Priority = TicketPriority.High,
            ResponseTimeMinutes = 120,
            ResolutionTimeMinutes = 480
        };

        // Act
        var ticket = Ticket.Create(
            "HD-2026-000002",
            "Email tidak bisa kirim",
            "Semua email outbox nyangkut",
            1,
            TicketPriority.High,
            _requesterId,
            slaPolicy,
            _utcNow,
            "user@helpdeskpro.local");

        // Assert
        ticket.ResponseDueAt.Should().Be(_utcNow.AddHours(2));
        ticket.ResolutionDueAt.Should().Be(_utcNow.AddHours(8));
        ticket.Priority.Should().Be(TicketPriority.High);
    }

    [Fact]
    public void Create_MediumPriority_CalculatesResponse4HoursAndResolution24Hours()
    {
        // Arrange
        var slaPolicy = new SlaPolicy
        {
            Id = 3,
            Priority = TicketPriority.Medium,
            ResponseTimeMinutes = 240,
            ResolutionTimeMinutes = 1440
        };

        // Act
        var ticket = Ticket.Create(
            "HD-2026-000003",
            "Install Aplikasi Office",
            "Perlu Office 365",
            2,
            TicketPriority.Medium,
            _requesterId,
            slaPolicy,
            _utcNow,
            "user@helpdeskpro.local");

        // Assert
        ticket.ResponseDueAt.Should().Be(_utcNow.AddHours(4));
        ticket.ResolutionDueAt.Should().Be(_utcNow.AddHours(24));
        ticket.Priority.Should().Be(TicketPriority.Medium);
    }

    [Fact]
    public void Create_LowPriority_CalculatesResponse8HoursAndResolution72Hours()
    {
        // Arrange
        var slaPolicy = new SlaPolicy
        {
            Id = 4,
            Priority = TicketPriority.Low,
            ResponseTimeMinutes = 480,
            ResolutionTimeMinutes = 4320
        };

        // Act
        var ticket = Ticket.Create(
            "HD-2026-000004",
            "Ganti Mouse Rusak",
            "Klik kiri macet",
            1,
            TicketPriority.Low,
            _requesterId,
            slaPolicy,
            _utcNow,
            "user@helpdeskpro.local");

        // Assert
        ticket.ResponseDueAt.Should().Be(_utcNow.AddHours(8));
        ticket.ResolutionDueAt.Should().Be(_utcNow.AddHours(72));
        ticket.Priority.Should().Be(TicketPriority.Low);
    }

    [Fact]
    public void Create_InitialStatusAndState_ShouldBeOpenAndNotBreached()
    {
        // Arrange
        var slaPolicy = new SlaPolicy
        {
            Id = 3,
            Priority = TicketPriority.Medium,
            ResponseTimeMinutes = 240,
            ResolutionTimeMinutes = 1440
        };

        // Act
        var ticket = Ticket.Create(
            "HD-2026-000005",
            "Printer Paper Jam",
            "Macet di tray 2",
            1,
            TicketPriority.Medium,
            _requesterId,
            slaPolicy,
            _utcNow);

        // Assert
        ticket.Status.Should().Be(TicketStatus.Open);
        ticket.IsSlaBreached.Should().BeFalse();
        ticket.EscalationLevel.Should().Be(0);
        ticket.FirstRespondedAt.Should().BeNull();
        ticket.ResolvedAt.Should().BeNull();
        ticket.ClosedAt.Should().BeNull();
        ticket.PausedAt.Should().BeNull();
        ticket.TotalPausedMinutes.Should().Be(0);
    }

    [Fact]
    public void Create_Histories_ShouldContainInitialCreatedEntry()
    {
        // Arrange
        var slaPolicy = new SlaPolicy
        {
            Id = 3,
            Priority = TicketPriority.Medium,
            ResponseTimeMinutes = 240,
            ResolutionTimeMinutes = 1440
        };

        // Act
        var ticket = Ticket.Create(
            "HD-2026-000006",
            "Akses VPN Error",
            "Gagal login VPN",
            1,
            TicketPriority.Medium,
            _requesterId,
            slaPolicy,
            _utcNow,
            "creator");

        // Assert
        ticket.Histories.Should().HaveCount(1);
        var history = ticket.Histories.First();
        history.FieldName.Should().Be("Ticket");
        history.OldValue.Should().BeNull();
        history.NewValue.Should().Be("Created");
        history.ChangedById.Should().Be(_requesterId);
        history.ChangedAt.Should().Be(_utcNow);
    }
}

