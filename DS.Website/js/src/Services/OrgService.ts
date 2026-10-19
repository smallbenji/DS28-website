import type { OrgPositionCreateDto, OrgPositionDto, OrgPositionUpdateDto } from "@/types";
import { apiDelete, apiGet, apiPost, apiPut, safeApiCall, safeApiOk } from "@/lib/api";

export default class OrgService {
    public async getPositions(): Promise<OrgPositionDto[]> {
        return safeApiCall(async () => {
            const data = await apiGet<OrgPositionDto[]>("/api/v1/org");
            return Array.isArray(data) ? data : [];
        }, []);
    }

    public async getAssignableGroups(): Promise<string[]> {
        return safeApiCall(async () => {
            const data = await apiGet<string[]>("/api/v1/org/assignable-groups");
            return Array.isArray(data) ? data : [];
        }, []);
    }

    public async createPosition(data: OrgPositionCreateDto): Promise<boolean> {
        return safeApiOk(() => apiPost("/api/v1/org", data));
    }

    public async updatePosition(id: number, data: OrgPositionUpdateDto): Promise<boolean> {
        return safeApiOk(() => apiPut(`/api/v1/org/${id}`, data));
    }

    public async createPa(id: number): Promise<boolean> {
        return safeApiOk(() => apiPost(`/api/v1/org/${id}/pa`));
    }

    public async movePosition(id: number, parentPositionId: number | null): Promise<boolean> {
        return safeApiOk(() => apiPut(`/api/v1/org/${id}/parent`, { parentPositionId }));
    }

    public async deletePosition(id: number): Promise<boolean> {
        return safeApiOk(() => apiDelete(`/api/v1/org/${id}`));
    }

    public async addHolder(id: number, userId: string): Promise<boolean> {
        return safeApiOk(() => apiPost(`/api/v1/org/${id}/holders`, { userId }));
    }

    public async removeHolder(id: number, userId: string): Promise<boolean> {
        return safeApiOk(() => apiDelete(`/api/v1/org/${id}/holders/${userId}`));
    }

    public async invite(id: number, email: string): Promise<boolean> {
        return safeApiOk(() => apiPost(`/api/v1/org/${id}/invite`, { email }));
    }
}
