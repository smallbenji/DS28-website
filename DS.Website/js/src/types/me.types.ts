export interface MeDto {
    isAuthenticated: boolean;
    name: string;
    firstName: string;
    lastName: string;
    mustEnableTwoFactor: boolean;
    roles: string[];
    appRoles: string[];
}
