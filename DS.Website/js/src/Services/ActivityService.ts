import type { AxiosResponse } from "axios";
import axios from "axios";
import type { ActivityTeamDto } from "@/types";

export default class ActivityService {
    public async getTeams(): Promise<ActivityTeamDto[]> {
        try {
            const response: AxiosResponse<ActivityTeamDto[]> = await axios({
                url: "/api/v1/activity/teams",
                method: "GET"
            });
            return response.data ? response.data : [];
        } catch {
            return [];
        }
    }

    public async addActivity(teamId: number, name: string): Promise<boolean> {
        try {
            await axios({
                url: `/api/v1/activity/team/${teamId}/activity/add`,
                method: "POST",
                data: { name }
            });
            return true;
        } catch {
            return false;
        }
    }

    public async addTeam(name: string): Promise<boolean> {
        try {
            await axios({
                url: "/api/v1/activity/teams/add",
                method: "POST",
                data: { name }
            });
            return true;
        } catch {
            return false;
        }
    }
}
