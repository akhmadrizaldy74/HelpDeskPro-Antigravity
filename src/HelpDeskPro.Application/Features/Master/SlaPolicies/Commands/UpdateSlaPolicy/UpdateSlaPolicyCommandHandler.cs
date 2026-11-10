namespace HelpDeskPro.Application.Features.Master.SlaPolicies.Commands.UpdateSlaPolicy;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Master.SlaPolicies.Models;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class UpdateSlaPolicyCommandHandler : IRequestHandler<UpdateSlaPolicyCommand, SlaPolicyDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateSlaPolicyCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<SlaPolicyDto> Handle(UpdateSlaPolicyCommand request, CancellationToken cancellationToken)
    {
        var policy = await _context.SlaPolicies
            .FirstOrDefaultAsync(p => p.Priority == request.Priority, cancellationToken);

        if (policy is null)
        {
            throw new NotFoundException("SlaPolicy", request.Priority);
        }

        policy.ResponseTimeMinutes = request.ResponseTimeMinutes;
        policy.ResolutionTimeMinutes = request.ResolutionTimeMinutes;

        await _context.SaveChangesAsync(cancellationToken);

        return new SlaPolicyDto(
            policy.Id,
            policy.Priority,
            policy.Priority.ToString(),
            policy.ResponseTimeMinutes,
            policy.ResolutionTimeMinutes);
    }
}

