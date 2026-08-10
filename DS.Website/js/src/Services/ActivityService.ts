import type { AxiosResponse } from "axios";
import axios from "axios";
import type { ActivityDto, ActivityTeamDto, ActivityTeamInviteDto, ActivityTeamMembershipDto, UserDto } from "@/types";

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

    public async searchUsers(search: string, teamId: number): Promise<UserDto[]> {
        try {
            const response: AxiosResponse<UserDto[]> = await axios({
                url: "/api/v1/activity/users/search",
                method: "GET",
                params: { search, teamId }
            });
            return response.data ? response.data : [];
        } catch {
            return [];
        }
    }

    public async addMember(teamId: number, membership: ActivityTeamMembershipDto): Promise<boolean> {
        try {
            await axios({
                url: `/api/v1/activity/team/${teamId}/member/add`,
                method: "POST",
                data: membership
            });
            return true;
        } catch {
            return false;
        }
    }

    public async removeMember(teamId: number, userId: string): Promise<boolean> {
        try {
            await axios({
                url: `/api/v1/activity/team/${teamId}/member/remove`,
                method: "POST",
                data: { userId, isAdmin: false }
            });
            return true;
        } catch {
            return false;
        }
    }

    public async inviteUser(teamId: number, data: ActivityTeamInviteDto): Promise<boolean> {
        try {
            await axios({
                url: `/api/v1/activity/team/${teamId}/invite`,
                method: "POST",
                data
            });
            return true;
        } catch {
            return false;
        }
    }

    public async getActivity(id: number): Promise<ActivityDto | null> {
        try {
            const response: AxiosResponse<ActivityDto> = await axios({
                url: `/api/v1/activity/activity/${id}`,
                method: "GET"
            });
            return response.data ? response.data : null;
        } catch {
            return null;
        }
    }

    public async updateActivity(id: number, data: ActivityDto): Promise<boolean> {
        try {
            await axios({
                url: `/api/v1/activity/activity/${id}`,
                method: "PUT",
                data
            });
            return true;
        } catch {
            return false;
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
