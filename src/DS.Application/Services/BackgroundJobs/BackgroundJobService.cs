using System.Text.Json;
using DS.Domain.DTOs;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Hangfire.Storage;
using Hangfire.Storage.Monitoring;

namespace DS.Application.Services.BackgroundJobs
{
    public class BackgroundJobService(JobStorage storage, IBackgroundJobClient backgroundJobs, IRecurringJobManager recurringJobs)
    {
        public const int HistoryPageSize = 25;

        private const int ListLimit = 500;
        private const int FailedLimit = 50;
        private const int ActivityCount = 15;
        private const string RetryReasonPrefix = "Retry attempt";

        private static readonly Dictionary<string, string> JobTitles = new()
        {
            ["MailJobs.Send"] = "Send mail",
            ["FileProcessingJobs.ConvertToWebp"] = "Konvertér billede til WebP",
            ["HistoricRegistrationSyncJob.Run"] = "Synkronisér historiske tilmeldinger",
            ["OpenIddictJobs.Prune"] = "Ryd udløbne login-tokens"
        };

        private static readonly Dictionary<string, string> RecurringTitles = new()
        {
            ["prune-openiddict"] = "Ryd udløbne login-tokens"
        };

        public BackgroundJobOverviewDto GetOverview(bool canManage)
        {
            var monitoring = storage.GetMonitoringApi();
            var statistics = monitoring.GetStatistics();

            using var connection = storage.GetConnection();

            var failed = monitoring.FailedJobs(0, FailedLimit)
                .Where(job => job.Value != null)
                .Select(job => new FailedBackgroundJobDto
                {
                    Id = job.Key,
                    Title = TitleOf(job.Value.Job, job.Value.InvocationData),
                    ExceptionType = ShortTypeName(job.Value.ExceptionType),
                    ExceptionMessage = BackgroundJobArgumentMasker.MaskText(job.Value.ExceptionMessage),
                    FailedAt = job.Value.FailedAt,
                    Attempts = AttemptsOf(connection, job.Key)
                })
                .ToList();

            var recurring = connection.GetRecurringJobs()
                .Select(job => new RecurringBackgroundJobDto
                {
                    Id = job.Id,
                    Title = RecurringTitles.GetValueOrDefault(job.Id, TitleOf(job.Job, null)),
                    Cron = job.Cron,
                    TimeZoneId = job.TimeZoneId,
                    NextExecution = job.NextExecution,
                    LastExecution = job.LastExecution,
                    LastJobId = job.LastJobId,
                    LastJobState = job.LastJobState,
                    Error = job.Error
                })
                .OrderBy(job => job.Title)
                .ToList();

            return new BackgroundJobOverviewDto
            {
                CanManage = canManage,
                Statistics = new BackgroundJobStatisticsDto
                {
                    Processing = statistics.Processing,
                    Enqueued = statistics.Enqueued,
                    Scheduled = statistics.Scheduled,
                    Failed = statistics.Failed,
                    SucceededLast24Hours = monitoring.HourlySucceededJobs().Values.Sum()
                },
                Failed = failed,
                Recurring = recurring,
                Activity = GetActivity(monitoring)
            };
        }

        public BackgroundJobHistoryDto GetHistory(string state, string type, string search, int page)
        {
            var monitoring = storage.GetMonitoringApi();
            var includeAll = string.IsNullOrEmpty(state) || state == "all";
            var items = new List<BackgroundJobHistoryItemDto>();

            if (includeAll || state == "succeeded")
            {
                items.AddRange(monitoring.SucceededJobs(0, ListLimit)
                    .Where(job => job.Value != null)
                    .Select(job => HistoryItem(job.Key, TitleOf(job.Value.Job, job.Value.InvocationData), "succeeded", job.Value.SucceededAt, DurationOf(job.Value))));
            }

            if (includeAll || state == "failed")
            {
                items.AddRange(monitoring.FailedJobs(0, ListLimit)
                    .Where(job => job.Value != null)
                    .Select(job => HistoryItem(job.Key, TitleOf(job.Value.Job, job.Value.InvocationData), "failed", job.Value.FailedAt, null)));
            }

            if (includeAll || state == "deleted")
            {
                items.AddRange(monitoring.DeletedJobs(0, ListLimit)
                    .Where(job => job.Value != null)
                    .Select(job => HistoryItem(job.Key, TitleOf(job.Value.Job, job.Value.InvocationData), "deleted", job.Value.DeletedAt, null)));
            }

            var types = items.Select(item => item.Title).Distinct().Order().ToList();
            var jobId = search?.Trim().TrimStart('#');

            var filtered = items
                .Where(item => string.IsNullOrEmpty(type) || item.Title == type)
                .Where(item => string.IsNullOrEmpty(jobId) || item.Id.Contains(jobId))
                .OrderByDescending(item => item.FinishedAt)
                .ToList();

            var pageCount = Math.Max(1, (int)Math.Ceiling(filtered.Count / (double)HistoryPageSize));
            var currentPage = Math.Clamp(page, 0, pageCount - 1);
            var pageItems = filtered.Skip(currentPage * HistoryPageSize).Take(HistoryPageSize).ToList();

            using var connection = storage.GetConnection();
            foreach (var item in pageItems)
            {
                item.Attempts = AttemptsOf(connection, item.Id);
            }

            return new BackgroundJobHistoryDto
            {
                Total = filtered.Count,
                Page = currentPage,
                PageSize = HistoryPageSize,
                Types = types,
                Items = pageItems
            };
        }

