import type { PatrolMembershipDto } from "./patrol.types";

export enum Gender {
    Male,
    Female
}

export interface ScoutDto {
    id: number;
    name: string;
    birthday: string;
    gender: Gender;
    groupId: number;
    memberships: PatrolMembershipDto[];
}