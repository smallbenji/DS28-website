namespace DS.DTOs
{
    public class ParticipantDayDto
    {
        public string Day { get; set; }
        public string Label { get; set; }
        public int Count { get; set; }
    }

    public class ParticipantStatsDto
    {
        public List<ParticipantDayDto> Days { get; set; } = [];
        public int TotalUniqueParticipants { get; set; }
    }
}
