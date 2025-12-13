using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;
using AurionCal.Api.Enums;

namespace AurionCal.Api.Services;

public class MauriaApiService(HttpClient client, IConfiguration configuration) : IDisposable
{
    private readonly HttpClient? _client = client;

    public async Task<CheckLoginInfoResponse> CheckLoginInfoAsync(string email, string password, CancellationToken c = default)
    {
        var route = GetRoute(MauriaRoutes.AurionCheckLogin);
        var request = new CheckLoginInfoRequest
        {
            Email = email.Trim().ToLowerInvariant(),
            Password = password
        };
        try
        {
            var response = await _client.PostAsJsonAsync(route, request, c);
            Console.WriteLine(await response.Content.ReadAsStringAsync(c));
            //response.EnsureSuccessStatusCode();

            var result = await response.Content.ReadFromJsonAsync<CheckLoginInfoResponse>(c);
            return result;
        }
        catch (Exception e)
        {
            throw new InvalidOperationException("Failed to reach Mauria.");
        }
    }

    public async Task<GetPlanningResponse> GetPlanningAsync(string email, string password,
        CancellationToken c = default)
    {
        var route = GetRoute(MauriaRoutes.AurionPlanning);
        var options = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };
        options.Converters.Add(new CustomDateTimeOffsetConverter());
    
        var request = new GetPlanningRequest
        {
            Email = email,
            Password = password,
            StartDate = DateTime.UtcNow.AddDays(-7),
            EndDate = DateTime.UtcNow.AddMonths(2)
        };
    
        var response = await _client.PostAsJsonAsync(route, request, c);
        var jsonContent = await response.Content.ReadAsStringAsync(c);
    
        var result = JsonSerializer.Deserialize<GetPlanningResponse>(jsonContent, options);
        return result;
    }

    
    
    private async Task<TResult?> GetAsync<TResult>(string route, CancellationToken c = default)
    {

        var response = await _client.GetAsync(route, c);
        response.EnsureSuccessStatusCode();

        return await response.Content.ReadFromJsonAsync<TResult>(c);

    }

    public void Dispose()
    {
        _client?.Dispose();
    }

    private string GetRoute(string route)
    {
        var baseUrl = configuration["ApiSettings:BaseUrl"];
        return string.IsNullOrEmpty(baseUrl) ? throw new InvalidOperationException("BaseUrl is not configured.") : string.Concat(baseUrl.TrimEnd('/'), "/", route.TrimStart('/'));
    }
}


class CustomDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();

        // Corrige le format sans deux-points dans l’offset
        if (s != null && s.Length > 5 && (s[^5] == '+' || s[^5] == '-'))
            s = s.Insert(s.Length - 2, ":");

        return DateTimeOffset.Parse(s, CultureInfo.InvariantCulture);
    }

    public override void Write(Utf8JsonWriter writer, DateTimeOffset value, JsonSerializerOptions options)
    {
        writer.WriteStringValue(value.ToString("yyyy-MM-ddTHH:mm:sszzz"));
    }
}

public class CheckLoginInfoRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class GetPlanningRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
    public DateTimeOffset StartDate { get; set; }
    public DateTimeOffset EndDate { get; set; }
}

public class GetPlanningResponse
{
    public bool Success { get; set; }
    public List<PlanningEvent>? Data { get; set; }
}

public class PlanningEvent
{
    public string Id { get; set; }
    public string Title { get; set; } 
    [JsonConverter(typeof(CustomDateTimeOffsetConverter))]
    public DateTimeOffset Start { get; set; }
    [JsonConverter(typeof(CustomDateTimeOffsetConverter))]
    public DateTimeOffset End { get; set; }
    public bool AllDay { get; set; }
    public bool Editable { get; set; }
    public string ClassName { get; set; }
}

public class CheckLoginInfoResponse
{
    public bool Success { get; set; }
    public string? Error { get; set; }
}