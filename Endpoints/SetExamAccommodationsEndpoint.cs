using System.Security.Claims;
using AurionCal.Api.Contexts;
using AurionCal.Api.Schools;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AurionCal.Api.Endpoints;

public class SetExamAccommodationsRequest
{
    public bool Enabled { get; set; }
}

public class SetExamAccommodationsEndpoint(ApplicationDbContext db, ISchoolCatalog schools) : Endpoint<SetExamAccommodationsRequest>
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

        if (r.Enabled && schools.GetById(user.SchoolId)?.SupportsExamAccommodations != true)
        {
            AddError("EXAM_ACCOMMODATIONS_NOT_SUPPORTED");
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, ct);
            return;
        }

        user.ExamAccommodations = r.Enabled;
        await db.SaveChangesAsync(ct);
        await Send.OkAsync(ct);
    }
}
