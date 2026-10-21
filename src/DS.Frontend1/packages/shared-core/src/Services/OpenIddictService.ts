import type { OpenIddictClientDto, OpenIddictClientSavedDto, OpenIddictClientUpdateDto } from '../types';
import { apiDelete, apiGet, apiPost, apiPut } from '../lib/api';

const baseUrl = '/api/v1/openiddict/clients';

export default class OpenIddictService {
    public async getClients(): Promise<OpenIddictClientDto[]> {
        return await apiGet<OpenIddictClientDto[]>(baseUrl);
    }

    public async getAvailableRoles(): Promise<string[]> {
        return await apiGet<string[]>(`${baseUrl}/roles`);
    }

    public async create(data: OpenIddictClientUpdateDto): Promise<OpenIddictClientSavedDto> {
        return await apiPost<OpenIddictClientSavedDto>(baseUrl, data);
    }

    public async update(id: string, data: OpenIddictClientUpdateDto): Promise<OpenIddictClientSavedDto> {
        return await apiPut<OpenIddictClientSavedDto>(`${baseUrl}/${encodeURIComponent(id)}`, data);
    }

    public async rotateSecret(id: string): Promise<string> {
        const data = await apiPost<{ clientSecret: string }>(`${baseUrl}/${encodeURIComponent(id)}/rotate-secret`);
        return data.clientSecret;
    }

    public async delete(id: string): Promise<void> {
        await apiDelete(`${baseUrl}/${encodeURIComponent(id)}`);
    }
}