        public ServiceResult<BackgroundJobDetailsDto> GetDetails(string id)
        {
            var details = storage.GetMonitoringApi().JobDetails(id);
            if (details == null) return new ServiceResult(ServiceOutcome.NotFound, "Jobbet findes ikke.");

            using var connection = storage.GetConnection();

            var history = details.History ?? [];
            var current = history.FirstOrDefault();
            var processing = history.FirstOrDefault(entry => entry.StateName == ProcessingState.StateName);
            var failure = current?.StateName == FailedState.StateName ? current : null;
            var isFinished = current != null
                && (current.StateName == SucceededState.StateName || current.StateName == FailedState.StateName || current.StateName == DeletedState.StateName);

            var result = new BackgroundJobDetailsDto
            {
                Id = id,
                Title = TitleOf(details.Job, details.InvocationData),
                Method = MethodOf(details.Job, details.InvocationData),
                Queue = details.Job?.Queue ?? EnqueuedQueueOf(history) ?? EnqueuedState.DefaultQueue,
                State = current?.StateName.ToLowerInvariant(),
                CreatedAt = details.CreatedAt,
                StartedAt = processing?.CreatedAt,
                FinishedAt = isFinished ? current.CreatedAt : null,
                Server = ValueOf(processing?.Data, "ServerId"),
                Attempts = AttemptsOf(connection, id),
                AttemptHistory = AttemptHistoryOf(history),
                ExceptionType = ShortTypeName(ValueOf(failure?.Data, "ExceptionType")),
                ExceptionMessage = BackgroundJobArgumentMasker.MaskText(ValueOf(failure?.Data, "ExceptionMessage")),
                StackTrace = BackgroundJobArgumentMasker.MaskText(ValueOf(failure?.Data, "ExceptionDetails"))
            };

            if (details.Job == null)
            {
                result.ArgumentsUnavailable = true;
            }
            else
            {
                result.Arguments = ArgumentsOf(details.Job);
            }

            return new ServiceResult<BackgroundJobDetailsDto>(ServiceOutcome.Success, result);
        }

        public ServiceResult Requeue(string id)
        {
            if (!backgroundJobs.Requeue(id)) return new ServiceResult(ServiceOutcome.NotFound, "Jobbet findes ikke.");

            return new ServiceResult(ServiceOutcome.Success);
        }

        public int RequeueFailed()
        {
            return storage.GetMonitoringApi().FailedJobs(0, ListLimit)
                .Count(job => backgroundJobs.Requeue(job.Key));
        }

        public ServiceResult Delete(string id)
        {
            if (!backgroundJobs.Delete(id)) return new ServiceResult(ServiceOutcome.NotFound, "Jobbet findes ikke.");

            return new ServiceResult(ServiceOutcome.Success);
        }

        public ServiceResult TriggerRecurring(string id)
        {
            using var connection = storage.GetConnection();

            var exists = connection.GetRecurringJobs().Any(job => job.Id == id);
            if (!exists) return new ServiceResult(ServiceOutcome.NotFound, "Det tilbagevendende job findes ikke.");

            recurringJobs.Trigger(id);

            return new ServiceResult(ServiceOutcome.Success);
        }

        private static List<BackgroundJobActivityDto> GetActivity(IMonitoringApi monitoring)
        {
            var active = new List<BackgroundJobActivityDto>();

            active.AddRange(monitoring.ProcessingJobs(0, ActivityCount)
                .Where(job => job.Value != null)
                .Select(job => Activity(job.Key, TitleOf(job.Value.Job, job.Value.InvocationData), "processing", job.Value.StartedAt, null)));

            active.AddRange(monitoring.Queues()
                .SelectMany(queue => queue.FirstJobs)
                .Where(job => job.Value != null)
                .OrderBy(job => job.Value.EnqueuedAt)
                .Select(job => Activity(job.Key, TitleOf(job.Value.Job, job.Value.InvocationData), "enqueued", job.Value.EnqueuedAt, null)));

            active.AddRange(monitoring.ScheduledJobs(0, ActivityCount)
                .Where(job => job.Value != null)
                .OrderBy(job => job.Value.EnqueueAt)
                .Select(job => Activity(job.Key, TitleOf(job.Value.Job, job.Value.InvocationData), "scheduled", job.Value.EnqueueAt, null)));

            var finished = new List<BackgroundJobActivityDto>();

            finished.AddRange(monitoring.SucceededJobs(0, ActivityCount)
                .Where(job => job.Value != null)
                .Select(job => Activity(job.Key, TitleOf(job.Value.Job, job.Value.InvocationData), "succeeded", job.Value.SucceededAt, DurationOf(job.Value))));

            finished.AddRange(monitoring.FailedJobs(0, ActivityCount)
                .Where(job => job.Value != null)
                .Select(job => Activity(job.Key, TitleOf(job.Value.Job, job.Value.InvocationData), "failed", job.Value.FailedAt, null)));

            finished.AddRange(monitoring.DeletedJobs(0, ActivityCount)
                .Where(job => job.Value != null)
                .Select(job => Activity(job.Key, TitleOf(job.Value.Job, job.Value.InvocationData), "deleted", job.Value.DeletedAt, null)));

            return active
                .Concat(finished.OrderByDescending(item => item.At))
                .Take(ActivityCount)
                .ToList();
        }

