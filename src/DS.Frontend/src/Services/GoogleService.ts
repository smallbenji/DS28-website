import type { GoogleConnectionStatusDto } from "@/types";
import { apiGet, apiPost } from "@/lib/api";

export default class GoogleService {
    public async getStatus(): Promise<GoogleConnectionStatusDto> {
        return await apiGet<GoogleConnectionStatusDto>("/api/v1/google/status");
    }

    public async disconnect(): Promise<void> {
        await apiPost<void>("/api/v1/google/disconnect");
    }
}
