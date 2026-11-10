namespace HelpDeskPro.Application.Features.Tickets.Queries.GetTicketById;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Tickets.Common;
using HelpDeskPro.Application.Features.Tickets.Models;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class GetTicketByIdQueryHandler : IRequestHandler<GetTicketByIdQuery, TicketDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetTicketByIdQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<TicketDetailDto> Handle(GetTicketByIdQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentUserService.UserId) || !Guid.TryParse(_currentUserService.UserId, out var currentUserId))
        {
            throw new UnauthorizedAccessException("Pengguna belum diautentikasi.");
        }

        var ticket = await _context.Tickets
            .AsNoTracking()
            .AsSplitQuery()
            .Include(t => t.Category)
            .Include(t => t.Requester).ThenInclude(r => r.Department)
            .Include(t => t.Assignee).ThenInclude(a => a!.Department)
            .Include(t => t.Attachments).ThenInclude(a => a.UploadedBy)
            .Include(t => t.Comments).ThenInclude(c => c.Author)
            .Include(t => t.Histories).ThenInclude(h => h.ChangedBy)
            .FirstOrDefaultAsync(t => t.Id == request.Id, cancellationToken);

        if (ticket == null)
        {
            throw new NotFoundException("Tiket", request.Id);
        }

        var role = _currentUserService.Role ?? "Requester";
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
            // Requirement 6: Tiket di luar hak akses → 404 (bukan 403) agar tidak bocor keberadaannya.
            throw new NotFoundException("Tiket", request.Id);
        }

        var utcNow = _dateTimeProvider.UtcNow;
        var isRequester = role == "Requester";

        // Filter internal comments for Requester (AC-05)
        var allowedComments = isRequester
            ? ticket.Comments.Where(c => !c.IsInternal)
            : ticket.Comments;

        var commentTimeline = allowedComments.Select(c => new TimelineItemDto
        {
            Type = TimelineItemType.Comment,
            Timestamp = c.CreatedAt,
            AuthorId = c.AuthorId,
            AuthorName = c.Author?.FullName ?? string.Empty,
            CommentBody = c.Body,
            IsInternal = c.IsInternal
        });

        var historyTimeline = ticket.Histories.Select(h => new TimelineItemDto
        {
            Type = TimelineItemType.History,
            Timestamp = h.ChangedAt,
            AuthorId = h.ChangedById,
            AuthorName = h.ChangedBy?.FullName ?? string.Empty,
            FieldName = h.FieldName,
            OldValue = h.OldValue,
            NewValue = h.NewValue
        });

        var mergedTimeline = commentTimeline
            .Concat(historyTimeline)
            .OrderBy(t => t.Timestamp)
            .ToList();

        var attachments = ticket.Attachments.Select(a => new TicketAttachmentDto
        {
            Id = a.Id,
            FileName = a.FileName,
            ContentType = a.ContentType,
            FileSizeBytes = a.FileSizeBytes,
            UploadedById = a.UploadedById,
            UploadedByName = a.UploadedBy?.FullName ?? string.Empty,
            UploadedAt = a.UploadedAt
        }).ToList();

        return new TicketDetailDto
        {
            Id = ticket.Id,
            TicketNo = ticket.TicketNo,
            Title = ticket.Title,
            Description = ticket.Description,
            CategoryId = ticket.CategoryId,
            CategoryName = ticket.Category?.Name ?? string.Empty,
            Priority = ticket.Priority,
            Status = ticket.Status,
            RequesterId = ticket.RequesterId,
            RequesterName = ticket.Requester?.FullName ?? string.Empty,
            AssigneeId = ticket.AssigneeId,
            AssigneeName = ticket.Assignee?.FullName,
            ResponseDueAt = ticket.ResponseDueAt,
            ResolutionDueAt = ticket.ResolutionDueAt,
            SlaRemainingMinutes = SlaCalculator.CalculateRemainingMinutes(
                ticket.ResolutionDueAt,
                ticket.Status,
                ticket.ResolvedAt,
                ticket.ClosedAt,
                ticket.PausedAt,
                utcNow),
            FirstRespondedAt = ticket.FirstRespondedAt,
            ResolvedAt = ticket.ResolvedAt,
            ClosedAt = ticket.ClosedAt,
            PausedAt = ticket.PausedAt,
            TotalPausedMinutes = ticket.TotalPausedMinutes,
            IsSlaBreached = ticket.IsSlaBreached,
            EscalationLevel = ticket.EscalationLevel,
            RowVersion = ticket.RowVersion,
            CreatedAt = ticket.CreatedAt,
            CreatedBy = ticket.CreatedBy,
            Attachments = attachments,
            Timeline = mergedTimeline
        };
    }
}

