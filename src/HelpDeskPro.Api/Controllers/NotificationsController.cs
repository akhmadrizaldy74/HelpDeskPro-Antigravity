namespace HelpDeskPro.Api.Controllers;

using HelpDeskPro.Application.Common.Models;
using HelpDeskPro.Application.Features.Notifications.Commands.MarkNotificationsAsRead;
using HelpDeskPro.Application.Features.Notifications.Models;
using HelpDeskPro.Application.Features.Notifications.Queries.GetNotifications;
using HelpDeskPro.Application.Features.Notifications.Queries.GetUnreadNotificationCount;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

[Authorize]
public class NotificationsController : ApiControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(ApiResponse<IReadOnlyList<NotificationDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<NotificationDto>>>> GetList(
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetNotificationsQuery(), cancellationToken);
        return Ok(ApiResponse.Ok(result, TraceId));
    }

    [HttpGet("unread-count")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<int>>> GetUnreadCount(
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new GetUnreadNotificationCountQuery(), cancellationToken);
        return Ok(ApiResponse.Ok(result, TraceId));
    }

    [HttpPatch("read-all")]
    [ProducesResponseType(typeof(ApiResponse<int>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status401Unauthorized)]
    public async Task<ActionResult<ApiResponse<int>>> MarkAllAsRead(
        CancellationToken cancellationToken)
    {
        var result = await Mediator.Send(new MarkNotificationsAsReadCommand(), cancellationToken);
        return Ok(ApiResponse.Ok(result, TraceId));
    }
}

