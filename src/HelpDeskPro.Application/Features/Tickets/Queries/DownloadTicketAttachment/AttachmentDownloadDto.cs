namespace HelpDeskPro.Application.Features.Tickets.Queries.DownloadTicketAttachment;

public record AttachmentDownloadDto(
    Stream Stream,
    string ContentType,
    string FileName
);

