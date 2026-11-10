namespace HelpDeskPro.Application.Features.Master.Users.Commands.CreateUser;

using HelpDeskPro.Application.Features.Master.Users.Models;
using MediatR;

public sealed record CreateUserCommand(
    string EmployeeNo,
    string FullName,
    string Email,
    string Password,
    int DepartmentId,
    string Role,
    Guid? SupervisorId = null
) : IRequest<UserDetailDto>;