        private static BackgroundJobActivityDto Activity(string id, string title, string state, DateTime? at, long? durationMs)
        {
            return new BackgroundJobActivityDto
            {
                Id = id,
                Title = title,
                State = state,
                At = at,
                DurationMs = durationMs
            };
        }

        private static BackgroundJobHistoryItemDto HistoryItem(string id, string title, string state, DateTime? finishedAt, long? durationMs)
        {
            return new BackgroundJobHistoryItemDto
            {
                Id = id,
                Title = title,
                State = state,
                FinishedAt = finishedAt,
                DurationMs = durationMs,
                Attempts = 1
            };
        }

        private static List<BackgroundJobAttemptDto> AttemptHistoryOf(IList<StateHistoryDto> history)
        {
            var failures = history
                .Where(entry => entry.StateName == FailedState.StateName
                    || entry.StateName == ScheduledState.StateName && entry.Reason?.StartsWith(RetryReasonPrefix) == true)
                .OrderBy(entry => entry.CreatedAt)
                .ToList();

            return failures
                .Select((entry, index) => new BackgroundJobAttemptDto
                {
                    Number = index + 1,
                    At = entry.CreatedAt,
                    Text = BackgroundJobArgumentMasker.MaskText(AttemptText(entry))
                })
                .OrderByDescending(attempt => attempt.Number)
                .ToList();
        }

        private static string AttemptText(StateHistoryDto entry)
        {
            if (entry.StateName == FailedState.StateName) return ValueOf(entry.Data, "ExceptionMessage") ?? entry.Reason;

            var separator = entry.Reason.IndexOf(": ", StringComparison.Ordinal);

            return separator < 0 ? entry.Reason : entry.Reason[(separator + 2)..];
        }

        private static List<BackgroundJobArgumentDto> ArgumentsOf(Job job)
        {
            var parameters = job.Method.GetParameters();
            var arguments = new List<BackgroundJobArgumentDto>();

            for (var index = 0; index < job.Args.Count; index++)
            {
                var name = index < parameters.Length ? parameters[index].Name : $"arg{index}";
                var argument = job.Args[index];

                try
                {
                    var element = JsonSerializer.SerializeToElement(argument, argument?.GetType() ?? typeof(object));
                    arguments.AddRange(BackgroundJobArgumentMasker.Mask(name, element));
                }
                catch (NotSupportedException)
                {
                    arguments.Add(new BackgroundJobArgumentDto { Name = name, Hidden = true });
                }
            }

            return arguments;
        }

        private static string EnqueuedQueueOf(IList<StateHistoryDto> history)
        {
            var enqueued = history.FirstOrDefault(entry => entry.StateName == EnqueuedState.StateName);

            return ValueOf(enqueued?.Data, "Queue");
        }

        private static int AttemptsOf(IStorageConnection connection, string id)
        {
            var retryCount = connection.GetJobParameter(id, "RetryCount");

            return int.TryParse(retryCount, out var retries) ? retries + 1 : 1;
        }

        private static long? DurationOf(SucceededJobDto job)
        {
            var performance = ValueOf(job.StateData, "PerformanceDuration");
            if (long.TryParse(performance, out var duration)) return duration;

            return job.TotalDuration;
        }

        private static string ValueOf(IDictionary<string, string> data, string key)
        {
            if (data == null) return null;

            return data.TryGetValue(key, out var value) ? value : null;
        }

        private static string TitleOf(Job job, InvocationData invocationData)
        {
            var method = MethodOf(job, invocationData);
            if (method == null) return "Ukendt job";

            return JobTitles.GetValueOrDefault(method, method);
        }

        private static string MethodOf(Job job, InvocationData invocationData)
        {
            if (job != null) return $"{job.Type.Name}.{job.Method.Name}";

            if (string.IsNullOrEmpty(invocationData?.Type) || string.IsNullOrEmpty(invocationData.Method)) return null;

            var typeName = invocationData.Type.Split(',')[0];
            var shortName = typeName[(typeName.LastIndexOfAny(['.', '+']) + 1)..];

            return $"{shortName}.{invocationData.Method}";
        }

        private static string ShortTypeName(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return typeName;

            var dot = typeName.LastIndexOf('.');

            return dot < 0 ? typeName : typeName[(dot + 1)..];
        }
    }
}
