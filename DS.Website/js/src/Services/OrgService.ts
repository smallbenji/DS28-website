import type { AxiosResponse } from "axios";
import axios from "axios";
import type { OrgPositionCreateDto, OrgPositionDto, OrgPositionUpdateDto } from "@/types";

export default class OrgService {
    public async getPositions(): Promise<OrgPositionDto[]> {
        try {
            const response: AxiosResponse<OrgPositionDto[]> = await axios({
                url: "/api/v1/org",
                method: "GET"
            });
            return Array.isArray(response.data) ? response.data : [];
        } catch {
            return [];
        }
    }

    public async getAssignableGroups(): Promise<string[]> {
        try {
            const response: AxiosResponse<string[]> = await axios({
                url: "/api/v1/org/assignable-groups",
                method: "GET"
            });
            return Array.isArray(response.data) ? response.data : [];
        } catch {
            return [];
        }
    }

    public async createPosition(data: OrgPositionCreateDto): Promise<boolean> {
        try {
            await axios({
                url: "/api/v1/org",
                method: "POST",
                data
            });
            return true;
        } catch {
            return false;
        }
    }

    public async updatePosition(id: number, data: OrgPositionUpdateDto): Promise<boolean> {
        try {
            await axios({
                url: `/api/v1/org/${id}`,
                method: "PUT",
                data
            });
            return true;
        } catch {
            return false;
        }
    }

    public async movePosition(id: number, parentPositionId: number | null): Promise<boolean> {
        try {
            await axios({
                url: `/api/v1/org/${id}/parent`,
                method: "PUT",
                data: { parentPositionId }
            });
            return true;
        } catch {
            return false;
        }
    }

    public async deletePosition(id: number): Promise<boolean> {
        try {
            await axios({
                url: `/api/v1/org/${id}`,
                method: "DELETE"
            });
            return true;
        } catch {
            return false;
        }
    }

    public async addHolder(id: number, userId: string): Promise<boolean> {
        try {
            await axios({
                url: `/api/v1/org/${id}/holders`,
                method: "POST",
                data: { userId }
            });
            return true;
        } catch {
            return false;
        }
    }

    public async removeHolder(id: number, userId: string): Promise<boolean> {
        try {
            await axios({
                url: `/api/v1/org/${id}/holders/${userId}`,
                method: "DELETE"
            });
            return true;
        } catch {
            return false;
        }
    }

    public async invite(id: number, email: string): Promise<boolean> {
        try {
            await axios({
                url: `/api/v1/org/${id}/invite`,
                method: "POST",
                data: { email }
            });
            return true;
        } catch {
            return false;
        }
    }
}
