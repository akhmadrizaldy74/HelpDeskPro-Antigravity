namespace HelpDeskPro.Application.Features.Master.Holidays.Commands.UpdateHoliday;

using HelpDeskPro.Application.Features.Master.Holidays.Models;
using MediatR;

public sealed record UpdateHolidayCommand(int Id, DateOnly Date, string Name) : IRequest<HolidayDto>;
