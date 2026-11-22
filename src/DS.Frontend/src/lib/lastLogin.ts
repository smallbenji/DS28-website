const listFormat = new Intl.DateTimeFormat("da-DK", { day: "numeric", month: "short", year: "numeric", timeZone: "Europe/Copenhagen" });
const detailFormat = new Intl.DateTimeFormat("da-DK", { dateStyle: "long", timeStyle: "short", timeZone: "Europe/Copenhagen" });

export function lastLoginLabel(lastLoginAt: string | null): string {
    return lastLoginAt ? `Sidst logget ind ${listFormat.format(new Date(lastLoginAt))}` : "Intet login registreret";
}

export function lastLoginDetailLabel(lastLoginAt: string | null): string {
    return lastLoginAt ? `Sidst logget ind ${detailFormat.format(new Date(lastLoginAt))}` : "Intet login registreret";
}
