namespace HelpDeskPro.UnitTests.Features.Sla;

using FluentAssertions;
using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.Infrastructure.Services;
using NSubstitute;
using Xunit;

public class BusinessHoursSlaCalculatorTests
{
    private readonly IApplicationDbContext _context;
    private readonly BusinessCalendar _calendar;
    private readonly BusinessHoursSlaCalculator _calculator;

    public BusinessHoursSlaCalculatorTests()
    {
        _context = Substitute.For<IApplicationDbContext>();
        _calendar = new BusinessCalendar(_context);
        _calculator = new BusinessHoursSlaCalculator(_calendar);
    }

    [Fact]
    public void Scenario1_CreatedDuringWorkHours_ShouldAdvanceWithinSameDay()
    {
        // Arrange: Tuesday 10:00 WIB (03:00 UTC) + 2 working hours (120 min) -> Tuesday 12:00 WIB (05:00 UTC)
        var createdUtc = new DateTime(2026, 11, 10, 3, 0, 0, DateTimeKind.Utc); // Tuesday 10:00 WIB
        var responseMinutes = 60;  // 1 hour
        var resolutionMinutes = 120; // 2 hours

        // Act
        var (responseDueAt, resolutionDueAt) = _calculator.CalculateDueDates(createdUtc, responseMinutes, resolutionMinutes);

        // Assert
        var responseWib = _calendar.ToWib(responseDueAt);
        var resolutionWib = _calendar.ToWib(resolutionDueAt);

        responseWib.Should().Be(new DateTime(2026, 11, 10, 11, 0, 0));
        resolutionWib.Should().Be(new DateTime(2026, 11, 10, 12, 0, 0));
    }

    [Fact]
    public void Scenario2_CreatedBeforeWorkHours_ShouldStartAt0800Wib()
    {
        // Arrange: Wednesday 06:30 WIB (Tuesday 23:30 UTC) + 60 working minutes
        // Should start at 08:00 WIB and finish at 09:00 WIB
        var createdUtc = new DateTime(2026, 11, 10, 23, 30, 0, DateTimeKind.Utc); // Wednesday 06:30 WIB
        var responseMinutes = 30; // 0.5 hour -> 08:30 WIB
        var resolutionMinutes = 60; // 1 hour -> 09:00 WIB

        // Act
        var (responseDueAt, resolutionDueAt) = _calculator.CalculateDueDates(createdUtc, responseMinutes, resolutionMinutes);

        // Assert
        var responseWib = _calendar.ToWib(responseDueAt);
        var resolutionWib = _calendar.ToWib(resolutionDueAt);

        responseWib.Should().Be(new DateTime(2026, 11, 11, 8, 30, 0));
        resolutionWib.Should().Be(new DateTime(2026, 11, 11, 9, 0, 0));
    }

    [Fact]
    public void Scenario3_CreatedAfter1700Wib_ShouldStartNextWorkingDayAt0800Wib()
    {
        // Arrange: Thursday 19:00 WIB (12:00 UTC) + 120 working minutes
        // Starts Friday 08:00 WIB -> Friday 10:00 WIB
        var createdUtc = new DateTime(2026, 11, 12, 12, 0, 0, DateTimeKind.Utc); // Thursday 19:00 WIB
        var responseMinutes = 60; // 1 hour -> Friday 09:00 WIB
        var resolutionMinutes = 120; // 2 hours -> Friday 10:00 WIB

        // Act
        var (responseDueAt, resolutionDueAt) = _calculator.CalculateDueDates(createdUtc, responseMinutes, resolutionMinutes);

        // Assert
        var responseWib = _calendar.ToWib(responseDueAt);
        var resolutionWib = _calendar.ToWib(resolutionDueAt);

        responseWib.Should().Be(new DateTime(2026, 11, 13, 9, 0, 0));
        resolutionWib.Should().Be(new DateTime(2026, 11, 13, 10, 0, 0));
    }

    [Fact]
    public void Scenario4_CreatedFridayAfternoon_ShouldRollOverWeekend()
    {
        // Prompt specification: Tiket Medium (24 jam kerja = 1440 menit) dibuat Jumat 16.00 WIB
        // Jumat 16.00 - 17.00 = 1 jam (sisa 23 jam)
        // Sabtu & Minggu libur = 0 jam
        // Senin 08.00 - 17.00 = 9 jam (sisa 14 jam)
        // Selasa 08.00 - 17.00 = 9 jam (sisa 5 jam)
        // Rabu 08.00 + 5 jam = Rabu 13.00 WIB
        var createdUtc = new DateTime(2026, 11, 13, 9, 0, 0, DateTimeKind.Utc); // Friday 16:00 WIB
        var resolutionMinutes = 24 * 60; // 1440 minutes = 24 working hours
        var responseMinutes = 4 * 60; // 240 minutes = 4 working hours -> Fri 1h + Mon 3h = Mon 11:00 WIB

        // Act
        var (responseDueAt, resolutionDueAt) = _calculator.CalculateDueDates(createdUtc, responseMinutes, resolutionMinutes);

        // Assert
        var responseWib = _calendar.ToWib(responseDueAt);
        var resolutionWib = _calendar.ToWib(resolutionDueAt);

        responseWib.Should().Be(new DateTime(2026, 11, 16, 11, 0, 0)); // Monday 11:00 WIB
        resolutionWib.Should().Be(new DateTime(2026, 11, 18, 13, 0, 0)); // Wednesday 13:00 WIB
    }

