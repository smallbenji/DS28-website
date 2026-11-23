namespace DS.Domain.Models;

public class Board : IAuditableEntity, ISoftDeleteable
{
    public int Id { get; set; }
    public string Key { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Icon { get; set; }
    public string Color { get; set; }
    public bool IsArchived { get; set; }
    public bool IsDeveloperBoard { get; set; }

    public string CreatedByUserId { get; set; }
    public User CreatedBy { get; set; }
    public List<BoardColumn> Columns { get; set; } = [];
    public List<BoardMember> Members { get; set; } = [];
    public List<BoardTag> Tags { get; set; } = [];
    public List<BoardColor> Colors { get; set; } = [];

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}

public class BoardTag
{
    public int Id { get; set; }
    public int BoardId { get; set; }
    public Board Board { get; set; }
    public string Name { get; set; }
    public string Color { get; set; }
}

public class BoardCardTag
{
    public int BoardId { get; set; }
    public int BoardCardId { get; set; }
    public BoardCard BoardCard { get; set; }
    public int BoardTagId { get; set; }
    public BoardTag BoardTag { get; set; }
}

public class BoardColor
{
    public int Id { get; set; }
    public int BoardId { get; set; }
    public Board Board { get; set; }
    public string Name { get; set; }
    public string Color { get; set; }
}

public class BoardMember
{
    public int BoardId { get; set; }
    public Board Board { get; set; }
    public string UserId { get; set; }
    public User User { get; set; }
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
    public string Color { get; set; }

    public List<WorkItemAssignee> Assignees { get; set; } = [];

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

public class WorkItemAssignee
{
    public int WorkItemId { get; set; }
    public WorkItem WorkItem { get; set; }
    public string UserId { get; set; }
    public User User { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class WorkItemDependency
{
    public int WorkItemId { get; set; }
    public WorkItem WorkItem { get; set; }
    public int DependsOnWorkItemId { get; set; }
    public WorkItem DependsOnWorkItem { get; set; }
}

public class BoardCard
{
    public List<BoardCardPullRequest> PullRequests { get; set; } = [];
    public List<BoardCardTag> Tags { get; set; } = [];
    public int Id { get; set; }
    public int BoardId { get; set; }
    public Board Board { get; set; }
    public int ColumnId { get; set; }
    public BoardColumn Column { get; set; }
    public int WorkItemId { get; set; }
    public WorkItem WorkItem { get; set; }
    public int Position { get; set; }
    public bool IsEpic { get; set; }
    public int? EpicBoardCardId { get; set; }
    public BoardCard EpicBoardCard { get; set; }
}

public class BoardCardPullRequest
{
    public int BoardCardId { get; set; }
    public string Url { get; set; }
}

public class WorkItemLink
{
    public int Id { get; set; }
    public int WorkItemId { get; set; }
    public WorkItem WorkItem { get; set; }
    public string EntityType { get; set; }
    public int EntityId { get; set; }
}
