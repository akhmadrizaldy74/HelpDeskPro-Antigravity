namespace HelpDeskPro.Application.Features.Tickets.Queries.GetTicketHistory;

using HelpDeskPro.Application.Features.Tickets.Models;
using MediatR;

public record GetTicketHistoryQuery(
    long TicketId
) : IRequest<IReadOnlyList<TicketHistoryDto>>;

