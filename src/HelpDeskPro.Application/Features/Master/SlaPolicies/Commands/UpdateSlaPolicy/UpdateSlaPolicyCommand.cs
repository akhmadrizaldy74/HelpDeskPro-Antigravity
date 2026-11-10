namespace HelpDeskPro.Application.Features.Master.SlaPolicies.Commands.UpdateSlaPolicy;

using HelpDeskPro.Application.Features.Master.SlaPolicies.Models;
using HelpDeskPro.Domain.Enums;
using MediatR;

public sealed record UpdateSlaPolicyCommand(
    TicketPriority Priority,
    int ResponseTimeMinutes,
    int ResolutionTimeMinutes
) : IRequest<SlaPolicyDto>;

