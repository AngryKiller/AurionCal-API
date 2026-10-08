using System.Globalization;
using AurionCal.Api.Schools;
using IcalEvent = Ical.Net.CalendarComponents.CalendarEvent;

namespace AurionCal.Api.Services.Formatting;

/// <summary>
/// Junia conventions: courses as "Location / Name / Type / Teacher",
/// exams with an "Horaire TT" (extra time) line, make-up exams and conferences.
/// </summary>
public sealed class JuniaEventFormatter(School school) : EventFormatterBase(school)
{
    private const string AccommodationsPrefix = "Horaire TT";
    private const string ExamCode = "EXAM_SURV";
    private const string RattrapageCode = "RATTRAPAGE_SURV";

    private static readonly string[] TimeFormats = ["H'h'mm", "H'h'm", "H'h'"];

    protected override IcalEvent FormatConference(EventParts p)
    {
        var title = p.Lines.Length > 0 ? p.Lines[0] : string.Empty;
        if (string.IsNullOrWhiteSpace(title)) title = p.FirstLine;

        return CreateEvent(p.Event, BuildSummary(title, p.Event.ClassName), p.FirstLine);
    }

    // Layout: Room / Make-up exam title / RATTRAPAGE_SURV / Teacher
    protected override IcalEvent FormatRattrapage(EventParts p)
        => CreateNameAndTeacherEvent(p, RemoveLinesContaining(p.Lines, RattrapageCode));

    protected override IcalEvent FormatExam(EventParts p)
    {
        // Extract the "Horaire TT" line before any other filtering
        var accommodationsLine = p.Lines.FirstOrDefault(IsAccommodationsLine);

        var filteredLines = RemoveLinesContaining(p.Lines.Where(l => !IsAccommodationsLine(l)), ExamCode);

        var (location, examName) = filteredLines switch
        {
            // At least 2 lines left -> [Room, Name, ...]
            [var loc, var name, ..] => (loc, name),

            // 1 line left -> check whether it differs from the raw first line
            [var single] when !string.Equals(p.FirstLine, single, StringComparison.OrdinalIgnoreCase)
                => (p.FirstLine, single),

            // 1 line left (same as the first line): no distinct room
            [var single] => (string.Empty, single),

            // No line left -> use the raw first line
            _ => (string.Empty, p.FirstLine)
        };

        var summary = BuildSummary(examName, p.Event.ClassName);

        if (p.ExamAccommodations && accommodationsLine is not null
            && TryParseAccommodationsTimes(accommodationsLine, p.Event.Start.DateTime, out var start, out var end))
        {
            return CreateEvent(p.Event, summary, location, start, end);
        }

        return CreateEvent(p.Event, summary, location);
    }

    private static string[] RemoveLinesContaining(IEnumerable<string> lines, string code)
        => lines.Where(l => !l.Contains(code, StringComparison.OrdinalIgnoreCase)).ToArray();

    private static bool IsAccommodationsLine(string line)
        => line.StartsWith(AccommodationsPrefix, StringComparison.OrdinalIgnoreCase);

    private static bool TryParseAccommodationsTimes(string line, DateTime eventDate, out DateTime start, out DateTime end)
    {
        start = default;
        end = default;

        var colonIdx = line.IndexOf(':');
        if (colonIdx < 0) return false;

        var timePart = line[(colonIdx + 1)..].Trim();
        var dashIdx = timePart.IndexOf('-');
        if (dashIdx < 0) return false;

        if (!TryParseTime(timePart[..dashIdx].Trim(), out var startTime)
            || !TryParseTime(timePart[(dashIdx + 1)..].Trim(), out var endTime))
            return false;

        if (endTime <= startTime)
            return false;

        var date = eventDate.Date;
        start = date.Add(startTime.ToTimeSpan());
        end = date.Add(endTime.ToTimeSpan());
        return true;
    }

    private static bool TryParseTime(string value, out TimeOnly result)
        => TimeOnly.TryParseExact(value, TimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);
}
