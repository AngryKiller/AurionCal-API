using AurionCal.Api.Contexts;
using AurionCal.Api.Services;
using AurionCal.Api.Services.Interfaces;
using FastEndpoints;
using FastEndpoints.Security;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;

namespace AurionCal.Api.Endpoints;

public class CheckLoginInfoRequest
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class CheckLoginInfoRequestValidator : Validator<CheckLoginInfoRequest>
{
    public CheckLoginInfoRequestValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("L'email est requis.")
            .EmailAddress().WithMessage("Format d'email invalide.");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Le mot de passe est requis.")
            .MinimumLength(6).WithMessage("Le mot de passe doit contenir au moins 6 caractères.");
    }
}

public class CheckLoginInfoEndpoint(
    MauriaApiService apiService,
    ApplicationDbContext db,
    ILogger<CheckLoginInfoEndpoint> logger,
    IConfiguration config,
    CalendarService calendarService,
    IEncryptionService encryptionService)
    : Endpoint<CheckLoginInfoRequest, CheckLoginInfoResponse>
{
    public override void Configure()
    {
        AllowAnonymous();
        Post("/api/check-aurion-auth");
        Validator<CheckLoginInfoRequestValidator>();
    }

    private Task SendProblemAsync(int statusCode, string title, string? detail, CancellationToken c)
    {
        HttpContext.Response.StatusCode = statusCode;
        return HttpContext.Response.WriteAsJsonAsync(new { error = title, detail, traceId = HttpContext.TraceIdentifier }, c);
    }
    
    public override async Task HandleAsync(CheckLoginInfoRequest r, CancellationToken c)
    {
        if (c.IsCancellationRequested)
            return;

        try
        {
            var user = await db.Users
                .FirstOrDefaultAsync(u => u.JuniaEmail == r.Email, c);

            if (user is null)
            {
                await Send.UnauthorizedAsync(c); 
                return;
            }

            var result = await apiService.CheckLoginInfoAsync(r.Email, r.Password, c);

            if (!result.Success)
            {
                await Send.UnauthorizedAsync(c);
                return;
            }

            try
            {
                var currentDecryptedPassword = await encryptionService.DecryptAsync(user.JuniaPassword, c);
                if (currentDecryptedPassword != r.Password)
                {
                    user.JuniaPassword = await encryptionService.EncryptAsync(r.Password, c);
                    await db.SaveChangesAsync(c);
                    logger.LogInformation("Mot de passe mis à jour localement pour {Email}. TraceId={TraceId}", r.Email, HttpContext.TraceIdentifier);
                    _ = Task.Run(async () =>
                    {
                        await calendarService.RefreshCalendarEventsAsync(user.Id, CancellationToken.None);
                    }, CancellationToken.None);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Erreur lors de la vérification/mise à jour du mot de passe local pour {Email}. TraceId={TraceId}", r.Email, HttpContext.TraceIdentifier);
            }

            string jwtToken;
            try
            {
                var expireHours = config.GetValue<int?>("Jwt:ExpireHours") ?? 3;
                var signingKey = config["Jwt:SigningKey"];
                var issuer = config["Jwt:Issuer"]; 
                var audience = config["Jwt:Audience"]; 

                if (string.IsNullOrWhiteSpace(signingKey))
                {
                    logger.LogError("Jwt:SigningKey manquant dans la configuration. TraceId={TraceId}", HttpContext.TraceIdentifier);
                    await SendProblemAsync(500, "Erreur interne", "Une erreur interne est survenue.", c);
                    return;
                }

                jwtToken = JwtBearer.CreateToken(o =>
                {
                    o.SigningKey = signingKey;           
                    o.ExpireAt = DateTime.UtcNow.AddHours(expireHours);
                    if (!string.IsNullOrWhiteSpace(issuer)) o.Issuer = issuer;
                    if (!string.IsNullOrWhiteSpace(audience)) o.Audience = audience;
                    o.User["UserId"] = user.Id.ToString();
                });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Échec de génération du token pour {Email}. TraceId={TraceId}", r.Email, HttpContext.TraceIdentifier);
                await SendProblemAsync(500, "Erreur interne", "Une erreur interne est survenue.", c);
                return;
            }

            await Send.ResponseAsync(
                new CheckLoginInfoResponse
                {
                    IsValid = true,
                    Message = string.Empty,
                    Token = jwtToken
                }, 200, c);
        }
        catch (TaskCanceledException ex) when (!c.IsCancellationRequested)
        {
            logger.LogWarning(ex, "Délai dépassé lors de l'appel externe pour {Email}. TraceId={TraceId}", r.Email, HttpContext.TraceIdentifier);
            await SendProblemAsync(504, "Délai dépassé", "Le service d'authentification a mis trop de temps à répondre.", c);
        }
        catch (OperationCanceledException)
        {
            logger.LogInformation("Requête annulée pour {Email}. TraceId={TraceId}", r.Email, HttpContext.TraceIdentifier);
        }
        catch (HttpRequestException ex)
        {
            logger.LogWarning(ex, "Mauria indisponible pour {Email}. TraceId={TraceId}", r.Email, HttpContext.TraceIdentifier);
            await SendProblemAsync(500, "Erreur interne", "Une erreur interne est survenue.", c);
        }
        catch (DbException ex)
        {
            logger.LogError(ex, "Erreur base de données lors de la vérification pour {Email}. TraceId={TraceId}", r.Email, HttpContext.TraceIdentifier);
            await SendProblemAsync(500, "Erreur interne", "Une erreur interne est survenue.", c);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Erreur inconnue lors de la vérification pour {Email}. TraceId={TraceId}", r.Email, HttpContext.TraceIdentifier);
            await SendProblemAsync(500, "Erreur interne", "Une erreur interne est survenue. Veuillez réessayer plus tard.", c);
        }
    }
    
}

public class CheckLoginInfoResponse
{
    public bool IsValid { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Token { get; set; } = string.Empty;
}