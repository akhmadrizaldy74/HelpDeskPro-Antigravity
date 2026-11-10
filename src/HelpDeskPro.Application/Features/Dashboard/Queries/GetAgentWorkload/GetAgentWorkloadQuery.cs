namespace HelpDeskPro.Application.Features.Dashboard.Queries.GetAgentWorkload;

using HelpDeskPro.Application.Features.Dashboard.Models;
using MediatR;

public sealed record GetAgentWorkloadQuery : IRequest<IReadOnlyList<AgentWorkloadDto>>;

