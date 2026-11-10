import type { SearchResultDto } from "@/types";
import { apiGet } from "@/lib/api";

export default class SearchService {
    public async search(query: string): Promise<SearchResultDto[]> {
        const data = await apiGet<SearchResultDto[]>("/api/v1/search", { params: { q: query } });
        return Array.isArray(data) ? data : [];
    }
}
