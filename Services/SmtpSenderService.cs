using AurionCal.Api.Services.Interfaces;
using MailKitSimplified.Sender.Abstractions;

namespace AurionCal.Api.Services;

public class SmtpSenderService(ISmtpSender smtpSender, IConfiguration configuration) : IEmailSenderService
{
    public async Task SendEmailAsync(string to, string subject, string htmlBody)
    {
        smtpSender.SmtpClient.CheckCertificateRevocation = false;
        await smtpSender.WriteEmail
            .From(configuration["EmailSender:Sender:Name"], configuration["EmailSender:Sender:Email"])
            .To(to)
            .Subject(subject)
            .BodyHtml(htmlBody)
            .SendAsync();
    }

}

