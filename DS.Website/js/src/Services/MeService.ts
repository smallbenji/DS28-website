import type { AxiosResponse } from "axios";
import axios from "axios";
import type { HomeViewModelDto, MeDto } from "@/types";

const emptyMe: MeDto = {
    id: "",
    name: "",
    firstName: "",
    lastName: "",
    mustEnableTwoFactor: false,
    roles: [],
    appRoles: [],
    isAuthenticated: false,
    passkeys: [],
    phone: "",
    profilePicture: null
};

export default class MeService {
    public async getMe(): Promise<MeDto> {
        try {
            const response: AxiosResponse<MeDto> = await axios({
                url: "/api/v1/me",
                method: "GET"
            });

            return response.data ? response.data : { ...emptyMe };
        } catch {
            return { ...emptyMe };
        }
    }

    public async updateProfilePicture(publicId: string | null): Promise<boolean> {
        try {
            const response: AxiosResponse = await axios({
                url: "/api/v1/me/profile-picture",
                method: "PUT",
                data: { image: publicId ? { publicId } : null }
            });

            return response.status === 200;
        } catch {
            return false;
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