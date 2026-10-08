using Microsoft.Extensions.Options;

namespace AurionCal.Api.Schools;

public sealed class ConfigSchoolCatalog : ISchoolCatalog
{
    private readonly Dictionary<string, School> _byId;

    public ConfigSchoolCatalog(IOptions<SchoolsOptions> options)
    {
        var value = options.Value;
        All = value.Items;
        _byId = value.Items.ToDictionary(s => s.Id, StringComparer.OrdinalIgnoreCase);
        Default = _byId[value.DefaultSchoolId];
    }

    public IReadOnlyList<School> All { get; }

    public School Default { get; }

    public School? GetById(string id) => _byId.GetValueOrDefault(id);

    public School? Resolve(string? id) => string.IsNullOrWhiteSpace(id) ? Default : GetById(id.Trim());

    public bool EmailMatches(School school, string email)
    {
        var at = email.LastIndexOf('@');
        if (at < 0 || at == email.Length - 1) return false;

        var domain = email[(at + 1)..].Trim();
        return school.EmailDomains.Any(pattern => pattern.StartsWith("*.", StringComparison.Ordinal)
            ? domain.EndsWith(pattern[1..], StringComparison.OrdinalIgnoreCase)
            : string.Equals(domain, pattern, StringComparison.OrdinalIgnoreCase));
    }
}
