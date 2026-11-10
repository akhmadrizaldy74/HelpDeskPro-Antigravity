namespace HelpDeskPro.Api.Controllers;

using HelpDeskPro.Application.Common.Models;
using HelpDeskPro.Application.Features.Master.Users.Commands.CreateUser;
using HelpDeskPro.Application.Features.Master.Users.Commands.DeleteUser;
using HelpDeskPro.Application.Features.Master.Users.Commands.ResetUserPassword;
using HelpDeskPro.Application.Features.Master.Users.Commands.ToggleUserStatus;
using HelpDeskPro.Application.Features.Master.Users.Commands.UpdateUser;
using HelpDeskPro.Application.Features.Master.Users.Models;
using HelpDeskPro.Application.Features.Master.Users.Queries.GetUserById;
using HelpDeskPro.Application.Features.Master.Users.Queries.GetUsers;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Policy = "RequireAdmin")]
[Route("api/master/users")]
public class MasterUsersController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<PagedResult<UserListItemDto>>>> GetUsers(
        [FromQuery] int? departmentId = null,
        [FromQuery] string? role = null,
        [FromQuery] bool? isActive = null,
        [FromQuery] string? search = null,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(
            new GetUsersQuery(departmentId, role, isActive, search, page, pageSize),
            cancellationToken);

        return HandleResult(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDetailDto>>> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetUserByIdQuery(id), cancellationToken);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<UserDetailDto>>> Create(
        [FromBody] CreateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse.Ok(result, TraceId));
    }

    [HttpPut("{id:guid}")]
    public async Task<ActionResult<ApiResponse<UserDetailDto>>> Update(
        Guid id,
        [FromBody] UpdateUserCommand command,
        CancellationToken cancellationToken = default)
    {
        if (id != command.Id)
        {
            return BadRequest(ApiResponse.Failure<UserDetailDto>("ID pada rute tidak cocok dengan ID pada body.", TraceId));
        }

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPatch("{id:guid}/toggle-status")]
    public async Task<ActionResult<ApiResponse<UserDetailDto>>> ToggleStatus(
        Guid id,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new ToggleUserStatusCommand(id, isActive), cancellationToken);
        return HandleResult(result);
    }

    [HttpPost("{id:guid}/reset-password")]
    public async Task<ActionResult<ApiResponse<string>>> ResetPassword(
        Guid id,
        [FromBody] ResetPasswordRequest request,
        CancellationToken cancellationToken = default)
    {
        await Mediator.Send(new ResetUserPasswordCommand(id, request.NewPassword), cancellationToken);
        return HandleResult("Password pengguna berhasil direset.");
    }

    [HttpDelete("{id:guid}")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await Mediator.Send(new DeleteUserCommand(id), cancellationToken);
        return HandleResult("Pengguna berhasil dihapus.");
    }
}

public sealed record ResetPasswordRequest(string NewPassword);

