import type { AxiosResponse } from "axios";
import axios from "axios";

export default class GroupService {
    public async getGroups(): Promise<GroupDTO> {
        try {
            const response: AxiosResponse = await axios({
                url: "/api/v1/group",
                method: "GET"
            });
            return response.data ? response.data : { groups: [], users: {}};
        } catch {
            return { groups: [], users: {}};
        }
    }

    public async createGroup(group: DSGroup): Promise<boolean> {
        try {
            await axios({
                url: "/api/v1/group",
                method: "POST",
                data: group
            });
            return true;
        } catch {
            return false;
        }
    }

    public async updateGroup(group: DSGroup): Promise<boolean> {
        try {
            await axios({
                url: `/api/v1/group/${group.id}`,
                method: "PUT",
                data: group
            });
            return true;
        } catch {
            return false;
        }
    }

    public async createPatrol(groupId: number, name: string): Promise<DSPatrol | null> {
        try {
            const response: AxiosResponse = await axios({
                url: "/api/v1/group/patrol",
                method: "POST",
                data: { groupId, name }
            });
            return response.data ? response.data : null;
        } catch {
            return null;
        }
    }

    public async createScout(groupId: number, name: string, birthday: string, gender: 'Male' | 'Female'): Promise<DSScout | null> {
        try {
            const response: AxiosResponse = await axios({
                url: "/api/v1/group/scout",
                method: "POST",
                data: { groupId, name, birthday, gender }
            });
            return response.data ? response.data : null;
        } catch {
            return null;
        }
    }

    public async addPatrol(scoutId: number, patrolId: number): Promise<boolean> {
        try {
            await axios({
                url: "/api/v1/group/scout/add-patrol",
                method: "POST",
                data: { scoutId, patrolId }
            });
            return true;
        } catch {
            return false;
        }
    }

    public async removePatrol(scoutId: number, patrolId: number): Promise<boolean> {
        try {
            await axios({
                url: "/api/v1/group/scout/remove-patrol",
                method: "POST",
                data: { scoutId, patrolId }
            });
            return true;
        } catch {
            return false;
        }
    }

    public async toggleLeader(scoutId: number, patrolId: number): Promise<boolean> {
        try {
            await axios({
                url: "/api/v1/group/scout/toggle-leader",
                method: "POST",
                data: { scoutId, patrolId }
            });
            return true;
        } catch {
            return false;
        }
    }
}
