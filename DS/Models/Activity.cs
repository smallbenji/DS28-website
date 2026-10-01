using DS.DTOs;

namespace DS.Models;

public class Activity : IAuditableEntity, ISoftDeleteable
{
    public Activity() { }
    public Activity(ActivityDto data)
    {
        Name = data.Name;
    }

    public int Id { get; set; }
    public string Name { get; set; }

    public int ActivityTeamId { get; set; }
    public ActivityTeam ActivityTeam { get; set; }

    public ActivityBudget Budget { get; set; }
    public CatalogData Catalog { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public class ActivityTeam : IAuditableEntity, ISoftDeleteable
{
    public int Id { get; set; }
    public string Name { get; set; }

    public List<Activity> Activities { get; set; }
    public List<ActivityTeamMembership> Memberships { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public class ActivityTeamMembership
{
    public int Id { get; set; }
    public string UserId { get; set; }
    public User User { get; set; }
    public int ActivityTeamId { get; set; }
    public ActivityTeam ActivityTeam { get; set; }
    public bool IsAdmin { get; set; }
}

public class ActivityBudget
{
    public int Id { get; set; }
    public int Budget { get; set; }

    public int ActivityId { get; set; }
    public Activity Activity { get; set; }
}

public class CatalogData
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string Summary { get; set; }
    public string Description { get; set; }

    public int ActivityId { get; set; }
    public Activity Activity { get; set; }

    public List<ActivityCategory> Categories { get; set; }
}

public class ActivityCategory
{
    public int Id { get; set; }
    public string Name { get; set; }
}

public class ActivityTimeslot
{
    public int Id { get; set; }
    public int ActivityId { get; set; }
    public Activity Activity { get; set; }
    public DateTime StartTime { get; set; }
    public int Duration { get; set; }
}