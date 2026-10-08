namespace AurionCal.Api.Schools;

public interface ISchoolCatalog
{
    IReadOnlyList<School> All { get; }

    School Default { get; }

    School? GetById(string id);

    /// <summary>Returns the requested school, or the default school if <paramref name="id"/> is empty.</summary>
    School? Resolve(string? id);

    /// <summary>Checks that the email domain is accepted by the school.</summary>
    bool EmailMatches(School school, string email);
}
