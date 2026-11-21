import type { ImpersonationGroupDto } from "@/types";
import { apiGet, apiPost, safeApiCall, safeApiOk } from "@/lib/api";

export default class ImpersonationService {
    public async getGroups(): Promise<ImpersonationGroupDto[]> {
        return safeApiCall(() => apiGet<ImpersonationGroupDto[]>("/api/v1/impersonation/groups"), []);
    }

    public async start(group: string): Promise<boolean> {
        return safeApiOk(() => apiPost("/api/v1/impersonation/start", { group }));
    }

    public async stop(): Promise<boolean> {
        return safeApiOk(() => apiPost("/api/v1/impersonation/stop"));
    }
}
