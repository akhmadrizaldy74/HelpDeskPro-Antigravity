namespace HelpDeskPro.Api.Controllers;

using HelpDeskPro.Application.Common.Models;
using HelpDeskPro.Application.Features.Master.Categories.Commands.CreateCategory;
using HelpDeskPro.Application.Features.Master.Categories.Commands.DeleteCategory;
using HelpDeskPro.Application.Features.Master.Categories.Commands.ToggleCategoryStatus;
using HelpDeskPro.Application.Features.Master.Categories.Commands.UpdateCategory;
using HelpDeskPro.Application.Features.Master.Categories.Models;
using HelpDeskPro.Application.Features.Master.Categories.Queries.GetCategories;
using HelpDeskPro.Application.Features.Master.Categories.Queries.GetCategoryById;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Policy = "RequireAdmin")]
[Route("api/master/categories")]
public class MasterCategoriesController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<CategoryDto>>>> GetAll(
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetCategoriesQuery(isActive), cancellationToken);
        return HandleResult(result);
    }

    [HttpGet("{id:int}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> GetById(
        int id,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetCategoryByIdQuery(id), cancellationToken);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> Create(
        [FromBody] CreateCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, ApiResponse.Ok(result, TraceId));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> Update(
        int id,
        [FromBody] UpdateCategoryCommand command,
        CancellationToken cancellationToken = default)
    {
        if (id != command.Id)
        {
            return BadRequest(ApiResponse.Failure<CategoryDto>("ID pada rute tidak cocok dengan ID pada body.", TraceId));
        }

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpPatch("{id:int}/toggle-status")]
    public async Task<ActionResult<ApiResponse<CategoryDto>>> ToggleStatus(
        int id,
        [FromQuery] bool? isActive = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new ToggleCategoryStatusCommand(id, isActive), cancellationToken);
        return HandleResult(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(
        int id,
        CancellationToken cancellationToken = default)
    {
        await Mediator.Send(new DeleteCategoryCommand(id), cancellationToken);
        return HandleResult("Kategori berhasil dihapus.");
    }
}

