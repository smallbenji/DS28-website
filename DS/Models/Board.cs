namespace DS.Models;

public class Board : IAuditableEntity, ISoftDeleteable
{
    public int Id { get; set; }
    public string Key { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; }
    public string Color { get; set; }
    public bool IsArchived { get; set; }
    public string RequiredRole { get; set; }

    public string CreatedByUserId { get; set; }
    public User CreatedBy { get; set; }
    public List<BoardColumn> Columns { get; set; } = [];

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public class BoardColumn : IAuditableEntity
{
    public int Id { get; set; }
    public int BoardId { get; set; }
    public Board Board { get; set; }
    public string Title { get; set; }
    public int Position { get; set; }
    public int? WipLimit { get; set; }
    public bool IsDoneColumn { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public class WorkItem : IAuditableEntity, ISoftDeleteable
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }

    public string AssigneeUserId { get; set; }
    public User Assignee { get; set; }

    public int? OwnerPositionId { get; set; }
    public OrgPosition OwnerPosition { get; set; }

    public DateOnly? DueDate { get; set; }
    public int Priority { get; set; }

    public string CreatedByUserId { get; set; }
    public User CreatedBy { get; set; }

    public List<WorkItemLink> Links { get; set; } = [];

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public class BoardCard
{
    public int Id { get; set; }
    public int BoardId { get; set; }
    public Board Board { get; set; }
    public int ColumnId { get; set; }
    public BoardColumn Column { get; set; }
    public int WorkItemId { get; set; }
    public WorkItem WorkItem { get; set; }
    public int Position { get; set; }
}

public class WorkItemLink
{
    public int Id { get; set; }
    public int WorkItemId { get; set; }
    public WorkItem WorkItem { get; set; }
    public string EntityType { get; set; }
    public int EntityId { get; set; }
}
