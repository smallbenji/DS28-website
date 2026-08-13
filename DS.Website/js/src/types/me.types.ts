import type { PasskeyDto } from "./passkeys.types";

export interface MeDto {
  isAuthenticated: boolean;
  id: string;
  name: string;
  firstName: string;
  lastName: string;
  mustEnableTwoFactor: boolean;
  roles: string[];
  appRoles: string[];
  passkeys: PasskeyDto[]
}
