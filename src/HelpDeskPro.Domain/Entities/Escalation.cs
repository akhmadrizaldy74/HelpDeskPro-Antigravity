namespace HelpDeskPro.Domain.Entities;

using HelpDeskPro.Domain.Common;
using HelpDeskPro.Domain.Enums;

public class Escalation : BaseEntity<long>
{
    public long TicketId { get; set; }
    public byte Level { get; set; }
    public EscalationReason Reason { get; set; }
    public string? Note { get; set; }
    public Guid EscalatedToId { get; set; }
    public Guid? EscalatedById { get; set; }
    public DateTime EscalatedAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }

    public Ticket Ticket { get; set; } = null!;
    public ApplicationUser EscalatedTo { get; set; } = null!;
    public ApplicationUser? EscalatedBy { get; set; }
}

