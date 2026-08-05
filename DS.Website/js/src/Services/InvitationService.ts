import type { AxiosResponse } from "axios";
import axios from "axios";
import type { UserInvitationCreationDto, UserInvitationDto } from "@/types";


export default class InvitationService {
    public async getInvitation(id: string): Promise<UserInvitationDto | null> {
        try {
            const response: AxiosResponse<UserInvitationDto> = await axios({
                url: `/api/v1/invitation/${id}`,
                method: "GET"
            });

            return response.data;
        } catch (ex) {
            console.error(`Error fetching invitation with id ${id}:`, ex);
            return null;
        }
    }

    public async useInvitation(id: string, data: UserInvitationCreationDto) {
        try {
            const response: AxiosResponse = await axios({
                url: `/api/v1/invitation/${id}`,
                method: "POST",
                data: JSON.stringify(data),
                headers: {
                    "Content-Type": "application/json"
                }
            });

            if (response.status == 200)
                return true
            return false;
        } catch {
            return false;
        }
    }
}