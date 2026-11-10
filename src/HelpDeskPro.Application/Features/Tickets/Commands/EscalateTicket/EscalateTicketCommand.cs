namespace HelpDeskPro.Application.Features.Tickets.Commands.EscalateTicket;

using HelpDeskPro.Application.Features.Escalations.Models;
using MediatR;

public record EscalateTicketCommand(
    long TicketId,
    string Note
) : IRequest<EscalationDto>;

