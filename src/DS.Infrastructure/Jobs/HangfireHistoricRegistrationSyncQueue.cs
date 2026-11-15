using DS.Application.Services.HistoricRegistration;
using DS.Domain;
using Hangfire;
using Microsoft.Extensions.Options;

namespace DS.Infrastructure.Jobs
{
    public class HangfireHistoricRegistrationSyncQueue(IBackgroundJobClient backgroundJobs, IOptions<DSSettings> options) : IHistoricRegistrationSyncQueue
    {
        private HistoricRegistrationSettings Settings => options.Value.HistoricRegistration;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(Settings.FirebaseCredentials)
            && Settings.Year is >= 1900 and <= 9999;

        public void Enqueue()
        {
            backgroundJobs.Enqueue<HistoricRegistrationSyncJob>(job => job.Run());
        }
    }
}
