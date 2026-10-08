using AurionCal.Api.Schools;
using FastEndpoints;

namespace AurionCal.Api.Endpoints;

public class SchoolSummary
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<string> EmailDomains { get; set; } = [];
    public bool SupportsExamAccommodations { get; set; }
}

public class GetSchoolsResponse
{
    public List<SchoolSummary> Schools { get; set; } = [];
    public string DefaultSchoolId { get; set; } = string.Empty;
}

/// <summary>
/// Public list of available schools. The Aurion URL is deliberately not exposed.
/// </summary>
public class GetSchoolsEndpoint(ISchoolCatalog schools) : EndpointWithoutRequest<GetSchoolsResponse>
{
    public override void Configure()
    {
        AllowAnonymous();
        Get("/api/schools");
    }

    public override async Task HandleAsync(CancellationToken c)
    {
        var response = new GetSchoolsResponse
        {
            DefaultSchoolId = schools.Default.Id,
            Schools = schools.All
                .Select(s => new SchoolSummary { Id = s.Id, Name = s.Name, EmailDomains = s.EmailDomains, SupportsExamAccommodations = s.SupportsExamAccommodations })
                .ToList()
        };

        await Send.OkAsync(response, c);
    }
}
