namespace HelpDeskPro.Application.Features.Master.Departments.Queries.GetDepartments;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Master.Departments.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

public sealed record GetDepartmentsQuery(bool? IsActive = null) : IRequest<IReadOnlyList<DepartmentDto>>;

public class GetDepartmentsQueryHandler : IRequestHandler<GetDepartmentsQuery, IReadOnlyList<DepartmentDto>>
{
    private readonly IApplicationDbContext _context;

    public GetDepartmentsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<DepartmentDto>> Handle(GetDepartmentsQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Departments.AsNoTracking();

        if (request.IsActive.HasValue)
        {
            query = query.Where(d => d.IsActive == request.IsActive.Value);
        }

        var list = await query
            .OrderBy(d => d.Name)
            .Select(d => new DepartmentDto(
                d.Id,
                d.Code,
                d.Name,
                d.IsActive,
                d.Users.Count,
                _context.Tickets.Count(t => t.Requester.DepartmentId == d.Id || (t.Assignee != null && t.Assignee.DepartmentId == d.Id))
            ))
            .ToListAsync(cancellationToken);

        return list;
    }
}

