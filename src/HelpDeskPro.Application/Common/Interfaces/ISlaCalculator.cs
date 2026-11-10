namespace HelpDeskPro.Application.Common.Interfaces;

using HelpDeskPro.Domain.Enums;

public interface ISlaCalculator
{
    Task<(DateTime ResponseDueAt, DateTime ResolutionDueAt)> CalculateDueDatesAsync(
        DateTime createdUtc,
        int responseMinutes,
        int resolutionMinutes,
        CancellationToken cancellationToken = default);

    (DateTime ResponseDueAt, DateTime ResolutionDueAt) CalculateDueDates(
        DateTime createdUtc,
        int responseMinutes,
        int resolutionMinutes,
        IReadOnlySet<DateOnly>? holidays = null);

    Task<(DateTime NewResolutionDueAt, int WorkingPausedMinutes)> AdjustDueDatesForPausedDurationAsync(
        DateTime currentResolutionDueAtUtc,
        DateTime pausedAtUtc,
        DateTime resumedAtUtc,
        CancellationToken cancellationToken = default);

    (DateTime NewResolutionDueAt, int WorkingPausedMinutes) AdjustDueDatesForPausedDuration(
        DateTime currentResolutionDueAtUtc,
        DateTime pausedAtUtc,
        DateTime resumedAtUtc,
        IReadOnlySet<DateOnly>? holidays = null);

    int CalculateRemainingMinutes(
        DateTime dueAtUtc,
        TicketStatus status,
        DateTime? resolvedAtUtc,
        DateTime? closedAtUtc,
        DateTime? pausedAtUtc,
        DateTime nowUtc);
}
