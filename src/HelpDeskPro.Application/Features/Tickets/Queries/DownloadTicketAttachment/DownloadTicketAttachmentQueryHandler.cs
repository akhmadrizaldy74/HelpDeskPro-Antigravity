namespace HelpDeskPro.Application.Features.Tickets.Queries.DownloadTicketAttachment;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class DownloadTicketAttachmentQueryHandler : IRequestHandler<DownloadTicketAttachmentQuery, AttachmentDownloadDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IFileStorage _fileStorage;

    public DownloadTicketAttachmentQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IFileStorage fileStorage)
    {
        _context = context;
        _currentUserService = currentUserService;
        _fileStorage = fileStorage;
    }

    public async Task<AttachmentDownloadDto> Handle(DownloadTicketAttachmentQuery request, CancellationToken cancellationToken)
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

        var attachment = ticket.Attachments.FirstOrDefault(a => a.Id == request.AttachmentId);
        if (attachment == null)
        {
            throw new NotFoundException("Lampiran", request.AttachmentId);
        }

        var fileStream = await _fileStorage.GetFileAsync(attachment.StoragePath, cancellationToken);
        if (fileStream == null)
        {
            throw new NotFoundException("File fisik lampiran", attachment.StoragePath);
        }

        return new AttachmentDownloadDto(fileStream, attachment.ContentType, attachment.FileName);
    }
}

