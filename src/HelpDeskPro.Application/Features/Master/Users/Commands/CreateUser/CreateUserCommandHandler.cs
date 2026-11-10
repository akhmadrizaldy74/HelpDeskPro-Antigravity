namespace HelpDeskPro.Application.Features.Master.Users.Commands.CreateUser;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Master.Users.Models;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, UserDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public CreateUserCommandHandler(
        IApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<UserDetailDto> Handle(CreateUserCommand request, CancellationToken cancellationToken)
    {
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var normalizedEmployeeNo = request.EmployeeNo.Trim().ToUpperInvariant();

        var nikExists = await _context.Users
            .AnyAsync(u => u.EmployeeNo == normalizedEmployeeNo, cancellationToken);
        if (nikExists)
        {
            throw new BusinessRuleException($"Nomor karyawan (NIK) '{request.EmployeeNo}' sudah digunakan.");
        }

        var emailExists = await _context.Users
            .AnyAsync(u => u.NormalizedEmail == normalizedEmail, cancellationToken);
        if (emailExists)
        {
            throw new BusinessRuleException($"Email '{request.Email}' sudah digunakan.");
        }

        var department = await _context.Departments
            .FirstOrDefaultAsync(d => d.Id == request.DepartmentId, cancellationToken);
        if (department is null)
        {
            throw new NotFoundException("Departemen", request.DepartmentId);
        }

        ApplicationUser? supervisor = null;
        if (request.SupervisorId.HasValue)
        {
            supervisor = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.SupervisorId.Value, cancellationToken);
            if (supervisor is null)
            {
                throw new NotFoundException("Supervisor", request.SupervisorId.Value);
            }
        }

        var user = new ApplicationUser
        {
            UserName = request.Email.Trim(),
            Email = request.Email.Trim(),
            EmployeeNo = normalizedEmployeeNo,
            FullName = request.FullName.Trim(),
            DepartmentId = department.Id,
            SupervisorId = request.SupervisorId,
            IsActive = true,
            EmailConfirmed = true
        };

        var result = await _userManager.CreateAsync(user, request.Password);
        if (!result.Succeeded)
        {
            var firstError = result.Errors.FirstOrDefault()?.Description ?? "Gagal membuat pengguna.";
            throw new BusinessRuleException(firstError);
        }

        await _userManager.AddToRoleAsync(user, request.Role);

        return new UserDetailDto(
            user.Id,
            user.EmployeeNo,
            user.FullName,
            user.Email,
            user.DepartmentId,
            department.Name,
            request.Role,
            user.SupervisorId,
            supervisor?.FullName,
            user.IsActive);
    }
}
