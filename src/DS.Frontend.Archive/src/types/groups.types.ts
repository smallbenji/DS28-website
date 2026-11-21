import type { GroupDto } from "./group.types";
import type { UserDto } from "./user.types";

export interface GroupsDto {
    groups: GroupDto[];
    users: Record<string, UserDto[]>;
}
