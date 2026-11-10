using HelpDeskPro.Application.Common.Models;
using HelpDeskPro.Application.Features.Escalations.Models;
using HelpDeskPro.Application.Features.Tickets.Commands.AddComment;
using HelpDeskPro.Application.Features.Tickets.Commands.AssignTicket;
using HelpDeskPro.Application.Features.Tickets.Commands.ChangeTicketStatus;
using HelpDeskPro.Application.Features.Tickets.Commands.CreateTicket;
using HelpDeskPro.Application.Features.Tickets.Commands.EscalateTicket;
using HelpDeskPro.Application.Features.Tickets.Commands.RateTicket;
using HelpDeskPro.Application.Features.Tickets.Commands.UpdateTicket;
using HelpDeskPro.Application.Features.Tickets.Commands.UploadTicketAttachment;
using HelpDeskPro.Application.Features.Tickets.Models;
using HelpDeskPro.Application.Features.Tickets.Queries.DownloadTicketAttachment;
using HelpDeskPro.Application.Features.Tickets.Queries.GetTicketById;
using HelpDeskPro.Application.Features.Tickets.Queries.GetTicketFilterOptions;
using HelpDeskPro.Application.Features.Tickets.Queries.GetTicketHistory;
using HelpDeskPro.Application.Features.Tickets.Queries.GetTickets;
using HelpDeskPro.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskPro.Api.Controllers;

public record AssignTicketRequest(Guid AssigneeId, byte[]? RowVersion);
public record ChangeTicketStatusRequest(TicketStatus Status, string? Reason, byte[]? RowVersion);
public record AddCommentRequest(string Body, bool IsInternal);
public record UpdateTicketRequest(string Title, string? Description, int CategoryId, TicketPriority Priority, byte[]? RowVersion);
public record EscalateTicketRequest(string Note);
public record RateTicketRequest(int Rating, string? Comment);

[Authorize]
public class TicketsController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<PagedResult<TicketListItemDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<PagedResult<TicketListItemDto>>>> GetList(
        [FromQuery] GetTicketsQuery query,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(query, cancellationToken);
        return Ok(ApiResponse.Ok(result, TraceId));
    }

    [HttpGet("filter-options")]
    [ProducesResponseType(typeof(ApiResponse<TicketFilterOptionsDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<TicketFilterOptionsDto>>> GetFilterOptions(
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetTicketFilterOptionsQuery(), cancellationToken);
        return Ok(ApiResponse.Ok(result, TraceId));
    }

    [HttpGet("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<TicketDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TicketDetailDto>>> GetById(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetTicketByIdQuery(id), cancellationToken);
        return Ok(ApiResponse.Ok(result, TraceId));
    }

    [HttpPost]
    [ProducesResponseType(typeof(ApiResponse<TicketDetailDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<TicketDetailDto>>> Create(
        [FromBody] CreateTicketCommand command,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(command, cancellationToken);
        return CreatedAtAction(
            nameof(GetById),
            new { id = result.Id },
            ApiResponse.Ok(result, TraceId));
    }

    [HttpPatch("{id:long}/assign")]
    [ProducesResponseType(typeof(ApiResponse<TicketDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiResponse<TicketDetailDto>>> Assign(
        long id,
        [FromBody] AssignTicketRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AssignTicketCommand(id, request.AssigneeId, request.RowVersion ?? Array.Empty<byte>());
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(ApiResponse.Ok(result, TraceId));
    }

    [HttpPatch("{id:long}/status")]
    [ProducesResponseType(typeof(ApiResponse<TicketDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiResponse<TicketDetailDto>>> ChangeStatus(
        long id,
        [FromBody] ChangeTicketStatusRequest request,
        CancellationToken cancellationToken)
    {
        var command = new ChangeTicketStatusCommand(id, request.Status, request.Reason, request.RowVersion ?? Array.Empty<byte>());
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(ApiResponse.Ok(result, TraceId));
    }

    [HttpPost("{id:long}/comments")]
    [ProducesResponseType(typeof(ApiResponse<TicketCommentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiResponse<TicketCommentDto>>> AddComment(
        long id,
        [FromBody] AddCommentRequest request,
        CancellationToken cancellationToken)
    {
        var command = new AddCommentCommand(id, request.Body, request.IsInternal);
        var result = await Mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(result, TraceId));
    }

    [HttpPost("{id:long}/attachments")]
    [Consumes("multipart/form-data")]
    [ProducesResponseType(typeof(ApiResponse<TicketAttachmentDto>), StatusCodes.Status201Created)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiResponse<TicketAttachmentDto>>> UploadAttachment(
        long id,
        IFormFile file,
        CancellationToken cancellationToken)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(ApiResponse.Failure<TicketAttachmentDto>("File tidak boleh kosong.", TraceId));
        }

        using var stream = file.OpenReadStream();
        var command = new UploadTicketAttachmentCommand(
            id,
            stream,
            file.FileName,
            file.ContentType,
            file.Length);

        var result = await Mediator.Send(command, cancellationToken);
        return StatusCode(StatusCodes.Status201Created, ApiResponse.Ok(result, TraceId));
    }

    [HttpGet("{id:long}/attachments/{attachmentId:long}")]
    [ProducesResponseType(typeof(FileStreamResult), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DownloadAttachment(
        long id,
        long attachmentId,
        CancellationToken cancellationToken)
    {
        var query = new DownloadTicketAttachmentQuery(id, attachmentId);
        var result = await Mediator.Send(query, cancellationToken);
        return File(result.Stream, result.ContentType, result.FileName);
    }

    [HttpPut("{id:long}")]
    [ProducesResponseType(typeof(ApiResponse<TicketDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status409Conflict)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiResponse<TicketDetailDto>>> Update(
        long id,
        [FromBody] UpdateTicketRequest request,
        CancellationToken cancellationToken)
    {
        var command = new UpdateTicketCommand(
            id,
            request.Title,
            request.Description,
            request.CategoryId,
            request.Priority,
            request.RowVersion ?? Array.Empty<byte>());

        var result = await Mediator.Send(command, cancellationToken);
        return Ok(ApiResponse.Ok(result, TraceId));
    }

    [HttpGet("{id:long}/history")]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<TicketHistoryDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<TicketHistoryDto>>>> GetHistory(
        long id,
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetTicketHistoryQuery(id), cancellationToken);
        return Ok(ApiResponse.Ok(result, TraceId));
    }

    [HttpPost("{id:long}/escalate")]
    [ProducesResponseType(typeof(ApiResponse<EscalationDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiResponse<EscalationDto>>> Escalate(
        long id,
        [FromBody] EscalateTicketRequest request,
        CancellationToken cancellationToken)
    {
        var command = new EscalateTicketCommand(id, request.Note);
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(ApiResponse.Ok(result, TraceId));
    }

    [HttpPost("{id:long}/rate")]
    [Authorize(Roles = "Requester,Admin")]
    [ProducesResponseType(typeof(ApiResponse<TicketDetailDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status403Forbidden)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status422UnprocessableEntity)]
    public async Task<ActionResult<ApiResponse<TicketDetailDto>>> Rate(
        long id,
        [FromBody] RateTicketRequest request,
        CancellationToken cancellationToken)
    {
        var command = new RateTicketCommand(id, request.Rating, request.Comment);
        var result = await Mediator.Send(command, cancellationToken);
        return Ok(ApiResponse.Ok(result, TraceId));
    }
}
