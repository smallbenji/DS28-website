import { apiGet, apiPut } from '../lib/api';

export interface RegistrationSettings {
    isPreSignupOpen: boolean;
    isSignupOpen: boolean;
}

export default class RegistrationSettingsService {
    async get(): Promise<RegistrationSettings> {
        return await apiGet<RegistrationSettings>('/api/v1/registration-settings');
    }

    async update(settings: Partial<RegistrationSettings>): Promise<RegistrationSettings> {
        return await apiPut<RegistrationSettings>('/api/v1/registration-settings', settings);
    }
}
