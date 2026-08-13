import type { AxiosResponse } from "axios";
import axios from "axios";
import type { PasskeyAttestationRequestDto, PasskeyDto, PasskeyOptionsDto, TwoFactorResultDto, TwoFactorSetupDto, TwoFactorStatusDto } from "@/types";

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

    public async disableTwoFactor(password: string): Promise<string | null> {
        try {
            const response: AxiosResponse = await axios({
                url: "/api/v1/account/2fa/disable",
                method: "POST",
                data: JSON.stringify({ password }),
                headers: {
                    "Content-Type": "application/json"
                }
            });

            return response.status == 200 ? null : "Der skete en fejl.";
        } catch (error) {
            if (axios.isAxiosError(error)) {
                const data = error.response?.data;
                if (typeof data === "string" && data.length > 0) {
                    return data;
                }
            }

            return "Der skete en fejl.";
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

    public async listPasskeys(): Promise<PasskeyDto[]> {
        try {
            const response: AxiosResponse<PasskeyDto[]> = await axios({
                url: "/api/v1/account/2fa/passkeys",
                method: "GET"
            });
            
            return response.data?? []
        } catch {
            return [];
        }
    }

    public async passkeyCreationOptions(displayName: string): Promise<PasskeyOptionsDto | null> {
        try {
            const response: AxiosResponse<PasskeyOptionsDto> = await axios({
                url: "/api/v1/account/2fa/passkeys/options",
                method: "POST",
                data: {displayName},
            })

            return response.data ?? null;
        } catch {
            return null;
        }
    }

    public async registerPasskey(data: PasskeyAttestationRequestDto): Promise<TwoFactorResultDto | null> {
        try {
            const response: AxiosResponse<TwoFactorResultDto> = await axios({
                url: "/api/v1/account/2fa/passkeys",
                method: "POST",
                data: data,
            })

            if(response.status !== 200) return null;

            return {recoveryCodes: response.data?.recoveryCodes ?? []};
        } catch(err) {
            return null;
        }
    }

    public async removePasskey(id: string): Promise<string | null> {
        try {
            const response: AxiosResponse = await axios({
                url: `/api/v1/account/2fa/passkeys/${id}`,
                method: "DELETE"
            });

            return response.status == 200 ? null : "der skete en fejl";
        } catch(error) {
            if(axios.isAxiosError(error)) {
                const data = error.response?.data;
                if(typeof data === "string" && data.length > 0) {
                    return data;
                }
            }

            return "Der skete en fejl."
        }
    }
}
