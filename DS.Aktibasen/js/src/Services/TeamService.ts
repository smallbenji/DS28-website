import type { AxiosResponse } from "axios";
import axios from "axios";

export default class TeamService {
    public async getTeams(): Promise<DSTeam[]> {
        try {
            const response: AxiosResponse = await axios({
                url: "/api/v1/team",
                method: "GET"
            });
            return response.data ? response.data : [];
        } catch {
            return [];
        }
    }

    public async createTeam(name: string): Promise<boolean> {
        try {
            await axios({
                url: "/api/v1/team",
                method: "POST",
                data: { name }
            });
            return true;
        } catch {
            return false;
        }
    }

    public async deleteTeam(teamId: number): Promise<boolean> {
        try {
            await axios({
                url: `/api/v1/team/${teamId}`,
                method: "DELETE"
            });
            return true;
        } catch {
            return false;
        }
    }

    public async addMember(teamId: number, userID: string): Promise<boolean> {
        try {
            await axios({
                url: `/api/v1/team/${teamId}/member`,
                method: "POST",
                data: { userID }
            });
            return true;
        } catch {
            return false;
        }
    }

    public async updateMember(teamId: number, userID: string, isAdmin: boolean): Promise<boolean> {
        try {
            await axios({
                url: `/api/v1/team/${teamId}/member/${userID}`,
                method: "PUT",
                data: { isAdmin }
            });
            return true;
        } catch {
            return false;
        }
    }

    public async removeMember(teamId: number, userID: string): Promise<boolean> {
        try {
            await axios({
                url: `/api/v1/team/${teamId}/member/${userID}`,
                method: "DELETE"
            });
            return true;
        } catch {
            return false;
        }
    }
}
