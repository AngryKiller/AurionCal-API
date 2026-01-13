using AurionCal.Api.Services.Interfaces;
using AurionCal.Api.Templates.Mail;

namespace AurionCal.Api.Services;

/// <summary>
/// Centralise l'envoi du mail d'erreur de fetch après N échecs.
/// </summary>
public class RefreshFailureNotifier(
    IMailTemplateService templateService,
    IEmailSenderService emailSender,
    IConfiguration configuration,
    ILogger<RefreshFailureNotifier> logger)
{
    public async Task SendDataFetchErrorAsync(string toEmail, DateTime? lastUpdatedUtc, CancellationToken c)
    {
        var appUrl = configuration.GetValue<string>("ApiSettings:AppUrl")
                     ?? configuration.GetValue<string>("ApiSettings:Cors")
                     ?? string.Empty;

        if (string.IsNullOrWhiteSpace(appUrl))
        {
            // On préfère quand même envoyer le mail plutôt que de bloquer la notif.
            logger.LogWarning("RefreshFailureNotifier: ApiSettings:AppUrl/FrontUrl/BaseUrl introuvable; le template utilisera une URL vide.");
        }

        var model = new DataFetchError
        {
            AppUrl = appUrl,
            LastUpdated = (lastUpdatedUtc ?? DateTime.UtcNow)
        };

        var html = await templateService.RenderAsync("DataFetchError.cshtml", model);
        await emailSender.SendEmailAsync(toEmail, "AurionCal - Échec de mise à jour de votre emploi du temps", html);
    }
}

