import type { GroupDto } from '../types';
import { apiDelete, apiGet, apiPost } from '../lib/api';

export interface GroupMemberInvitationDto {
    invitationId: string;
    email: string;
}

export default class GroupMembersService {
    async getGroup(): Promise<GroupDto> {
        return await apiGet<GroupDto>('/api/v1/group');
    }

    async getInvitations(): Promise<GroupMemberInvitationDto[]> {
        return await apiGet<GroupMemberInvitationDto[]>('/api/v1/group/members/invitations');
    }

    async invite(email: string): Promise<{ path: string }> {
        return await apiPost<{ path: string }>('/api/v1/group/members/invitations', { email });
    }

    async removeMember(userId: string): Promise<void> {
        await apiDelete(`/api/v1/group/members/${encodeURIComponent(userId)}`);
    }

    async revokeInvitation(invitationId: string): Promise<void> {
        await apiDelete(`/api/v1/group/members/invitations/${invitationId}`);
    }
}
