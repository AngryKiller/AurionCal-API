using AurionCal.Api.Contexts;
using AurionCal.Api.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AurionCal.Api.Endpoints;

public class GetDayWindowRequest
{
    public Guid UserId { get; set; }
    public Guid Token { get; set; }
    public string? Date { get; set; }
}

public class DayWindowResponse
{
    public string Date { get; set; } = string.Empty;
    public bool HasClasses { get; set; }
    public DateTimeOffset? DayStart { get; set; }
    public DateTimeOffset? DayEnd { get; set; }
}

public class GetDayWindowEndpoint(
    ApplicationDbContext db,
    ILogger<GetDayWindowEndpoint> logger)
    : Endpoint<GetDayWindowRequest, DayWindowResponse>
{
    private static readonly TimeZoneInfo ParisTz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Paris");

    public override void Configure()
    {
        AllowAnonymous();
        Get("/api/calendar/{UserId:guid}/{Token:guid}/day");
    }

    public override async Task HandleAsync(GetDayWindowRequest r, CancellationToken c)
    {
        DateOnly date;
        if (string.IsNullOrWhiteSpace(r.Date))
        {
            date = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(DateTimeOffset.UtcNow, ParisTz).DateTime);
        }
        else if (!DateOnly.TryParseExact(r.Date, "yyyy-MM-dd", out date))
        {
            logger.LogInformation("Day window: invalid date format '{Date}' for {UserId}", r.Date, r.UserId);
            await Send.ErrorsAsync(400, c);
            return;
        }

        var user = await db.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == r.UserId, c);

        if (user is null || user.CalendarToken != r.Token)
        {
            logger.LogWarning("Day window: user or token invalid (UserId={UserId})", r.UserId);
            await Send.NotFoundAsync(c);
            return;
        }

        var (startUtc, endUtc) = DayWindowCalculator.GetUtcRange(date, ParisTz);

        var dayEvents = await db.CalendarEvents
            .AsNoTracking()
            .Where(e => e.UserId == r.UserId && e.Start >= startUtc && e.Start < endUtc)
            .Select(e => new { e.Start, e.End })
            .ToListAsync(c);

        if (dayEvents.Count == 0)
        {
            logger.LogInformation("Day window: no classes for {UserId} on {Date}", r.UserId, date);
            await Send.OkAsync(new DayWindowResponse
            {
                Date = date.ToString("yyyy-MM-dd"),
                HasClasses = false,
                DayStart = null,
                DayEnd = null
            }, c);
            return;
        }

        var firstStart = dayEvents.Min(e => e.Start);
        var lastEnd = dayEvents.Max(e => e.End);

        await Send.OkAsync(new DayWindowResponse
        {
            Date = date.ToString("yyyy-MM-dd"),
            HasClasses = true,
            DayStart = TimeZoneInfo.ConvertTime(firstStart, ParisTz),
            DayEnd = TimeZoneInfo.ConvertTime(lastEnd, ParisTz)
        }, c);
    }
}