namespace HelpDeskPro.Application.Features.Tickets.Commands.UploadTicketAttachment;

using HelpDeskPro.Application.Features.Tickets.Models;
using MediatR;

public record UploadTicketAttachmentCommand(
    long TicketId,
    Stream FileStream,
    string FileName,
    string ContentType,
    long FileSizeBytes
) : IRequest<TicketAttachmentDto>;

