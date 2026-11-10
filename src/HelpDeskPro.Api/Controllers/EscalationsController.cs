namespace HelpDeskPro.Api.Controllers;

using HelpDeskPro.Application.Common.Models;
using HelpDeskPro.Application.Features.Escalations.Commands.AcknowledgeEscalation;
using HelpDeskPro.Application.Features.Escalations.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class EscalationsController : ApiControllerBase
{
    [HttpPatch("{id:long}/acknowledge")]
    [ProducesResponseType(typeof(ApiResponse<EscalationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiResponse<EscalationDto>>> Acknowledge(
        long id,
        CancellationToken cancellationToken)
    {
        var command = new AcknowledgeEscalationCommand(id);
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(ApiResponse.Ok(result, TraceId));
    }
}

