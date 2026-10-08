namespace AurionCal.Api.Services;

public static class EmailNormalizer
{
    /// <summary>Trims and lowercases an email so that accounts are matched regardless of how it was typed.</summary>
    public static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
