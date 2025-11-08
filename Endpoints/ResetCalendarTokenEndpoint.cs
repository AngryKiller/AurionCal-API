using System.Security.Claims;
using AurionCal.Api.Contexts;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AurionCal.Api.Endpoints;

public class ResetCalendarTokenEndpoint(ApplicationDbContext db) : EndpointWithoutRequest
{
    
    public override void Configure()
    {
        Get("/api/user/reset-calendar-token");
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

        var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
        if (user is null)
        {
            await Send.NotFoundAsync(ct);
            return;
        }
        
        user.CalendarToken = Guid.NewGuid();
        await db.SaveChangesAsync(ct);
        await Send.OkAsync(ct);
    }
}