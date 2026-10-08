using Microsoft.Extensions.Options;

namespace AurionCal.Api.Schools;

public sealed class SchoolsOptions
{
    public const string SectionName = "Schools";

    /// <summary>School used when a client does not send a <c>schoolId</c> (backward compatibility).</summary>
    public string DefaultSchoolId { get; set; } = string.Empty;

    public List<School> Items { get; set; } = [];
}

public sealed class SchoolsOptionsValidator : IValidateOptions<SchoolsOptions>
{
    public ValidateOptionsResult Validate(string? name, SchoolsOptions options)
    {
        var errors = new List<string>();

        if (options.Items.Count == 0)
            errors.Add("No school configured.");

        foreach (var s in options.Items)
        {
            if (string.IsNullOrWhiteSpace(s.Id)) errors.Add("A school has no id.");
            if (string.IsNullOrWhiteSpace(s.Name)) errors.Add($"School '{s.Id}': name is missing.");
            if (!Uri.TryCreate(s.AurionBaseUrl, UriKind.Absolute, out var url)
                || (url.Scheme != Uri.UriSchemeHttp && url.Scheme != Uri.UriSchemeHttps))
                errors.Add($"School '{s.Id}': AurionBaseUrl is invalid.");
            if (s.EmailDomains.Count == 0)
                errors.Add($"School '{s.Id}': at least one email domain is required.");
        }

        var duplicates = options.Items
            .GroupBy(s => s.Id, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .Select(g => g.Key);
        errors.AddRange(duplicates.Select(id => $"Duplicate school id: '{id}'."));

        if (!options.Items.Any(s => string.Equals(s.Id, options.DefaultSchoolId, StringComparison.OrdinalIgnoreCase)))
            errors.Add($"DefaultSchoolId '{options.DefaultSchoolId}' does not match any school.");

        return errors.Count == 0 ? ValidateOptionsResult.Success : ValidateOptionsResult.Fail(errors);
    }
}
