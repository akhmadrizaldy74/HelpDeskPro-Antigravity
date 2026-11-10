namespace HelpDeskPro.Application.Features.Tickets.Queries.GetTickets;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Common.Models;
using HelpDeskPro.Application.Features.Tickets.Common;
using HelpDeskPro.Application.Features.Tickets.Models;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class GetTicketsQueryHandler : IRequestHandler<GetTicketsQuery, PagedResult<TicketListItemDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetTicketsQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<PagedResult<TicketListItemDto>> Handle(GetTicketsQuery request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrEmpty(_currentUserService.UserId) || !Guid.TryParse(_currentUserService.UserId, out var currentUserId))
        {
            throw new UnauthorizedAccessException("Pengguna belum diautentikasi.");
        }

        var role = _currentUserService.Role ?? "Requester";
        int? userDepartmentId = null;
        var isItDepartment = false;

        if (role == "Supervisor")
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Id == currentUserId, cancellationToken);

            userDepartmentId = user?.DepartmentId;
            isItDepartment = user?.Department?.Code == "IT";
        }

        var query = _context.Tickets.AsNoTracking();

        // 1. Role-based scoping (FSD 3.1)
        query = role switch
        {
            "Admin" => query,
            "Supervisor" => isItDepartment
                ? query
                : query.Where(t => t.Requester.DepartmentId == userDepartmentId || (t.Assignee != null && t.Assignee.DepartmentId == userDepartmentId)),
            "Agent" => query.Where(t => t.AssigneeId == currentUserId || t.AssigneeId == null),
            _ => query.Where(t => t.RequesterId == currentUserId) // Requester
        };

        // 2. Filters
        if (request.Statuses is { Count: > 0 })
        {
            query = query.Where(t => request.Statuses.Contains(t.Status));
        }

        if (request.Priorities is { Count: > 0 })
        {
            query = query.Where(t => request.Priorities.Contains(t.Priority));
        }

        if (request.CategoryId.HasValue)
        {
            query = query.Where(t => t.CategoryId == request.CategoryId.Value);
        }

        if (request.AssigneeId.HasValue)
        {
            query = query.Where(t => t.AssigneeId == request.AssigneeId.Value);
        }

        if (request.Unassigned == true)
        {
            query = query.Where(t => t.AssigneeId == null);
        }

        if (request.DateFrom.HasValue)
        {
            query = query.Where(t => t.CreatedAt >= request.DateFrom.Value);
        }

        if (request.DateTo.HasValue)
        {
            query = query.Where(t => t.CreatedAt <= request.DateTo.Value);
        }

        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var search = request.Search.Trim();
            query = query.Where(t => t.TicketNo.Contains(search) || t.Title.Contains(search));
        }

        var totalCount = await query.CountAsync(cancellationToken);

        // 3. Default sorting (FR-08: SLA lewat paling atas, lalu ResolutionDueAt terdekat)
        var utcNow = _dateTimeProvider.UtcNow;
        query = query.OrderByDescending(t => t.IsSlaBreached || (t.ResolutionDueAt < utcNow && t.Status != TicketStatus.Resolved && t.Status != TicketStatus.Closed && t.Status != TicketStatus.Cancelled))
                     .ThenBy(t => t.ResolutionDueAt);

        // 4. Projection with Select (no full entity load) & Paging
        var items = await query
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(t => new
            {
                t.Id,
                t.TicketNo,
                t.Title,
                t.CategoryId,
                CategoryName = t.Category.Name,
                t.Priority,
                t.Status,
                t.RequesterId,
                RequesterName = t.Requester.FullName,
                t.AssigneeId,
                AssigneeName = t.Assignee != null ? t.Assignee.FullName : null,
                t.ResponseDueAt,
                t.ResolutionDueAt,
                t.ResolvedAt,
                t.ClosedAt,
                t.PausedAt,
                t.IsSlaBreached,
                t.CreatedAt,
                t.UpdatedAt
            })
            .ToListAsync(cancellationToken);

        var dtos = items.Select(t => new TicketListItemDto
        {
            Id = t.Id,
            TicketNo = t.TicketNo,
            Title = t.Title,
            CategoryId = t.CategoryId,
            CategoryName = t.CategoryName,
            Priority = t.Priority,
            Status = t.Status,
            RequesterId = t.RequesterId,
            RequesterName = t.RequesterName,
            AssigneeId = t.AssigneeId,
            AssigneeName = t.AssigneeName,
            ResponseDueAt = t.ResponseDueAt,
            ResolutionDueAt = t.ResolutionDueAt,
            SlaRemainingMinutes = SlaCalculator.CalculateRemainingMinutes(
                t.ResolutionDueAt,
                t.Status,
                t.ResolvedAt,
                t.ClosedAt,
                t.PausedAt,
                utcNow),
            IsSlaBreached = t.IsSlaBreached,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt
        }).ToList();

        return PagedResult.Create(dtos, totalCount, request.Page, request.PageSize);
    }
}
