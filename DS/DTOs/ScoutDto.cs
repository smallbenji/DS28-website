using DS.Models;

namespace DS.DTOs
{
    public class ScoutDto
    {
        public ScoutDto() { }

        public ScoutDto(Scout scout)
        {
            Id = scout.Id;
            Name = scout.Name;
            Birthday = scout.Birthday;
            Gender = scout.Gender;
            GroupId = scout.GroupId;
            Memberships = scout.Memberships?.Select(m => new PatrolMembershipDto(m)).ToList() ?? [];
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime Birthday { get; set; }
        public Gender Gender { get; set; }
        public int GroupId { get; set; }
        public List<PatrolMembershipDto> Memberships { get; set; } = [];
    }
}