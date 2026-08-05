using DS.Models;

namespace DS.DTOs
{
    public class GroupDto
    {
        public GroupDto() { }
        public GroupDto(Group group)
        {
            Id = group.Id;
            Name = group.Name;
            District = group.District;
            Patrols = group.Patrols?.Select(p => new PatrolDto(p)).ToList() ?? [];
            Scouts = group.Scouts?.Select(s => new ScoutDto(s)).ToList() ?? [];
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public District District { get; set; }
        public List<PatrolDto> Patrols { get; set; } = [];
        public List<ScoutDto> Scouts { get; set; } = [];
    }
}