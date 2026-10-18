import type { ImageReferenceDto } from './file.types';

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
    id: number;
    key: string;
    title: string;
    description: string | null;
    icon: string | null;
    color: string | null;
    isArchived: boolean;
    columns: BoardColumnDto[];
    cards: WorkItemDto[];
}

export interface BoardColumnDto {
    id: number;
    title: string;
    position: number;
    wipLimit: number | null;
    isDoneColumn: boolean;
}

export interface WorkItemDto {
    id: number;
    title: string;
    description: string | null;
    columnId: number;
    position: number;
    assigneeUserId: string | null;
    assigneeName: string | null;
    assigneeProfilePicture: ImageReferenceDto | null;
    ownerPositionId: number | null;
    ownerPositionTitle: string | null;
    dueDate: string | null;
    priority: number;
    links: WorkItemLinkDto[];
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
    columnId: number;
    title: string;
    description: string | null;
    assigneeUserId: string | null;
    ownerPositionId: number | null;
    dueDate: string | null;
    priority: number;
    links: WorkItemLinkDto[];
}

export interface WorkItemUpdateDto {
    title: string;
    description: string | null;
    assigneeUserId: string | null;
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
}
