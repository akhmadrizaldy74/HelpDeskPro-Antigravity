namespace HelpDeskPro.Application.Features.Dashboard.Queries.GetDashboardSummary;

using HelpDeskPro.Application.Features.Dashboard.Models;
using MediatR;

public sealed record GetDashboardSummaryQuery(int PeriodDays = 30) : IRequest<DashboardSummaryDto>;

