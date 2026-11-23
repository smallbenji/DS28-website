import type { BackgroundJobDetailsDto, BackgroundJobHistoryDto, BackgroundJobHistoryQuery, BackgroundJobOverviewDto } from '@/types';
import { apiDelete, apiGet, apiPost } from '@/lib/api';

const baseUrl = '/api/v1/background-jobs';

function toNumber(value: number | string): number {
    return Number(value);
}

function toOptionalNumber(value: number | string | null): number | null {
    return value == null ? null : Number(value);
}

export default class BackgroundJobService {
    async getOverview(): Promise<BackgroundJobOverviewDto> {
        const overview = await apiGet<BackgroundJobOverviewDto>(baseUrl);
        const statistics = overview.statistics;

        return {
            ...overview,
            statistics: {
                processing: toNumber(statistics.processing),
                enqueued: toNumber(statistics.enqueued),
                scheduled: toNumber(statistics.scheduled),
                failed: toNumber(statistics.failed),
                succeededLast24Hours: toNumber(statistics.succeededLast24Hours)
            },
            failed: overview.failed.map(job => ({ ...job, attempts: toNumber(job.attempts) })),
            activity: overview.activity.map(item => ({ ...item, durationMs: toOptionalNumber(item.durationMs) }))
        };
    }

    async getHistory(query: BackgroundJobHistoryQuery): Promise<BackgroundJobHistoryDto> {
        const history = await apiGet<BackgroundJobHistoryDto>(`${baseUrl}/history`, { params: query });

        return {
            ...history,
            total: toNumber(history.total),
            page: toNumber(history.page),
            pageSize: toNumber(history.pageSize),
            items: history.items.map(item => ({
                ...item,
                attempts: toNumber(item.attempts),
                durationMs: toOptionalNumber(item.durationMs)
            }))
        };
    }

    async getDetails(id: string): Promise<BackgroundJobDetailsDto> {
        const details = await apiGet<BackgroundJobDetailsDto>(`${baseUrl}/${encodeURIComponent(id)}`);

        return {
            ...details,
            attempts: toNumber(details.attempts),
            arguments: details.arguments.map(argument => ({ ...argument, size: toOptionalNumber(argument.size) })),
            attemptHistory: details.attemptHistory.map(attempt => ({ ...attempt, number: toNumber(attempt.number) }))
        };
    }

    async requeue(id: string): Promise<void> {
        await apiPost(`${baseUrl}/${encodeURIComponent(id)}/requeue`);
    }

    async requeueFailed(): Promise<number> {
        const result = await apiPost<{ count: number | string }>(`${baseUrl}/requeue-failed`);
        return toNumber(result.count);
    }

    async delete(id: string): Promise<void> {
        await apiDelete(`${baseUrl}/${encodeURIComponent(id)}`);
    }

    async triggerRecurring(id: string): Promise<void> {
        await apiPost(`${baseUrl}/recurring/${encodeURIComponent(id)}/trigger`);
    }
}
