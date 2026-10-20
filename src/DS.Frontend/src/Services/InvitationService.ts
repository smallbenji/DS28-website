import type { UserInvitationCreationDto, UserInvitationDto } from "@/types";
import { apiGet, apiPost, safeApiOk } from "@/lib/api";

export default class InvitationService {
    public async getInvitation(id: string): Promise<UserInvitationDto | null> {
        try {
            return await apiGet<UserInvitationDto>(`/api/v1/invitation/${id}`);
        } catch {
            console.error("Error fetching invitation.");
            return null;
        }
    }

    public async useInvitation(id: string, data: UserInvitationCreationDto) {
        return safeApiOk(() => apiPost(`/api/v1/invitation/${id}`, data));
    }
}
