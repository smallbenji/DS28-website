using DS.Models;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace DS.Website.Services
{
    public class EmailService(IOptions<DSSettings> options)
    {
        private void SendMail(MimeMessage message)
        {
            var settings = options.Value;

            using var client = new SmtpClient();
            client.CheckCertificateRevocation = false;
            client.Connect(settings.SMTPHost, 587, SecureSocketOptions.StartTls);

            if (!string.IsNullOrWhiteSpace(settings.SMTPUser) && client.Capabilities.HasFlag(SmtpCapabilities.Authentication))
            {
                client.Authenticate(settings.SMTPUser, settings.SMTPPassword);
            }

            message.From.Add(new MailboxAddress(settings.SMTPFromName, settings.SMTPFromEmail));
            client.Send(message);
            client.Disconnect(true);
        }

        public void SendInvitation(UserInvitation invitation)
        {
            var message = new MimeMessage();
            message.To.Add(new MailboxAddress("", invitation.Email));
            message.Subject = "Du er blevet inviteret til DS28";

            var path = invitation.GroupId.HasValue ? "group-invitation" : "invitation";
            var baseUri = new Uri(options.Value.PublicBaseUrl, UriKind.Absolute);
            var link = new Uri(baseUri, $"/{path}/{invitation.InvitationId}").AbsoluteUri;

            message.Body = new TextPart("plain") 
            {
                Text = 
@$"Du er blevet inviteret til DS28 systemet.

For at komme i gang skal du oprette en bruger ved brug af følgende link:
{link}

Mvh. DS28 teamet"
            };

            SendMail(message);
        }

        public void SendResetPasswordMail(User user, string token)
        {
            var message = new MimeMessage();
            message.To.Add(new MailboxAddress(user.GetFullName(), user.Email));
            message.Subject = "Nulstil din adgangskode i DS28";

            var baseUri = new Uri(options.Value.PublicBaseUrl, UriKind.Absolute);
            var link = new Uri(baseUri, $"/reset-password/{user.Id}?token={Uri.EscapeDataString(token)}").AbsoluteUri;

            message.Body = new TextPart("plain")
            {
                Text =
$@"Hej {user.GetFullName()}

Du har bedt om at nulstille din adgangskode. Følg dette link for at vælge en ny adgangskode:
{link}

Linket er gyldigt i 24 timer. Hvis du ikke selv har bedt om nulstillingen, kan du ignorere denne mail. Din adgangskode er stadig uændret.

Mvh. DS28 teamet
                "
            };

            SendMail(message);
        }
    }
}