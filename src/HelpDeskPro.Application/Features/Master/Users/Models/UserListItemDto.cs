namespace HelpDeskPro.Application.Features.Master.Users.Models;

public sealed record UserListItemDto(
    Guid Id,
    string EmployeeNo,
    string FullName,
    string Email,
    int DepartmentId,
    string DepartmentName,
    string Role,
    Guid? SupervisorId,
    string? SupervisorName,
    bool IsActive
);

