import type { PatrolDto } from "./patrol.types";
import type { ScoutDto } from "./scout.types";
import type { UserDto } from "./user.types";

export enum District {
    DANEHOF = 'DANEHOF',
    FIONIA = 'FIONIA'
}

export const DistrictLabels: Record<District, string> = {
    [District.DANEHOF]: 'Danehof',
    [District.FIONIA]: 'Fionia'
};

export function districtLabel(district: string | undefined): string | undefined {
    return DistrictLabels[district as District] ?? district;
}

export interface GroupDto {
    id: string;
    name: string;
    district: District;
    patrols: PatrolDto[];
    scouts: ScoutDto[];
    users: UserDto[];
}

export interface GroupUserDto {
    id: string;
    userName: string;
    email: string;
    firstName: string;
    lastName: string;
}