namespace DS.Domain.DTOs
{
    public class BackgroundJobOverviewDto
    {
        public bool CanManage { get; set; }
        public BackgroundJobStatisticsDto Statistics { get; set; }
        public List<FailedBackgroundJobDto> Failed { get; set; } = [];
        public List<RecurringBackgroundJobDto> Recurring { get; set; } = [];
        public List<BackgroundJobActivityDto> Activity { get; set; } = [];
    }

    public class BackgroundJobStatisticsDto
    {
        public long Processing { get; set; }
        public long Enqueued { get; set; }
        public long Scheduled { get; set; }
        public long Failed { get; set; }
        public long SucceededLast24Hours { get; set; }
    }

    public class FailedBackgroundJobDto
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string ExceptionType { get; set; }
        public string ExceptionMessage { get; set; }
        public DateTime? FailedAt { get; set; }
        public int Attempts { get; set; }
    }

    public class RecurringBackgroundJobDto
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Cron { get; set; }
        public string TimeZoneId { get; set; }
        public DateTime? NextExecution { get; set; }
        public DateTime? LastExecution { get; set; }
        public string LastJobId { get; set; }
        public string LastJobState { get; set; }
        public string Error { get; set; }
    }

    public class BackgroundJobActivityDto
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string State { get; set; }
        public DateTime? At { get; set; }
        public long? DurationMs { get; set; }
    }

    public class BackgroundJobHistoryDto
    {
        public int Total { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public List<string> Types { get; set; } = [];
        public List<BackgroundJobHistoryItemDto> Items { get; set; } = [];
    }

    public class BackgroundJobHistoryItemDto
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string State { get; set; }
        public DateTime? FinishedAt { get; set; }
        public long? DurationMs { get; set; }
        public int Attempts { get; set; }
    }

    public class BackgroundJobDetailsDto
    {
        public string Id { get; set; }
        public string Title { get; set; }
        public string Method { get; set; }
        public string Queue { get; set; }
        public string State { get; set; }
        public DateTime? CreatedAt { get; set; }
        public DateTime? StartedAt { get; set; }
        public DateTime? FinishedAt { get; set; }
        public string Server { get; set; }
        public int Attempts { get; set; }
        public bool ArgumentsUnavailable { get; set; }
        public List<BackgroundJobArgumentDto> Arguments { get; set; } = [];
        public List<BackgroundJobAttemptDto> AttemptHistory { get; set; } = [];
        public string ExceptionType { get; set; }
        public string ExceptionMessage { get; set; }
        public string StackTrace { get; set; }
    }

    public class BackgroundJobArgumentDto
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool Masked { get; set; }
        public bool Hidden { get; set; }
        public int? Size { get; set; }
    }

    public class BackgroundJobAttemptDto
    {
        public int Number { get; set; }
        public DateTime At { get; set; }
        public string Text { get; set; }
    }
}
