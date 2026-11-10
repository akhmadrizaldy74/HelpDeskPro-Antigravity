namespace HelpDeskPro.Application.Features.Tickets.Common;

using HelpDeskPro.Domain.Enums;

public static class SlaCalculator
{
    public static int CalculateRemainingMinutes(
        DateTime resolutionDueAt,
        TicketStatus status,
        DateTime? resolvedAt,
        DateTime? closedAt,
        DateTime? pausedAt,
        DateTime utcNow)
    {
        if (status == TicketStatus.Resolved && resolvedAt.HasValue)
        {
            return (int)Math.Floor((resolutionDueAt - resolvedAt.Value).TotalMinutes);
        }

        if (status == TicketStatus.Closed && closedAt.HasValue)
        {
            var compareTime = resolvedAt ?? closedAt.Value;
            return (int)Math.Floor((resolutionDueAt - compareTime).TotalMinutes);
        }

        if (status == TicketStatus.Pending && pausedAt.HasValue)
        {
            return (int)Math.Floor((resolutionDueAt - pausedAt.Value).TotalMinutes);
        }

        return (int)Math.Floor((resolutionDueAt - utcNow).TotalMinutes);
    }
}

