namespace HelpDeskPro.Domain.Entities;

using HelpDeskPro.Domain.Common;

public class TicketHistory : BaseEntity<long>
{
    public long TicketId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public Guid? ChangedById { get; set; }
    public DateTime ChangedAt { get; set; }

    public Ticket Ticket { get; set; } = null!;
    public ApplicationUser? ChangedBy { get; set; }
}

