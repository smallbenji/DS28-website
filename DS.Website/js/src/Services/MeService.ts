import type { AxiosResponse } from "axios";
import axios from "axios";

export default class MeService {
    public async getMe(): Promise<MeDTO> {
        try {
            const response: AxiosResponse<MeDTO> = await axios({
                url: "/api/v1/me",
                method: "GET"
            });

            return response.data ? response.data : {name: ""};
        } catch {
            return {name:""};
        }
    }

    public async getHQ(): Promise<HomeViewModel> {
        try {
            const response: AxiosResponse<HomeViewModel> = await axios({
                url: "/api/v1/home",
                method: "GET"
            });

            return response.data ? response.data : {shortcuts:[]}
        } catch {
            return {shortcuts:[]}
        }
    }
}