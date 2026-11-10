namespace HelpDeskPro.Application.Features.Tickets.Models;

public enum TimelineItemType
{
    Comment = 1,
    History = 2
}

public class TimelineItemDto
{
    public TimelineItemType Type { get; set; }
    public DateTime Timestamp { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string? CommentBody { get; set; }
    public bool IsInternal { get; set; }
    public string? FieldName { get; set; }
    public string? OldValue { get; set; }
    public string? NewValue { get; set; }
}

