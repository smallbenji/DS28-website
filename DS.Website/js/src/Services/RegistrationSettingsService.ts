import axios from 'axios';

export interface RegistrationSettings {
    isPreSignupOpen: boolean;
    isSignupOpen: boolean;
}

export default class RegistrationSettingsService {
    async get(): Promise<RegistrationSettings> {
        return (await axios.get<RegistrationSettings>('/api/v1/registration-settings')).data;
    }

    async update(settings: Partial<RegistrationSettings>): Promise<RegistrationSettings> {
        return (await axios.put<RegistrationSettings>('/api/v1/registration-settings', settings)).data;
    }
}
