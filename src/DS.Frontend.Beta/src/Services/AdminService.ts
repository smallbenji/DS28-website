import type { AdminViewModelDto, SignupProgressDto } from '@/types';
import { apiGet, apiPost } from '@/lib/api';

export default class AdminService {
    async get(): Promise<AdminViewModelDto> {
        return await apiGet<AdminViewModelDto>('/api/v1/admin');
    }

    async getSignupProgress(): Promise<SignupProgressDto[]> {
        return await apiGet<SignupProgressDto[]>('/api/v1/admin/signup-progress');
    }

    async syncHistoricRegistration(): Promise<void> {
        await apiPost<void>('/api/v1/admin/historic-registration/sync');
    }
}
