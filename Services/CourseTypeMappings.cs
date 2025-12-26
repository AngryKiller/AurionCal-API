using System.Collections.Immutable;
using AurionCal.Api.Enums;

namespace AurionCal.Api.Services;

public static class CourseTypeMappings
{
    private static readonly ImmutableDictionary<string, CourseType> RawToEnum =
        new Dictionary<string, CourseType>(StringComparer.OrdinalIgnoreCase)
        {
            [RawCourseTypes.CoursTd] = CourseType.CoursTd,
            [RawCourseTypes.CoursTp] = CourseType.CoursTp,
            [RawCourseTypes.Projet] = CourseType.Projet,
            [RawCourseTypes.Epreuve] = CourseType.Epreuve,
            [RawCourseTypes.AutoAppr] = CourseType.AutoAppr,
        }.ToImmutableDictionary();

    private static readonly ImmutableDictionary<CourseType, string> EnumToDisplay =
        new Dictionary<CourseType, string>
        {
            [CourseType.Unknown] = "Autre",
            [CourseType.CoursTd] = "TD",
            [CourseType.CoursTp] = "TP",
            [CourseType.AutoAppr] = "Auto-apprentissage",
            [CourseType.Projet] = "Projet",
            [CourseType.Epreuve] = "Épreuve",
        }.ToImmutableDictionary();

    public static CourseType Parse(string? rawType)
    {
        if (string.IsNullOrWhiteSpace(rawType))
            return CourseType.Unknown;

        var normalized = Normalize(rawType);

        if (RawToEnum.TryGetValue(normalized, out var found))
            return found;

        if (RawToEnum.TryGetValue(rawType, out found))
            return found;

        return CourseType.Unknown;
    }

    public static string ToDisplayName(CourseType type)
    {
        return EnumToDisplay.TryGetValue(type, out var display)
            ? display
            : EnumToDisplay[CourseType.Unknown];
    }

    public static string ToDisplayNameFromRaw(string? rawType)
    {
        // Si on ne connaît pas le type, on préfère afficher la valeur brute nettoyée
        // plutôt que "Autre" pour garder une info utile pour l'utilisateur.
        if (string.IsNullOrWhiteSpace(rawType))
            return ToDisplayName(CourseType.Unknown);

        var parsed = Parse(rawType);
        if (parsed == CourseType.Unknown)
        {
            // On renvoie la valeur brute trimée (éventuellement légèrement normalisée)
            return NormalizeForDisplay(rawType);
        }

        return ToDisplayName(parsed);
    }

    private static string Normalize(string value)
    {
        var trimmed = value.Trim();

        var chars = trimmed
            .Select(c => c is '-' or ' ' ? '_' : c)
            .ToArray();

        return new string(chars).ToUpperInvariant();
    }

    private static string NormalizeForDisplay(string value)
    {
        var trimmed = value.Trim();

        var parts = trimmed
            .Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return string.Join(' ', parts);
    }
}
