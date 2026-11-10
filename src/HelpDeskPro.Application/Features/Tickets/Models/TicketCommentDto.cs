namespace HelpDeskPro.Application.Features.Tickets.Models;

public class TicketCommentDto
{
    public long Id { get; set; }
    public long TicketId { get; set; }
    public Guid AuthorId { get; set; }
    public string AuthorName { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public DateTime CreatedAt { get; set; }
}

