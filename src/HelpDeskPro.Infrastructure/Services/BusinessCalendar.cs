namespace HelpDeskPro.Infrastructure.Services;

using HelpDeskPro.Application.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

public class BusinessCalendar : IBusinessCalendar
{
    private static readonly TimeSpan WorkStartTime = new(8, 0, 0);  // 08:00 WIB
    private static readonly TimeSpan WorkEndTime = new(17, 0, 0);   // 17:00 WIB
    private static readonly TimeSpan WibOffset = TimeSpan.FromHours(7);

    private readonly IApplicationDbContext _context;

    public BusinessCalendar(IApplicationDbContext context)
    {
        _context = context;
    }

    public DateTime ToWib(DateTime utcDateTime)
    {
        var utc = utcDateTime.Kind == DateTimeKind.Utc
            ? utcDateTime
            : DateTime.SpecifyKind(utcDateTime, DateTimeKind.Utc);

        return utc.Add(WibOffset);
    }

    public DateTime ToUtc(DateTime wibDateTime)
    {
        return DateTime.SpecifyKind(wibDateTime.Subtract(WibOffset), DateTimeKind.Utc);
    }

    public bool IsWorkingDay(DateOnly targetDate, IReadOnlySet<DateOnly>? holidays = null)
    {
        var dow = targetDate.DayOfWeek;
        if (dow == DayOfWeek.Saturday || dow == DayOfWeek.Sunday)
        {
            return false;
        }

        if (holidays != null && holidays.Contains(targetDate))
        {
            return false;
        }

        return true;
    }

    public bool IsWorkingDay(DateTime wibDateTime, IReadOnlySet<DateOnly>? holidays = null)
    {
        return IsWorkingDay(DateOnly.FromDateTime(wibDateTime), holidays);
    }

    public async Task<IReadOnlySet<DateOnly>> GetHolidaysAsync(int yearFrom, int yearTo, CancellationToken cancellationToken = default)
    {
        var startDate = new DateOnly(yearFrom, 1, 1);
        var endDate = new DateOnly(yearTo, 12, 31);

        var list = await _context.Holidays
            .AsNoTracking()
            .Where(h => h.Date >= startDate && h.Date <= endDate)
            .Select(h => h.Date)
            .ToListAsync(cancellationToken);

        return list.ToHashSet();
    }

    public DateTime AddWorkingMinutes(DateTime startUtc, int workingMinutes, IReadOnlySet<DateOnly>? holidays = null)
    {
        if (workingMinutes <= 0)
        {
            return startUtc;
        }

        var currentWib = ToWib(startUtc);

        // Normalize start time to next valid working window if currently outside
        currentWib = NormalizeToWorkingWindow(currentWib, holidays);

        while (workingMinutes > 0)
        {
            var todayEndWib = currentWib.Date.Add(WorkEndTime);
            var remainingToday = (int)(todayEndWib - currentWib).TotalMinutes;

            if (workingMinutes <= remainingToday)
            {
                currentWib = currentWib.AddMinutes(workingMinutes);
                workingMinutes = 0;
            }
            else
            {
                workingMinutes -= remainingToday;
                currentWib = MoveToNextWorkingDayStart(currentWib, holidays);
            }
        }

        return ToUtc(currentWib);
    }

    public int GetWorkingMinutesBetween(DateTime startUtc, DateTime endUtc, IReadOnlySet<DateOnly>? holidays = null)
    {
        if (startUtc >= endUtc)
        {
            return 0;
        }

        var startWib = ToWib(startUtc);
        var endWib = ToWib(endUtc);

        var current = startWib;
        var totalWorkingMinutes = 0;

        while (current.Date <= endWib.Date)
        {
            var dateOnly = DateOnly.FromDateTime(current);
            if (IsWorkingDay(dateOnly, holidays))
            {
                var workStart = current.Date.Add(WorkStartTime);
                var workEnd = current.Date.Add(WorkEndTime);

                var effectiveStart = current > workStart ? current : workStart;
                var effectiveEnd = endWib < workEnd ? endWib : workEnd;

                if (effectiveEnd > effectiveStart)
                {
                    totalWorkingMinutes += (int)(effectiveEnd - effectiveStart).TotalMinutes;
                }
            }

            // Move to start of next day
            current = current.Date.AddDays(1);
        }

        return totalWorkingMinutes;
    }

    private DateTime NormalizeToWorkingWindow(DateTime wibDateTime, IReadOnlySet<DateOnly>? holidays)
    {
        var current = wibDateTime;
        var dateOnly = DateOnly.FromDateTime(current);

        if (!IsWorkingDay(dateOnly, holidays))
        {
            return MoveToNextWorkingDayStart(current, holidays);
        }

        if (current.TimeOfDay < WorkStartTime)
        {
            return current.Date.Add(WorkStartTime);
        }

        if (current.TimeOfDay >= WorkEndTime)
        {
            return MoveToNextWorkingDayStart(current, holidays);
        }

        return current;
    }

    private DateTime MoveToNextWorkingDayStart(DateTime wibDateTime, IReadOnlySet<DateOnly>? holidays)
    {
        var nextDate = DateOnly.FromDateTime(wibDateTime).AddDays(1);

        while (!IsWorkingDay(nextDate, holidays))
        {
            nextDate = nextDate.AddDays(1);
        }

        return nextDate.ToDateTime(TimeOnly.FromTimeSpan(WorkStartTime));
    }
}
