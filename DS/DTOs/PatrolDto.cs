using DS.Models;

namespace DS.DTOs
{
    public class PatrolDto
    {
        public PatrolDto() { }

        public PatrolDto(Patrol patrol)
        {
            Id = patrol.Id;
            Name = patrol.Name;
            GroupId = patrol.GroupId;
            Memberships = patrol.Memberships?.Select(m => new PatrolMembershipDto(m)).ToList() ?? [];
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public int GroupId { get; set; }
        public List<PatrolMembershipDto> Memberships { get; set; } = [];
    }
}