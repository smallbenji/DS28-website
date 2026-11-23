import type { CampDay } from "@/Services/CampDayService";

export type CampPeriod = "whole" | "first" | "second" | "custom";

export type FixedCampPeriod = Exclude<CampPeriod, "custom">;

export const fixedPeriodLabels: Record<FixedCampPeriod, string> = {
    whole: "Hel uge",
    first: "1. halvdel",
    second: "2. halvdel"
};

export const periodLabels: Record<CampPeriod, string> = {
    ...fixedPeriodLabels,
    custom: "Enkelte dage"
};

export function daysForPeriod(days: CampDay[], period: FixedCampPeriod): number[] {
    return days
        .filter((day) => period === "whole" || (period === "first" ? day.half !== "Second" : day.half !== "First"))
        .map((day) => day.id);
}

function sameDays(selected: Set<number>, ids: number[]): boolean {
    return ids.length > 0 && ids.length === selected.size && ids.every((id) => selected.has(id));
}

export function periodOf(campDayIds: number[], days: CampDay[]): CampPeriod | null {
    if (campDayIds.length === 0) return null;

    const selected = new Set(campDayIds);
    const fixed: FixedCampPeriod[] = ["whole", "first", "second"];

    return fixed.find((period) => sameDays(selected, daysForPeriod(days, period))) ?? "custom";
}

export function availablePeriods(days: CampDay[]): FixedCampPeriod[] {
    const fixed: FixedCampPeriod[] = ["whole", "first", "second"];

    return fixed.filter((period) => daysForPeriod(days, period).length > 0);
}

function parseDate(value: string): Date {
    const [year, month, day] = value.split("-").map(Number);
    return new Date(year, month - 1, day);
}

const shortDayFormat = new Intl.DateTimeFormat("da-DK", { weekday: "short", day: "numeric", month: "numeric" });
const longDayFormat = new Intl.DateTimeFormat("da-DK", { weekday: "long", day: "numeric", month: "long" });

export function formatShortDay(value: string): string {
    return shortDayFormat.format(parseDate(value)).replace(/\.$/, "");
}

export function formatLongDay(value: string): string {
    return longDayFormat.format(parseDate(value));
}

export function halfLabel(day: CampDay): string {
    if (day.half === "Both") return "Skiftedag";

    return day.half === "First" ? "1. halvdel" : "2. halvdel";
}

export function sortCampDays(days: CampDay[]): CampDay[] {
    return [...days].sort((a, b) => a.date.localeCompare(b.date));
}
