import axios from 'axios';
import type { NotificationPreferenceDto } from '@/types';

export default class NotificationService {
    async getPreferences(): Promise<NotificationPreferenceDto[]> {
        return (await axios.get<NotificationPreferenceDto[]>('/api/v1/me/notifications')).data;
    }

    async updatePreferences(types: string[]): Promise<void> {
        await axios.put('/api/v1/me/notifications', { types });
    }
}
