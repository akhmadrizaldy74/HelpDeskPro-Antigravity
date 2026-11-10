namespace HelpDeskPro.Application.Features.Tickets.Commands.UpdateTicket;

using HelpDeskPro.Application.Features.Tickets.Models;
using HelpDeskPro.Domain.Enums;
using MediatR;

public record UpdateTicketCommand(
    long Id,
    string Title,
    string? Description,
    int CategoryId,
    TicketPriority Priority,
    byte[] RowVersion
) : IRequest<TicketDetailDto>;

