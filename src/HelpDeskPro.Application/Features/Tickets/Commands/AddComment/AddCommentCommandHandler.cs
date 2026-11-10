namespace HelpDeskPro.Application.Features.Tickets.Commands.AddComment;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Tickets.Models;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class AddCommentCommandHandler : IRequestHandler<AddCommentCommand, TicketCommentDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly INotificationService _notificationService;

    public AddCommentCommandHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        INotificationService notificationService)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _notificationService = notificationService;
    }

    public async Task<TicketCommentDto> Handle(AddCommentCommand request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentUserService.UserId) || !Guid.TryParse(_currentUserService.UserId, out var currentUserId))
        {
            throw new UnauthorizedAccessException("Pengguna belum diautentikasi.");
        }

        var role = _currentUserService.Role ?? "Requester";

        var ticket = await _context.Tickets
            .Include(t => t.Requester)
            .Include(t => t.Assignee)
            .Include(t => t.Comments)
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

        var utcNow = _dateTimeProvider.UtcNow;
        var comment = ticket.AddComment(request.Body, request.IsInternal, currentUserId, role, utcNow);

        // FR-28: Notifikasi komentar baru di tiket saya
        if (!request.IsInternal && currentUserId != ticket.RequesterId)
        {
            await _notificationService.CreateNotificationAsync(
                ticket.RequesterId,
                ticket.Id,
                Domain.Enums.NotificationType.NewComment,
                $"Tanggapan baru pada tiket {ticket.TicketNo}.",
                cancellationToken);
        }

        if (ticket.AssigneeId.HasValue && currentUserId != ticket.AssigneeId.Value)
        {
            await _notificationService.CreateNotificationAsync(
                ticket.AssigneeId.Value,
                ticket.Id,
                Domain.Enums.NotificationType.NewComment,
                $"Komentar baru pada tiket {ticket.TicketNo}.",
                cancellationToken);
        }

        await _context.SaveChangesAsync(cancellationToken);

        var currentUser = await _context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

        return new TicketCommentDto
        {
            Id = comment.Id,
            TicketId = ticket.Id,
            AuthorId = currentUserId,
            AuthorName = currentUser?.FullName ?? _currentUserService.UserName ?? "User",
            Body = comment.Body,
            IsInternal = comment.IsInternal,
            CreatedAt = comment.CreatedAt
        };
    }
}

