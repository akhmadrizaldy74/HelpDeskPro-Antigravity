namespace HelpDeskPro.Application.Features.Master.Holidays.Commands.UpdateHoliday;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Master.Holidays.Models;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class UpdateHolidayCommandHandler : IRequestHandler<UpdateHolidayCommand, HolidayDto>
{
    private readonly IApplicationDbContext _context;

    public UpdateHolidayCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HolidayDto> Handle(UpdateHolidayCommand request, CancellationToken cancellationToken)
    {
        var holiday = await _context.Holidays
            .FirstOrDefaultAsync(h => h.Id == request.Id, cancellationToken);

        if (holiday is null)
        {
            throw new NotFoundException("Hari Libur", request.Id);
        }

        var normalizedName = request.Name.Trim();

        var dateConflict = await _context.Holidays
            .AnyAsync(h => h.Id != request.Id && h.Date == request.Date, cancellationToken);
        if (dateConflict)
        {
            throw new BusinessRuleException($"Hari libur pada tanggal {request.Date:yyyy-MM-dd} sudah terdaftar.");
        }

        holiday.Date = request.Date;
        holiday.Name = normalizedName;

        await _context.SaveChangesAsync(cancellationToken);

        return new HolidayDto(holiday.Id, holiday.Date, holiday.Name);
    }
}
