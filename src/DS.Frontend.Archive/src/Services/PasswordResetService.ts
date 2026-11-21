import type { ResetPasswordDto, ResetPasswordLinkDto } from "@/types";
import { apiPost, safeApiOk, safeApiResult } from "@/lib/api";

export default class PasswordResetService {
    public async createResetPasswordLink(userId: string): Promise<ResetPasswordLinkDto | null> {
        return safeApiResult(() => apiPost<ResetPasswordLinkDto>(`/api/v1/user/${userId}/reset-password-link`));
    }

    public async resetPassword(data: ResetPasswordDto) {
        return safeApiOk(() => apiPost("/api/v1/reset-password", data));
    }
}
