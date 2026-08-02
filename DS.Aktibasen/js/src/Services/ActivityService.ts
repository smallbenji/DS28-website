import type { AxiosResponse } from "axios";
import axios from "axios";

export default class ActivityService {
    public async getActivities(): Promise<ActivityDTO[]> {
        try {
            const response: AxiosResponse = await axios({
                url: "/api/v1/activity",
                method: "GET"
            });
            return response.data ? response.data : [];
        } catch {
            return [];
        }
    }

    public async createActivity(data: createActivityDTO): Promise<boolean> {
        try {
            await axios({
                url: "/api/v1/activity",
                method: "POST",
                data: { data }
            });

            return true;
        } catch {
            return false;
        }
    }
}