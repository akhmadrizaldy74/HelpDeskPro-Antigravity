namespace HelpDeskPro.Domain.Entities;

using HelpDeskPro.Domain.Common;

public class TicketComment : BaseEntity<long>
{
    public long TicketId { get; set; }
    public Guid AuthorId { get; set; }
    public string Body { get; set; } = string.Empty;
    public bool IsInternal { get; set; }
    public DateTime CreatedAt { get; set; }

    public Ticket Ticket { get; set; } = null!;
    public ApplicationUser Author { get; set; } = null!;
}

