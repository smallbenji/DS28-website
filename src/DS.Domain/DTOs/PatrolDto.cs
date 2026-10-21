using DS.Domain.Models;

namespace DS.Domain.DTOs
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

    public class PatrolMembershipDto
    {
        public PatrolMembershipDto() { }

        public PatrolMembershipDto(PatrolMembership patrolMembership)
        {
            Id = patrolMembership.Id;
            ScoutId = patrolMembership.ScoutId;
            PatrolId = patrolMembership.PatrolId;
            JoinedDate = patrolMembership.JoinedDate;
            IsPatrolLeader = patrolMembership.IsPatrolLeader;
        }

        public int Id { get; set; }
        public int ScoutId { get; set; }
        public int PatrolId { get; set; }
        public DateTime JoinedDate { get; set; }
        public bool IsPatrolLeader { get; set; }
    }

    public class CreatePatrolDto
    {
        public string Name { get; set; }
        public int GroupId { get; set; }
    }
}