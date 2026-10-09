import axios from 'axios';
import type { StoredFileDto } from '@/types';

export default class FileService {
    async uploadProfilePicture(file: File): Promise<StoredFileDto> {
        const formData = new FormData();
        formData.append('file', file);

        const response = await axios.post<StoredFileDto>('/api/v1/files/profile', formData);

        return response.data;
    }

    async waitUntilReady(publicId: string, timeoutMs = 15000): Promise<boolean> {
        const deadline = Date.now() + timeoutMs;

        while (Date.now() < deadline) {
            try {
                await axios.get(`/api/v1/files/${publicId}`, { responseType: 'blob' });
                return true;
            } catch {
                await new Promise((resolve) => setTimeout(resolve, 500));
            }
        }

        return false;
    }
}
