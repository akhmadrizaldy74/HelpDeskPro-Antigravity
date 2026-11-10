namespace HelpDeskPro.Application.Features.Tickets.Queries.GetTicketFilterOptions;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Tickets.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

public record GetTicketFilterOptionsQuery : IRequest<TicketFilterOptionsDto>;

public class GetTicketFilterOptionsQueryHandler : IRequestHandler<GetTicketFilterOptionsQuery, TicketFilterOptionsDto>
{
    private readonly IApplicationDbContext _context;

    public GetTicketFilterOptionsQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<TicketFilterOptionsDto> Handle(GetTicketFilterOptionsQuery request, CancellationToken cancellationToken)
    {
        var categories = await _context.Categories
            .AsNoTracking()
            .Where(c => c.IsActive)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryLookupDto(c.Id, c.Name))
            .ToListAsync(cancellationToken);

        var assignees = await _context.Users
            .AsNoTracking()
            .Where(u => u.IsActive && u.Department != null && u.Department.Code == "IT")
            .OrderBy(u => u.FullName)
            .Select(u => new AssigneeLookupDto(u.Id, u.FullName))
            .ToListAsync(cancellationToken);

        // Fallback: If no IT department users found, take any active users
        if (assignees.Count == 0)
        {
            assignees = await _context.Users
                .AsNoTracking()
                .Where(u => u.IsActive)
                .OrderBy(u => u.FullName)
                .Select(u => new AssigneeLookupDto(u.Id, u.FullName))
                .ToListAsync(cancellationToken);
        }

        return new TicketFilterOptionsDto
        {
            Categories = categories,
            Assignees = assignees
        };
    }
}

