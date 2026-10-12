namespace AurionCal.Api.Services.Formatting;

/// <summary>
/// Per-user choices that change how events are rendered in the feed.
/// </summary>
public sealed record FormatOptions(bool ExamAccommodations = false, bool ShowWorkPlacement = false)
{
    public static FormatOptions Default { get; } = new();
}
