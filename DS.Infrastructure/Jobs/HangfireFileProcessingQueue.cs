using DS.Application.Services.Files;
using Hangfire;

namespace DS.Infrastructure.Jobs
{
    public class HangfireFileProcessingQueue(IBackgroundJobClient backgroundJobs) : IFileProcessingQueue
    {
        public void Enqueue(int fileId)
        {
            backgroundJobs.Enqueue<FileProcessingJobs>(jobs => jobs.ConvertToWebp(fileId));
        }
    }
}
