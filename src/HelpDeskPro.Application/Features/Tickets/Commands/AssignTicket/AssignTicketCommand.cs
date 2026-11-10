namespace HelpDeskPro.Application.Features.Tickets.Commands.AssignTicket;

using HelpDeskPro.Application.Features.Tickets.Models;
using MediatR;

public record AssignTicketCommand(
    long Id,
    Guid AssigneeId,
    byte[] RowVersion
) : IRequest<TicketDetailDto>;

