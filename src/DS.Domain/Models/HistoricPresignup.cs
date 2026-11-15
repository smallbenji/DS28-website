using System;

namespace DS.Domain.Models
{
    public class HistoricPresignup
    {
        public int GroupId { get; set; }
        public Group Group { get; set; }
        public int CampYear { get; set; }
        public string Category { get; set; }
        public int ParticipantCount { get; set; }
        public int GuestCount { get; set; }
        public string SourceDocumentId { get; set; }
        public string SourceSha256 { get; set; }
        public DateTime ImportedAt { get; set; }
    }
}
