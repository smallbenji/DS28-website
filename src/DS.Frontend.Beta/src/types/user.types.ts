import type { ImageReferenceDto } from "./file.types";
import type { GroupDto } from "./group.types";
import type { OrgPositionDto } from "./org.types";

export interface UserDto {
    id: string;
    userName: string;
    email: string;
    phone: string;
    firstName: string;
    lastName: string;
    roles: string[];
    positions: OrgPositionDto[];
    group: GroupDto | null;
    lockoutEnd: string | null;
    profilePicture: ImageReferenceDto | null;
}

export interface UserSummaryDto {
    id: string;
    firstName: string;
    lastName: string;
    profilePicture: ImageReferenceDto | null;
}