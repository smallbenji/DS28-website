using DS.Application.Data;
using DS.Application.Services.Notifications;
using DS.Domain;
using DS.Domain.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MimeKit;

namespace DS.Application.Services.Email
{
    public class EmailService(IOptions<DSSettings> options, IMailQueue mailQueue, IDataDbContext dataDb, ILogger<EmailService> logger)
    {
        public void QueueInvitationMail(UserInvitation invitation)
        {
            var subject = "Du er blevet inviteret til DS28";

            var path = invitation.GroupId.HasValue ? "group-invitation" : "invitation";
            var baseUri = new Uri(options.Value.PublicBaseUrl, UriKind.Absolute);
            var link = new Uri(baseUri, $"/{path}/{invitation.InvitationId}").AbsoluteUri;

            var body =
@$"Du er blevet inviteret til DS28 systemet.

For at komme i gang skal du oprette en bruger ved brug af følgende link:
{link}

Mvh. DS28 teamet";

            Enqueue(new MailboxAddress("", invitation.Email), subject, body);
        }

        public void QueueNewUserNotificationMail(User user)
        {
            var subject = "Ny bruger oprettet i DS28";

            var body =
@$"Der er oprettet en ny bruger i DS28.

Navn: {user.GetFullName()}
E-mail: {user.Email}
Brugernavn: {user.UserName}

Mvh. DS28 teamet";

            var recipients = dataDb.NotificationPreferences
                .Where(p => p.NotificationType == NotificationType.NewUser)
                .Select(p => p.User.Email)
                .Where(email => email != null)
                .Distinct()
                .ToList();

            foreach (var recipient in recipients)
            {
                Enqueue(new MailboxAddress("", recipient), subject, body);
            }
        }

        public void QueueBoardCreatedNotificationMail(Board board, User creator)
        {
            var subject = $"Ny tavle oprettet i DS28: {board.Title}";

            var baseUri = new Uri(options.Value.PublicBaseUrl, UriKind.Absolute);
            var link = new Uri(baseUri, $"/board/{board.Id}").AbsoluteUri;
            var creatorName = creator?.GetFullName();
            if (string.IsNullOrWhiteSpace(creatorName)) creatorName = "Ukendt bruger";

            var body =
@$"Der er oprettet en ny tavle i DS28.

Tavle: {board.Title}
Oprettet af: {creatorName}
Link: {link}

Mvh. DS28 teamet";

            var allowedGroups = NotificationPermissions.AllowedGroups(NotificationType.BoardCreated);
            var recipients = dataDb.NotificationPreferences
                .Where(p => p.NotificationType == NotificationType.BoardCreated)
                .Where(p => p.UserId != board.CreatedByUserId)
                .Where(p => dataDb.UserRoles.Any(userRole => userRole.UserId == p.UserId
                    && dataDb.Roles.Any(role => role.Id == userRole.RoleId && allowedGroups.Contains(role.Name))))
                .Select(p => p.User.Email)
                .Where(email => email != null)
                .Distinct()
                .ToList();

            foreach (var recipient in recipients)
            {
                Enqueue(new MailboxAddress("", recipient), subject, body);
            }
        }

        public void QueueResetPasswordMail(User user, string token)
        {
            var subject = "Nulstil din adgangskode i DS28";

            var baseUri = new Uri(options.Value.PublicBaseUrl, UriKind.Absolute);
            var link = new Uri(baseUri, $"/reset-password/{user.Id}?token={Uri.EscapeDataString(token)}").AbsoluteUri;

            var body =
$@"Hej {user.GetFullName()}

Du har bedt om at nulstille din adgangskode i DS28. Følg dette link for at vælge en ny adgangskode:
{link}

Linket er gyldigt i 24 timer. Hvis du ikke selv har bedt om nulstillingen, kan du ignorere denne mail. Din adgangskode er stadig uændret.

Mvh. DS28 teamet
                ";

            Enqueue(new MailboxAddress(user.GetFullName(), user.Email), subject, body);
        }

        private void Enqueue(MailboxAddress recipient, string subject, string body)
        {
            if (!options.Value.NotificationsEnabled)
            {
                logger.LogWarning("Springer mail til {ToEmail} over: NotificationsEnabled er slået fra.", recipient.Address);
                return;
            }

            mailQueue.Enqueue(recipient.Address, recipient.Name, subject, body);
            logger.LogInformation("Stillede mail til {ToEmail} i køen: {Subject}", recipient.Address, subject);
        }
    }
}
