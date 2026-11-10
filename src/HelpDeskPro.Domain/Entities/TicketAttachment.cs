namespace HelpDeskPro.Domain.Entities;

using HelpDeskPro.Domain.Common;

public class TicketAttachment : BaseEntity<long>
{
    public long TicketId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
    public string StoragePath { get; set; } = string.Empty;
    public Guid UploadedById { get; set; }
    public DateTime UploadedAt { get; set; }

    public Ticket Ticket { get; set; } = null!;
    public ApplicationUser UploadedBy { get; set; } = null!;
}

