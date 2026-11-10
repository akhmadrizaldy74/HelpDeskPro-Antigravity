namespace HelpDeskPro.Application.Features.Tickets.Commands.RateTicket;

using HelpDeskPro.Application.Features.Tickets.Models;
using MediatR;

public sealed record RateTicketCommand(
    long TicketId,
    int Rating,
    string? Comment
) : IRequest<TicketDetailDto>;
