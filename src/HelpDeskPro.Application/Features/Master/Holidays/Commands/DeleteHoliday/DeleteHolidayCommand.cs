namespace HelpDeskPro.Application.Features.Master.Holidays.Commands.DeleteHoliday;

using HelpDeskPro.Application.Common.Interfaces;
using HelpDeskPro.Domain.Exceptions;
using MediatR;
using Microsoft.EntityFrameworkCore;

public sealed record DeleteHolidayCommand(int Id) : IRequest;

public class DeleteHolidayCommandHandler : IRequestHandler<DeleteHolidayCommand>
{
    private readonly IApplicationDbContext _context;

    public DeleteHolidayCommandHandler(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task Handle(DeleteHolidayCommand request, CancellationToken cancellationToken)
    {
        var holiday = await _context.Holidays
            .FirstOrDefaultAsync(h => h.Id == request.Id, cancellationToken);

        if (holiday is null)
        {
            throw new NotFoundException("Hari Libur", request.Id);
        }

        _context.Holidays.Remove(holiday);
        await _context.SaveChangesAsync(cancellationToken);
    }
}
