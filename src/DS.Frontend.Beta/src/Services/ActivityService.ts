import type { ActivityReviewAction, ActivityUpdateDto, ActivityFileCreateDto, ActivityFileDto, ActivityDto, ActivityTeamDto, ActivityTeamInviteDto, ActivityTeamInviteLinkDto, ActivityTeamMembershipDto, UserDto } from "@/types";
import { apiGet, apiPost, apiPut, safeApiCall, safeApiOk } from "@/lib/api";

export default class ActivityService {
    public async getTeams(): Promise<ActivityTeamDto[]> {
        return safeApiCall(() => apiGet<ActivityTeamDto[]>("/api/v1/activity/teams"), []);
    }

    public async searchUsers(search: string, teamId: number): Promise<UserDto[]> {
        return safeApiCall(() => apiGet<UserDto[]>("/api/v1/activity/users/search", { params: { search, teamId } }), []);
    }

    public async addMember(teamId: number, membership: ActivityTeamMembershipDto): Promise<boolean> {
        return safeApiOk(() => apiPost(`/api/v1/activity/team/${teamId}/member/add`, membership));
    }

    public async removeMember(teamId: number, userId: string): Promise<boolean> {
        return safeApiOk(() => apiPost(`/api/v1/activity/team/${teamId}/member/remove`, { userId, isAdmin: false }));
    }

    public async inviteUser(teamId: number, data: ActivityTeamInviteDto): Promise<ActivityTeamInviteLinkDto | null> {
        return safeApiCall(() => apiPost<ActivityTeamInviteLinkDto>(`/api/v1/activity/team/${teamId}/invite`, data), null);
    }

    public async getActivity(id: number): Promise<ActivityDto | null> {
        return safeApiCall(() => apiGet<ActivityDto>(`/api/v1/activity/activity/${id}`), null);
    }

    public async updateActivity(id: number, data: ActivityUpdateDto): Promise<boolean> {
        await apiPut<void>(`/api/v1/activity/activity/${id}`, data);
        return true;
    }

    public async reviewActivity(id: number, action: ActivityReviewAction, reviewVersion: number, feedback?: string): Promise<void> {
        await apiPost<void>(`/api/v1/activity/activity/${id}/${action}`, { reviewVersion, feedback });
    }

    public async addActivity(teamId: number, name: string): Promise<boolean> {
        return safeApiOk(() => apiPost(`/api/v1/activity/team/${teamId}/activity/add`, { name }));
    }

    public async addTeam(name: string): Promise<boolean> {
        return safeApiOk(() => apiPost("/api/v1/activity/teams/add", { name }));
    }

    public async getFiles(activityId: number, folderId?: string | null): Promise<ActivityFileDto[]> {
        return safeApiCall(() => apiGet<ActivityFileDto[]>(`/api/v1/activity/activity/${activityId}/files`, { params: folderId ? { folderId } : {} }), []);
    }

    public async addFile(activityId: number, data: ActivityFileCreateDto): Promise<ActivityFileDto> {
        return await apiPost<ActivityFileDto>(`/api/v1/activity/activity/${activityId}/file/add`, data);
    }
}
