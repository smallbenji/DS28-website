using DS.Infrastructure.Jobs;
using Hangfire;
using Microsoft.Extensions.Hosting;

namespace DS.Infrastructure
{
    public class RecurringJobRegistrar(IRecurringJobManager recurringJobs) : IHostedService
    {
        public Task StartAsync(CancellationToken cancellationToken)
        {
            recurringJobs.AddOrUpdate<OpenIddictJobs>(
                "prune-openiddict", job => job.Prune(), Cron.Daily(3, 0));

            return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            return Task.CompletedTask;
        }
    }
}
