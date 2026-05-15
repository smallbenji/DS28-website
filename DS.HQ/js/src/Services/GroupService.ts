import type { AxiosResponse } from "axios";
import axios from "axios";

export default class GroupService {
    public async getGroups(): Promise<DSGroup[]> {
        try {
            const response: AxiosResponse = await axios({
                url: "/api/v1/group",
                method: "GET"
            });
            return Array.isArray(response.data) ? response.data : [];
        } catch {
            return [];
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
}
