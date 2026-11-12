namespace HelpDeskPro.Api.Controllers;

using HelpDeskPro.Application.Common.Models;
using HelpDeskPro.Application.Features.Dashboard.Models;
using HelpDeskPro.Application.Features.Dashboard.Queries.GetAgentWorkload;
using HelpDeskPro.Application.Features.Dashboard.Queries.GetDashboardSummary;
using HelpDeskPro.Application.Features.Dashboard.Queries.GetPendingEscalations;
using HelpDeskPro.Application.Features.Dashboard.Queries.GetTicketTrend;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Policy = "RequireSupervisorOrAbove")]
[Route("api/dashboard")]
public class DashboardController : ApiControllerBase
{
    [HttpGet("summary")]
    public async Task<ActionResult<ApiResponse<DashboardSummaryDto>>> GetSummary(
        [FromQuery] int periodDays = 30,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetDashboardSummaryQuery(periodDays), cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("agent-workload")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<AgentWorkloadDto>>>> GetAgentWorkload(
        [FromQuery] int? days = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetAgentWorkloadQuery(days), cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("pending-escalations")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<PendingEscalationDto>>>> GetPendingEscalations(
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetPendingEscalationsQuery(), cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("ticket-trend")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TicketTrendPointDto>>>> GetTicketTrend(
        [FromQuery] int days = 7,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetTicketTrendQuery(days), cancellationToken);
        return HandleResult(result);
    }
}
