using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace DS.Models;

public class Activity
{
    [Key]
    public int Id { get; set; }
    public string Name { get; set; }

    public ActivityBudget Budget { get; set; }
    public CatalogData Catalog { get; set; }
}

[Owned]
public class ActivityBudget
{
    public int Budget { get; set; }
}

[Owned]
public class CatalogData
{
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