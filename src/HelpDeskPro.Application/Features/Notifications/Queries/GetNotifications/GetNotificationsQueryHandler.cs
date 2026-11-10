namespace HelpDeskPro.Application.Features.Notifications.Queries.GetNotifications;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Notifications.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class GetNotificationsQueryHandler : IRequestHandler<GetNotificationsQuery, IReadOnlyList<NotificationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetNotificationsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<NotificationDto>> Handle(GetNotificationsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentUserService.UserId) || !Guid.TryParse(_currentUserService.UserId, out var currentUserId))
        {
            throw new UnauthorizedAccessException("Pengguna belum diautentikasi.");
        }

        var notifications = await _context.Notifications
            .Where(n => n.UserId == currentUserId)
            .Include(n => n.Ticket)
            .OrderByDescending(n => n.CreatedAt)
            .AsNoTracking()
            .Select(n => new NotificationDto
            {
                Id = n.Id,
                UserId = n.UserId,
                TicketId = n.TicketId,
                TicketNo = n.Ticket != null ? n.Ticket.TicketNo : null,
                Type = n.Type,
                Message = n.Message,
                IsRead = n.IsRead,
                CreatedAt = n.CreatedAt
            })
            .ToListAsync(cancellationToken);

        return notifications;
    }
}

