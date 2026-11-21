import type { UserSummaryDto } from './user.types';

export interface BoardSummaryDto {
    id: number;
    key: string;
    title: string;
    description: string | null;
    icon: string | null;
    color: string | null;
    isArchived: boolean;
    cardCount: number;
}

export interface BoardDetailDto {
    isDeveloperBoard: boolean;
    id: number;
    key: string;
    title: string;
    description: string | null;
    icon: string | null;
    color: string | null;
    isArchived: boolean;
    columns: BoardColumnDto[];
    cards: WorkItemDto[];
    colors: BoardColorDto[];
    tags: BoardTagDto[];
}

export interface BoardTagDto {
    id: number;
    name: string;
    color: string | null;
}

export interface BoardTagUpdateDto {
    name: string;
    color: string | null;
}

export interface BoardColorDto {
    id: number;
    name: string;
    color: string;
}

export interface BoardColorUpdateDto {
    name: string;
    color: string;
}

export interface BoardColumnDto {
    id: number;
    title: string;
    position: number;
    wipLimit: number | null;
    isDoneColumn: boolean;
}

export interface WorkItemDto {
    pullRequests: string[];
    tags: BoardTagDto[];
    id: number;
    title: string;
    description: string | null;
    color: string | null;
    columnId: number;
    position: number;
    assignees: UserSummaryDto[];
    ownerPositionId: number | null;
    ownerPositionTitle: string | null;
    dueDate: string | null;
    priority: number;
    links: WorkItemLinkDto[];
    placements: WorkItemPlacementDto[];
    waitingOn: WorkItemDependencyDto[];
    blocking: WorkItemDependencyDto[];
}

export interface WorkItemDependencyDto {
    id: number;
    isVisible: boolean;
    title: string | null;
    isDone: boolean;
    placements: WorkItemPlacementDto[];
}

export interface WorkItemSearchResultDto {
    id: number;
    title: string;
    placements: WorkItemPlacementDto[];
}

export interface WorkItemLinkDto {
    entityType: string;
    entityId: number;
}

export interface WorkItemPlacementDto {
    boardId: number;
    boardTitle: string;
    boardIcon: string | null;
    columnId: number;
    columnTitle: string;
    isDoneColumn: boolean;
}

export interface BoardCreateDto {
    title: string;
    description: string | null;
    icon: string | null;
    color: string | null;
    columns: string[];
}

export interface BoardUpdateDto {
    isDeveloperBoard?: boolean;
    title: string;
    description: string | null;
    icon: string | null;
    color: string | null;
    isArchived: boolean;
}

export interface BoardColumnCreateDto {
    title: string;
    wipLimit: number | null;
    isDoneColumn: boolean;
}

export interface BoardColumnUpdateDto {
    title: string;
    wipLimit: number | null;
    isDoneColumn: boolean;
}

export interface WorkItemCreateDto {
    newTagNames?: string[];
    tagIds?: number[];
    columnId: number;
    title: string;
    description: string | null;
    color: string | null;
    assigneeUserIds: string[];
    ownerPositionId: number | null;
    dueDate: string | null;
    priority: number;
    links: WorkItemLinkDto[];
}

export interface WorkItemUpdateDto {
    newTagNames?: string[];
    tagIds?: number[];
    title: string;
    description: string | null;
    color: string | null;
    assigneeUserIds: string[];
    ownerPositionId: number | null;
    dueDate: string | null;
    priority: number;
}

export interface WorkItemMoveDto {
    columnId: number;
    position: number;
}

export interface WorkItemPlacementCreateDto {
    columnId: number;
}

export const boardPriorityOptions: { value: string; label: string }[] = [
    { value: '-1', label: 'Lav' },
    { value: '0', label: 'Normal' },
    { value: '1', label: 'Høj' },
];

export function boardPriorityLabel(priority: string | number): string {
    return boardPriorityOptions.find((option) => option.value === String(priority))?.label ?? 'Normal';
}

export interface BoardMemberDto {
    id: string;
    firstName: string;
    lastName: string;
    email: string;
    assignedCardCount?: number | null;
}
