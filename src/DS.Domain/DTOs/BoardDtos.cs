using DS.Domain.Models;

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
        public bool IsDeveloperBoard { get; set; }
        public int Id { get; set; }
        public string Key { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Icon { get; set; }
        public string Color { get; set; }
        public bool IsArchived { get; set; }
        public List<BoardColumnDto> Columns { get; set; } = [];
        public List<WorkItemDto> Cards { get; set; } = [];
        public List<BoardTagDto> Tags { get; set; } = [];
        public List<BoardColorDto> Colors { get; set; } = [];
    }

    public class BoardTagDto
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Color { get; set; }
    }

    public class BoardTagUpdateDto
    {
        public string Name { get; set; }
        public string Color { get; set; }
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
        public List<string> PullRequests { get; set; } = [];
        public List<BoardTagDto> Tags { get; set; } = [];
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public int ColumnId { get; set; }
        public int Position { get; set; }
        public List<WorkItemAssigneeDto> Assignees { get; set; } = [];
        public int? OwnerPositionId { get; set; }
        public string OwnerPositionTitle { get; set; }
        public DateOnly? DueDate { get; set; }
        public int Priority { get; set; }
        public List<WorkItemLinkDto> Links { get; set; } = [];
        public List<WorkItemPlacementDto> Placements { get; set; } = [];
        public List<WorkItemDependencyDto> WaitingOn { get; set; } = [];
        public List<WorkItemDependencyDto> Blocking { get; set; } = [];
        public bool IsEpic { get; set; }
        public int? EpicCardId { get; set; }
        public EpicProgressDto EpicProgress { get; set; }
    }

    public class EpicProgressDto
    {
        public int Done { get; set; }
        public int Total { get; set; }
    }

    public class WorkItemEpicLinkDto
    {
        public int EpicWorkItemId { get; set; }
    }

    public class WorkItemAssigneeDto : UserSummaryDto
    {
        public WorkItemAssigneeDto() { }

        public WorkItemAssigneeDto(WorkItemAssignee assignee) : base(assignee.User)
        {
            CompletedAt = assignee.CompletedAt;
        }

        public DateTime? CompletedAt { get; set; }
    }

    public class WorkItemAssigneeCreateDto
    {
        public string UserId { get; set; }
    }

    public class PullRequestLinkDto
    {
        public string Url { get; set; }
    }

    public class WorkItemDependencyDto
    {
        public int Id { get; set; }
        public bool IsVisible { get; set; }
        public string Title { get; set; }
        public bool IsDone { get; set; }
        public List<WorkItemPlacementDto> Placements { get; set; } = [];
    }

    public class WorkItemDependencyCreateDto
    {
        public int DependsOnWorkItemId { get; set; }
    }

    public class WorkItemSearchResultDto
    {
        public int Id { get; set; }
        public string Title { get; set; }
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
        public bool? IsDeveloperBoard { get; set; }
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
        public ImageReferenceDto ProfilePicture { get; set; }
        public int? AssignedCardCount { get; set; }
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
        public List<string> NewTagNames { get; set; } = [];
        public List<int> TagIds { get; set; } = [];
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
        public List<string> NewTagNames { get; set; } = [];
        public List<int> TagIds { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Color { get; set; }
        public List<string> AssigneeUserIds { get; set; }
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
