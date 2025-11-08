using System.Security.Claims;
using AurionCal.Api.Contexts;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AurionCal.Api.Endpoints;

public class UserProfileResponse
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = string.Empty;
    public string CalendarFeedUrl { get; set; } = string.Empty;
}

public class GetUserProfileEndpoint(ApplicationDbContext db, IConfiguration config) : EndpointWithoutRequest<UserProfileResponse>
{
    public override void Configure()
    {
        Get("/api/user/profile");
        Claims("UserId");
    }
    
    public override async Task HandleAsync(CancellationToken ct)
    {
        var userIdValue = User.FindFirstValue("UserId");
        if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }


        var baseUrl = string.IsNullOrWhiteSpace(HttpContext.Request.Host.Host)
            ? config.GetValue<string>("ApiSettings:BaseUrl")?.TrimEnd('/')
            : $"{HttpContext.Request.Scheme}://{HttpContext.Request.Host}";

        var calendarUrl = $"{baseUrl}/api/calendar/{user.Id}/{user.CalendarToken}.ics";

        var response = new UserProfileResponse
        {
            UserId = user.Id,
            Email = user.JuniaEmail,
            CalendarFeedUrl = calendarUrl
        };

        await Send.OkAsync(response, ct);
    }
}