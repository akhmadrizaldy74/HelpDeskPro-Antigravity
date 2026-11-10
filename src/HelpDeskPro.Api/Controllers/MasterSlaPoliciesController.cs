namespace HelpDeskPro.Api.Controllers;

using HelpDeskPro.Application.Common.Models;
using HelpDeskPro.Application.Features.Master.SlaPolicies.Commands.UpdateSlaPolicy;
using HelpDeskPro.Application.Features.Master.SlaPolicies.Models;
using HelpDeskPro.Application.Features.Master.SlaPolicies.Queries.GetSlaPolicies;
using HelpDeskPro.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Policy = "RequireAdmin")]
[Route("api/master/sla-policies")]
public class MasterSlaPoliciesController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<SlaPolicyDto>>>> GetAll(
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetSlaPoliciesQuery(), cancellationToken);
        return HandleResult(result);
    }

    [HttpPut("{priority}")]
    public async Task<ActionResult<ApiResponse<SlaPolicyDto>>> Update(
        TicketPriority priority,
        [FromBody] UpdateSlaPolicyRequest request,
        CancellationToken cancellationToken = default)
    {
        var command = new UpdateSlaPolicyCommand(priority, request.ResponseTimeMinutes, request.ResolutionTimeMinutes);
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }
}

public sealed record UpdateSlaPolicyRequest(int ResponseTimeMinutes, int ResolutionTimeMinutes);

