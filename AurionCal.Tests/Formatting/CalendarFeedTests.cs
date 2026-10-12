using AurionCal.Api.Services;
using AurionCal.Api.Services.Formatting;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using AurionCal.Api.Schools;

namespace AurionCal.Tests.Formatting;

public class CalendarFeedTests
{
    private static readonly School Junia = TestData.School();

    // Feed generation only needs the school catalog and the formatter factory
    private readonly CalendarService _service = new(
        keyVaultService: null!,
        scopeFactory: null!,
        NullLogger<CalendarService>.Instance,
        new ConfigSchoolCatalog(Options.Create(new SchoolsOptions { DefaultSchoolId = "junia", Items = [Junia] })),
        new EventFormatterFactory(NullLogger<EventFormatterFactory>.Instance));

    private static readonly Api.Entities.CalendarEvent[] Events =
    [
        TestData.Event("Salle A1\nAlgo\nCOURS_TD\nProf", "COURS_TD", id: "course"),
        TestData.Event("Période en entreprise\nENTREPRISE", "ENTREPRISE", id: "placement"),
    ];

    [Fact]
    public void WorkPlacement_IsLeftOutOfTheFeedByDefault()
    {
        var feed = _service.GenerateCalendarFeed(Events, Junia);

        Assert.Contains("UID:course", feed);
        Assert.DoesNotContain("UID:placement", feed);
    }

    [Fact]
    public void WorkPlacement_IsInTheFeedWhenEnabled()
    {
        var feed = _service.GenerateCalendarFeed(Events, Junia, new FormatOptions(ShowWorkPlacement: true));

        Assert.Contains("UID:course", feed);
        Assert.Contains("UID:placement", feed);
        Assert.Contains("SUMMARY:Période en entreprise", feed);
    }
}
