import type { AxiosResponse } from "axios";
import axios from "axios";
import type { GoogleConnectionStatusDto } from "@/types";

export default class GoogleService {
    public async getStatus(): Promise<GoogleConnectionStatusDto> {
        const response: AxiosResponse<GoogleConnectionStatusDto> = await axios({
            url: "/api/v1/google/status",
            method: "GET"
        });
        return response.data;
    }

    public async disconnect(): Promise<void> {
        await axios({
            url: "/api/v1/google/disconnect",
            method: "POST"
        });
    }
}
