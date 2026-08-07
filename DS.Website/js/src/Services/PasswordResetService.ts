import type { AxiosResponse } from "axios";
import axios from "axios";
import type { ResetPasswordDto, ResetPasswordLinkDto } from "@/types";

export default class PasswordResetService {
    public async createResetPasswordLink(userId: string): Promise<ResetPasswordLinkDto | null> {
        try {
            const response: AxiosResponse<ResetPasswordLinkDto> = await axios({
                url: `/api/v1/user/${userId}/reset-password-link`,
                method: "POST"
            });

            return response.data ?? null;
        } catch {
            return null;
        }
    }

    public async resetPassword(data: ResetPasswordDto) {
        try {
            const response: AxiosResponse = await axios({
                url: "/api/v1/reset-password",
                method: "POST",
                data: JSON.stringify(data),
                headers: {
                    "Content-Type": "application/json"
                }
            });

            return response.status == 200;
        } catch {
            return false;
        }
    }
}
