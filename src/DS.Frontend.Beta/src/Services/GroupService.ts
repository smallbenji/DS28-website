import { District, type GroupDto } from "@/types";
import { apiGet, safeApiCall } from "@/lib/api";

export default class GroupService {
    public async getGroup(): Promise<GroupDto> {
        return safeApiCall(
            () => apiGet<GroupDto>("/api/v1/group"),
            { name: "", id: "0", district: District.DANEHOF, patrols: [], scouts: [], users: [] }
        );
    }
}
