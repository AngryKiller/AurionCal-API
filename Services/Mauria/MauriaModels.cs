using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace AurionCal.Api.Services;

class CustomDateTimeOffsetConverter : JsonConverter<DateTimeOffset>
{
    public override DateTimeOffset Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var s = reader.GetString();

        // Fixes the offset format that has no colon
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
    public string BaseUrl { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}

public class GetPlanningRequest
{
    public string BaseUrl { get; set; }
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