namespace HelpDeskPro.Application.Features.Master.Holidays.Commands.CreateHoliday;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Application.Features.Master.Holidays.Models;
using HelpDeskPro.Domain.Entities;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

public class CreateHolidayCommandHandler : IRequestHandler<CreateHolidayCommand, HolidayDto>
{
    private readonly IApplicationDbContext _context;

    public CreateHolidayCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<HolidayDto> Handle(CreateHolidayCommand request, CancellationToken cancellationToken)
    {
        var normalizedName = request.Name.Trim();

        var exists = await _context.Holidays
            .AnyAsync(h => h.Date == request.Date, cancellationToken);
        if (exists)
        {
            throw new BusinessRuleException($"Hari libur pada tanggal {request.Date:yyyy-MM-dd} sudah terdaftar.");
        }

        var holiday = new Holiday
        {
            Date = request.Date,
            Name = normalizedName
        };

        _context.Holidays.Add(holiday);
        await _context.SaveChangesAsync(cancellationToken);

        return new HolidayDto(holiday.Id, holiday.Date, holiday.Name);
    }
}
