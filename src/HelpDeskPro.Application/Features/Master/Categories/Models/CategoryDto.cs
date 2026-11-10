namespace HelpDeskPro.Application.Features.Master.Categories.Models;

public sealed record CategoryDto(
    int Id,
    string Name,
    bool IsActive,
    int TicketCount
);

