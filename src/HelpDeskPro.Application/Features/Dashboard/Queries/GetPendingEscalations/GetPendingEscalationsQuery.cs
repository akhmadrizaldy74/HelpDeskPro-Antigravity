namespace HelpDeskPro.Application.Features.Dashboard.Queries.GetPendingEscalations;

using HelpDeskPro.Application.Features.Dashboard.Models;
using MediatR;

public sealed record GetPendingEscalationsQuery : IRequest<IReadOnlyList<PendingEscalationDto>>;

