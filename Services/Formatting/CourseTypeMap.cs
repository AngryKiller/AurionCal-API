using AurionCal.Api.Enums;
using AurionCal.Api.Schools;

namespace AurionCal.Api.Services.Formatting;

/// <summary>
/// Mapping from Aurion's raw <c>ClassName</c> to a <see cref="CourseType"/>.
/// Shared defaults, overridable per school via <see cref="ParsingOptions.ClassNames"/>.
/// </summary>
public sealed class CourseTypeMap
{
    private static readonly IReadOnlyDictionary<string, CourseType> Defaults =
        new Dictionary<string, CourseType>(StringComparer.OrdinalIgnoreCase)
        {
            [RawCourseTypes.CoursTd] = CourseType.CoursTd,
            [RawCourseTypes.Td] = CourseType.CoursTd,
            [RawCourseTypes.CoursTp] = CourseType.CoursTp,
            [RawCourseTypes.Projet] = CourseType.Projet,
            [RawCourseTypes.Epreuve] = CourseType.Epreuve,
            [RawCourseTypes.EpreuveAlt] = CourseType.Epreuve,
            [RawCourseTypes.Rattrapage] = CourseType.Rattrapage,
            [RawCourseTypes.AutoAppr] = CourseType.AutoAppr,
            [RawCourseTypes.Reunion] = CourseType.Reunion,
            [RawCourseTypes.Conference] = CourseType.Conference,
            [RawCourseTypes.TdAutoGere] = CourseType.TdAutoGere,
        };

    private static readonly IReadOnlyDictionary<CourseType, string> DisplayNames =
        new Dictionary<CourseType, string>
        {
            [CourseType.Unknown] = "Autre",
            [CourseType.CoursTd] = "TD",
            [CourseType.CoursTp] = "TP",
            [CourseType.AutoAppr] = "Auto-apprentissage",
            [CourseType.Projet] = "Projet",
            [CourseType.Epreuve] = "Épreuve",
            [CourseType.Rattrapage] = "Rattrapage",
            [CourseType.Reunion] = "Réunion",
            [CourseType.Conference] = "Conférence",
            [CourseType.TdAutoGere] = "TD Auto-géré",
        };

    private readonly Dictionary<string, CourseType> _rawToType;

    private CourseTypeMap(Dictionary<string, CourseType> rawToType) => _rawToType = rawToType;

    public static CourseTypeMap For(ParsingOptions options)
    {
        var map = new Dictionary<string, CourseType>(Defaults, StringComparer.OrdinalIgnoreCase);
        foreach (var (raw, type) in options.ClassNames)
            map[raw] = type;
        return new CourseTypeMap(map);
    }

    public CourseType Parse(string? rawType)
    {
        if (string.IsNullOrWhiteSpace(rawType))
            return CourseType.Unknown;

        return _rawToType.TryGetValue(Normalize(rawType), out var found) || _rawToType.TryGetValue(rawType, out found)
            ? found
            : CourseType.Unknown;
    }

    public string ToDisplayNameFromRaw(string? rawType)
    {
        // For an unknown type, prefer the cleaned raw value over "Autre"
        // so the user keeps a useful piece of information.
        if (string.IsNullOrWhiteSpace(rawType))
            return DisplayNames[CourseType.Unknown];

        var parsed = Parse(rawType);
        return parsed == CourseType.Unknown
            ? NormalizeForDisplay(rawType)
            : DisplayNames.GetValueOrDefault(parsed, DisplayNames[CourseType.Unknown]);
    }

    private static string Normalize(string value)
    {
        var chars = value.Trim()
            .Select(c => c is '-' or ' ' ? '_' : c)
            .ToArray();

        return new string(chars).ToUpperInvariant();
    }

    private static string NormalizeForDisplay(string value)
        => string.Join(' ', value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));
}
