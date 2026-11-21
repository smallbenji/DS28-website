import type { AccessTokenCreatedDto, AccessTokenDto } from "@/types";
import { apiDelete, apiGet, apiPost, safeApiCall } from "@/lib/api";

export default class AccessTokenService {
    public async getTokens(): Promise<AccessTokenDto[]> {
        return safeApiCall(async () => {
            const data = await apiGet<AccessTokenDto[]>("/api/v1/access-tokens");
            return Array.isArray(data) ? data : [];
        }, []);
    }

    public async createToken(name: string, expiresInDays: number): Promise<AccessTokenCreatedDto> {
        return await apiPost<AccessTokenCreatedDto>("/api/v1/access-tokens", { name, expiresInDays });
    }

    public async revokeToken(id: number): Promise<boolean> {
        await apiDelete<void>(`/api/v1/access-tokens/${id}`);
        return true;
    }
}
