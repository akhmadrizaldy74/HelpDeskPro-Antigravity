namespace HelpDeskPro.Application.Features.Dashboard.Queries.GetTicketTrend;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Dashboard.Models;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class GetTicketTrendQueryHandler : IRequestHandler<GetTicketTrendQuery, IReadOnlyList<TicketTrendPointDto>>
{
    private static readonly TimeSpan _wibOffset = TimeSpan.FromHours(7);

    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;

    public GetTicketTrendQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
    }

    public async Task<IReadOnlyList<TicketTrendPointDto>> Handle(GetTicketTrendQuery request, CancellationToken cancellationToken)
    {
        var role = _currentUserService.Role;
        if (role != "Supervisor" && role != "Admin")
        {
            throw new ForbiddenAccessException();
        }

        var todayWib = DateOnly.FromDateTime(_dateTimeProvider.UtcNow + _wibOffset);
        var firstDayWib = todayWib.AddDays(-(request.Days - 1));
        var fromUtc = firstDayWib.ToDateTime(TimeOnly.MinValue) - _wibOffset;

        var query = _context.Tickets.AsNoTracking();

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
                query = query.Where(t =>
                    t.Requester.DepartmentId == userDepartmentId.Value ||
                    (t.Assignee != null && t.Assignee.DepartmentId == userDepartmentId.Value));
            }
        }

        var rows = await query
            .Where(t => t.CreatedAt >= fromUtc || (t.ResolvedAt != null && t.ResolvedAt >= fromUtc))
            .Select(t => new { t.CreatedAt, t.ResolvedAt })
            .ToListAsync(cancellationToken);

        var created = rows
            .Where(r => r.CreatedAt >= fromUtc)
            .GroupBy(r => DateOnly.FromDateTime(r.CreatedAt + _wibOffset))
            .ToDictionary(g => g.Key, g => g.Count());

        var resolved = rows
            .Where(r => r.ResolvedAt.HasValue && r.ResolvedAt.Value >= fromUtc)
            .GroupBy(r => DateOnly.FromDateTime(r.ResolvedAt!.Value + _wibOffset))
            .ToDictionary(g => g.Key, g => g.Count());

        var result = new List<TicketTrendPointDto>(request.Days);
        for (var day = firstDayWib; day <= todayWib; day = day.AddDays(1))
        {
            result.Add(new TicketTrendPointDto(
                day,
                created.GetValueOrDefault(day),
                resolved.GetValueOrDefault(day)));
        }

        return result;
    }
}
