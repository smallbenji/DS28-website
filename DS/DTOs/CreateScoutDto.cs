using DS.Models;

namespace DS.DTOs
{
    public class CreateScoutDto
    {
        public string Name { get; set; }
        public DateTime Birthday { get; set; }
        public Gender Gender { get; set; }
        public int GroupId { get; set; }
    }
}
