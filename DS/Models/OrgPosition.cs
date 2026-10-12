namespace DS.Models;

public class OrgPosition : IAuditableEntity, ISoftDeleteable
{
    public int Id { get; set; }
    public string Title { get; set; }
    public int? ParentPositionId { get; set; }
    public OrgPosition Parent { get; set; }
    public bool IsSystem { get; set; }
    public List<OrgPositionGroup> Groups { get; set; } = [];
    public List<OrgPositionHolder> Holders { get; set; } = [];

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public class OrgPositionGroup
{
    public int PositionId { get; set; }
    public OrgPosition Position { get; set; }
    public string GroupName { get; set; }
}

public class OrgPositionHolder
{
    public int PositionId { get; set; }
    public OrgPosition Position { get; set; }
    public string UserId { get; set; }
    public User User { get; set; }
}
