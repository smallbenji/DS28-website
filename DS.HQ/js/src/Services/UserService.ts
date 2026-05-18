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

    public async getGroups() {
        try {
            const response: AxiosResponse<KcGroup[]> = await axios({
                url: "/api/v1/user/groups",
                method: "GET"
            });

            if (response.status == 200) {
                return response.data;
            }
            else {
                return [];
            }
        } catch {
            return [];
        }
    }

    public async AssignRoleToUser(user: DSUser, roleId: string) {
        try {
            const response: AxiosResponse = await axios({
                url: `/api/v1/user/${user.user.id}/role/add`,
                method: "PUT",
                data: JSON.stringify(roleId),
                headers: {
                    "Content-Type": "application/json"
                }
            });

            if (response.status == 200) {
                return true;
            } else {
                return false;
            }
        } catch {
            return false;
        }
    }

    public async RemoveRoleFromUser(user: DSUser, roleId: string) {
        try {
            const response: AxiosResponse = await axios({
                url: `api/v1/user/${user.user.id}/role/remove`,
                method: "PUT",
                data: JSON.stringify(roleId),
                headers: {
                    "Content-Type": "application/json"
                }
            });

            if (response.status == 200) {
                return true;
            } else {
                return false;
            }
        } catch {
            return false;
        }
    }
}