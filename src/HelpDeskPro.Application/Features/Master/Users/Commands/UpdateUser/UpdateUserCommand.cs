namespace HelpDeskPro.Application.Features.Master.Users.Commands.UpdateUser;

using HelpDeskPro.Application.Features.Master.Users.Models;
using MediatR;

public sealed record UpdateUserCommand(
    Guid Id,
    string FullName,
    int DepartmentId,
    string Role,
    Guid? SupervisorId,
    bool IsActive
) : IRequest<UserDetailDto>;

