using AurionCal.Api.Entities;
using AurionCal.Api.Schools;

namespace AurionCal.Tests;

internal static class TestData
{
    public static School School(string id = "junia", string parserId = "junia") => new()
    {
        Id = id,
        Name = id,
        AurionBaseUrl = "https://aurion.example.com",
        EmailDomains = ["junia.com", "student.junia.com"],
        ParserId = parserId,
        SupportsExamAccommodations = true
    };

    public static CalendarEvent Event(string title, string className, int startHour = 8, int endHour = 10) => new()
    {
        Id = "evt-1",
        Title = title,
        ClassName = className,
        Start = new DateTimeOffset(2026, 10, 12, startHour, 0, 0, TimeSpan.Zero),
        End = new DateTimeOffset(2026, 10, 12, endHour, 0, 0, TimeSpan.Zero)
    };
}
