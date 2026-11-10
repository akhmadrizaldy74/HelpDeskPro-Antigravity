namespace HelpDeskPro.Api.Controllers;

using HelpDeskPro.Application.Common.Models;
using HelpDeskPro.Application.Features.Master.Departments.Commands.CreateDepartment;
using HelpDeskPro.Application.Features.Master.Departments.Commands.DeleteDepartment;
using HelpDeskPro.Application.Features.Master.Departments.Commands.ToggleDepartmentStatus;
using HelpDeskPro.Application.Features.Master.Departments.Commands.UpdateDepartment;
using HelpDeskPro.Application.Features.Master.Departments.Models;
using HelpDeskPro.Application.Features.Master.Departments.Queries.GetDepartmentById;
using HelpDeskPro.Application.Features.Master.Departments.Queries.GetDepartments;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Policy = "RequireAdmin")]
[Route("api/master/departments")]
public class MasterDepartmentsController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<DepartmentDto>>>> GetAll(
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetDepartmentsQuery(isActive), cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> GetById(
        int id,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetDepartmentByIdQuery(id), cancellationToken);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> Create(
        [FromBody] CreateDepartmentCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse.Ok(result, TraceId));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> Update(
        int id,
        [FromBody] UpdateDepartmentCommand command,
        CancellationToken cancellationToken = default)
    {
        if (id != command.Id)
        {
            return BadRequest(ApiResponse.Failure<DepartmentDto>("ID pada rute tidak cocok dengan ID pada body.", TraceId));
        }

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPatch("{id:int}/toggle-status")]
    public async Task<ActionResult<ApiResponse<DepartmentDto>>> ToggleStatus(
        int id,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new ToggleDepartmentStatusCommand(id, isActive), cancellationToken);
        return HandleResult(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(
        int id,
        CancellationToken cancellationToken = default)
    {
        await Mediator.Send(new DeleteDepartmentCommand(id), cancellationToken);
        return HandleResult("Departemen berhasil dihapus.");
    }
}

