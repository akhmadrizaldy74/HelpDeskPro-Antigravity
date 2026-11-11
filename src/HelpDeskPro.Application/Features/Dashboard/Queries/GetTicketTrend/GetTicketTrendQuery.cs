namespace HelpDeskPro.Application.Features.Dashboard.Queries.GetTicketTrend;

using HelpDeskPro.Application.Features.Dashboard.Models;
using MediatR;

public sealed record GetTicketTrendQuery(int Days = 7) : IRequest<IReadOnlyList<TicketTrendPointDto>>;
