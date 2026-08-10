using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DS.Models;

public class Activity
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }

    public int ActivityTeamId { get; set; }
    public ActivityTeam ActivityTeam { get; set; }

    public ActivityBudget Budget { get; set; }
    public CatalogData Catalog { get; set; }
}

public class ActivityTeam
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }

    public List<Activity> Activities { get; set; }
    public List<ActivityTeamMembership> Memberships { get; set; }
}

public class ActivityTeamMembership
{
    [Key]
    public int Id { get; set; }
    public User User { get; set; }
    public int ActivityTeamId { get; set; }
    public ActivityTeam ActivityTeam { get; set; }
    public bool IsAdmin { get; set; }
}

[Owned]
public class ActivityBudget
{
    public int Budget { get; set; }
}

public class CatalogData
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
    public string Summary { get; set; }
    public string Description { get; set; }
    public List<ActivityCategory> Categories { get; set; }
}

public class ActivityCategory
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }
}