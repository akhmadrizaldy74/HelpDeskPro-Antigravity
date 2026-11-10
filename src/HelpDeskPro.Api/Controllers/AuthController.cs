namespace HelpDeskPro.Api.Controllers;

using HelpDeskPro.Application.Common.Models;
using HelpDeskPro.Application.Features.Auth.Commands.Login;
using HelpDeskPro.Application.Features.Auth.Models;
using HelpDeskPro.Application.Features.Auth.Queries.GetCurrentUser;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

public class AuthController : ApiControllerBase
{
    [HttpPost("login")]
    [AllowAnonymous]
    [ProducesResponseType(typeof(ApiResponse<AuthResponseDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiResponse<AuthResponseDto>>> Login(
        [FromBody] LoginCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("me")]
    [Authorize]
    [ProducesResponseType(typeof(ApiResponse<UserDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<UserDto>>> GetMe(CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetCurrentUserQuery(), cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("test-agent")]
    [Authorize(Policy = "RequireAgentOrAbove")]
    public ActionResult<ApiResponse<string>> TestAgentPolicy()
    {
        return HandleResult("Akses Agent berhasil.");
    }

    [HttpGet("test-supervisor")]
    [Authorize(Policy = "RequireSupervisorOrAbove")]
    public ActionResult<ApiResponse<string>> TestSupervisorPolicy()
    {
        return HandleResult("Akses Supervisor berhasil.");
    }

    [HttpGet("test-admin")]
    [Authorize(Policy = "RequireAdmin")]
    public ActionResult<ApiResponse<string>> TestAdminPolicy()
    {
        return HandleResult("Akses Admin berhasil.");
    }
}

