export interface PatrolDto {
    id: number;
    name: string;
    groupId: number;
    memberships: PatrolMembershipDto[];
}

export interface PatrolMembershipDto {
    id: number;
    scoutId: number;
    patrolId: number;
    joinedDate: string;
    isPatrolLeader: boolean;
}