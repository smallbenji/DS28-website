import type { PatrolDto } from "./patrol.types";
import type { ScoutDto } from "./scout.types";

export enum District {
    DANEHOF = 'DANEHOF',
    FIONIA = 'FIONIA'
}

export interface GroupDto {
    id: number;
    name: string;
    district: District;
    patrols: PatrolDto[];
    scouts: ScoutDto[];
}