import type { PatrolDto } from "./patrol.types";
import type { ScoutDto } from "./scout.types";

export enum District {
    DANEHOF = 'DANEHOF',
    FIONIA = 'FIONIA'
}

export interface GroupDto {
    id: string;
    name: string;
    district: District;
    patrols: PatrolDto[];
    scouts: ScoutDto[];
    users: GroupUserDto[];
}

export interface GroupUserDto {
    id: string;
    userName: string;
    email: string;
    firstName: string;
    lastName: string;
}