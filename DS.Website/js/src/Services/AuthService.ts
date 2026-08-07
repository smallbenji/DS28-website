import type { AxiosResponse } from "axios";
import axios from "axios";
import type { AuthResultDto, LoginDto, RecoveryCodeLoginDto, RegisterDto, TwoFactorLoginDto } from "@/types";

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

    private async post<T>(url: string, data: unknown): Promise<AuthResultDto> {
        try {
            const response: AxiosResponse<T> = await axios({
                url,
                method: "POST",
                data: JSON.stringify(data),
                headers: {
                    "Content-Type": "application/json"
                }
            });

            return response.data as unknown as AuthResultDto;
        } catch (error) {
            return {
                requiresTwoFactor: false,
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
