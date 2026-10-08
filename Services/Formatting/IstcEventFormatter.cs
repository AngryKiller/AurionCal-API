using AurionCal.Api.Schools;
using Ical.Net;
using IcalEvent = Ical.Net.CalendarComponents.CalendarEvent;

namespace AurionCal.Api.Services.Formatting;

/// <summary>
/// ISTC conventions: a single line of " - " separated fields
/// "Status - Cancellation -  -  - Teacher - Room - Subject - Type - Comment - Year[ - Group]".
/// Exam fields can hold several values ("B115 / B115", "M BOUTE / MME KENNEY").
/// </summary>
public sealed class IstcEventFormatter(School school) : EventFormatterBase(school)
{
    private const string FieldSeparator = " - ";
    private const string CancelledPrefix = "ANNULE";
    private const string GroupPrefix = "Groupe";

    private const int StatusIndex = 0;
    private const int TeacherIndex = 4;
    private const int RoomIndex = 5;
    private const int SubjectIndex = 6;
    private const int TypeIndex = 7;
    private const int CommentIndex = 8;

    // Every event type shares the same layout, so they all end up here.
    protected override IcalEvent FormatCourse(EventParts p)
    {
        // Comments can span several lines ("Claire CARRIN\nvisites de prévention...")
        var fields = string.Join(' ', [p.FirstLine, ..p.Lines])
            .Split(FieldSeparator)
            .Select(f => f.Trim())
            .ToArray();

        if (fields.Length <= TypeIndex) return base.FormatCourse(p);

        var teacher = MergeValues(fields[TeacherIndex]);
        var room = MergeValues(fields[RoomIndex]);
        var subject = MergeValues(fields[SubjectIndex]);
        var type = fields[TypeIndex];
        var comment = ExtractComment(fields);

        // Events without a subject (presentations, awareness sessions) carry their title in the comment
        var name = !string.IsNullOrWhiteSpace(subject) ? subject
            : !string.IsNullOrWhiteSpace(comment) ? comment
            : type;
        var extraInfo = name == comment ? string.Empty : comment;

        var baseName = string.IsNullOrWhiteSpace(teacher) ? name : $"{name} - {teacher}";
        var typeDisplay = string.IsNullOrWhiteSpace(type) ? CourseTypes.ToDisplayNameFromRaw(p.Event.ClassName) : type;
        var summary = string.IsNullOrWhiteSpace(baseName) || baseName == typeDisplay
            ? typeDisplay
            : $"{baseName} ({typeDisplay})";

        var cancelled = fields[StatusIndex].StartsWith(CancelledPrefix, StringComparison.OrdinalIgnoreCase);
        if (cancelled) summary = $"[Annulé] {summary}";

        var result = CreateEvent(p.Event, summary, room);
        if (!string.IsNullOrWhiteSpace(extraInfo)) result.Description = $"{extraInfo}\n\n{result.Description}";
        if (cancelled) result.Status = EventStatus.Cancelled;

        return result;
    }

    /// <summary>The comment sits between the type and the trailing "Year[ - Group]" fields.</summary>
    private static string ExtractComment(string[] fields)
    {
        var end = fields.Length;
        if (end > CommentIndex + 1 && fields[end - 1].StartsWith(GroupPrefix, StringComparison.OrdinalIgnoreCase)) end--;
        if (end > CommentIndex + 1) end--; // Year

        return string.Join(FieldSeparator, fields[CommentIndex..Math.Max(CommentIndex, end)]).Trim();
    }

    /// <summary>"B115 / B115" -> "B115", "M BOUTE / MME KENNEY" stays as is.</summary>
    private static string MergeValues(string field)
        => string.Join(" / ", field
            .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase));
}
