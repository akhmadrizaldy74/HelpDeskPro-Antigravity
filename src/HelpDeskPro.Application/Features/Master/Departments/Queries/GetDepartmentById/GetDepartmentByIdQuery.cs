namespace HelpDeskPro.Application.Features.Master.Departments.Queries.GetDepartmentById;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Master.Departments.Models;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

public sealed record GetDepartmentByIdQuery(int Id) : IRequest<DepartmentDto>;

public class GetDepartmentByIdQueryHandler : IRequestHandler<GetDepartmentByIdQuery, DepartmentDto>
{
    private readonly IApplicationDbContext _context;

    public GetDepartmentByIdQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DepartmentDto> Handle(GetDepartmentByIdQuery request, CancellationToken cancellationToken)
    {
        var department = await _context.Departments
            .AsNoTracking()
            .Where(d => d.Id == request.Id)
            .Select(d => new DepartmentDto(
                d.Id,
                d.Code,
                d.Name,
                d.IsActive,
                d.Users.Count,
                _context.Tickets.Count(t => t.Requester.DepartmentId == d.Id || (t.Assignee != null && t.Assignee.DepartmentId == d.Id))
            ))
            .FirstOrDefaultAsync(cancellationToken);

        if (department is null)
        {
            throw new NotFoundException("Departemen", request.Id);
        }

        return department;
    }
}

