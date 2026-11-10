namespace HelpDeskPro.Api.Controllers;

using HelpDeskPro.Application.Common.Models;
using HelpDeskPro.Application.Features.Master.Holidays.Commands.CreateHoliday;
using HelpDeskPro.Application.Features.Master.Holidays.Commands.DeleteHoliday;
using HelpDeskPro.Application.Features.Master.Holidays.Commands.UpdateHoliday;
using HelpDeskPro.Application.Features.Master.Holidays.Models;
using HelpDeskPro.Application.Features.Master.Holidays.Queries.GetHolidays;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize(Policy = "RequireAdmin")]
[Route("api/master/holidays")]
public class MasterHolidaysController : ApiControllerBase
{
    [HttpGet]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<HolidayDto>>>> GetAll(
        [FromQuery] int? year = null,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(new GetHolidaysQuery(year), cancellationToken);
        return HandleResult(result);
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<HolidayDto>>> Create(
        [FromBody] CreateHolidayCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(ApiResponse.Ok(result, TraceId));
    }

    [HttpPut("{id:int}")]
    public async Task<ActionResult<ApiResponse<HolidayDto>>> Update(
        int id,
        [FromBody] UpdateHolidayCommand command,
        CancellationToken cancellationToken = default)
    {
        if (id != command.Id)
        {
            return BadRequest(ApiResponse.Failure<HolidayDto>("ID pada rute tidak cocok dengan ID pada body.", TraceId));
        }

        var result = await Mediator.Send(command, cancellationToken);
        return HandleResult(result);
    }

    [HttpDelete("{id:int}")]
    public async Task<ActionResult<ApiResponse<string>>> Delete(
        int id,
        CancellationToken cancellationToken = default)
    {
        await Mediator.Send(new DeleteHolidayCommand(id), cancellationToken);
        return HandleResult("Hari libur berhasil dihapus.");
    }
}
