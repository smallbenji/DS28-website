import type { BackgroundJobState } from "@/types";

export interface JobStateMeta {
    label: string;
    icon: string;
    color: string;
    spin?: boolean;
}

export const jobStates: Record<BackgroundJobState, JobStateMeta> = {
    processing: { label: "Kører", icon: "i-lucide-loader", color: "text-info", spin: true },
    enqueued: { label: "I kø", icon: "i-lucide-list-ordered", color: "text-muted" },
    scheduled: { label: "Planlagt", icon: "i-lucide-clock", color: "text-warning" },
    awaiting: { label: "Venter", icon: "i-lucide-hourglass", color: "text-muted" },
    succeeded: { label: "Gennemført", icon: "i-lucide-circle-check", color: "text-success" },
    failed: { label: "Fejlet", icon: "i-lucide-circle-alert", color: "text-error" },
    deleted: { label: "Slettet", icon: "i-lucide-trash-2", color: "text-muted" }
};

export function jobState(state: string | null | undefined): JobStateMeta {
    return jobStates[(state ?? "").toLowerCase() as BackgroundJobState] ?? { label: state ?? "Ukendt", icon: "i-lucide-circle-help", color: "text-muted" };
}

export function parseUtc(value: string | null | undefined): Date | null {
    if (!value) return null;

    const hasZone = /([zZ]|[+-]\d{2}:?\d{2})$/.test(value);
    const date = new Date(hasZone ? value : `${value}Z`);

    return Number.isNaN(date.getTime()) ? null : date;
}

function startOfDay(date: Date): number {
    return new Date(date.getFullYear(), date.getMonth(), date.getDate()).getTime();
}

export function formatJobTime(value: string | null | undefined): string {
    const date = parseUtc(value);
    if (!date) return "-";

    const time = date.toLocaleTimeString("da-DK", { hour: "2-digit", minute: "2-digit" });
    const dayDiff = Math.round((startOfDay(date) - startOfDay(new Date())) / 86_400_000);

    if (dayDiff === 0) return `I dag kl. ${time}`;
    if (dayDiff === -1) return `I går kl. ${time}`;
    if (dayDiff === 1) return `I morgen kl. ${time}`;

    return `${date.toLocaleDateString("da-DK", { weekday: "short", day: "numeric", month: "short" })} kl. ${time}`;
}

export function formatJobDateTime(value: string | null | undefined): string {
    const date = parseUtc(value);
    if (!date) return "-";

    return date.toLocaleString("da-DK", { dateStyle: "short", timeStyle: "medium" });
}

export function formatRelative(value: string | null | undefined, now = Date.now()): string {
    const date = parseUtc(value);
    if (!date) return "";

    const seconds = Math.round((date.getTime() - now) / 1000);
    const abs = Math.abs(seconds);
    const future = seconds > 0;

    let text: string;
    if (abs < 10) return "nu";
    if (abs < 60) text = `${abs} sek.`;
    else if (abs < 3600) text = `${Math.round(abs / 60)} min.`;
    else if (abs < 86_400) {
        const hours = Math.floor(abs / 3600);
        const minutes = Math.round((abs % 3600) / 60);
        text = minutes > 0 ? `${hours} t ${minutes} min.` : `${hours} t`;
    } else text = `${Math.round(abs / 86_400)} dage`;

    return future ? `om ${text}` : `for ${text} siden`;
}

export function formatDuration(ms: number | null | undefined): string {
    if (ms == null) return "-";

    const seconds = ms / 1000;
    if (seconds < 10) return `${seconds.toLocaleString("da-DK", { minimumFractionDigits: 1, maximumFractionDigits: 1 })} sek.`;
    if (seconds < 120) return `${Math.round(seconds)} sek.`;

    return `${Math.round(seconds / 60)} min.`;
}

export function formatSize(bytes: number | null | undefined): string {
    if (bytes == null) return "";
    if (bytes < 1024) return `${bytes} B`;

    return `${(bytes / 1024).toLocaleString("da-DK", { maximumFractionDigits: 1 })} kB`;
}

export function describeCron(cron: string, timeZoneId: string | null): string {
    const parts = cron.trim().split(/\s+/);
    if (parts.length !== 5) return cron;

    const [minute, hour, dayOfMonth, month, dayOfWeek] = parts;
    const isNumber = (part: string) => /^\d+$/.test(part);

    if (isNumber(minute) && hour === "*" && dayOfMonth === "*" && month === "*" && dayOfWeek === "*") {
        return `Hver time, ${minute.padStart(2, "0")} min. over`;
    }

    if (isNumber(minute) && isNumber(hour) && dayOfMonth === "*" && month === "*" && dayOfWeek === "*") {
        const isUtc = !timeZoneId || timeZoneId.toUpperCase() === "UTC" || timeZoneId === "Etc/UTC";
        const time = isUtc
            ? new Date(Date.UTC(2000, 0, 1, Number(hour), Number(minute))).toLocaleTimeString("da-DK", { hour: "2-digit", minute: "2-digit" })
            : `${hour.padStart(2, "0")}.${minute.padStart(2, "0")}`;

        return `Dagligt kl. ${time}`;
    }

    return cron;
}

export function shortenStackTrace(stackTrace: string): { text: string; hiddenFrames: number } {
    const lines = stackTrace.split("\n");
    const kept = lines.filter((line, index) => index === 0 || !line.trimStart().startsWith("at ") || line.includes(" DS."));
    const hiddenFrames = lines.length - kept.length;

    return { text: kept.join("\n"), hiddenFrames };
}
