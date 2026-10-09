using Hangfire;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace DS.Website.Services
{
    public record MailData(string ToEmail, string ToName, string Subject, string Body);

    public class MailJobs(IOptions<DSSettings> options, ILogger<MailJobs> logger)
    {
        [AutomaticRetry(Attempts = 5, DelaysInSeconds = new[] { 30, 60, 120, 240 })]
        public void Send(MailData data)
        {
            var settings = options.Value;

            try
            {
                using var client = new SmtpClient();
                client.CheckCertificateRevocation = false;
                client.Connect(settings.SMTPHost, 587, SecureSocketOptions.StartTls);

                if (!string.IsNullOrWhiteSpace(settings.SMTPUser) && client.Capabilities.HasFlag(SmtpCapabilities.Authentication))
                {
                    client.Authenticate(settings.SMTPUser, settings.SMTPPassword);
                }

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(settings.SMTPFromName, settings.SMTPFromEmail));
                message.To.Add(new MailboxAddress(data.ToName ?? "", data.ToEmail));
                message.Subject = data.Subject;
                message.Body = new TextPart("plain") { Text = data.Body };

                client.Send(message);
                client.Disconnect(true);

                logger.LogInformation("Sendte mail til {ToEmail}: {Subject}", data.ToEmail, data.Subject);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogWarning(ex, "Mail til {ToEmail} kunne ikke sendes: {Subject}", data.ToEmail, data.Subject);
                throw;
            }
        }
    }
}
