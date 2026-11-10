namespace HelpDeskPro.Application.Features.Master.Departments.Models;

public sealed record DepartmentDto(
    int Id,
    string Code,
    string Name,
    bool IsActive,
    int UserCount,
    int TicketCount
);

