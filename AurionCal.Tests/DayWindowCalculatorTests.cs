using AurionCal.Api.Entities;
using AurionCal.Api.Services;
using Xunit;

namespace AurionCal.Tests;

public class DayWindowCalculatorTests
{
    private static readonly TimeZoneInfo ParisTz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");
    private static readonly DateOnly TargetDate = new(2026, 5, 23);

    private static CalendarEvent Event(string id, DateTimeOffset start, DateTimeOffset end) => new()
    {
        Id = id,
        Title = $"Cours {id}",
        ClassName = "test",
        Start = start.ToUniversalTime(),
        End = end.ToUniversalTime()
    };

    private static DateTimeOffset Paris(int year, int month, int day, int hour, int minute)
    {
        var local = new DateTime(year, month, day, hour, minute, 0, DateTimeKind.Unspecified);
        var utc = TimeZoneInfo.ConvertTimeToUtc(local, ParisTz);
        return new DateTimeOffset(utc, TimeSpan.Zero);
    }

    [Fact]
    public void Compute_FullDay_ReturnsFirstStartAndLastEnd()
    {
        var events = new[]
        {
            Event("1", Paris(2026, 5, 23,  8,  0), Paris(2026, 5, 23, 10,  0)),
            Event("2", Paris(2026, 5, 23, 10, 15), Paris(2026, 5, 23, 12, 15)),
            Event("3", Paris(2026, 5, 23, 13, 30), Paris(2026, 5, 23, 15, 30)),
            Event("4", Paris(2026, 5, 23, 15, 45), Paris(2026, 5, 23, 17,  0))
        };

        var window = DayWindowCalculator.Compute(events, TargetDate, ParisTz);

        Assert.NotNull(window);
        Assert.Equal(Paris(2026, 5, 23,  8, 0), window!.DayStart);
        Assert.Equal(Paris(2026, 5, 23, 17, 0), window.DayEnd);
        Assert.Equal(TimeSpan.FromHours(2), window.DayStart.Offset);
    }

    [Fact]
    public void Compute_EmptyDay_ReturnsNull()
    {
        var events = new[]
        {
            Event("1", Paris(2026, 5, 22,  8, 0), Paris(2026, 5, 22, 10, 0)),
            Event("2", Paris(2026, 5, 24,  9, 0), Paris(2026, 5, 24, 11, 0))
        };

        var window = DayWindowCalculator.Compute(events, TargetDate, ParisTz);

        Assert.Null(window);
    }

    [Fact]
    public void Compute_NoEventsAtAll_ReturnsNull()
    {
        var window = DayWindowCalculator.Compute(Array.Empty<CalendarEvent>(), TargetDate, ParisTz);
        Assert.Null(window);
    }

    [Fact]
    public void Compute_SingleEvent_DayStartEqualsEventStart()
    {
        var events = new[]
        {
            Event("1", Paris(2026, 5, 23, 14, 0), Paris(2026, 5, 23, 16, 0))
        };

        var window = DayWindowCalculator.Compute(events, TargetDate, ParisTz);

        Assert.NotNull(window);
        Assert.Equal(Paris(2026, 5, 23, 14, 0), window!.DayStart);
        Assert.Equal(Paris(2026, 5, 23, 16, 0), window.DayEnd);
    }

    [Fact]
    public void Compute_LargeGapBetweenEvents_SpansEntireRange()
    {
        var events = new[]
        {
            Event("morning", Paris(2026, 5, 23,  8, 0), Paris(2026, 5, 23,  9, 30)),
            Event("evening", Paris(2026, 5, 23, 17, 0), Paris(2026, 5, 23, 18, 30))
        };

        var window = DayWindowCalculator.Compute(events, TargetDate, ParisTz);

        Assert.NotNull(window);
        Assert.Equal(Paris(2026, 5, 23,  8,  0), window!.DayStart);
        Assert.Equal(Paris(2026, 5, 23, 18, 30), window.DayEnd);
    }

    [Fact]
    public void Compute_EventsOnOtherDays_AreIgnored()
    {
        var events = new[]
        {
            Event("yesterday-late", Paris(2026, 5, 22, 22, 0), Paris(2026, 5, 22, 23, 30)),
            Event("today",          Paris(2026, 5, 23,  9, 0), Paris(2026, 5, 23, 12, 0)),
            Event("tomorrow-early", Paris(2026, 5, 24,  6, 0), Paris(2026, 5, 24,  8, 0))
        };

        var window = DayWindowCalculator.Compute(events, TargetDate, ParisTz);

        Assert.NotNull(window);
        Assert.Equal(Paris(2026, 5, 23,  9, 0), window!.DayStart);
        Assert.Equal(Paris(2026, 5, 23, 12, 0), window.DayEnd);
    }

    [Fact]
    public void GetUtcRange_AppliesParisOffset()
    {
        // May = CEST (+02:00) -> midnight Paris = 22:00 UTC the previous day
        var (startUtc, endUtc) = DayWindowCalculator.GetUtcRange(TargetDate, ParisTz);

        Assert.Equal(new DateTimeOffset(2026, 5, 22, 22, 0, 0, TimeSpan.Zero), startUtc);
        Assert.Equal(new DateTimeOffset(2026, 5, 23, 22, 0, 0, TimeSpan.Zero), endUtc);
    }

    [Fact]
    public void GetUtcRange_WinterUsesCETOffset()
    {
        // January = CET (+01:00) -> midnight Paris = 23:00 UTC the previous day
        var winterDate = new DateOnly(2026, 1, 15);
        var (startUtc, endUtc) = DayWindowCalculator.GetUtcRange(winterDate, ParisTz);

        Assert.Equal(new DateTimeOffset(2026, 1, 14, 23, 0, 0, TimeSpan.Zero), startUtc);
        Assert.Equal(new DateTimeOffset(2026, 1, 15, 23, 0, 0, TimeSpan.Zero), endUtc);
    }
}
