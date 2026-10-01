using DS.Data;
using DS.Models;
using MailKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using MimeKit;

namespace DS.Website.Services
{
    public class EmailOutboxWorker(IServiceScopeFactory scopeFactory, IOptions<DSSettings> options, ILogger<EmailOutboxWorker> logger) : BackgroundService
    {
        private const int BatchSize = 25;
        private const int MaxAttempts = 5;
        private const int MaxErrorLength = 500;

        private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan LockTimeout = TimeSpan.FromMinutes(5);
        private static readonly TimeSpan MaxBackoff = TimeSpan.FromHours(1);

        private SmtpClient client;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var instanceId = $"{Environment.MachineName}:{Environment.ProcessId}:{Guid.NewGuid()}";
            logger.LogInformation("Email outbox worker startet som {InstanceId}", instanceId);

            await ReleaseStaleLocksAsync(stoppingToken);

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var processed = options.Value.NotificationsEnabled
                        ? await ProcessBatchAsync(instanceId, stoppingToken)
                        : 0;

                    if (processed == 0)
                    {
                        await Task.Delay(PollInterval, stoppingToken);
                    }
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Behandling af mailkøen fejlede. Næste forsøg om {PollInterval}.", PollInterval);
                    await Task.Delay(PollInterval, stoppingToken);
                }
            }

            client?.Dispose();
            logger.LogInformation("Email outbox worker stoppet.");
        }

        private async Task<int> ProcessBatchAsync(string instanceId, CancellationToken stoppingToken)
        {
            using var scope = scopeFactory.CreateScope();
            var dataDb = scope.ServiceProvider.GetRequiredService<DataDbContext>();

            var now = DateTime.UtcNow;

            var candidateIds = await dataDb.EmailOutbox
                .Where(m => m.SentAt == null && m.LockedAt == null && m.Attempts < MaxAttempts && m.NextAttemptAt <= now)
                .OrderBy(m => m.NextAttemptAt).ThenBy(m => m.Id)
                .Select(m => m.Id)
                .Take(BatchSize)
                .ToListAsync(stoppingToken);

            if (candidateIds.Count == 0) return 0;

            var claimed = await dataDb.EmailOutbox
                .Where(m => candidateIds.Contains(m.Id) && m.SentAt == null && m.LockedAt == null)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(m => m.LockedAt, now)
                    .SetProperty(m => m.LockedBy, instanceId), stoppingToken);

            if (claimed == 0) return 0;

            var messages = await dataDb.EmailOutbox
                .Where(m => m.SentAt == null && m.LockedAt != null && m.LockedBy == instanceId)
                .OrderBy(m => m.NextAttemptAt).ThenBy(m => m.Id)
                .ToListAsync(stoppingToken);

            foreach (var message in messages)
            {
                await RecordOutcomeAsync(dataDb, message, Send(message, options.Value), stoppingToken);
            }

            return messages.Count;
        }

        private async Task RecordOutcomeAsync(DataDbContext dataDb, EmailOutbox message, Exception failure, CancellationToken stoppingToken)
        {
            message.LockedAt = null;
            message.LockedBy = null;

            if (failure == null)
            {
                message.SentAt = DateTime.UtcNow;
                logger.LogInformation("Sendte mail {Id} ({EventType}) til {ToEmail}", message.Id, message.EventType, message.ToEmail);
            }
            else
            {
                client?.Dispose();
                client = null;

                message.Attempts++;
                message.LastError = Truncate(failure.Message);

                if (message.Attempts >= MaxAttempts)
                {
                    message.FailedAt = DateTime.UtcNow;
                    logger.LogError(failure, "Mail {Id} til {ToEmail} fejlede {Attempts} gange og gives op.", message.Id, message.ToEmail, message.Attempts);
                }
                else
                {
                    message.NextAttemptAt = DateTime.UtcNow.Add(Backoff(message.Attempts));
                    logger.LogWarning(failure, "Mail {Id} til {ToEmail} fejlede. Næste forsøg {NextAttemptAt}.", message.Id, message.ToEmail, message.NextAttemptAt);
                }
            }

            await dataDb.SaveChangesAsync(stoppingToken);
        }

        private Exception Send(EmailOutbox message, DSSettings settings)
        {
            try
            {
                EnsureConnected(settings);

                var mail = new MimeMessage();
                mail.From.Add(new MailboxAddress(settings.SMTPFromName, settings.SMTPFromEmail));
                mail.To.Add(new MailboxAddress("", message.ToEmail));
                mail.Subject = message.Subject;
                mail.Body = new TextPart("plain") { Text = message.Body };

                if (message.CorrelationId.HasValue)
                {
                    mail.MessageId = $"<{message.CorrelationId.Value:N}@ds28>";
                }

                client.Send(mail);
                return null;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                return ex;
            }
        }

        private void EnsureConnected(DSSettings settings)
        {
            if (client?.IsConnected == true) return;

            client?.Dispose();
            client = new SmtpClient();
            client.CheckCertificateRevocation = false;
            client.Connect(settings.SMTPHost, 587, SecureSocketOptions.StartTls);

            if (!string.IsNullOrWhiteSpace(settings.SMTPUser) && client.Capabilities.HasFlag(SmtpCapabilities.Authentication))
            {
                client.Authenticate(settings.SMTPUser, settings.SMTPPassword);
            }
        }

        private async Task ReleaseStaleLocksAsync(CancellationToken stoppingToken)
        {
            using var scope = scopeFactory.CreateScope();
            var dataDb = scope.ServiceProvider.GetRequiredService<DataDbContext>();

            var staleBefore = DateTime.UtcNow.Subtract(LockTimeout);

            var released = await dataDb.EmailOutbox
                .Where(m => m.LockedAt != null && m.LockedAt < staleBefore)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(m => m.LockedAt, (DateTime?)null)
                    .SetProperty(m => m.LockedBy, (string)null), stoppingToken);

            if (released > 0)
            {
                logger.LogWarning("Frigjorde {Count} mailrækker med udløbet lås ved start.", released);
            }
        }

        private static TimeSpan Backoff(int attempts)
        {
            return TimeSpan.FromSeconds(Math.Min(Math.Pow(2, attempts) * 15, MaxBackoff.TotalSeconds));
        }

        private static string Truncate(string value)
        {
            return value.Length <= MaxErrorLength ? value : value[..MaxErrorLength];
        }
    }
}
