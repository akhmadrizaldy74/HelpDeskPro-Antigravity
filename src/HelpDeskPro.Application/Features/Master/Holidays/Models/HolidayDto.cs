namespace HelpDeskPro.Application.Features.Master.Holidays.Models;

public sealed record HolidayDto(
    int Id,
    DateOnly Date,
    string Name
);
