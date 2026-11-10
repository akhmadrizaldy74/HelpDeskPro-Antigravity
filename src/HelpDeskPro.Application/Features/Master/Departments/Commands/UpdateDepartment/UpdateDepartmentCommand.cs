namespace HelpDeskPro.Application.Features.Master.Departments.Commands.UpdateDepartment;

using HelpDeskPro.Application.Features.Master.Departments.Models;
using MediatR;

public sealed record UpdateDepartmentCommand(int Id, string Code, string Name, bool IsActive) : IRequest<DepartmentDto>;

