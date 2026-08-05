export enum Gender {
    Male,
    Female
}

export interface ScoutDto {
    id: number;
    name: string;
    birthday: string;
    gender: Gender;
}