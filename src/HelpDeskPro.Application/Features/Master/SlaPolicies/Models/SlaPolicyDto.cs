namespace HelpDeskPro.Application.Features.Master.SlaPolicies.Models;

using HelpDeskPro.Domain.Enums;

public sealed record SlaPolicyDto(
    int Id,
    TicketPriority Priority,
    string PriorityName,
    int ResponseTimeMinutes,
    int ResolutionTimeMinutes
);

