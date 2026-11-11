namespace HelpDeskPro.Application.Features.Dashboard.Queries.GetDashboardSummary;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Dashboard.Models;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class GetDashboardSummaryQueryHandler : IRequestHandler<GetDashboardSummaryQuery, DashboardSummaryDto>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetDashboardSummaryQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<DashboardSummaryDto> Handle(GetDashboardSummaryQuery request, CancellationToken cancellationToken)
    {
        var role = _currentUserService.Role;
        if (role != "Supervisor" && role != "Admin")
        {
            throw new ForbiddenAccessException();
        }

        var cutoffDate = _dateTimeProvider.UtcNow.AddDays(-request.PeriodDays);
        var baseQuery = _context.Tickets.AsNoTracking()
            .Where(t => t.CreatedAt >= cutoffDate);

        if (role == "Supervisor" && Guid.TryParse(_currentUserService.UserId, out var supervisorId))
        {
            var user = await _context.Users
                .AsNoTracking()
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Id == supervisorId, cancellationToken);

            var isItDepartment = user?.Department?.Code == "IT";
            var userDepartmentId = user?.DepartmentId;

            if (!isItDepartment && userDepartmentId.HasValue)
            {
                baseQuery = baseQuery.Where(t =>
                    t.Requester.DepartmentId == userDepartmentId.Value ||
                    (t.Assignee != null && t.Assignee.DepartmentId == userDepartmentId.Value));
            }
        }

        var statusGroups = await baseQuery
            .GroupBy(t => t.Status)
            .Select(g => new { Status = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        var openCount = statusGroups.FirstOrDefault(x => x.Status == TicketStatus.Open)?.Count ?? 0;
        var inProgressCount = statusGroups.FirstOrDefault(x => x.Status == TicketStatus.InProgress)?.Count ?? 0;
        var pendingCount = statusGroups.FirstOrDefault(x => x.Status == TicketStatus.Pending)?.Count ?? 0;
        var resolvedCount = statusGroups.FirstOrDefault(x => x.Status == TicketStatus.Resolved)?.Count ?? 0;
        var closedCount = statusGroups.FirstOrDefault(x => x.Status == TicketStatus.Closed)?.Count ?? 0;
        var cancelledCount = statusGroups.FirstOrDefault(x => x.Status == TicketStatus.Cancelled)?.Count ?? 0;
        var totalCount = statusGroups.Sum(x => x.Count);

        var now = _dateTimeProvider.UtcNow;
        var slaBreachedCount = await baseQuery
            .CountAsync(t => t.IsSlaBreached ||
                (t.Status != TicketStatus.Resolved &&
                 t.Status != TicketStatus.Closed &&
                 t.Status != TicketStatus.Cancelled &&
                 t.Status != TicketStatus.Pending &&
                 now > t.ResolutionDueAt), cancellationToken);

        // Selisih DateTime -> TotalMinutes tidak dapat diterjemahkan ke SQL oleh EF Core,
        // sehingga hanya kolom yang dibutuhkan diambil lalu rata-rata dihitung di memori.
        var responseSamples = await baseQuery
            .Where(t => t.FirstRespondedAt != null)
            .Select(t => new { t.CreatedAt, RespondedAt = t.FirstRespondedAt!.Value })
            .ToListAsync(cancellationToken);

        var resolutionSamples = await baseQuery
            .Where(t => t.ResolvedAt != null)
            .Select(t => new { t.CreatedAt, ResolvedAt = t.ResolvedAt!.Value, t.TotalPausedMinutes })
            .ToListAsync(cancellationToken);

        double? avgResponse = responseSamples.Count > 0
            ? responseSamples.Average(x => (x.RespondedAt - x.CreatedAt).TotalMinutes)
            : null;

        double? avgResolution = resolutionSamples.Count > 0
            ? resolutionSamples.Average(x => Math.Max(0, (x.ResolvedAt - x.CreatedAt).TotalMinutes - x.TotalPausedMinutes))
            : null;

        return new DashboardSummaryDto(
            request.PeriodDays,
            totalCount,
            openCount,
            inProgressCount,
            pendingCount,
            resolvedCount,
            closedCount,
            cancelledCount,
            slaBreachedCount,
            Math.Round(avgResponse ?? 0.0, 1),
            Math.Round(avgResolution ?? 0.0, 1));
    }
}
