using System.Collections.Concurrent;
using AurionCal.Api.Schools;

namespace AurionCal.Api.Services.Formatting;

public sealed class EventFormatterFactory : IEventFormatterFactory
{
    private const string DefaultParserId = "default";

    // For a new school with a different title layout: create a formatter and register it here.
    private static readonly IReadOnlyDictionary<string, Func<School, IEventFormatter>> Builders =
        new Dictionary<string, Func<School, IEventFormatter>>(StringComparer.OrdinalIgnoreCase)
        {
            [DefaultParserId] = school => new DefaultEventFormatter(school),
            ["junia"] = school => new JuniaEventFormatter(school),
        };

    private readonly ILogger<EventFormatterFactory> _logger;
    private readonly ConcurrentDictionary<string, IEventFormatter> _cache = new(StringComparer.OrdinalIgnoreCase);

    public EventFormatterFactory(ILogger<EventFormatterFactory> logger) => _logger = logger;

    public IEventFormatter For(School school)
        => _cache.GetOrAdd(school.Id, _ => Create(school));

    private IEventFormatter Create(School school)
    {
        if (Builders.TryGetValue(school.ParserId, out var builder))
            return builder(school);

        _logger.LogWarning("Unknown ParserId '{ParserId}' for school {SchoolId}: falling back to the default formatter.",
            school.ParserId, school.Id);
        return Builders[DefaultParserId](school);
    }
}
