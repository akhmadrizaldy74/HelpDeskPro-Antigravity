namespace HelpDeskPro.Application.Features.Tickets.Queries.GetTicketById;

using HelpDeskPro.Application.Features.Tickets.Models;
using MediatR;

public record GetTicketByIdQuery(long Id) : IRequest<TicketDetailDto>;

