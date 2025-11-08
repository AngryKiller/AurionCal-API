using System.Security.Claims;
using AurionCal.Api.Contexts;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AurionCal.Api.Endpoints;

public class DeleteUserEndpoint(ApplicationDbContext db, ILogger<DeleteUserEndpoint> logger) : EndpointWithoutRequest
{
    public override void Configure()
    {
        Delete("/api/user/delete");
        Claims("UserId");
    }
    
    public override async Task HandleAsync(CancellationToken ct)
    {
        if (ct.IsCancellationRequested)
            return;

        var userIdValue = User.FindFirstValue("UserId");
        if (string.IsNullOrWhiteSpace(userIdValue) || !Guid.TryParse(userIdValue, out var userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        try
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Id == userId, ct);
            if (user is null)
            {
                await Send.NotFoundAsync(ct);
                return;
            }
            
            db.Users.Remove(user);
            await db.SaveChangesAsync(ct);
            
            await Send.OkAsync(new { message = "Utilisateur supprimé." }, ct);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Suppression annulée pour l'utilisateur {UserId}", userId);
        }
        catch (DbUpdateException ex)
        {
            logger.LogError(ex, "Erreur lors de la suppression de l'utilisateur {UserId}", userId);
            await Send.ErrorsAsync(500, ct);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur inattendue lors de la suppression de l'utilisateur {UserId}", userId);
            await Send.ErrorsAsync(500, ct);
        }
    }
}