using AurionCal.Api.Entities;

namespace AurionCal.Api.Services;

public record DayWindow(DateTimeOffset DayStart, DateTimeOffset DayEnd);

public static class DayWindowCalculator
{
    public static (DateTimeOffset StartUtc, DateTimeOffset EndUtcExclusive) GetUtcRange(DateOnly date, TimeZoneInfo tz)
    {
        var localStart = date.ToDateTime(TimeOnly.MinValue);
        var localNext = date.AddDays(1).ToDateTime(TimeOnly.MinValue);

        var startUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localStart, tz), TimeSpan.Zero);
        var endUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(localNext, tz), TimeSpan.Zero);
        return (startUtc, endUtc);
    }

    public static DayWindow? Compute(IEnumerable<CalendarEvent> events, DateOnly date, TimeZoneInfo tz)
    {
        var (startUtc, endUtc) = GetUtcRange(date, tz);

        DateTimeOffset? firstStart = null;
        DateTimeOffset? lastEnd = null;

        foreach (var e in events)
        {
            if (e.Start < startUtc || e.Start >= endUtc) continue;
            if (firstStart is null || e.Start < firstStart) firstStart = e.Start;
            if (lastEnd is null || e.End > lastEnd) lastEnd = e.End;
        }

        if (firstStart is null) return null;

        return new DayWindow(
            TimeZoneInfo.ConvertTime(firstStart.Value, tz),
            TimeZoneInfo.ConvertTime(lastEnd!.Value, tz)
        );
    }
}