import type { RoleDto, UserDto } from "@/types";
import { apiDelete, apiGet, apiPost, apiPut, safeApiCall, safeApiOk } from "@/lib/api";

export default class UserService {
    public async getUsers(): Promise<UserDto[]> {
        return safeApiCall(async () => {
            const data = await apiGet<UserDto[]>("/api/v1/user");
            return Array.isArray(data) ? data : [];
        }, []);
    }

    public async updateUser(user: UserDto) {
        return safeApiOk(() => apiPut("/api/v1/user", user));
    }

    public async updateUserProfilePicture(userId: string, publicId: string | null) {
        return safeApiOk(() => apiPut(`/api/v1/user/${userId}/profile-picture`, {
            image: publicId ? { publicId } : null
        }));
    }

    public async createUser(user: UserDto) {
        return safeApiOk(() => apiPost("/api/v1/user", user));
    }

    public async getAssignableGroups(): Promise<string[]> {
        return safeApiCall(() => apiGet<string[]>("/api/v1/user/assignable-groups"), []);
    }

    public async getGroups(): Promise<RoleDto[]> {
        return safeApiCall(() => apiGet<RoleDto[]>("/api/v1/user/groups"), []);
    }

    public async AssignRoleToUser(user: UserDto, roleName: string) {
        return safeApiOk(() => apiPut(`/api/v1/user/${user.id}/role/add`, roleName));
    }

    public async RemoveRoleFromUser(user: UserDto, roleName: string) {
        return safeApiOk(() => apiPut(`/api/v1/user/${user.id}/role/remove`, roleName));
    }

    public async deleteUser(user: UserDto) {
        return safeApiOk(() => apiDelete(`/api/v1/user/${user.id}`));
    }

    public async lockUser(user: UserDto) {
        return safeApiOk(() => apiPut(`/api/v1/user/${user.id}/lock`));
    }

    public async unlockUser(user: UserDto) {
        return safeApiOk(() => apiPut(`/api/v1/user/${user.id}/unlock`));
    }

    public async inviteUser(email: string, roles: string[]) {
        return safeApiOk(() => apiPost("/api/v1/user/invite", { email, roles }));
    }
}
