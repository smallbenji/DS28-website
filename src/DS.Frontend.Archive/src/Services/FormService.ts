import { apiGet } from "@/lib/api";
import type { FormTemplate } from "@/types";

export default class FormService {
    async getTemplate(id: string): Promise<FormTemplate> {
        return await apiGet<FormTemplate>(`/api/v1/forms/${encodeURIComponent(id)}`);
    }
}
