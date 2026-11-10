namespace HelpDeskPro.Application.Features.Master.Users.Commands.ToggleUserStatus;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Master.Users.Models;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

public sealed record ToggleUserStatusCommand(Guid UserId, bool? IsActive = null) : IRequest<UserDetailDto>;

public class ToggleUserStatusCommandHandler : IRequestHandler<ToggleUserStatusCommand, UserDetailDto>
{
    private readonly IApplicationDbContext _context;
    private readonly UserManager<ApplicationUser> _userManager;

    public ToggleUserStatusCommandHandler(
        IApplicationDbContext context,
        UserManager<ApplicationUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<UserDetailDto> Handle(ToggleUserStatusCommand request, CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Include(u => u.Department)
            .Include(u => u.Supervisor)
            .FirstOrDefaultAsync(u => u.Id == request.UserId, cancellationToken);

        if (user is null)
        {
            throw new NotFoundException("Pengguna", request.UserId);
        }

        user.IsActive = request.IsActive ?? !user.IsActive;

        if (!user.IsActive)
        {
            user.LockoutEnd = DateTimeOffset.MaxValue;
        }
        else if (user.LockoutEnd == DateTimeOffset.MaxValue)
        {
            user.LockoutEnd = null;
        }

        await _context.SaveChangesAsync(cancellationToken);

        var roles = await _userManager.GetRolesAsync(user);
        var roleName = roles.FirstOrDefault() ?? "Requester";

        return new UserDetailDto(
            user.Id,
            user.EmployeeNo,
            user.FullName,
            user.Email ?? string.Empty,
            user.DepartmentId,
            user.Department?.Name ?? string.Empty,
            roleName,
            user.SupervisorId,
            user.Supervisor?.FullName,
            user.IsActive);
    }
}

