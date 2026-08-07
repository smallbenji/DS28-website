export interface TwoFactorStatusDto {
    twoFactorEnabled: boolean;
    recoveryCodesLeft: number;
}

export interface TwoFactorSetupDto {
    authenticatorUri: string;
    manualEntryKey: string;
}

export interface TwoFactorResultDto {
    recoveryCodes: string[];
}
