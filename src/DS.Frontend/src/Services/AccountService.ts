import type { PasskeyAttestationRequestDto, PasskeyDto, PasskeyOptionsDto, TwoFactorResultDto, TwoFactorSetupDto, TwoFactorStatusDto } from "@/types";
import { apiDelete, apiGet, apiPost, getErrorMessage, safeApiCall, safeApiOk, safeApiResult } from "@/lib/api";

export default class AccountService {
    public async getTwoFactorStatus(): Promise<TwoFactorStatusDto | null> {
        return safeApiResult(() => apiGet<TwoFactorStatusDto>("/api/v1/account/2fa"));
    }

    public async getTwoFactorSetup(): Promise<TwoFactorSetupDto | null> {
        return safeApiResult(() => apiGet<TwoFactorSetupDto>("/api/v1/account/2fa/setup"));
    }

    public async enableTwoFactor(code: string): Promise<TwoFactorResultDto | null> {
        return safeApiResult(() => apiPost<TwoFactorResultDto>("/api/v1/account/2fa/enable", { code }));
    }

    public async generateRecoveryCodes(): Promise<TwoFactorResultDto | null> {
        return safeApiResult(() => apiPost<TwoFactorResultDto>("/api/v1/account/2fa/recovery-codes"));
    }

    public async resetAuthenticator() {
        return safeApiOk(() => apiPost("/api/v1/account/2fa/reset"));
    }

    public async disableTwoFactor(password: string): Promise<string | null> {
        try {
            await apiPost("/api/v1/account/2fa/disable", { password });
            return null;
        } catch (error) {
            return getErrorMessage(error, "Der skete en fejl.");
        }
    }

    public async updateName(firstName: string, lastName: string) {
        return safeApiOk(() => apiPost("/api/v1/account/name", { firstName, lastName }));
    }

    public async changePassword(oldPassword: string, newPassword: string) {
        return safeApiOk(() => apiPost("/api/v1/account/password", { oldPassword, newPassword }));
    }

    public async listPasskeys(): Promise<PasskeyDto[]> {
        return safeApiCall(() => apiGet<PasskeyDto[]>("/api/v1/account/2fa/passkeys"), []);
    }

    public async passkeyCreationOptions(displayName: string): Promise<PasskeyOptionsDto | null> {
        return safeApiResult(() => apiPost<PasskeyOptionsDto>("/api/v1/account/2fa/passkeys/options", { displayName }));
    }

    public async registerPasskey(data: PasskeyAttestationRequestDto): Promise<TwoFactorResultDto | null> {
        return safeApiCall(async () => {
            const result = await apiPost<TwoFactorResultDto>("/api/v1/account/2fa/passkeys", data);
            return { recoveryCodes: result?.recoveryCodes ?? [] };
        }, null);
    }

    public async removePasskey(id: string): Promise<string | null> {
        try {
            await apiDelete(`/api/v1/account/2fa/passkeys/${id}`);
            return null;
        } catch (error) {
            return getErrorMessage(error, "Der skete en fejl.");
        }
    }
}
