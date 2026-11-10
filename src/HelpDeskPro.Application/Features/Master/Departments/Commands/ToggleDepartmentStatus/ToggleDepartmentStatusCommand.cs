namespace HelpDeskPro.Application.Features.Master.Departments.Commands.ToggleDepartmentStatus;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Master.Departments.Models;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

public sealed record ToggleDepartmentStatusCommand(int Id, bool? IsActive = null) : IRequest<DepartmentDto>;

public class ToggleDepartmentStatusCommandHandler : IRequestHandler<ToggleDepartmentStatusCommand, DepartmentDto>
{
    private readonly IApplicationDbContext _context;

    public ToggleDepartmentStatusCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DepartmentDto> Handle(ToggleDepartmentStatusCommand request, CancellationToken cancellationToken)
    {
        var department = await _context.Departments
            .Include(d => d.Users)
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (department is null)
        {
            throw new NotFoundException("Departemen", request.Id);
        }

        department.IsActive = request.IsActive ?? !department.IsActive;
        await _context.SaveChangesAsync(cancellationToken);

        var ticketCount = await _context.Tickets
            .CountAsync(t => t.Requester.DepartmentId == department.Id || (t.Assignee != null && t.Assignee.DepartmentId == department.Id), cancellationToken);

        return new DepartmentDto(
            department.Id,
            department.Code,
            department.Name,
            department.IsActive,
            department.Users.Count,
            ticketCount);
    }
}

