using System.Security.Claims;
using AurionCal.Api.Contexts;
using AurionCal.Api.Schools;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AurionCal.Api.Endpoints;

public class UserProfileResponse
{
    public Guid UserId { get; set; }
    public string SchoolId { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string CalendarFeedUrl { get; set; } = string.Empty;
    public DateTime? LastUpdated { get; set; }
    public bool ExamAccommodations { get; set; }
    public bool SupportsExamAccommodations { get; set; }
}

public class GetUserProfileEndpoint(ApplicationDbContext db, ISchoolCatalog schools, IConfiguration config) : EndpointWithoutRequest<UserProfileResponse>
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
            SchoolId = user.SchoolId,
            Email = user.Email,
            CalendarFeedUrl = calendarUrl,
            LastUpdated = user.LastUpdate ?? null,
            ExamAccommodations = user.ExamAccommodations,
            SupportsExamAccommodations = schools.GetById(user.SchoolId)?.SupportsExamAccommodations ?? false
        };

        await Send.OkAsync(response, ct);
    }
}