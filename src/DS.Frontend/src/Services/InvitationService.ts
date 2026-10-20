import type { UserInvitationCreationDto, UserInvitationDto } from "@/types";
import { apiGet, apiPost, safeApiOk } from "@/lib/api";

export default class InvitationService {
    public async getInvitation(id: string): Promise<UserInvitationDto | null> {
        try {
            return await apiGet<UserInvitationDto>(`/api/v1/invitation/${id}`);
        } catch (ex) {
            console.error(`Error fetching invitation with id ${id}:`, ex);
            return null;
        }
    }

    public async useInvitation(id: string, data: UserInvitationCreationDto) {
        return safeApiOk(() => apiPost(`/api/v1/invitation/${id}`, data));
    }
}
