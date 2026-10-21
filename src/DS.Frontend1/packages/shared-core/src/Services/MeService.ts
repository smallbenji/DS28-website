import type { HomeViewModelDto, MeDto } from "../types";
import { apiGet, apiPut, safeApiCall, safeApiOk } from "../lib/api";

const emptyMe: MeDto = {
    id: "",
    name: "",
    firstName: "",
    lastName: "",
    title: "",
    email: "",
    mustEnableTwoFactor: false,
    isImpersonating: false,
    roles: [],
    appRoles: [],
    isAuthenticated: false,
    passkeys: [],
    phone: "",
    profilePicture: null
};

export default class MeService {
    public async getMe(): Promise<MeDto> {
        return safeApiCall(async () => {
            const data = await apiGet<MeDto>("/api/v1/me");
            return data ? data : { ...emptyMe };
        }, { ...emptyMe });
    }

    public async updateProfilePicture(publicId: string | null): Promise<boolean> {
        return safeApiOk(() => apiPut("/api/v1/me/profile-picture", {
            image: publicId ? { publicId } : null
        }));
    }

    public async updatePhone(phone: string): Promise<boolean> {
        return safeApiOk(() => apiPut("/api/v1/me/phone", { phone }));
    }

    public async getHQ(): Promise<HomeViewModelDto> {
        return safeApiCall(async () => {
            const data = await apiGet<HomeViewModelDto>("/api/v1/home");
            return data ? data : { shortcuts: [] };
        }, { shortcuts: [] });
    }
}
