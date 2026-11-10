namespace HelpDeskPro.Application.Features.Tickets.Queries.GetTicketHistory;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Tickets.Models;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class GetTicketHistoryQueryHandler : IRequestHandler<GetTicketHistoryQuery, IReadOnlyList<TicketHistoryDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetTicketHistoryQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<TicketHistoryDto>> Handle(GetTicketHistoryQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentUserService.UserId) || !Guid.TryParse(_currentUserService.UserId, out var currentUserId))
        {
            throw new UnauthorizedAccessException("Pengguna belum diautentikasi.");
        }

        var role = _currentUserService.Role ?? "Requester";

        var ticket = await _context.Tickets
            .Include(t => t.Requester)
            .Include(t => t.Assignee)
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

        var histories = await _context.TicketHistories
            .Where(h => h.TicketId == request.TicketId)
            .Include(h => h.ChangedBy)
            .OrderBy(h => h.ChangedAt)
            .AsNoTracking()
            .Select(h => new TicketHistoryDto
            {
                Id = h.Id,
                TicketId = h.TicketId,
                FieldName = h.FieldName,
                OldValue = h.OldValue,
                NewValue = h.NewValue,
                ChangedById = h.ChangedById,
                ChangedByName = h.ChangedBy != null ? h.ChangedBy.FullName : "System",
                ChangedAt = h.ChangedAt
            })
            .ToListAsync(cancellationToken);

        return histories;
    }
}

