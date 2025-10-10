using AurionCal.Api.Contexts;
using AurionCal.Api.Services;
using AurionCal.Api.Services.Interfaces;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AurionCal.Api.Endpoints;

public class GetCalendarFeedRequest
{
    public Guid UserId { get; set; }
    public Guid Token { get; set; }
}

public class GetCalendarFeedEndpoint(
    ApplicationDbContext db,
    CalendarService calendarService)
    : Endpoint<GetCalendarFeedRequest>
{
    
    public override void Configure()
    {
        AllowAnonymous();
        Get("/api/calendar/{UserId:guid}/{Token:guid}.ics");
    }
    
    public override async Task HandleAsync(GetCalendarFeedRequest r, CancellationToken c)
    {
        var user = await db.Users.FindAsync([r.UserId], cancellationToken: c);
        if (user == null || user.CalendarToken != r.Token)
        {
            await Send.NotFoundAsync(c);
            return;
        }

        bool needsRefresh = !user.LastUpdate.HasValue || (DateTime.Now.ToUniversalTime() - user.LastUpdate.Value).TotalHours > 1;
        
        await db.Entry(user).Collection(u => u.Planning).LoadAsync(c);
        var planningEvents = user.Planning?.Select(e => new Entities.CalendarEvent
        {
            Id = e.Id,
            Title = e.Title,
            Start = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(e.Start.DateTime, DateTimeKind.Utc), TimeZoneInfo.Local),
            End = TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(e.End.DateTime, DateTimeKind.Utc), TimeZoneInfo.Local),
            ClassName = e.ClassName,
        }).ToList() ?? new List<Entities.CalendarEvent>();

        var feed = calendarService.GenerateCalendarFeed(planningEvents);

        HttpContext.Response.Headers.Append("Content-Disposition", "attachment; filename=\"calendar.ics\"");
        HttpContext.Response.ContentType = "text/calendar";
        await Send.StringAsync(feed, 200, "text/calendar", c);

        // If needed, refresh data in the background
        if (needsRefresh)
        {
            _ = Task.Run(async () => await calendarService.RefreshCalendarEventsAsync(r.UserId, CancellationToken.None), CancellationToken.None);
        }
    }
}