const listFormat = new Intl.DateTimeFormat("da-DK", { day: "numeric", month: "short", year: "numeric", timeZone: "Europe/Copenhagen" });
const detailFormat = new Intl.DateTimeFormat("da-DK", { dateStyle: "long", timeStyle: "short", timeZone: "Europe/Copenhagen" });

export function lastActiveLabel(lastActiveAt: string | null): string {
    return lastActiveAt ? `Sidst aktiv ${listFormat.format(new Date(lastActiveAt))}` : "Ingen aktivitet registreret";
}

export function lastActiveDetailLabel(lastActiveAt: string | null): string {
    return lastActiveAt ? `Sidst aktiv ${detailFormat.format(new Date(lastActiveAt))}` : "Ingen aktivitet registreret";
}

export function lastLoginDetailLabel(lastLoginAt: string | null): string {
    return lastLoginAt ? `Sidst logget ind ${detailFormat.format(new Date(lastLoginAt))}` : "Intet login registreret";
}
