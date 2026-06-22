using System.Security.Claims;
using AurionCal.Api.Contexts;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AurionCal.Api.Endpoints;

public class SetExamAccommodationsRequest
{
    public bool Enabled { get; set; }
}

public class SetExamAccommodationsEndpoint(ApplicationDbContext db) : Endpoint<SetExamAccommodationsRequest>
{
    public override void Configure()
    {
        Patch("/api/user/exam-accommodations");
        Claims("UserId");
    }

    public override async Task HandleAsync(SetExamAccommodationsRequest r, CancellationToken ct)
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

        user.ExamAccommodations = r.Enabled;
        await db.SaveChangesAsync(ct);
        await Send.OkAsync(ct);
    }
}
