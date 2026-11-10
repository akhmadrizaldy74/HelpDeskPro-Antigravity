namespace HelpDeskPro.Application.Features.Tickets.Commands.UploadTicketAttachment;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Tickets.Models;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class UploadTicketAttachmentCommandHandler : IRequestHandler<UploadTicketAttachmentCommand, TicketAttachmentDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly IFileStorage _fileStorage;

    public UploadTicketAttachmentCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        IFileStorage fileStorage)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _fileStorage = fileStorage;
    }

    public async Task<TicketAttachmentDto> Handle(UploadTicketAttachmentCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentUserService.UserId) || !Guid.TryParse(_currentUserService.UserId, out var currentUserId))
        {
            throw new UnauthorizedAccessException("Pengguna belum diautentikasi.");
        }

        var role = _currentUserService.Role ?? "Requester";

        var ticket = await _context.Tickets
            .Include(t => t.Requester)
            .Include(t => t.Assignee)
            .Include(t => t.Attachments)
            .FirstOrDefaultAsync(t => t.Id == request.TicketId, cancellationToken);

        if (ticket == null)
        {
            throw new NotFoundException("Tiket", request.TicketId);
        }

        var hasAccess = false;
        switch (role)
        {
            case "Admin":
                hasAccess = true;
                break;

            case "Supervisor":
                var supervisor = await _context.Users
                    .AsNoTracking()
                    .Include(u => u.Department)
                    .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

                if (supervisor?.Department?.Code == "IT")
                {
                    hasAccess = true;
                }
                else if (supervisor?.DepartmentId != null)
                {
                    hasAccess = ticket.Requester.DepartmentId == supervisor.DepartmentId
                             || (ticket.Assignee != null && ticket.Assignee.DepartmentId == supervisor.DepartmentId);
                }
                break;

            case "Agent":
                hasAccess = ticket.AssigneeId == currentUserId || ticket.AssigneeId == null;
                break;

            default: // Requester
                hasAccess = ticket.RequesterId == currentUserId;
                break;
        }

        if (!hasAccess)
        {
            throw new NotFoundException("Tiket", request.TicketId);
        }

        if (ticket.Attachments.Count >= 5)
        {
            throw new BusinessRuleException("Jumlah lampiran untuk tiket ini sudah mencapai batas maksimal (5 file).");
        }

        var storagePath = await _fileStorage.SaveFileAsync(request.FileStream, request.FileName, cancellationToken);
        var utcNow = _dateTimeProvider.UtcNow;
        var attachment = ticket.AddAttachment(
            request.FileName,
            request.ContentType,
            request.FileSizeBytes,
            storagePath,
            currentUserId,
            utcNow);

        await _context.SaveChangesAsync(cancellationToken);

        return new TicketAttachmentDto
        {
            Id = attachment.Id,
            FileName = attachment.FileName,
            ContentType = attachment.ContentType,
            FileSizeBytes = attachment.FileSizeBytes,
            UploadedAt = attachment.UploadedAt
        };
    }
}

