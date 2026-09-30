using DS.DTOs;

namespace DS.Models;

public class Group : IAuditableEntity, ISoftDeleteable
{
    public Group() {  }
    public Group(GroupDto data)
    {
        Id = data.Id;
        Name = data.Name;
        District = data.District;
    }

    public int Id { get; set; }
    public string Name { get; set; }
    public District District { get; set; }
    public ICollection<Patrol> Patrols { get; set; }
    public ICollection<Scout> Scouts { get; set; }
    public GroupPreSignup PreSignup { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public Scout CreateScout(Scout scout)
    {
        scout.Group = this;
        Scouts.Add(scout);

        return scout;
    }

    public Patrol CreatePatrol(Patrol patrol)
    {
        patrol.Group = this;
        Patrols.Add(patrol);

        return patrol;
    }

    private void EnsureCanAcceptMember(User member)
    {
        if (member.Group != null)
        {
            throw new InvalidOperationException("Brugeren tilhører allerede en gruppe.");
        }
    }

    public void AssignMember(User member)
    {
        EnsureCanAcceptMember(member);
        member.Group = this;
    }
}

public class Patrol
{
    public Patrol() {  }
    public Patrol(CreatePatrolDto data)
    {
        Name = data.Name;
    }

    public int Id { get; set; }
    public string Name { get; set; }
    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;
    public ICollection<PatrolMembership> Memberships { get; set; }

    public void AssignScout(Scout scout)
    {
        if (Memberships.Any(m => m.ScoutId == scout.Id))
        {
            return;
        }

        Memberships.Add(new PatrolMembership
        {
            Patrol = this,
            Scout = scout,
            JoinedDate = DateTime.UtcNow,
            IsPatrolLeader = false
        });
    }

    public void RemoveScout(Scout scout)
    {
        var membership = Memberships.FirstOrDefault(m => m.ScoutId == scout.Id);
        if (membership == null)
        {
            return;
        }

        Memberships.Remove(membership);
    }
}

public class Scout : IAuditableEntity, ISoftDeleteable
{
    public Scout() { }
    public Scout(CreateScoutDto data)
    {
        Name = data.Name;
        Birthday = DateTime.SpecifyKind(data.Birthday, DateTimeKind.Utc);
        Gender = data.Gender;
    }

    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime Birthday { get; set; }
    public Gender Gender { get; set; }
    public Group Group { get; set; }
    public int GroupId { get; set; }
    public ICollection<PatrolMembership> Memberships { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public class PatrolMembership
{
    public int Id { get; set; }
    public int ScoutId { get; set; }
    public Scout Scout { get; set; }
    public int PatrolId { get; set; }
    public Patrol Patrol { get; set; }

    public DateTime JoinedDate { get; set; }
    public bool IsPatrolLeader { get; set; }
}

public enum Gender
{
    Male,
    Female
}

public enum District
{
    DANEHOF,
    FIONIA
}

public class GroupPreSignup
{
    public int Id { get; set; }
    public Group Group { get; set; }
    public int GroupId { get; set; }
    public int Beaver { get; set; }
    public int Wolf { get; set; }
    public int Junior { get; set; }
    public int Trop { get; set; }
    public int Senior { get; set; }
    public int Rover { get; set; }
    public int Leader { get; set; }
}
