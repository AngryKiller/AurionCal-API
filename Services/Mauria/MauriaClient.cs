using System.Text.Json;
using AurionCal.Api.Enums;
using AurionCal.Api.Schools;

namespace AurionCal.Api.Services;

public sealed class MauriaClient(HttpClient client, IConfiguration configuration) : IMauriaClient
{
    private static readonly JsonSerializerOptions PlanningJsonOptions = CreatePlanningJsonOptions();

    public async Task<CheckLoginInfoResponse> CheckLoginAsync(School school, string email, string password,
        CancellationToken c = default)
    {
        var request = new CheckLoginInfoRequest
        {
            BaseUrl = school.AurionBaseUrl,
            Email = EmailNormalizer.Normalize(email),
            Password = password
        };
        try
        {
            var response = await client.PostAsJsonAsync(GetRoute(MauriaRoutes.AurionCheckLogin), request, c);
            return (await response.Content.ReadFromJsonAsync<CheckLoginInfoResponse>(c))!;
        }
        catch (Exception)
        {
            throw new InvalidOperationException("Failed to reach Mauria.");
        }
    }

    public async Task<GetPlanningResponse> GetPlanningAsync(School school, string email, string password,
        CancellationToken c = default)
    {
        var request = new GetPlanningRequest
        {
            BaseUrl = school.AurionBaseUrl,
            Email = email,
            Password = password,
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow.AddMonths(2)
        };

        var response = await client.PostAsJsonAsync(GetRoute(MauriaRoutes.AurionPlanning), request, c);
        var jsonContent = await response.Content.ReadAsStringAsync(c);

        return JsonSerializer.Deserialize<GetPlanningResponse>(jsonContent, PlanningJsonOptions)!;
    }

    private string GetRoute(string route)
    {
        var baseUrl = configuration["ApiSettings:BaseUrl"];
        return string.IsNullOrEmpty(baseUrl)
            ? throw new InvalidOperationException("BaseUrl is not configured.")
            : string.Concat(baseUrl.TrimEnd('/'), "/", route.TrimStart('/'));
    }

    private static JsonSerializerOptions CreatePlanningJsonOptions()
    {
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        options.Converters.Add(new CustomDateTimeOffsetConverter());
        return options;
    }
}
