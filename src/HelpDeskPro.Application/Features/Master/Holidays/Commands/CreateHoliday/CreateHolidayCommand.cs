namespace HelpDeskPro.Application.Features.Master.Holidays.Commands.CreateHoliday;

using HelpDeskPro.Application.Features.Master.Holidays.Models;
using MediatR;

public sealed record CreateHolidayCommand(DateOnly Date, string Name) : IRequest<HolidayDto>;
