import type { ImageReferenceDto } from "./file.types";

export interface OrgPositionDto {
    id: number;
    title: string;
    parentPositionId: number | null;
    paForPositionId: number | null;
    isSystem: boolean;
    color: string | null;
    groups: string[];
    holders: OrgPositionHolderDto[];
}

export interface OrgPositionHolderDto {
    userId: string;
    name: string;
    profilePicture: ImageReferenceDto | null;
}

export interface OrgPositionCreateDto {
    title: string;
    parentPositionId: number | null;
    color: string | null;
    groups: string[];
}

export interface OrgPositionUpdateDto {
    title: string;
    color: string | null;
    groups: string[];
}

export interface OrgPositionMoveDto {
    parentPositionId: number | null;
}

export interface OrgNodeModel {
    position: OrgPositionDto;
    pa: OrgPositionDto | null;
    children: OrgNodeModel[];
}

export const groupTitleLabels: Record<string, string> = {
    SysAdmin: 'Systemadministrator',
    CampAdmin: 'Lejrchef',
    EventAdmin: 'Arrangementsansvarlig',
    ActivityAdmin: 'Aktivitetsansvarlig',
    ActivityUser: 'Aktivitets-teammedlem',
    FinanceAdmin: 'Kasserer',
    PRAdmin: 'PR-ansvarlig',
    FoodAdmin: 'Madansvarlig',
    PersonalAssistant: 'Personlig assistent',
    User: 'Deltager'
};

export function groupTitle(group: string): string {
    return groupTitleLabels[group] ?? group;
}
