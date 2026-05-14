import type { AxiosResponse } from "axios";
import axios from "axios";

export default class UserService {
    public async getUsers(): Promise<DSUser[]> {
        try {
            const response: AxiosResponse = await axios({
                url: "/api/v1/user",
                method: "GET"
            });

            return Array.isArray(response.data) ? response.data : [];
        } catch {
            return [];
        }
    }
}