namespace HelpDeskPro.Infrastructure.Services;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;

public class NotificationService : INotificationService
{
    private readonly IApplicationDbContext _context;
    private readonly IDateTimeProvider _dateTimeProvider;

    public NotificationService(
        IApplicationDbContext context,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task CreateNotificationAsync(
        Guid userId,
        long? ticketId,
        NotificationType type,
        string message,
        CancellationToken cancellationToken = default)
    {
        var notification = new Notification
        {
            UserId = userId,
            TicketId = ticketId,
            Type = type,
            Message = message,
            IsRead = false,
            CreatedAt = _dateTimeProvider.UtcNow
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(cancellationToken);
    }
}

