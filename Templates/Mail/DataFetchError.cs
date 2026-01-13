namespace AurionCal.Api.Templates.Mail;

public record DataFetchError
{
    public string AppUrl { get; init; }
    public DateTime LastUpdated { get; init; }
};