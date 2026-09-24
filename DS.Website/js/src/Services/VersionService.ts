import axios from 'axios';
import type { VersionDto } from '@/types';

export default class VersionService {
    async get(): Promise<VersionDto> {
        return (await axios.get<VersionDto>('/api/version')).data;
    }
}