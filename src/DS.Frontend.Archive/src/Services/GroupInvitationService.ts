import { apiGet, apiPost } from '@/lib/api';

export interface GroupInvitationDto {
    email: string;
    groupName: string;
    existingUser: boolean;
}

export interface GroupInvitationAcceptanceDto {
    firstName: string;
    lastName: string;
    password: string;
}

export default class GroupInvitationService {
    async get(id: string): Promise<GroupInvitationDto> {
        return await apiGet<GroupInvitationDto>(this.endpoint(id));
    }

    async accept(id: string, data: GroupInvitationAcceptanceDto): Promise<void> {
        await apiPost<void>(this.endpoint(id), data);
    }

    private endpoint(id: string): string {
        return `/api/v1/group-invitations/${encodeURIComponent(id)}`;
    }
}
