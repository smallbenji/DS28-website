import type { AxiosResponse } from "axios";
import axios from "axios";

export default class UserService {
    public async getUsers(): Promise<UserSummaryDTO[]> {
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

    public async updateUser(user: UserSummaryDTO) {
        try {
            // const response: AxiosResponse = await axios({
            //     url: "/api/v1/user",
            //     method: "PUT",
            //     data: user,
            //     headers: {
            //         "Content-Type": "application/json"
            //     }
            // });

            const response: AxiosResponse = await axios.put("/api/v1/user", user);

            if (response.status == 200){
                return true;
            } else {
                return false;
            }
        } catch {
            return false;
        }
    }

    public async createUser(user: UserSummaryDTO) {
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
            const response: AxiosResponse<AppRole[]> = await axios({
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

    public async AssignRoleToUser(user: UserSummaryDTO, roleName: string) {
        try {
            const response: AxiosResponse = await axios({
                url: `/api/v1/user/${user.id}/role/add`,
                method: "PUT",
                data: JSON.stringify(roleName),
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

    public async RemoveRoleFromUser(user: UserSummaryDTO, roleName: string) {
        try {
            const response: AxiosResponse = await axios({
                url: `api/v1/user/${user.id}/role/remove`,
                method: "PUT",
                data: JSON.stringify(roleName),
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

    public async deleteUser(user: UserSummaryDTO) {
        try {
            const response: AxiosResponse = await axios({
                url: `/api/v1/user/${user.id}`,
                method: "DELETE"
            });

            return response.status == 200;
        } catch {
            return false;
        }
    }

    public async lockUser(user: UserSummaryDTO) {
        try {
            const response: AxiosResponse = await axios({
                url: `/api/v1/user/${user.id}/lock`,
                method: "PUT"
            });

            return response.status == 200;
        } catch {
            return false;
        }
    }

    public async unlockUser(user: UserSummaryDTO) {
        try {
            const response: AxiosResponse = await axios({
                url: `/api/v1/user/${user.id}/unlock`,
                method: "PUT"
            });

            return response.status == 200;
        } catch {
            return false;
        }
    }

    public async inviteUser(email: string, roles: string[]) {        try {
            const response: AxiosResponse = await axios({
                url: "/api/v1/user/invite",
                method: "POST",
                data: JSON.stringify({ email, roles }),
                headers: {
                    "Content-Type": "application/json"
                }
            });

            return response.status == 200;
        } catch {
            return false;
        }
    }
}