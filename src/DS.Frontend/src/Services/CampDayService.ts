import { apiDelete, apiGet, apiPost, apiPut } from '@/lib/api';

export type CampHalf = 'First' | 'Second' | 'Both';

export interface CampDay {
    id: number;
    date: string;
    half: CampHalf;
}

export interface CampDayInput {
    date: string;
    half: CampHalf;
}

export default class CampDayService {
    async list(): Promise<CampDay[]> {
        return await apiGet<CampDay[]>('/api/v1/camp-days');
    }

    async create(day: CampDayInput): Promise<CampDay> {
        return await apiPost<CampDay>('/api/v1/camp-days', day);
    }

    async update(id: number, day: CampDayInput): Promise<CampDay> {
        return await apiPut<CampDay>(`/api/v1/camp-days/${id}`, day);
    }

    async remove(id: number): Promise<void> {
        await apiDelete<void>(`/api/v1/camp-days/${id}`);
    }
}
