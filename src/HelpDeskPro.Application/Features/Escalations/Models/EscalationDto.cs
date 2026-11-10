namespace HelpDeskPro.Application.Features.Escalations.Models;

using HelpDeskPro.Domain.Enums;

public class EscalationDto
{
    public long Id { get; set; }
    public long TicketId { get; set; }
    public byte Level { get; set; }
    public EscalationReason Reason { get; set; }
    public string? Note { get; set; }
    public Guid EscalatedToId { get; set; }
    public string EscalatedToName { get; set; } = string.Empty;
    public Guid? EscalatedById { get; set; }
    public string? EscalatedByName { get; set; }
    public DateTime EscalatedAt { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
}

