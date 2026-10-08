using AurionCal.Api.Contexts;
using AurionCal.Api.Entities;
using AurionCal.Api.Schools;
using AurionCal.Api.Services;
using AurionCal.Api.Services.Interfaces;
using FastEndpoints;
using FluentValidation;
using Microsoft.EntityFrameworkCore;

namespace AurionCal.Api.Endpoints;

public class RegisterUserRequest
{
    /// <summary>School id; the default school is used when absent.</summary>
    public string? SchoolId { get; set; }
    public string Email { get; set; }
    public string Password { get; set; }
}

public class RegisterUserEndpoint(ApplicationDbContext db, IMauriaClient mauriaClient, ISchoolCatalog schools, IEncryptionService keyVaultService,
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
                .MinimumLength(6).WithMessage("Le mot de passe doit contenir au moins 6 caractères.");
        }
    }
    
    public override async Task HandleAsync(RegisterUserRequest r, CancellationToken c)
    {
        var email = EmailNormalizer.Normalize(r.Email);

        var school = schools.Resolve(r.SchoolId);
        if (school is null)
        {
            AddError(x => x.SchoolId!, "UNKNOWN_SCHOOL");
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, c);
            return;
        }

        if (!schools.EmailMatches(school, email))
        {
            AddError(x => x.Email, "EMAIL_DOMAIN_NOT_ALLOWED");
            await Send.ErrorsAsync(StatusCodes.Status400BadRequest, c);
            return;
        }

        var result = await mauriaClient.CheckLoginAsync(school, email, r.Password, c);

        if (result.Success)
        {
            var exists = await db.Users.AnyAsync(u => u.Email == email, cancellationToken: c);
            if (exists)
            {
                AddError(x => x.Email, "ACCOUNT_ALREADY_EXISTS");
                await Send.ErrorsAsync(StatusCodes.Status409Conflict, c);
                return;
            }

            var user = new User
            {
                Id = Guid.NewGuid(),
                SchoolId = school.Id,
                Email = email,
                Password = await keyVaultService.EncryptAsync(r.Password, c),
                CalendarToken = Guid.NewGuid()
            };
            db.Users.Add(user);
            try
            {
                await db.SaveChangesAsync(c);
            }
            catch (DbUpdateException ex) when (ex.InnerException?.Message.Contains("unique", StringComparison.OrdinalIgnoreCase) ?? false)
            {
                AddError(x => x.Email, "ACCOUNT_ALREADY_EXISTS");
                await Send.ErrorsAsync(StatusCodes.Status409Conflict, c);
                return;
            }
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