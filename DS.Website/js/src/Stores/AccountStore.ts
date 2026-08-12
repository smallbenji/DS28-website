import AccountService from "@/Services/AccountService";
import { defineStore } from "pinia";
import { ref } from "vue";
import type { PasskeyDto, TwoFactorResultDto, TwoFactorSetupDto, TwoFactorStatusDto } from "@/types";
import { startCreation } from "@/lib/passkeys";

export const useAccountStore = defineStore("account", () => {
  const accountService = new AccountService();

  const Status = ref<TwoFactorStatusDto>({ twoFactorEnabled: false, recoveryCodesLeft: 0 });
  const Passkeys = ref<PasskeyDto[]>([])

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

  async function GET_PASSKEYS() {
    Passkeys.value = (await accountService.listPasskeys()) ?? []
  }

  async function CREATE_PASSKEY(displayName: string) {
    const options = await accountService.passkeyCreationOptions(displayName);
    console.log("we got the options:", options)
    if (!options) return null;
    
    console.log("before we start creating")
    const credentialJson = await startCreation(options.optionsJson);
    console.log("we have started the creation")

    if (!credentialJson) return null
    console.log("we register the passkey??")
    const result = await accountService.registerPasskey({
      credentialJson: credentialJson,
      name: displayName ?? ''
    });

    console.log("we try and get with it ya know")
    await GET_PASSKEYS();
    await GET_STATUS();
    return result;
  }

  async function REMOVE_PASSKEY(id: string) {
    const error = await accountService.removePasskey(id);

    if (error == null) {
      await GET_PASSKEYS();
      await GET_STATUS();
    }

    return error;
  }

  return {
    Status,
    Passkeys,
    GET_STATUS,
    GET_SETUP,
    ENABLE_2FA,
    GENERATE_RECOVERY_CODES,
    RESET_AUTHENTICATOR,
    DISABLE_2FA,
    UPDATE_NAME,
    CHANGE_PASSWORD,
    GET_PASSKEYS,
    CREATE_PASSKEY,
    REMOVE_PASSKEY
  }
});
