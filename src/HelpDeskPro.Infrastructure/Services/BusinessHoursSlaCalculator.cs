namespace HelpDeskPro.Infrastructure.Services;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Domain.Enums;

public class BusinessHoursSlaCalculator : ISlaCalculator
{
    private readonly IBusinessCalendar _calendar;

    public BusinessHoursSlaCalculator(IBusinessCalendar calendar)
    {
        _calendar = calendar;
    }

    public async Task<(DateTime ResponseDueAt, DateTime ResolutionDueAt)> CalculateDueDatesAsync(
        DateTime createdUtc,
        int responseMinutes,
        int resolutionMinutes,
        CancellationToken cancellationToken = default)
    {
        var holidays = await _calendar.GetHolidaysAsync(createdUtc.Year, createdUtc.Year + 1, cancellationToken);
        return CalculateDueDates(createdUtc, responseMinutes, resolutionMinutes, holidays);
    }

    public (DateTime ResponseDueAt, DateTime ResolutionDueAt) CalculateDueDates(
        DateTime createdUtc,
        int responseMinutes,
        int resolutionMinutes,
        IReadOnlySet<DateOnly>? holidays = null)
    {
        var responseDueAt = _calendar.AddWorkingMinutes(createdUtc, responseMinutes, holidays);
        var resolutionDueAt = _calendar.AddWorkingMinutes(createdUtc, resolutionMinutes, holidays);

        return (responseDueAt, resolutionDueAt);
    }

    public async Task<(DateTime NewResolutionDueAt, int WorkingPausedMinutes)> AdjustDueDatesForPausedDurationAsync(
        DateTime currentResolutionDueAtUtc,
        DateTime pausedAtUtc,
        DateTime resumedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var holidays = await _calendar.GetHolidaysAsync(pausedAtUtc.Year, resumedAtUtc.Year + 1, cancellationToken);
        return AdjustDueDatesForPausedDuration(currentResolutionDueAtUtc, pausedAtUtc, resumedAtUtc, holidays);
    }

    public (DateTime NewResolutionDueAt, int WorkingPausedMinutes) AdjustDueDatesForPausedDuration(
        DateTime currentResolutionDueAtUtc,
        DateTime pausedAtUtc,
        DateTime resumedAtUtc,
        IReadOnlySet<DateOnly>? holidays = null)
    {
        var workingPausedMinutes = _calendar.GetWorkingMinutesBetween(pausedAtUtc, resumedAtUtc, holidays);
        if (workingPausedMinutes <= 0)
        {
            return (currentResolutionDueAtUtc, 0);
        }

        var newDueAt = _calendar.AddWorkingMinutes(currentResolutionDueAtUtc, workingPausedMinutes, holidays);
        return (newDueAt, workingPausedMinutes);
    }

    public int CalculateRemainingMinutes(
        DateTime dueAtUtc,
        TicketStatus status,
        DateTime? resolvedAtUtc,
        DateTime? closedAtUtc,
        DateTime? pausedAtUtc,
        DateTime nowUtc)
    {
        if (status is TicketStatus.Resolved or TicketStatus.Closed)
        {
            var completionTime = resolvedAtUtc ?? closedAtUtc ?? nowUtc;
            return (int)Math.Round((dueAtUtc - completionTime).TotalMinutes);
        }

        if (status == TicketStatus.Pending)
        {
            var pauseTime = pausedAtUtc ?? nowUtc;
            return (int)Math.Round((dueAtUtc - pauseTime).TotalMinutes);
        }

        return (int)Math.Round((dueAtUtc - nowUtc).TotalMinutes);
    }
}
