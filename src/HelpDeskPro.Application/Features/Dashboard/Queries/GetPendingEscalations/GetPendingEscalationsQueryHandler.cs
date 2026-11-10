namespace HelpDeskPro.Application.Features.Dashboard.Queries.GetPendingEscalations;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Dashboard.Models;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class GetPendingEscalationsQueryHandler : IRequestHandler<GetPendingEscalationsQuery, IReadOnlyList<PendingEscalationDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public GetPendingEscalationsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    public async Task<IReadOnlyList<PendingEscalationDto>> Handle(GetPendingEscalationsQuery request, CancellationToken cancellationToken)
    {
        var role = _currentUserService.Role;
        if (role != "Supervisor" && role != "Admin")
        {
            throw new ForbiddenAccessException();
        }

        var query = _context.Escalations.AsNoTracking()
            .Where(e => e.AcknowledgedAt == null);

        if (role == "Supervisor" && Guid.TryParse(_currentUserService.UserId, out var supervisorId))
        {
            var supervisor = await _context.Users
                .AsNoTracking()
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Id == supervisorId, cancellationToken);

            var isItDepartment = supervisor?.Department?.Code == "IT";
            var userDepartmentId = supervisor?.DepartmentId;

            if (!isItDepartment && userDepartmentId.HasValue)
            {
                query = query.Where(e =>
                    e.Ticket.Requester.DepartmentId == userDepartmentId.Value ||
                    (e.Ticket.Assignee != null && e.Ticket.Assignee.DepartmentId == userDepartmentId.Value));
            }
        }

        var result = await query
            .OrderBy(e => e.EscalatedAt)
            .Select(e => new PendingEscalationDto(
                e.Id,
                e.TicketId,
                e.Ticket.TicketNo,
                e.Ticket.Title,
                e.Ticket.Priority,
                e.Level,
                e.Reason,
                e.Note,
                e.EscalatedAt,
                e.EscalatedTo.FullName,
                e.EscalatedBy != null ? e.EscalatedBy.FullName : null
            ))
            .ToListAsync(cancellationToken);

        return result;
    }
}
