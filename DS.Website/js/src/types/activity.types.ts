export interface ActivityDto {
    id: number;
    name: string;
    budget: number;
    catalog: CatalogDataDto | null;
}

export interface CatalogDataDto {
    id: number;
    name: string;
    summary: string;
    description: string;
}

export interface ActivityTeamDto {
    id: number;
    name: string;
    members: ActivityTeamMemberDto[];
    activities: ActivityDto[];
}

export interface ActivityTeamMemberDto {
    userId: string;
    name: string;
    email: string;
    isAdmin: boolean;
}

export interface ActivityTeamMembershipDto {
    userId: string;
    isAdmin: boolean;
}

export interface ActivityTeamInviteDto {
    email: string;
    isAdmin: boolean;
}

export interface ActivityTeamInviteLinkDto {
    link: string;
    email: string;
}

export interface ActivityWithTeamDto extends ActivityDto {
    teamName: string;
}

export type ActivityFileKind = 'doc' | 'sheet' | 'slides' | 'file';

export interface ActivityFileDto {
    id: string;
    name: string;
    kind: ActivityFileKind;
    url: string;
    modifiedAt: string | null;
    createdBy: string | null;
    createdAt: string | null;
}

export interface ActivityFileCreateDto {
    title: string | null;
    kind: ActivityFileKind;
}
