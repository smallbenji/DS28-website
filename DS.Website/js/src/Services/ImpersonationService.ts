import type { AxiosResponse } from "axios";
import axios from "axios";
import type { ImpersonationGroupDto } from "@/types";

export default class ImpersonationService {
    public async getGroups(): Promise<ImpersonationGroupDto[]> {
        try {
            const response: AxiosResponse<ImpersonationGroupDto[]> = await axios({
                url: "/api/v1/impersonation/groups",
                method: "GET"
            });

            return response.data ?? [];
        } catch {
            return [];
        }
    }

    public async start(group: string): Promise<boolean> {
        try {
            const response: AxiosResponse = await axios({
                url: "/api/v1/impersonation/start",
                method: "POST",
                data: { group }
            });

            return response.status === 200;
        } catch {
            return false;
        }
    }

    public async stop(): Promise<boolean> {
        try {
            const response: AxiosResponse = await axios({
                url: "/api/v1/impersonation/stop",
                method: "POST"
            });

            return response.status === 200;
        } catch {
            return false;
        }
    }
}