import axios from 'axios';
import type { OpenIddictClientDto, OpenIddictClientSavedDto, OpenIddictClientUpdateDto } from '@/types';

const baseUrl = '/api/v1/openiddict/clients';

export default class OpenIddictService {
    public async getClients(): Promise<OpenIddictClientDto[]> {
        return (await axios.get<OpenIddictClientDto[]>(baseUrl)).data;
    }

    public async getAvailableRoles(): Promise<string[]> {
        return (await axios.get<string[]>(`${baseUrl}/roles`)).data;
    }

    private async headers() {
        const { data } = await axios.get<{ token: string }>(`${baseUrl}/antiforgery`);
        return { RequestVerificationToken: data.token };
    }

    public async create(data: OpenIddictClientUpdateDto): Promise<OpenIddictClientSavedDto> {
        return (await axios.post<OpenIddictClientSavedDto>(baseUrl, data, { headers: await this.headers() })).data;
    }

    public async update(id: string, data: OpenIddictClientUpdateDto): Promise<OpenIddictClientSavedDto> {
        return (await axios.put<OpenIddictClientSavedDto>(`${baseUrl}/${encodeURIComponent(id)}`, data, { headers: await this.headers() })).data;
    }

    public async rotateSecret(id: string): Promise<string> {
        const { data } = await axios.post<{ clientSecret: string }>(`${baseUrl}/${encodeURIComponent(id)}/rotate-secret`, null, { headers: await this.headers() });
        return data.clientSecret;
    }

    public async delete(id: string): Promise<void> {
        await axios.delete(`${baseUrl}/${encodeURIComponent(id)}`, { headers: await this.headers() });
    }
}
