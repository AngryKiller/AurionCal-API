using AurionCal.Api.Contexts;
using AurionCal.Api.Entities;
using AurionCal.Api.Services;
using AurionCal.Api.Services.Interfaces;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AurionCal.Api.Endpoints;

public class RegisterUserRequest
{
    public string Email { get; set; }
    public string Password { get; set; }
}

public class RegisterUserEndpoint(ApplicationDbContext db, MauriaApiService apiService, IEncryptionService keyVaultService,
    CalendarService calendarService)
    : Endpoint<RegisterUserRequest, RegisterUserResponse>
{
    public override void Configure()
    {
        AllowAnonymous();
        Post("/api/register");
    }
    
    public class RegisterUserRequestValidator : Validator<RegisterUserRequest>
    {
        public RegisterUserRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("L'email est requis.")
                .EmailAddress().WithMessage("Format d'email invalide.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("Le mot de passe est requis.")
                .MinimumLength(4).WithMessage("Le mot de passe doit contenir au moins 4 caractères.");
        }
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
                JuniaPassword = await keyVaultService.EncryptAsync(r.Password, c),
                CalendarToken = Guid.NewGuid()
            };
            db.Users.Add(user);
            await db.SaveChangesAsync(c);
            await Send.ResponseAsync(
                new RegisterUserResponse { UserId = user.Id }, 200, c);
            _ = Task.Run(async () => await calendarService.RefreshCalendarEventsAsync(user.Id, CancellationToken.None), CancellationToken.None);
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