using System.Security.Claims;
using AurionCal.Api.Contexts;
using AurionCal.Api.Schools;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AurionCal.Api.Endpoints;

public class SetWorkPlacementRequest
{
    public bool Enabled { get; set; }
}

public class SetWorkPlacementEndpoint(ApplicationDbContext db, ISchoolCatalog schools) : Endpoint<SetWorkPlacementRequest>
{
    public override void Configure()
    {
        Patch("/api/user/work-placement");
        Claims("UserId");
    }

    public override async Task HandleAsync(SetWorkPlacementRequest r, CancellationToken ct)
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

        if (r.Enabled && schools.GetById(user.SchoolId)?.SupportsWorkPlacement != true)
        {
            AddError("WORK_PLACEMENT_NOT_SUPPORTED");
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, ct);
            return;
        }

        user.ShowWorkPlacement = r.Enabled;
        await db.SaveChangesAsync(ct);
        await Send.OkAsync(ct);
    }
}
