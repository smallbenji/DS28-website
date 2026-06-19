using Microsoft.Extensions.Options;
using MimeKit;
using MailKit.Net.Smtp;

namespace DS
{
    public class DSMailer(IOptions<DSSettings> options)
    {
        public MimeMessage CreateMessage()
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(options.Value.SMTPFromName, options.Value.SMTPFromEmail));

            return message;
        }

        public async Task SendMail(MimeMessage message)
        {
            using (var client = new SmtpClient())
            {
                await client.ConnectAsync(options.Value.SMTPHost, 587, MailKit.Security.SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(options.Value.SMTPUser, options.Value.SMTPPassword);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);
            }
        }
    }
}