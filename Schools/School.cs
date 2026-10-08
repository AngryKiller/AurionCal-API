using AurionCal.Api.Enums;

namespace AurionCal.Api.Schools;

/// <summary>
/// A school reachable on Aurion through Mauria. Defined in <c>schools.json</c>.
/// </summary>
public sealed record School
{
    /// <summary>Stable identifier (slug), stored on each user: "junia", ...</summary>
    public string Id { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    /// <summary>The school's Aurion URL, sent to Mauria with every request.</summary>
    public string AurionBaseUrl { get; init; } = string.Empty;

    /// <summary>
    /// Accepted email domains ("junia.com"). A "*." prefix also accepts subdomains ("*.junia.com").
    /// </summary>
    public List<string> EmailDomains { get; init; } = [];

    /// <summary>Whether the extra-time exam schedule option is available for this school.</summary>
    public bool SupportsExamAccommodations { get; init; }

    /// <summary>Key of the event formatter to use (see <c>EventFormatterFactory</c>).</summary>
    public string ParserId { get; init; } = "default";

    public ParsingOptions Parsing { get; init; } = new();
}

/// <summary>
/// Configuration-driven parsing parameters (no logic).
/// </summary>
public sealed record ParsingOptions
{
    /// <summary>Overrides of the Aurion ClassName -> event type mapping.</summary>
    public Dictionary<string, CourseType> ClassNames { get; init; } = [];
}
