import type { AxiosResponse } from "axios";
import axios from "axios";
import type { HomeViewModelDto, MeDto } from "@/types";

export default class MeService {
    public async getMe(): Promise<MeDto> {
        try {
            const response: AxiosResponse<MeDto> = await axios({
                url: "/api/v1/me",
                method: "GET"
            });

            return response.data ? response.data : {id: "", name: "", firstName: "", lastName: "", mustEnableTwoFactor: false, roles: [], appRoles: [], isAuthenticated: false};
        } catch {
            return {id:"", name: "", firstName: "", lastName: "", mustEnableTwoFactor: false, roles: [], appRoles: [], isAuthenticated: false};
        }
    }

    public async getHQ(): Promise<HomeViewModelDto> {
        try {
            const response: AxiosResponse<HomeViewModelDto> = await axios({
                url: "/api/v1/home",
                method: "GET"
            });

            return response.data ? response.data : {shortcuts:[]}
        } catch {
            return {shortcuts:[]}
        }
    }
}