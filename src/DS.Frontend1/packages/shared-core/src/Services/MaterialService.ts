import { apiGet, safeApiCall } from "../lib/api";
import type { MaterialDto } from "../types";

export default class MaterialService {
    public async getMaterials(): Promise<MaterialDto[]> {
        return safeApiCall(() => apiGet<MaterialDto[]>("/api/v1/material"), []);
    }
}
