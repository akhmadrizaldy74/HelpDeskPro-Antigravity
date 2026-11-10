namespace HelpDeskPro.Application.Features.Master.Departments.Commands.UpdateDepartment;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Master.Departments.Models;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class UpdateDepartmentCommandHandler : IRequestHandler<UpdateDepartmentCommand, DepartmentDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateDepartmentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DepartmentDto> Handle(UpdateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var department = await _context.Departments
            .Include(d => d.Users)
            .FirstOrDefaultAsync(d => d.Id == request.Id, cancellationToken);

        if (department is null)
        {
            throw new NotFoundException("Departemen", request.Id);
        }

        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        var normalizedName = request.Name.Trim();

        var codeExists = await _context.Departments
            .AnyAsync(d => d.Code == normalizedCode && d.Id != request.Id, cancellationToken);
        if (codeExists)
        {
            throw new BusinessRuleException($"Kode departemen '{normalizedCode}' sudah digunakan.");
        }

        var nameExists = await _context.Departments
            .AnyAsync(d => d.Name == normalizedName && d.Id != request.Id, cancellationToken);
        if (nameExists)
        {
            throw new BusinessRuleException($"Nama departemen '{normalizedName}' sudah digunakan.");
        }

        department.Code = normalizedCode;
        department.Name = normalizedName;
        department.IsActive = request.IsActive;

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

