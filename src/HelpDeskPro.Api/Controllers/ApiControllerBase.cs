namespace HelpDeskPro.Api.Controllers;

using System.Diagnostics;
using HelpDeskPro.Application.Common.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public abstract class ApiControllerBase : ControllerBase
{
    private ISender? _mediator;

    protected ISender Mediator => _mediator ??= HttpContext.RequestServices.GetRequiredService<ISender>();

    protected string TraceId => Activity.Current?.Id ?? HttpContext.TraceIdentifier;

    protected ActionResult<ApiResponse<T>> HandleResult<T>(T data)
    {
        return Ok(ApiResponse.Ok(data, TraceId));
    }
}

