export type BackgroundJobState = "processing" | "enqueued" | "scheduled" | "succeeded" | "failed" | "deleted" | "awaiting";

export interface BackgroundJobStatisticsDto {
    processing: number;
    enqueued: number;
    scheduled: number;
    failed: number;
    succeededLast24Hours: number;
}

export interface FailedBackgroundJobDto {
    id: string;
    title: string;
    exceptionType: string | null;
    exceptionMessage: string | null;
    failedAt: string | null;
    attempts: number;
}

export interface RecurringBackgroundJobDto {
    id: string;
    title: string;
    cron: string;
    timeZoneId: string | null;
    nextExecution: string | null;
    lastExecution: string | null;
    lastJobId: string | null;
    lastJobState: string | null;
    error: string | null;
}

export interface BackgroundJobActivityDto {
    id: string;
    title: string;
    state: BackgroundJobState;
    at: string | null;
    durationMs: number | null;
}

export interface BackgroundJobOverviewDto {
    canManage: boolean;
    statistics: BackgroundJobStatisticsDto;
    failed: FailedBackgroundJobDto[];
    recurring: RecurringBackgroundJobDto[];
    activity: BackgroundJobActivityDto[];
}

export interface BackgroundJobHistoryItemDto {
    id: string;
    title: string;
    state: BackgroundJobState;
    finishedAt: string | null;
    durationMs: number | null;
    attempts: number;
}

export interface BackgroundJobHistoryDto {
    total: number;
    page: number;
    pageSize: number;
    types: string[];
    items: BackgroundJobHistoryItemDto[];
}

export interface BackgroundJobArgumentDto {
    name: string;
    value: string | null;
    masked: boolean;
    hidden: boolean;
    size: number | null;
}

export interface BackgroundJobAttemptDto {
    number: number;
    at: string;
    text: string | null;
}

export interface BackgroundJobDetailsDto {
    id: string;
    title: string;
    method: string | null;
    queue: string;
    state: string | null;
    createdAt: string | null;
    startedAt: string | null;
    finishedAt: string | null;
    server: string | null;
    attempts: number;
    argumentsUnavailable: boolean;
    arguments: BackgroundJobArgumentDto[];
    attemptHistory: BackgroundJobAttemptDto[];
    exceptionType: string | null;
    exceptionMessage: string | null;
    stackTrace: string | null;
}

export interface BackgroundJobHistoryQuery {
    state: string;
    type: string;
    search: string;
    page: number;
}
