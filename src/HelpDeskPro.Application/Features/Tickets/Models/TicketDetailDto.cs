namespace HelpDeskPro.Application.Features.Tickets.Models;

using HelpDeskPro.Domain.Enums;

public class TicketDetailDto
{
    public long Id { get; set; }
    public string TicketNo { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public TicketPriority Priority { get; set; }
    public TicketStatus Status { get; set; }
    public Guid RequesterId { get; set; }
    public string RequesterName { get; set; } = string.Empty;
    public Guid? AssigneeId { get; set; }
    public string? AssigneeName { get; set; }
    public DateTime ResponseDueAt { get; set; }
    public DateTime ResolutionDueAt { get; set; }
    public int SlaRemainingMinutes { get; set; }
    public DateTime? FirstRespondedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
    public DateTime? PausedAt { get; set; }
    public int TotalPausedMinutes { get; set; }
    public bool IsSlaBreached { get; set; }
    public byte EscalationLevel { get; set; }
    public int? Rating { get; set; }
    public string? RatingComment { get; set; }
    public DateTime? RatedAt { get; set; }
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
    public DateTime CreatedAt { get; set; }
    public string? CreatedBy { get; set; }

    public IReadOnlyList<TicketAttachmentDto> Attachments { get; set; } = Array.Empty<TicketAttachmentDto>();
    public IReadOnlyList<TimelineItemDto> Timeline { get; set; } = Array.Empty<TimelineItemDto>();
}
