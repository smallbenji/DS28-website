import type { VersionDto } from '../types';
import { apiGet } from '../lib/api';

export default class VersionService {
    async get(): Promise<VersionDto> {
        return await apiGet<VersionDto>('/api/version');
    }
}
