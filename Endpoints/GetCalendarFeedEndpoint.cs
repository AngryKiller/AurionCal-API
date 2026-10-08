using AurionCal.Api.Contexts;
using AurionCal.Api.Schools;
using AurionCal.Api.Services;
using FastEndpoints;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.EntityFrameworkCore;
using Microsoft.Net.Http.Headers;

namespace AurionCal.Api.Endpoints;

public class GetCalendarFeedRequest
{
    public Guid UserId { get; set; }
    public Guid Token { get; set; }
}

public class GetCalendarFeedEndpoint(
    ApplicationDbContext db,
    CalendarService calendarService,
    ISchoolCatalog schools,
    IMemoryCache cache,
    ILogger<GetCalendarFeedEndpoint> logger)
    : Endpoint<GetCalendarFeedRequest>
{
    public override void Configure()
    {
        AllowAnonymous();
        Get("/api/calendar/{UserId:guid}/{Token:guid}.ics");
    }

    public override async Task HandleAsync(GetCalendarFeedRequest r, CancellationToken c)
    {
        var user = await db.Users
            .Include(u => u.RefreshStatus)
            .FirstOrDefaultAsync(u => u.Id == r.UserId, c);


        if (user == null || user.CalendarToken != r.Token)
        {
            await Send.NotFoundAsync(c);
            return;
        }

        var school = schools.GetById(user.SchoolId);
        if (school is null)
        {
            logger.LogError("Unknown school '{SchoolId}' for user {UserId}", user.SchoolId, user.Id);
            await Send.NotFoundAsync(c);
            return;
        }

        bool needsRefresh = !user.LastUpdate.HasValue || (DateTime.UtcNow - user.LastUpdate.Value).TotalHours > 1;
        
        if (user.RefreshStatus?.NextAttemptUtc is { } next && next > DateTime.UtcNow)
        {
            needsRefresh = false;
        }

        var cacheKey = $"planning:{r.UserId}";
        var parisTz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");
        var planningEvents = await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            await db.Entry(user).Collection(u => u.Planning).LoadAsync(c);
            return user.Planning?.Select(e => new Entities.CalendarEvent
            {
                Id = e.Id,
                Title = e.Title,
                Start = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(e.Start.DateTime, DateTimeKind.Utc), parisTz),
                End = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(e.End.DateTime, DateTimeKind.Utc), parisTz),
                ClassName = e.ClassName,
            }).ToList() ?? [];
        });

        var feed = calendarService.GenerateCalendarFeed(planningEvents, school, user.ExamAccommodations && school.SupportsExamAccommodations);

        var contentDisposition = new ContentDispositionHeaderValue("attachment");
        contentDisposition.SetHttpFileName($"Planning {school.Name}.ics");
        HttpContext.Response.Headers.Append(HeaderNames.ContentDisposition, contentDisposition.ToString());
        HttpContext.Response.ContentType = "text/calendar";

        await Send.StringAsync(feed, 200, "text/calendar", c);

        if (needsRefresh)
        {
            _ = Task.Run(async () =>
            {
                await calendarService.RefreshCalendarEventsAsync(r.UserId, CancellationToken.None);
                cache.Remove(cacheKey);
            }, CancellationToken.None);
        }
    }
}
