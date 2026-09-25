using DS.Models;
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
            using var client = new SmtpClient();
            client.Connect(options.Value.SMTPHost, 587, SecureSocketOptions.StartTls);
            client.Authenticate(options.Value.SMTPUser, options.Value.SMTPPassword);
            message.From.Add(new MailboxAddress(options.Value.SMTPFromName, options.Value.SMTPFromEmail));
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

        public void SendResetPasswordMail()
        {
            var message = new MimeMessage();
            message.Subject = "Hello, World!";

            message.Body = new TextPart("plain")
            {
                Text =
@$"Hej {message.To.FirstOrDefault().Name} 

Hermed sendes din kode til vores applikation.

Indsæt link her

Mhv. DS28 teamet
                "
            };

            SendMail(message);
        }
    }
}