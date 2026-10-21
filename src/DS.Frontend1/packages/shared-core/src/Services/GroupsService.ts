import type { GroupDto, GroupsDto, PatrolDto, ScoutDto } from "../types";
import { apiDelete, apiGet, apiPost, apiPut, safeApiCall, safeApiOk } from "../lib/api";

export default class GroupsService {
    public async getGroups(): Promise<GroupsDto> {
        return safeApiCall(async () => {
            const data = await apiGet<GroupsDto>("/api/v1/groups");
            return data ? data : { groups: [], users: {}};
        }, { groups: [], users: {}});
    }

    public async createGroup(group: GroupDto): Promise<boolean> {
        return safeApiOk(() => apiPost("/api/v1/groups", group));
    }

    public async updateGroup(group: GroupDto): Promise<boolean> {
        return safeApiOk(() => apiPut(`/api/v1/groups/${group.id}`, group));
    }

    public async createPatrol(groupId: number | string, name: string): Promise<PatrolDto | null> {
        return safeApiCall(() => apiPost<PatrolDto>("/api/v1/groups/patrol", { groupId, name }), null);
    }

    public async createScout(groupId: number | string, name: string, birthday: string, gender: 'Male' | 'Female'): Promise<ScoutDto | null> {
        return safeApiCall(() => apiPost<ScoutDto>("/api/v1/groups/scout", { groupId, name, birthday, gender }), null);
    }

    public async addPatrol(scoutId: number, patrolId: number): Promise<boolean> {
        return safeApiOk(() => apiPost("/api/v1/groups/scout/add-patrol", { scoutId, patrolId }));
    }

    public async removePatrol(scoutId: number, patrolId: number): Promise<boolean> {
        return safeApiOk(() => apiPost("/api/v1/groups/scout/remove-patrol", { scoutId, patrolId }));
    }

    public async toggleLeader(scoutId: number, patrolId: number): Promise<boolean> {
        return safeApiOk(() => apiPost("/api/v1/groups/scout/toggle-leader", { scoutId, patrolId }));
    }

    public async deletePatrol(patrolId: number): Promise<boolean> {
        return safeApiOk(() => apiDelete(`/api/v1/groups/patrol/${patrolId}`));
    }

    public async deleteScout(scoutId: number): Promise<boolean> {
        return safeApiOk(() => apiDelete(`/api/v1/groups/scout/${scoutId}`));
    }
}
