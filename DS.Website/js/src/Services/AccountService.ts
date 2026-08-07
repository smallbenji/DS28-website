import type { AxiosResponse } from "axios";
import axios from "axios";
import type { TwoFactorResultDto, TwoFactorSetupDto, TwoFactorStatusDto } from "@/types";

export default class AccountService {
    public async getTwoFactorStatus(): Promise<TwoFactorStatusDto | null> {
        try {
            const response: AxiosResponse<TwoFactorStatusDto> = await axios({
                url: "/api/v1/account/2fa",
                method: "GET"
            });

            return response.data ?? null;
        } catch {
            return null;
        }
    }

    public async getTwoFactorSetup(): Promise<TwoFactorSetupDto | null> {
        try {
            const response: AxiosResponse<TwoFactorSetupDto> = await axios({
                url: "/api/v1/account/2fa/setup",
                method: "GET"
            });

            return response.data ?? null;
        } catch {
            return null;
        }
    }

    public async enableTwoFactor(code: string): Promise<TwoFactorResultDto | null> {
        try {
            const response: AxiosResponse<TwoFactorResultDto> = await axios({
                url: "/api/v1/account/2fa/enable",
                method: "POST",
                data: JSON.stringify({ code }),
                headers: {
                    "Content-Type": "application/json"
                }
            });

            if (response.status != 200) {
                return null;
            }

            return response.data ?? null;
        } catch {
            return null;
        }
    }

    public async generateRecoveryCodes(): Promise<TwoFactorResultDto | null> {
        try {
            const response: AxiosResponse<TwoFactorResultDto> = await axios({
                url: "/api/v1/account/2fa/recovery-codes",
                method: "POST"
            });

            if (response.status != 200) {
                return null;
            }

            return response.data ?? null;
        } catch {
            return null;
        }
    }

    public async resetAuthenticator() {
        try {
            const response: AxiosResponse = await axios({
                url: "/api/v1/account/2fa/reset",
                method: "POST"
            });

            return response.status == 200;
        } catch {
            return false;
        }
    }

    public async disableTwoFactor(password: string) {
        try {
            const response: AxiosResponse = await axios({
                url: "/api/v1/account/2fa/disable",
                method: "POST",
                data: JSON.stringify({ password }),
                headers: {
                    "Content-Type": "application/json"
                }
            });

            return response.status == 200;
        } catch {
            return false;
        }
    }

    public async updateName(firstName: string, lastName: string) {
        try {
            const response: AxiosResponse = await axios({
                url: "/api/v1/account/name",
                method: "POST",
                data: JSON.stringify({ firstName, lastName }),
                headers: {
                    "Content-Type": "application/json"
                }
            });

            return response.status == 200;
        } catch {
            return false;
        }
    }

    public async changePassword(oldPassword: string, newPassword: string) {
        try {
            const response: AxiosResponse = await axios({
                url: "/api/v1/account/password",
                method: "POST",
                data: JSON.stringify({ oldPassword, newPassword }),
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
