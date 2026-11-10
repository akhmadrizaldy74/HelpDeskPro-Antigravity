namespace HelpDeskPro.Application.Features.Master.Users.Commands.UpdateUser;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Master.Users.Models;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public class UpdateUserCommandHandler : IRequestHandler<UpdateUserCommand, UserDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public UpdateUserCommandHandler(
        IApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<UserDetailDto> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.Department)
            .Include(u => u.Supervisor)
            .FirstOrDefaultAsync(u => u.Id == request.Id, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("Pengguna", request.Id);
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
            if (request.SupervisorId.Value == user.Id)
            {
                throw new BusinessRuleException("Pengguna tidak dapat menjadi supervisor bagi dirinya sendiri.");
            }

            supervisor = await _context.Users
                .FirstOrDefaultAsync(u => u.Id == request.SupervisorId.Value, cancellationToken);
            if (supervisor is null)
            {
                throw new NotFoundException("Supervisor", request.SupervisorId.Value);
            }
        }

        user.FullName = request.FullName.Trim();
        user.DepartmentId = department.Id;
        user.SupervisorId = request.SupervisorId;
        user.IsActive = request.IsActive;

        if (!user.IsActive)
        {
            user.LockoutEnd = DateTimeOffset.MaxValue;
        }
        else if (user.LockoutEnd == DateTimeOffset.MaxValue)
        {
            user.LockoutEnd = null;
        }

        var currentRoles = await _userManager.GetRolesAsync(user);
        if (!currentRoles.Contains(request.Role))
        {
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, request.Role);
        }

        await _context.SaveChangesAsync(cancellationToken);

        return new UserDetailDto(
            user.Id,
            user.EmployeeNo,
            user.FullName,
            user.Email ?? string.Empty,
            user.DepartmentId,
            department.Name,
            request.Role,
            user.SupervisorId,
            supervisor?.FullName,
            user.IsActive);
    }
}

