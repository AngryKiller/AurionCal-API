using AurionCal.Api.Contexts;
using AurionCal.Api.Services;
using FastEndpoints;
using Microsoft.Extensions.Caching.Memory;

namespace AurionCal.Api.Endpoints;

public class GetCalendarFeedRequest
{
    public Guid UserId { get; set; }
    public Guid Token { get; set; }
}

public class GetCalendarFeedEndpoint(
    ApplicationDbContext db,
    CalendarService calendarService,
    IMemoryCache cache)
    : Endpoint<GetCalendarFeedRequest>
{
    public override void Configure()
    {
        AllowAnonymous();
        Get("/api/calendar/{UserId:guid}/{Token:guid}.ics");
    }

    public override async Task HandleAsync(GetCalendarFeedRequest r, CancellationToken c)
    {
        var user = await db.Users.FindAsync([r.UserId], c);
        if (user == null || user.CalendarToken != r.Token)
        {
            await Send.NotFoundAsync(c);
            return;
        }

        bool needsRefresh = !user.LastUpdate.HasValue || (DateTime.UtcNow - user.LastUpdate.Value).TotalHours > 1;

        var cacheKey = $"planning:{r.UserId}";
        var planningEvents = await cache.GetOrCreateAsync(cacheKey, async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(5);
            await db.Entry(user).Collection(u => u.Planning).LoadAsync(c);
            return user.Planning?.Select(e => new Entities.CalendarEvent
            {
                Id = e.Id,
                Title = e.Title,
                Start = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(e.Start.DateTime, DateTimeKind.Utc), TimeZoneInfo.Local),
                End = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(e.End.DateTime, DateTimeKind.Utc), TimeZoneInfo.Local),
                ClassName = e.ClassName,
            }).ToList() ?? [];
        });

        var feed = calendarService.GenerateCalendarFeed(planningEvents);

        HttpContext.Response.Headers.Append("Content-Disposition", "attachment; filename=\"Planning Junia.ics\"");
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
