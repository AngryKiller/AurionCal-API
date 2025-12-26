namespace AurionCal.Api.Services.Formatters;

using Entities;
using Enums;
using Ical.Net.DataTypes;
using IcalEvent = Ical.Net.CalendarComponents.CalendarEvent;

public static class CalendarEventFormatter
{
    private const string TimeZoneId = "Europe/Paris";

    public static IcalEvent ToIcalEvent(CalendarEvent cEvent)
    {
        var lines = cEvent.Title
            .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (lines.Length == 0) return CreateBaseEvent(cEvent, "Sans titre", string.Empty);

        var firstLine = lines[0];
        var remainingLines = lines.Skip(1).ToArray();
        
        var courseType = CourseTypeMappings.Parse(cEvent.ClassName);

        return courseType == CourseType.Epreuve
            ? FormatExam(cEvent, firstLine, remainingLines)
            : FormatCourse(cEvent, courseType, firstLine, remainingLines);
    }

    private static IcalEvent FormatExam(CalendarEvent cEvent, string firstLine, string[] lines)
    {
        // Filtrage spécifique aux examens
        var filteredLines = lines
            .Where(l => !l.Contains("EXAM_SURV", StringComparison.OrdinalIgnoreCase))
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
        return CreateBaseEvent(cEvent, summary, location);
    }

    private static IcalEvent FormatCourse(CalendarEvent cEvent, CourseType type, string firstLine, string[] lines)
    {
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
            [var c, var t, ..] => (c, t), // Au moins 2 lignes
            [var c] => (c, string.Empty), // 1 ligne
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
        if (string.IsNullOrWhiteSpace(baseName)) return typeDisplay;
        
        return $"{baseName} ({typeDisplay})";
    }

    private static IcalEvent CreateBaseEvent(CalendarEvent cEvent, string summary, string location)
    {
        return new IcalEvent
        {
            Summary = summary,
            Start = new CalDateTime(cEvent.Start.DateTime).ToTimeZone(TimeZoneId),
            End = new CalDateTime(cEvent.End.DateTime).ToTimeZone(TimeZoneId),
            Description = cEvent.Title.Trim(),
            Location = location ?? string.Empty,
            Uid = cEvent.Id
        };
    }
}