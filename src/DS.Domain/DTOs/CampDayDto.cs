using DS.Domain.Models;

namespace DS.Domain.DTOs
{
    public class CampDayDto
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public CampHalf Half { get; set; }
    }

    public class CampDayUpsertDto
    {
        public DateOnly? Date { get; set; }
        public CampHalf? Half { get; set; }
    }
}
