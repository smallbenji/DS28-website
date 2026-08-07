import AccountService from "@/Services/AccountService";
import { defineStore } from "pinia";
import { ref } from "vue";
import type { TwoFactorResultDto, TwoFactorSetupDto, TwoFactorStatusDto } from "@/types";

export const useAccountStore = defineStore("account", () => {
    const accountService = new AccountService();

    const Status = ref<TwoFactorStatusDto>({ twoFactorEnabled: false, recoveryCodesLeft: 0 });

    async function GET_STATUS() {
        var data = await accountService.getTwoFactorStatus();
        if (data) {
            Status.value = data;
        }
        return data;
    }

    async function GET_SETUP(): Promise<TwoFactorSetupDto | null> {
        return await accountService.getTwoFactorSetup();
    }

    async function ENABLE_2FA(code: string): Promise<TwoFactorResultDto | null> {
        var data = await accountService.enableTwoFactor(code);
        if (data) {
            await GET_STATUS();
        }
        return data;
    }

    async function GENERATE_RECOVERY_CODES(): Promise<TwoFactorResultDto | null> {
        var data = await accountService.generateRecoveryCodes();
        if (data) {
            await GET_STATUS();
        }
        return data;
    }

    async function RESET_AUTHENTICATOR() {
        var success = await accountService.resetAuthenticator();
        if (success) {
            await GET_STATUS();
        }
        return success;
    }

    async function DISABLE_2FA(password: string): Promise<string | null> {
        var error = await accountService.disableTwoFactor(password);
        if (error == null) {
            await GET_STATUS();
        }
        return error;
    }

    async function UPDATE_NAME(firstName: string, lastName: string) {
        return await accountService.updateName(firstName, lastName);
    }

    async function CHANGE_PASSWORD(oldPassword: string, newPassword: string) {
        return await accountService.changePassword(oldPassword, newPassword);
    }

    return {
        Status,
        GET_STATUS,
        GET_SETUP,
        ENABLE_2FA,
        GENERATE_RECOVERY_CODES,
        RESET_AUTHENTICATOR,
        DISABLE_2FA,
        UPDATE_NAME,
        CHANGE_PASSWORD
    }
});
