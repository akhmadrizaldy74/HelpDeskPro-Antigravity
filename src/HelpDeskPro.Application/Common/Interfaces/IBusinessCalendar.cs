namespace HelpDeskPro.Application.Common.Interfaces;

public interface IBusinessCalendar
{
    DateTime ToWib(DateTime utcDateTime);
    DateTime ToUtc(DateTime wibDateTime);
    bool IsWorkingDay(DateOnly targetDate, IReadOnlySet<DateOnly>? holidays = null);
    bool IsWorkingDay(DateTime wibDateTime, IReadOnlySet<DateOnly>? holidays = null);
    DateTime AddWorkingMinutes(DateTime startUtc, int workingMinutes, IReadOnlySet<DateOnly>? holidays = null);
    int GetWorkingMinutesBetween(DateTime startUtc, DateTime endUtc, IReadOnlySet<DateOnly>? holidays = null);
    Task<IReadOnlySet<DateOnly>> GetHolidaysAsync(int yearFrom, int yearTo, CancellationToken cancellationToken = default);
}
