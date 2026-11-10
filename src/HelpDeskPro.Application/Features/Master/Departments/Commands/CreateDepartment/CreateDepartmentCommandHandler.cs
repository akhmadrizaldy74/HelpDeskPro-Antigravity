namespace HelpDeskPro.Application.Features.Master.Departments.Commands.CreateDepartment;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Master.Departments.Models;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class CreateDepartmentCommandHandler : IRequestHandler<CreateDepartmentCommand, DepartmentDto>
{
    private readonly IApplicationDbContext _context;

    public CreateDepartmentCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<DepartmentDto> Handle(CreateDepartmentCommand request, CancellationToken cancellationToken)
    {
        var normalizedCode = request.Code.Trim().ToUpperInvariant();
        var normalizedName = request.Name.Trim();

        var codeExists = await _context.Departments
            .AnyAsync(d => d.Code == normalizedCode, cancellationToken);
        if (codeExists)
        {
            throw new BusinessRuleException($"Kode departemen '{normalizedCode}' sudah digunakan.");
        }

        var nameExists = await _context.Departments
            .AnyAsync(d => d.Name == normalizedName, cancellationToken);
        if (nameExists)
        {
            throw new BusinessRuleException($"Nama departemen '{normalizedName}' sudah digunakan.");
        }

        var department = new Department
        {
            Code = normalizedCode,
            Name = normalizedName,
            IsActive = true
        };

        _context.Departments.Add(department);
        await _context.SaveChangesAsync(cancellationToken);

        return new DepartmentDto(
            department.Id,
            department.Code,
            department.Name,
            department.IsActive,
            0,
            0);
    }
}

