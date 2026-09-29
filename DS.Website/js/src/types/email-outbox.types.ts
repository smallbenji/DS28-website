export interface EmailOutboxDto {
    id: number;
    eventType: string;
    correlationId: string | null;
    toEmail: string;
    subject: string;
    body: string;
    createdAt: string;
    nextAttemptAt: string;
    sentAt: string | null;
    failedAt: string | null;
    lockedAt: string | null;
    lockedBy: string | null;
    attempts: number;
    lastError: string | null;
}

export interface EmailOutboxPageDto {
    items: EmailOutboxDto[];
    total: number;
    pendingCount: number;
    lockedCount: number;
    failedCount: number;
}
