using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DS.Models;

public class Group
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public int Id { get; set; }
    public string Name { get; set; }
    public ICollection<Patrol> Patrols { get; set; }
    public ICollection<Scout> Scouts { get; set; }
}

public class Patrol
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public int GroupId { get; set; }
    public Group Group { get; set; } = null!;
    public ICollection<PatrolMembership> Memberships { get; set; }
}

public class Scout
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public DateTime Birthday { get; set; }
    public Gender Gender { get; set; }
    public Group Group { get; set; }
    public int GroupId { get; set; }
    public ICollection<PatrolMembership> Memberships { get; set; }
}

public class PatrolMembership
{
    [Key]
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