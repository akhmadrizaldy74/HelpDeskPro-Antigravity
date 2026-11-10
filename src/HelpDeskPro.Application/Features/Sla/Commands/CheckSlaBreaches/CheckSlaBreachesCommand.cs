namespace HelpDeskPro.Application.Features.Sla.Commands.CheckSlaBreaches;

using MediatR;

public record CheckSlaBreachesResult(
    int ResponseBreachesCount,
    int ResolutionBreachesCount,
    int AutoClosedCount
);

public record CheckSlaBreachesCommand : IRequest<CheckSlaBreachesResult>;

