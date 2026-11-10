namespace HelpDeskPro.Application.Features.Dashboard.Models;

public sealed record DashboardSummaryDto(
    int PeriodDays,
    int TotalTickets,
    int OpenTickets,
    int InProgressTickets,
    int PendingTickets,
    int ResolvedTickets,
    int ClosedTickets,
    int CancelledTickets,
    int SlaBreachedTickets,
    double AverageResponseMinutes,
    double AverageResolutionMinutes
);

