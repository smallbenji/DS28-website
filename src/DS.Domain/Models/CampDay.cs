namespace DS.Domain.Models
{
    public class CampDay
    {
        public int Id { get; set; }
        public DateOnly Date { get; set; }
        public CampHalf Half { get; set; }
    }

    public enum CampHalf
    {
        First,
        Second,
        Both
    }
}
