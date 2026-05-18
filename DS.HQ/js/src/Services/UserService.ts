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

    public async updateUser(user: DSUser) {
        try {
            const response: AxiosResponse = await axios({
                url: "/api/v1/user",
                method: "PUT",
                data: JSON.stringify(user),
                headers: {
                    "Content-Type": "application/json"
                }
            });

            if (response.status == 200){
                return true;
            } else {
                return false;
            }
        } catch {
            return false;
        }
    }

    public async createUser(user: DSUser) {
        try {
            const response: AxiosResponse = await axios({
                url: "/api/v1/user",
                method: "POST",
                data: JSON.stringify(user),
                headers: {
                    "Content-Type": "application/json"
                }
            });

            if (response.status == 200){
                return true;
            } else
            {
                return false;
            }
        } catch {
            return false;
        }
    }
}