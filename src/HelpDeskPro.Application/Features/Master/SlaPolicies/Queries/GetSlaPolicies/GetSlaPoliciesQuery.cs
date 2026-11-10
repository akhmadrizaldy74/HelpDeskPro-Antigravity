namespace HelpDeskPro.Application.Features.Master.SlaPolicies.Queries.GetSlaPolicies;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Master.SlaPolicies.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

public sealed record GetSlaPoliciesQuery : IRequest<IReadOnlyList<SlaPolicyDto>>;

public class GetSlaPoliciesQueryHandler : IRequestHandler<GetSlaPoliciesQuery, IReadOnlyList<SlaPolicyDto>>
{
    private readonly IApplicationDbContext _context;

    public GetSlaPoliciesQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<SlaPolicyDto>> Handle(GetSlaPoliciesQuery request, CancellationToken cancellationToken)
    {
        var policies = await _context.SlaPolicies
            .AsNoTracking()
            .OrderBy(p => p.Priority)
            .Select(p => new SlaPolicyDto(
                p.Id,
                p.Priority,
                p.Priority.ToString(),
                p.ResponseTimeMinutes,
                p.ResolutionTimeMinutes
            ))
            .ToListAsync(cancellationToken);

        return policies;
    }
}

