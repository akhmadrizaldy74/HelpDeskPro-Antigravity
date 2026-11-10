namespace HelpDeskPro.Application.Features.Escalations.Commands.AcknowledgeEscalation;

using HelpDeskPro.Application.Features.Escalations.Models;
using MediatR;

public record AcknowledgeEscalationCommand(
    long EscalationId
) : IRequest<EscalationDto>;

