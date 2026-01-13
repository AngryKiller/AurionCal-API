namespace AurionCal.Api.Services.Interfaces;

public interface IEmailSenderService
{
    Task SendEmailAsync(string to, string subject, string htmlBody);
}