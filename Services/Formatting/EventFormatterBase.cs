using AurionCal.Api.Enums;
using AurionCal.Api.Schools;
using Ical.Net.DataTypes;
using CalendarEvent = AurionCal.Api.Entities.CalendarEvent;
using IcalEvent = Ical.Net.CalendarComponents.CalendarEvent;

namespace AurionCal.Api.Services.Formatting;

/// <summary>
/// Parsed event: first title line, remaining lines and the type inferred from ClassName.
/// </summary>
public sealed record EventParts(
    CalendarEvent Event,
    CourseType Type,
    string FirstLine,
    string[] Lines,
    bool ExamAccommodations);

/// <summary>
/// Common base for formatters: title splitting and iCal event construction.
/// Each event type has an extension point which defaults to the generic course format (<see cref="FormatCourse"/>).
/// </summary>
public abstract class EventFormatterBase : IEventFormatter
{
    private const string TimeZoneId = "Europe/Paris";
    private static readonly char[] LineSeparators = ['\r', '\n'];

    protected EventFormatterBase(School school)
    {
        School = school;
        CourseTypes = CourseTypeMap.For(school.Parsing);
    }

    protected School School { get; }

    protected CourseTypeMap CourseTypes { get; }

    public IcalEvent Format(CalendarEvent evt, bool examAccommodations)
    {
        var lines = evt.Title
            .Split(LineSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (lines.Length == 0) return CreateEvent(evt, "Sans titre", string.Empty);

        var parts = new EventParts(evt, CourseTypes.Parse(evt.ClassName), lines[0], lines[1..], examAccommodations);

        return parts.Type switch
        {
            CourseType.Epreuve => FormatExam(parts),
            CourseType.Rattrapage => FormatRattrapage(parts),
            CourseType.Conference => FormatConference(parts),
            _ => FormatCourse(parts)
        };
    }

    protected virtual IcalEvent FormatExam(EventParts p) => FormatCourse(p);

    protected virtual IcalEvent FormatRattrapage(EventParts p) => FormatCourse(p);

    protected virtual IcalEvent FormatConference(EventParts p) => FormatCourse(p);

    /// <summary>Generic format: [Location, Name, Teacher] with the type removed from the lines.</summary>
    protected virtual IcalEvent FormatCourse(EventParts p)
    {
        var lines = p.Lines;

        // Filter out the ClassName from the remaining lines
        if (p.Type != CourseType.Unknown && !string.IsNullOrWhiteSpace(p.Event.ClassName))
        {
            var className = p.Event.ClassName.Trim();
            lines = lines
                .Where(l => !l.Contains(className, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        return CreateNameAndTeacherEvent(p, lines);
    }

    /// <summary>[Name, Teacher] -> "Name - Teacher (Type)"; the location is the first line.</summary>
    protected IcalEvent CreateNameAndTeacherEvent(EventParts p, string[] lines)
    {
        var (name, teacher) = lines switch
        {
            [var n, var t, ..] => (n, t),
            [var n] => (n, string.Empty),
            _ => (string.Empty, string.Empty)
        };

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(name)) parts.Add(name);
        if (!string.IsNullOrWhiteSpace(teacher)) parts.Add($"- {teacher}");

        var summary = BuildSummary(string.Join(" ", parts), p.Event.ClassName);
        return CreateEvent(p.Event, summary, p.FirstLine);
    }

    protected string BuildSummary(string baseName, string className)
    {
        var typeDisplay = CourseTypes.ToDisplayNameFromRaw(className);

        if (string.IsNullOrWhiteSpace(typeDisplay)) return baseName;
        return string.IsNullOrWhiteSpace(baseName) ? typeDisplay : $"{baseName} ({typeDisplay})";
    }

    protected IcalEvent CreateEvent(CalendarEvent evt, string summary, string? location,
        DateTime? overrideStart = null, DateTime? overrideEnd = null)
    {
        var start = overrideStart ?? evt.Start.DateTime;
        var end = overrideEnd ?? evt.End.DateTime;

        return new IcalEvent
        {
            Summary = summary,
            Start = new CalDateTime(start).ToTimeZone(TimeZoneId),
            End = new CalDateTime(end).ToTimeZone(TimeZoneId),
            Description = evt.Title.Trim(),
            Location = location ?? string.Empty,
            Uid = evt.Id
        };
    }
}
