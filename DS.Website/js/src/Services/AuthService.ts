import type { AxiosResponse } from "axios";
import axios from "axios";
import type { AuthResultDto, LoginDto, PasskeyAssertionRequestDto, RecoveryCodeLoginDto, RegisterDto, TwoFactorLoginDto } from "@/types";
import type { PasskeyOptionsDto } from "@/types/passkeys.types";

export default class AuthService {
  public async logout(): Promise<void> {
    await axios({
      url: "/api/v1/auth/logout",
      method: "POST"
    });
  }

  public async login(data: LoginDto): Promise<AuthResultDto> {
    return await this.post<AuthResultDto>("/api/v1/auth/login", data);
  }

  public async twoFactorLogin(data: TwoFactorLoginDto): Promise<AuthResultDto> {
    return await this.post<AuthResultDto>("/api/v1/auth/2fa", data);
  }

  public async recoveryCodeLogin(data: RecoveryCodeLoginDto): Promise<AuthResultDto> {
    return await this.post<AuthResultDto>("/api/v1/auth/recovery-code", data);
  }

  public async register(data: RegisterDto): Promise<string | null> {
    const result = await this.post<never>("/api/v1/auth/register", data);
    return result.error ?? null;
  }

  public async passkeyOptions(): Promise<PasskeyOptionsDto> {
    const result = await axios.post<PasskeyOptionsDto>("/api/v1/auth/2fa/passkeys/options");
    return result.data;
  }

  public async passkeyVerify(data: PasskeyAssertionRequestDto): Promise<AuthResultDto> {
    return await this.post<AuthResultDto>("/api/v1/auth/2fa/passkeys/verify", data)
  }

  private async post<T>(url: string, data: unknown): Promise<AuthResultDto> {
    try {
      const response: AxiosResponse<T> = await axios({
        url,
        method: "POST",
        data: JSON.stringify(data),
        withCredentials: true,
        headers: {
          "Content-Type": "application/json"
        }
      });

      return response.data as unknown as AuthResultDto;
    } catch (error) {
      return {
        requiresTwoFactor: false,
        hasAuthenticator: false,
        passkeysAvailable: false,
        error: this.extractErrorMessage(error)
      };
    }
  }

  private extractErrorMessage(error: unknown): string {
    if (axios.isAxiosError(error)) {
      const data = error.response?.data;
      if (typeof data === "string" && data.length > 0) {
        return data;
      }
    }

    return "Der skete en uventet fejl.";
  }
}
