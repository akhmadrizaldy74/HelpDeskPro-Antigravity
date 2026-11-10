namespace HelpDeskPro.Application.Features.Tickets.Queries.DownloadTicketAttachment;

using MediatR;

public record DownloadTicketAttachmentQuery(
    long TicketId,
    long AttachmentId
) : IRequest<AttachmentDownloadDto>;