    [Fact]
    public void Scenario5_CreatedBeforeHoliday_ShouldRollOverNationalHoliday()
    {
        // Arrange: Friday 16:00 WIB + 24 working hours, but Monday 16 Nov 2026 is a national holiday
        // Jumat 16.00 - 17.00 = 1 jam (sisa 23 jam)
        // Sabtu & Minggu libur = 0 jam
        // Senin 16 Nov = Hari Libur Nasional (0 jam)
        // Selasa 17 Nov 08.00 - 17.00 = 9 jam (sisa 14 jam)
        // Rabu 18 Nov 08.00 - 17.00 = 9 jam (sisa 5 jam)
        // Kamis 19 Nov 08.00 + 5 jam = Kamis 13.00 WIB
        var createdUtc = new DateTime(2026, 11, 13, 9, 0, 0, DateTimeKind.Utc); // Friday 16:00 WIB
        var resolutionMinutes = 24 * 60;
        var responseMinutes = 4 * 60; // Fri 1h + Tue 3h = Tue 11:00 WIB

        var holidays = new HashSet<DateOnly>
        {
            new(2026, 11, 16) // Monday holiday
        };

        // Act
        var (responseDueAt, resolutionDueAt) = _calculator.CalculateDueDates(createdUtc, responseMinutes, resolutionMinutes, holidays);

        // Assert
        var responseWib = _calendar.ToWib(responseDueAt);
        var resolutionWib = _calendar.ToWib(resolutionDueAt);

        responseWib.Should().Be(new DateTime(2026, 11, 17, 11, 0, 0)); // Tuesday 11:00 WIB
        resolutionWib.Should().Be(new DateTime(2026, 11, 19, 13, 0, 0)); // Thursday 13:00 WIB
    }

    [Fact]
    public void Scenario6_PendingStatusOvernight_ShouldOnlyCountWorkingMinutesInPause()
    {
        // Arrange: Paused Monday 16:30 WIB, Resumed Tuesday 09:30 WIB
        // Monday 16:30 to 17:00 = 30 working minutes
        // Monday 17:00 to Tuesday 08:00 = 0 working minutes (overnight)
        // Tuesday 08:00 to 09:30 = 90 working minutes
        // Total working paused minutes = 120 minutes (2 hours)
        var pausedUtc = new DateTime(2026, 11, 16, 9, 30, 0, DateTimeKind.Utc); // Monday 16:30 WIB
        var resumedUtc = new DateTime(2026, 11, 17, 2, 30, 0, DateTimeKind.Utc); // Tuesday 09:30 WIB

        var currentResolutionDueAtUtc = new DateTime(2026, 11, 18, 6, 0, 0, DateTimeKind.Utc); // Wednesday 13:00 WIB

        // Act
        var (newResolutionDueAt, workingPausedMinutes) = _calculator.AdjustDueDatesForPausedDuration(
            currentResolutionDueAtUtc,
            pausedUtc,
            resumedUtc);

        // Assert
        workingPausedMinutes.Should().Be(120);

        var originalDueWib = _calendar.ToWib(currentResolutionDueAtUtc); // Wednesday 13:00 WIB
        var newDueWib = _calendar.ToWib(newResolutionDueAt);

        // Wednesday 13:00 WIB + 120 minutes (2 working hours) -> Wednesday 15:00 WIB
        newDueWib.Should().Be(new DateTime(2026, 11, 18, 15, 0, 0));
    }

    [Fact]
    public void CalculateRemainingMinutes_WhenTicketClosedOrResolved_ReturnsRemainingMinutesAtResolution()
    {
        var dueUtc = new DateTime(2026, 11, 10, 10, 0, 0, DateTimeKind.Utc);
        var resolvedUtc = new DateTime(2026, 11, 10, 9, 0, 0, DateTimeKind.Utc);
        var nowUtc = new DateTime(2026, 11, 10, 12, 0, 0, DateTimeKind.Utc);

        var remaining = _calculator.CalculateRemainingMinutes(
            dueUtc,
            TicketStatus.Resolved,
            resolvedUtc,
            null,
            null,
            nowUtc);

        remaining.Should().Be(60); // 10:00 - 09:00 = 60 mins
    }
}
