namespace HelpDeskPro.Application.Features.Tickets.Commands.AddComment;

using HelpDeskPro.Application.Features.Tickets.Models;
using MediatR;

public record AddCommentCommand(
    long TicketId,
    string Body,
    bool IsInternal
) : IRequest<TicketCommentDto>;

