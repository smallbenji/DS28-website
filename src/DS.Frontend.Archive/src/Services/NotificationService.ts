import type { NotificationPreferenceDto } from '@/types';
import { apiGet, apiPut } from '@/lib/api';

export default class NotificationService {
    async getPreferences(): Promise<NotificationPreferenceDto[]> {
        return await apiGet<NotificationPreferenceDto[]>('/api/v1/me/notifications');
    }

    async updatePreferences(types: string[]): Promise<void> {
        await apiPut<void>('/api/v1/me/notifications', { types });
    }
}
