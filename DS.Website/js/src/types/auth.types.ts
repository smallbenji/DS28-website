export interface LoginDto {
    email: string;
    password: string;
    returnUrl: string;
}

export interface RegisterDto {
    firstName: string;
    lastName: string;
    email: string;
    password: string;
}

export interface TwoFactorLoginDto {
    twoFactorCode: string;
    rememberMachine: boolean;
    returnUrl: string;
}

export interface RecoveryCodeLoginDto {
    recoveryCode: string;
    returnUrl: string;
}

export interface AuthResultDto {
    requiresTwoFactor: boolean;
    returnUrl?: string;
    error?: string;
}
