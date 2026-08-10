import { District, type GroupDto } from "@/types";
import axios, { type AxiosResponse } from "axios";

export default class GroupService {
    public async getGroup(): Promise<GroupDto> {
        try {
            const response: AxiosResponse<GroupDto> = await axios({
                url: "/api/v1/group",
                method: "GET"
            });

            return response.data;
        } catch {
            return { name: "", id: "0", district: District.DANEHOF, patrols: [], scouts: [] };
        }
    }
}