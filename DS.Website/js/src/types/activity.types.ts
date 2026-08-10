export interface ActivityDto {
    id: number;
    name: string;
}

export interface ActivityTeamDto {
    id: number;
    name: string;
    activities?: ActivityDto[];
}
