using AurionCal.Api.Contexts;
using AurionCal.Api.Entities;
using AurionCal.Api.Services;
using FastEndpoints;
using Microsoft.EntityFrameworkCore;

namespace AurionCal.Api.Endpoints;

public class RegisterUserRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class RegisterUserEndpoint(ApplicationDbContext db, MauriaApiService apiService)
    : Endpoint<RegisterUserRequest, RegisterUserResponse>
{
    public override void Configure()
    {
        AllowAnonymous();
        Post("/api/register");
    }
    
    public override async Task HandleAsync(RegisterUserRequest r, CancellationToken c)
    {
        var result = await apiService.CheckLoginInfoAsync(r.Email, r.Password, c);

        if (result.Success)
        {
            var exists = await db.Users.FirstOrDefaultAsync(u => u.JuniaEmail == r.Email, cancellationToken: c);
            if (exists != null)
            {
                await Send.ForbiddenAsync(c);
                return;
            }
            var user = new User
            {
                Id = Guid.NewGuid(),
                JuniaEmail = r.Email,
                JuniaPassword = r.Password,
                CalendarToken = Guid.NewGuid()
            };
            db.Users.Add(user);
            await db.SaveChangesAsync(c);
            await Send.ResponseAsync(
                new RegisterUserResponse { UserId = user.Id }, 200, c);
        }
        else
        {
            await Send.UnauthorizedAsync(c);
        }

    }
}

public class RegisterUserResponse
{
    public Guid UserId { get; set; }
}