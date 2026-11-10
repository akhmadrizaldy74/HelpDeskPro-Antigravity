namespace HelpDeskPro.Application.Features.Tickets.Commands.ChangeTicketStatus;

using HelpDeskPro.Application.Features.Tickets.Models;
using HelpDeskPro.Domain.Enums;
using MediatR;

public record ChangeTicketStatusCommand(
    long Id,
    TicketStatus Status,
    string? Reason,
    byte[] RowVersion
) : IRequest<TicketDetailDto>;

