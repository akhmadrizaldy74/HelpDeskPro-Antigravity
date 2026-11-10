namespace HelpDeskPro.Application.Features.Dashboard.Queries.GetAgentWorkload;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Dashboard.Models;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Enums;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class GetAgentWorkloadQueryHandler : IRequestHandler<GetAgentWorkloadQuery, IReadOnlyList<AgentWorkloadDto>>
{
    private readonly IApplicationDbContext _context;
    private readonly ICurrentUserService _currentUserService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly UserManager<ApplicationUser> _userManager;

    public GetAgentWorkloadQueryHandler(
        IApplicationDbContext context,
        ICurrentUserService currentUserService,
        IDateTimeProvider dateTimeProvider,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _currentUserService = currentUserService;
        _dateTimeProvider = dateTimeProvider;
        _userManager = userManager;
    }

    public async Task<IReadOnlyList<AgentWorkloadDto>> Handle(GetAgentWorkloadQuery request, CancellationToken cancellationToken)
    {
        var role = _currentUserService.Role;
        if (role != "Supervisor" && role != "Admin")
        {
            throw new ForbiddenAccessException();
        }

        int? supervisorDeptId = null;
        var isItDepartment = false;

        if (role == "Supervisor" && Guid.TryParse(_currentUserService.UserId, out var supervisorId))
        {
            var supervisor = await _context.Users
                .AsNoTracking()
                .Include(u => u.Department)
                .FirstOrDefaultAsync(u => u.Id == supervisorId, cancellationToken);

            supervisorDeptId = supervisor?.DepartmentId;
            isItDepartment = supervisor?.Department?.Code == "IT";
        }

        var allAgents = await _userManager.GetUsersInRoleAsync("Agent");

        var targetAgents = allAgents.AsEnumerable();
        if (role == "Supervisor" && !isItDepartment && supervisorDeptId.HasValue)
        {
            targetAgents = targetAgents.Where(a => a.DepartmentId == supervisorDeptId.Value);
        }
        else if (role == "Supervisor" && isItDepartment)
        {
            var itDept = await _context.Departments.AsNoTracking().FirstOrDefaultAsync(d => d.Code == "IT", cancellationToken);
            if (itDept != null)
            {
                targetAgents = targetAgents.Where(a => a.DepartmentId == itDept.Id);
            }
        }

        var agentList = targetAgents.Where(a => a.IsActive).ToList();
        if (agentList.Count == 0)
        {
            return Array.Empty<AgentWorkloadDto>();
        }

        var agentIds = agentList.Select(a => a.Id).ToList();

        var now = _dateTimeProvider.UtcNow;
        DateTime? fromDate = request.Days.HasValue && request.Days.Value > 0
            ? now.AddDays(-request.Days.Value)
            : null;

        var stats = await _context.Tickets.AsNoTracking()
            .Where(t => t.AssigneeId != null && agentIds.Contains(t.AssigneeId.Value) &&
                (t.Status == TicketStatus.Open || t.Status == TicketStatus.InProgress || t.Status == TicketStatus.Pending))
            .GroupBy(t => t.AssigneeId!.Value)
            .Select(g => new
            {
                AgentId = g.Key,
                ActiveCount = g.Count(),
                BreachedCount = g.Count(t => t.IsSlaBreached || (t.Status != TicketStatus.Pending && now > t.ResolutionDueAt))
            })
            .ToListAsync(cancellationToken);

        var statsMap = stats.ToDictionary(s => s.AgentId);

        var ratingQuery = _context.Tickets.AsNoTracking()
            .Where(t => t.AssigneeId != null && agentIds.Contains(t.AssigneeId.Value) && t.Rating != null);

        if (fromDate.HasValue)
        {
            ratingQuery = ratingQuery.Where(t => t.RatedAt >= fromDate.Value || (t.RatedAt == null && t.ClosedAt >= fromDate.Value));
        }

        var ratingStats = await ratingQuery
            .GroupBy(t => t.AssigneeId!.Value)
            .Select(g => new
            {
                AgentId = g.Key,
                Count = g.Count(),
                Average = g.Average(t => (double)t.Rating!.Value)
            })
            .ToListAsync(cancellationToken);

        var ratingMap = ratingStats.ToDictionary(r => r.AgentId);

        var result = agentList.Select(agent =>
        {
            statsMap.TryGetValue(agent.Id, out var stat);
            ratingMap.TryGetValue(agent.Id, out var ratingStat);

            return new AgentWorkloadDto(
                agent.Id,
                agent.FullName,
                agent.Email ?? string.Empty,
                stat?.ActiveCount ?? 0,
                stat?.BreachedCount ?? 0,
                ratingStat != null ? Math.Round(ratingStat.Average, 1) : null,
                ratingStat?.Count ?? 0
            );
        })
        .OrderByDescending(x => x.ActiveTicketCount)
        .ThenBy(x => x.FullName)
        .ToList();

        return result;
    }
}
