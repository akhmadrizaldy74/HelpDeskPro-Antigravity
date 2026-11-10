namespace HelpDeskPro.Domain.Entities;

using HelpDeskPro.Domain.Common;
using HelpDeskPro.Domain.Enums;

public class Notification : BaseEntity<long>
{
    public Guid UserId { get; set; }
    public long? TicketId { get; set; }
    public NotificationType Type { get; set; }
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime CreatedAt { get; set; }

    public ApplicationUser User { get; set; } = null!;
    public Ticket? Ticket { get; set; }
}

