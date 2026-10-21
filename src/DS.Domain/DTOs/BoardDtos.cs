namespace DS.Domain.DTOs
{
    public class BoardSummaryDto
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
        public bool IsArchived { get; set; }
        public int CardCount { get; set; }
    }

    public class BoardDetailDto
    {
        public int Id { get; set; }
        public string Key { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
        public bool IsArchived { get; set; }
        public List<BoardColumnDto> Columns { get; set; } = [];
        public List<WorkItemDto> Cards { get; set; } = [];
        public List<BoardColorDto> Colors { get; set; } = [];
    }

    public class BoardColorDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
    }

    public class BoardColorUpdateDto
    {
        public string Name { get; set; }
        public string Color { get; set; }
    }

    public class BoardColumnDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public int Position { get; set; }
        public int? WipLimit { get; set; }
        public bool IsDoneColumn { get; set; }
    }

    public class WorkItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public int ColumnId { get; set; }
        public int Position { get; set; }
        public List<UserSummaryDto> Assignees { get; set; } = [];
        public int? OwnerPositionId { get; set; }
        public string OwnerPositionTitle { get; set; }
        public DateOnly? DueDate { get; set; }
        public int Priority { get; set; }
        public List<WorkItemLinkDto> Links { get; set; } = [];
        public List<WorkItemPlacementDto> Placements { get; set; } = [];
    }

    public class WorkItemLinkDto
    {
        public string EntityType { get; set; }
        public int EntityId { get; set; }
    }

    public class WorkItemPlacementDto
    {
        public int BoardId { get; set; }
        public string BoardTitle { get; set; }
        public string BoardIcon { get; set; }
        public int ColumnId { get; set; }
        public string ColumnTitle { get; set; }
        public bool IsDoneColumn { get; set; }
    }

    public class BoardCreateDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
        public List<string> Columns { get; set; }
    }

    public class BoardUpdateDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
        public bool IsArchived { get; set; }
    }

    public class BoardMemberDto
    {
        public string Id { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
    }

    public class BoardMemberCreateDto
    {
        public string UserId { get; set; }
    }

    public class BoardColumnCreateDto
    {
        public string Title { get; set; }
        public int? WipLimit { get; set; }
        public bool IsDoneColumn { get; set; }
    }

    public class BoardColumnUpdateDto
    {
        public string Title { get; set; }
        public int? WipLimit { get; set; }
        public bool IsDoneColumn { get; set; }
    }

    public class WorkItemCreateDto
    {
        public int ColumnId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public List<string> AssigneeUserIds { get; set; } = [];
        public int? OwnerPositionId { get; set; }
        public DateOnly? DueDate { get; set; }
        public int Priority { get; set; }
        public List<WorkItemLinkDto> Links { get; set; } = [];
    }

    public class WorkItemUpdateDto
    {
        public string Title { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public List<string> AssigneeUserIds { get; set; } = [];
        public int? OwnerPositionId { get; set; }
        public DateOnly? DueDate { get; set; }
        public int Priority { get; set; }
    }

    public class WorkItemMoveDto
    {
        public int ColumnId { get; set; }
        public int Position { get; set; }
    }

    public class WorkItemPlacementCreateDto
    {
        public int ColumnId { get; set; }
    }
}
