import type { AxiosResponse } from "axios";
import axios from "axios";


export default class InvitationService {
    public async getInvitation(id: string): Promise<Invitation | null> {
        try {
            const response: AxiosResponse<Invitation> = await axios({
                url: `/api/v1/invitation/${id}`,
                method: "GET"
            });

            return response.data;
        } catch (ex) {
            console.error(`Error fetching invitation with id ${id}:`, ex);
            return null;
        }
    }

    public async useInvitation(id: string, data: UserInvitationCreationDTO) {
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