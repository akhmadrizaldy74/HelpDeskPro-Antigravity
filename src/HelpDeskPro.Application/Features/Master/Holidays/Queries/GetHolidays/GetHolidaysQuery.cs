namespace HelpDeskPro.Application.Features.Master.Holidays.Queries.GetHolidays;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Master.Holidays.Models;
using MediatR;
using Microsoft.EntityFrameworkCore;

public sealed record GetHolidaysQuery(int? Year = null) : IRequest<IReadOnlyList<HolidayDto>>;

public class GetHolidaysQueryHandler : IRequestHandler<GetHolidaysQuery, IReadOnlyList<HolidayDto>>
{
    private readonly IApplicationDbContext _context;

    public GetHolidaysQueryHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyList<HolidayDto>> Handle(GetHolidaysQuery request, CancellationToken cancellationToken)
    {
        var query = _context.Holidays.AsNoTracking();

        if (request.Year.HasValue)
        {
            var year = request.Year.Value;
            var start = new DateOnly(year, 1, 1);
            var end = new DateOnly(year, 12, 31);
            query = query.Where(h => h.Date >= start && h.Date <= end);
        }

        var list = await query
            .OrderBy(h => h.Date)
            .Select(h => new HolidayDto(h.Id, h.Date, h.Name))
            .ToListAsync(cancellationToken);

        return list;
    }
}
