import type { GroupDto } from "./group.types";

export interface UserDto {
    id: string;
    userName: string;
    email: string;
    firstName: string;
    lastName: string;
    roles: string[];
    group: GroupDto | null;
    lockoutEnd: string | null;
}
