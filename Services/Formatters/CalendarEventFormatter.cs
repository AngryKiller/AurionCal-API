namespace AurionCal.Api.Services.Formatters;

using System.Globalization;
using Entities;
using Enums;
using Ical.Net.DataTypes;
using IcalEvent = Ical.Net.CalendarComponents.CalendarEvent;

public static class CalendarEventFormatter
{
    private const string TimeZoneId = "Europe/Paris";

    public static IcalEvent ToIcalEvent(CalendarEvent cEvent, bool examAccommodations = false)
    {
        var lines = cEvent.Title
            .Split(Separator, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (lines.Length == 0) return CreateBaseEvent(cEvent, "Sans titre", string.Empty);

        var firstLine = lines[0];
        var remainingLines = lines.Skip(1).ToArray();

        var courseType = CourseTypeMappings.Parse(cEvent.ClassName);

        return courseType == CourseType.Epreuve
            ? FormatExam(cEvent, firstLine, remainingLines, examAccommodations)
            : FormatCourse(cEvent, courseType, firstLine, remainingLines);
    }

    private static IcalEvent FormatExam(CalendarEvent cEvent, string firstLine, string[] lines, bool examAccommodations)
    {
        // Filtrage spécifique aux examens
        // Extraire la ligne "Horaire TT" avant tout autre filtrage
        var examAccommodationsLine = lines.FirstOrDefault(l => l.StartsWith("Horaire TT", StringComparison.OrdinalIgnoreCase));

        var filteredLines = lines
            .Where(l => !l.Contains("EXAM_SURV", StringComparison.OrdinalIgnoreCase)
                     && !l.StartsWith("Horaire TT", StringComparison.OrdinalIgnoreCase))
            .ToArray();

        // Pattern matching sur le contenu du tableau
        var (location, examName) = filteredLines switch
        {
            // Cas : Au moins 2 lignes restantes -> [Salle, Nom, ...]
            [var loc, var name, ..] => (loc, name),
            
            // Cas : 1 ligne restante -> Vérifier si c'est diff de la 1ere ligne brute
            [var single] when !string.Equals(firstLine, single, StringComparison.OrdinalIgnoreCase) 
                => (firstLine, single),
            
            // Cas : 1 ligne restante (identique à firstLine) ou aucune salle distincte
            [var single] => (string.Empty, single),
            
            // Cas : Aucune ligne restante -> on prend la première ligne brute
            _ => (string.Empty, firstLine)
        };

        var summary = BuildSummary(examName, cEvent.ClassName);

        if (examAccommodations && examAccommodationsLine is not null && TryParseExamAccommodationsTimes(examAccommodationsLine, cEvent.Start.DateTime, out var examAccommodationsStart, out var examAccommodationsEnd))
        {
            return CreateBaseEvent(cEvent, summary, location, examAccommodationsStart, examAccommodationsEnd);
        }

        return CreateBaseEvent(cEvent, summary, location);
    }

    private static bool TryParseExamAccommodationsTimes(string examAccommodationsLine, DateTime eventDate, out DateTime start, out DateTime end)
    {
        start = default;
        end = default;

        var colonIdx = examAccommodationsLine.IndexOf(':');
        if (colonIdx < 0) return false;

        var timePart = examAccommodationsLine[(colonIdx + 1)..].Trim();
        var dashIdx = timePart.IndexOf('-');
        if (dashIdx < 0) return false;

        var startStr = timePart[..dashIdx].Trim();
        var endStr = timePart[(dashIdx + 1)..].Trim();

        if (!TryParseTime(startStr, out var startTime) || !TryParseTime(endStr, out var endTime))
            return false;

        if (endTime <= startTime)
            return false;

        var date = eventDate.Date;
        start = date.Add(startTime.ToTimeSpan());
        end = date.Add(endTime.ToTimeSpan());
        return true;
    }

    private static readonly string[] TimeFormats = ["H'h'mm", "H'h'm", "H'h'"];
    private static readonly char[] Separator = ['\r', '\n'];

    private static bool TryParseTime(string value, out TimeOnly result)
        => TimeOnly.TryParseExact(value, TimeFormats, CultureInfo.InvariantCulture, DateTimeStyles.None, out result);

    private static IcalEvent FormatCourse(CalendarEvent cEvent, CourseType type, string firstLine, string[] lines)
    {
        if (type == CourseType.Conference)
        {
            var conferenceTitle = lines.Length > 0 ? lines[0] : string.Empty;
            if (string.IsNullOrWhiteSpace(conferenceTitle)) conferenceTitle = firstLine;

            var conferenceSummary = BuildSummary(conferenceTitle, cEvent.ClassName);
            return CreateBaseEvent(cEvent, conferenceSummary, firstLine);
        }

        // Filtrage du ClassName dans les lignes restantes
        if (type != CourseType.Unknown && !string.IsNullOrWhiteSpace(cEvent.ClassName))
        {
            var className = cEvent.ClassName.Trim();
            lines = lines
                .Where(l => !l.Contains(className, StringComparison.OrdinalIgnoreCase))
                .ToArray();
        }

        var (courseName, teacher) = lines switch
        {
            [var c, var t, ..] => (c, t),
            [var c] => (c, string.Empty),
            _ => (string.Empty, string.Empty)
        };

        var parts = new List<string>();
        if (!string.IsNullOrWhiteSpace(courseName)) parts.Add(courseName);
        if (!string.IsNullOrWhiteSpace(teacher)) parts.Add($"- {teacher}");

        var baseSummary = string.Join(" ", parts);
        var summary = BuildSummary(baseSummary, cEvent.ClassName);

        return CreateBaseEvent(cEvent, summary, firstLine);
    }

    private static string BuildSummary(string baseName, string className)
    {
        var typeDisplay = CourseTypeMappings.ToDisplayNameFromRaw(className);

        if (string.IsNullOrWhiteSpace(typeDisplay)) return baseName;
        return string.IsNullOrWhiteSpace(baseName) ? typeDisplay : $"{baseName} ({typeDisplay})";
    }

    private static IcalEvent CreateBaseEvent(CalendarEvent cEvent, string summary, string location,
        DateTime? overrideStart = null, DateTime? overrideEnd = null)
    {
        var start = overrideStart ?? cEvent.Start.DateTime;
        var end = overrideEnd ?? cEvent.End.DateTime;

        return new IcalEvent
        {
            Summary = summary,
            Start = new CalDateTime(start).ToTimeZone(TimeZoneId),
            End = new CalDateTime(end).ToTimeZone(TimeZoneId),
            Description = cEvent.Title.Trim(),
            Location = location ?? string.Empty,
            Uid = cEvent.Id
        };
    }
}
