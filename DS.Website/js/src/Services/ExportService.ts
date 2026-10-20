import type { DataExportDto } from '@/types';
import { apiGet } from '@/lib/api';

export default class ExportService {
    async getExports(): Promise<DataExportDto[]> {
        return await apiGet<DataExportDto[]>('/api/v1/exports');
    }

    async download(key: string): Promise<Blob> {
        return await apiGet<Blob>(`/api/v1/exports/${encodeURIComponent(key)}`, { responseType: 'blob' });
    }
}
