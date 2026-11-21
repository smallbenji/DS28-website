import axios from "axios";
import { District, type GroupDto } from "@/types";
import { apiGet, safeApiCall } from "@/lib/api";

export default class GroupService {
    public async getGroup(): Promise<GroupDto> {
        return safeApiCall(
            () => apiGet<GroupDto>("/api/v1/group"),
            { name: "", id: "0", district: District.DANEHOF, patrols: [], scouts: [], users: [] }
        );
    }

    public async findGroup(): Promise<GroupDto | null> {
        try {
            return await apiGet<GroupDto>("/api/v1/group");
        } catch (error) {
            if (axios.isAxiosError(error) && error.response?.status === 404) return null;
            throw error;
        }
    }
}
