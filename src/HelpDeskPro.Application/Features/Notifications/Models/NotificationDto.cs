namespace HelpDeskPro.Application.Features.Notifications.Models;

using HelpDeskPro.Domain.Enums;

public class NotificationDto
{
    public long Id { get; set; }
    public Guid UserId { get; set; }
    public long? TicketId { get; set; }
    public string? TicketNo { get; set; }
    public NotificationType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }
}

