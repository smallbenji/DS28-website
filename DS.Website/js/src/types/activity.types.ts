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

export interface ActivityWithTeamDto extends ActivityDto {
    teamName: string;
}
