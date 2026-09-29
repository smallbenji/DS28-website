namespace DS.DTOs
{
    public class EmailOutboxDto
    {
        public int Id { get; set; }
        public string EventType { get; set; }
        public Guid? CorrelationId { get; set; }
        public string ToEmail { get; set; }
        public string Subject { get; set; }
        public string Body { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime NextAttemptAt { get; set; }
        public DateTime? SentAt { get; set; }
        public DateTime? FailedAt { get; set; }
        public DateTime? LockedAt { get; set; }
        public string LockedBy { get; set; }
        public int Attempts { get; set; }
        public string LastError { get; set; }
    }

    public class EmailOutboxPageDto
    {
        public List<EmailOutboxDto> Items { get; set; }
        public int Total { get; set; }
        public int PendingCount { get; set; }
        public int LockedCount { get; set; }
        public int FailedCount { get; set; }
    }
}
