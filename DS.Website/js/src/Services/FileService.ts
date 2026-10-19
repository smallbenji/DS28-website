import type { StoredFileDto } from '@/types';
import { apiGet, apiPost } from '@/lib/api';

export default class FileService {
    async uploadProfilePicture(file: File): Promise<StoredFileDto> {
        const formData = new FormData();
        formData.append('file', file);

        return await apiPost<StoredFileDto>('/api/v1/files/profile', formData, {
            headers: { 'Content-Type': undefined }
        });
    }

    async waitUntilReady(publicId: string, timeoutMs = 15000): Promise<boolean> {
        const deadline = Date.now() + timeoutMs;

        while (Date.now() < deadline) {
            try {
                await apiGet(`/api/v1/files/${publicId}`, { responseType: 'blob' });
                return true;
            } catch {
                await new Promise((resolve) => setTimeout(resolve, 500));
            }
        }

        return false;
    }
}
