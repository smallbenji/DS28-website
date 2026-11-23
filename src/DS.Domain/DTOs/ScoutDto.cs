using DS.Domain.Models;

namespace DS.Domain.DTOs
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
            CampDayIds = scout.CampDays?.Select(day => day.CampDayId).Order().ToList() ?? [];
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public DateTime Birthday { get; set; }
        public Gender Gender { get; set; }
        public int GroupId { get; set; }
        public List<PatrolMembershipDto> Memberships { get; set; } = [];
        public List<int> CampDayIds { get; set; } = [];
    }

    public class CreateScoutDto
    {
        public string Name { get; set; }
        public DateTime Birthday { get; set; }
        public Gender Gender { get; set; }
        public int GroupId { get; set; }
        public List<int> CampDayIds { get; set; } = [];
    }

    public class SetScoutCampDaysDto
    {
        public List<int> ScoutIds { get; set; } = [];
        public List<int> CampDayIds { get; set; } = [];
        public bool OnlyScoutsWithoutDays { get; set; }
    }
}