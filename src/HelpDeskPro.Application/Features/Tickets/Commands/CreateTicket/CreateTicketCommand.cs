namespace HelpDeskPro.Application.Features.Tickets.Commands.CreateTicket;

using HelpDeskPro.Application.Features.Tickets.Models;
using HelpDeskPro.Domain.Enums;
using MediatR;

public record CreateTicketCommand(
    string Title,
    string? Description,
    int CategoryId,
    TicketPriority Priority = TicketPriority.Medium
) : IRequest<TicketDetailDto>;

