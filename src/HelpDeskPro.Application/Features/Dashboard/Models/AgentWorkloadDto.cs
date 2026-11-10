namespace HelpDeskPro.Application.Features.Dashboard.Models;

public sealed record AgentWorkloadDto(
    Guid AgentId,
    string FullName,
    string Email,
    int ActiveTicketCount,
    int SlaBreachedCount
);

