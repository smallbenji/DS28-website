export interface TwoFactorStatusDto {
    twoFactorEnabled: boolean;
    recoveryCodesLeft: number;
    hasEnabledAuthenticator: boolean;
}

export interface TwoFactorSetupDto {
    authenticatorUri: string;
    manualEntryKey: string;
}

export interface TwoFactorResultDto {
    recoveryCodes: string[];
}
