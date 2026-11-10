namespace HelpDeskPro.Application.Features.Tickets.Commands.RateTicket;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Tickets.Models;
using HelpDeskPro.Application.Features.Tickets.Queries.GetTicketById;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class RateTicketCommandHandler : IRequestHandler<RateTicketCommand, TicketDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationService _notificationService;
    private readonly ISender _sender;

    public RateTicketCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        INotificationService notificationService,
        ISender sender)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _notificationService = notificationService;
        _sender = sender;
    }

    public async Task<TicketDetailDto> Handle(RateTicketCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentUserService.UserId) || !Guid.TryParse(_currentUserService.UserId, out var currentUserId))
        {
            throw new UnauthorizedAccessException("Pengguna belum diautentikasi.");
        }

        var userRole = _currentUserService.Role ?? "Requester";

        var ticket = await _context.Tickets
            .Include(t => t.Histories)
            .Include(t => t.Requester)
            .Include(t => t.Assignee)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket == null)
        {
            throw new NotFoundException("Tiket", request.TicketId);
        }

        var utcNow = _dateTimeProvider.UtcNow;

        // Domain method handles validation, status change to Closed, and history recording
        ticket.Rate(request.Rating, request.Comment, currentUserId, userRole, utcNow);

        // Notify Assignee if assigned
        if (ticket.AssigneeId.HasValue && ticket.AssigneeId.Value != currentUserId)
        {
            var commentSnippet = string.IsNullOrWhiteSpace(request.Comment)
                ? string.Empty
                : $" Catatan: \"{request.Comment}\"";

            await _notificationService.CreateNotificationAsync(
                ticket.AssigneeId.Value,
                ticket.Id,
                NotificationType.StatusChanged,
                $"Tiket {ticket.TicketNo} telah selesai dan diberi rating {request.Rating}/5 bintang.{commentSnippet}",
                cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        // Return refreshed detail DTO
        return await _sender.Send(new GetTicketByIdQuery(ticket.Id), cancellationToken);
    }
}
