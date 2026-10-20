using DS.Application.Services.Email;
using Hangfire;

namespace DS.Infrastructure.Jobs
{
    public class HangfireMailQueue(IBackgroundJobClient backgroundJobs) : IMailQueue
    {
        public void Enqueue(string toEmail, string toName, string subject, string body)
        {
            backgroundJobs.Enqueue<MailJobs>(jobs => jobs.Send(new MailData(toEmail, toName, subject, body)));
        }
    }
}
