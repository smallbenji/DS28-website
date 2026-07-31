import type { AxiosResponse } from "axios";
import axios from "axios";

export default class MeService {
    public async getMe(): Promise<MeDTO | null> {
        try {
            const response: AxiosResponse = await axios({
                url: "/api/v1/me",
                method: "GET"
            });
            return response.data ? response.data : null;
        } catch {
            return null;
        }
    }
}
