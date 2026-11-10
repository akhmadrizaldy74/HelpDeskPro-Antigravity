namespace HelpDeskPro.Application.Features.Master.Departments.Commands.CreateDepartment;

using HelpDeskPro.Application.Features.Master.Departments.Models;
using MediatR;

public sealed record CreateDepartmentCommand(string Code, string Name) : IRequest<DepartmentDto>;

