import type { AxiosResponse } from "axios";
import axios from "axios";

export default class GroupsService {
    public async getGroups(): Promise<GroupDTO> {
        try {
            const response: AxiosResponse = await axios({
                url: "/api/v1/groups",
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
                url: "/api/v1/groups",
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
                url: `/api/v1/groups/${group.id}`,
                method: "PUT",
                data: group
            });
            return true;
        } catch {
            return false;
        }
    }

    public async createPatrol(groupId: number | string, name: string): Promise<DSPatrol | null> {
        try {
            const response: AxiosResponse = await axios({
                url: "/api/v1/groups/patrol",
                method: "POST",
                data: { groupId, name }
            });
            return response.data ? response.data : null;
        } catch {
            return null;
        }
    }

    public async createScout(groupId: number | string, name: string, birthday: string, gender: 'Male' | 'Female'): Promise<DSScout | null> {
        try {
            const response: AxiosResponse = await axios({
                url: "/api/v1/groups/scout",
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
                url: "/api/v1/groups/scout/add-patrol",
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
                url: "/api/v1/groups/scout/remove-patrol",
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
                url: "/api/v1/groups/scout/toggle-leader",
                method: "POST",
                data: { scoutId, patrolId }
            });
            return true;
        } catch {
            return false;
        }
    }

    public async deletePatrol(patrolId: number): Promise<boolean> {
        try {
            await axios({
                url: `/api/v1/groups/patrol/${patrolId}`,
                method: "DELETE"
            });
            return true;
        } catch {
            return false;
        }
    }

    public async deleteScout(scoutId: number): Promise<boolean> {
        try {
            await axios({
                url: `/api/v1/groups/scout/${scoutId}`,
                method: "DELETE"
            });
            return true;
        } catch {
            return false;
        }
    }
}
