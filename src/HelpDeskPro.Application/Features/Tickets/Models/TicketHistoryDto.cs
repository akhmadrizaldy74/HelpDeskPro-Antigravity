namespace HelpDeskPro.Application.Features.Tickets.Models;

public class TicketHistoryDto
{
    public long Id { get; set; }
    public long TicketId { get; set; }
    public string FieldName { get; set; } = string.Empty;
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
    public Guid ChangedById { get; set; }
    public string ChangedByName { get; set; } = string.Empty;
    public DateTime ChangedAt { get; set; }
}

