using DS.Models;

namespace DS.DTOs
{
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
}