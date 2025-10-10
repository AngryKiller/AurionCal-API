using AurionCal.Api.Contexts;
using AurionCal.Api.Services;
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
    MauriaApiService apiService,
    CalendarService calendarService,
    ILogger<GetCalendarFeedEndpoint> logger,
    IServiceScopeFactory scopeFactory)
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
            _ = Task.Run(async () =>
            {
                try
                {
                    using var scope = scopeFactory.CreateScope();
                    var scopedDb = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                    var scopedApiService = scope.ServiceProvider.GetRequiredService<MauriaApiService>();
                    // Reload user
                    var scopedUser = await scopedDb.Users.Include(u => u.Planning).FirstOrDefaultAsync(u => u.Id == r.UserId, CancellationToken.None);
                    if (scopedUser == null) return;
                    var events = await scopedApiService.GetPlanningAsync(scopedUser.JuniaEmail, scopedUser.JuniaPassword, CancellationToken.None);
                    await scopedDb.CalendarEvents.Where(e => e.User.Id == scopedUser.Id).ExecuteDeleteAsync(CancellationToken.None);
                    if (events is { Success: true, Data: not null })
                    {
                        scopedUser.Planning = events.Data?.Select(e => new Entities.CalendarEvent
                        {
                            Id = e.Id,
                            Title = e.Title,
                            Start = e.Start.ToUniversalTime(),
                            End = e.End.ToUniversalTime(),
                            ClassName = e.ClassName,
                        }).ToList()!;
                        scopedUser.LastUpdate = DateTime.Now.ToUniversalTime();
                        await scopedDb.SaveChangesAsync(CancellationToken.None);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Erreur lors du rafraîchissement asynchrone du planning pour l'utilisateur {UserId}", r.UserId);
                }
            }, c);
        }
    }
}