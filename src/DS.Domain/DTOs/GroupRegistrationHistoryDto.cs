namespace DS.Domain.DTOs
{
    public class GroupRegistrationHistoryDto
    {
        public List<HistoricPresignupDto> Presignups { get; set; } = [];
        public List<HistoricSignupDto> Signups { get; set; } = [];
    }

    public class HistoricPresignupDto
    {
        public int CampYear { get; set; }
        public string Category { get; set; }
        public int ParticipantCount { get; set; }
        public int GuestCount { get; set; }
    }

    public class HistoricSignupDto
    {
        public int CampYear { get; set; }
        public string Category { get; set; }
        public int DayIndex { get; set; }
        public int ParticipantCount { get; set; }
    }
}
