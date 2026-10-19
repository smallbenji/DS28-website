import type { OpenIddictClientDto, OpenIddictClientSavedDto, OpenIddictClientUpdateDto } from '@/types';
import { apiDelete, apiGet, apiPost, apiPut } from '@/lib/api';

const baseUrl = '/api/v1/openiddict/clients';

export default class OpenIddictService {
    public async getClients(): Promise<OpenIddictClientDto[]> {
        return await apiGet<OpenIddictClientDto[]>(baseUrl);
    }

    public async getAvailableRoles(): Promise<string[]> {
        return await apiGet<string[]>(`${baseUrl}/roles`);
    }

    private async headers() {
        const data = await apiGet<{ token: string }>(`${baseUrl}/antiforgery`);
        return { RequestVerificationToken: data.token };
    }

    public async create(data: OpenIddictClientUpdateDto): Promise<OpenIddictClientSavedDto> {
        return await apiPost<OpenIddictClientSavedDto>(baseUrl, data, { headers: await this.headers() });
    }

    public async update(id: string, data: OpenIddictClientUpdateDto): Promise<OpenIddictClientSavedDto> {
        return await apiPut<OpenIddictClientSavedDto>(`${baseUrl}/${encodeURIComponent(id)}`, data, { headers: await this.headers() });
    }

    public async rotateSecret(id: string): Promise<string> {
        const data = await apiPost<{ clientSecret: string }>(`${baseUrl}/${encodeURIComponent(id)}/rotate-secret`, null, { headers: await this.headers() });
        return data.clientSecret;
    }

    public async delete(id: string): Promise<void> {
        await apiDelete(`${baseUrl}/${encodeURIComponent(id)}`, { headers: await this.headers() });
    }
}
