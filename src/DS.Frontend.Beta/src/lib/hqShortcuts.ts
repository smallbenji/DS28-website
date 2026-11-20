import type { HQPanelEntryDto } from "@/types";

const lucideIcons: Record<string, string> = {
    gear: "i-lucide-settings",
    newspaper: "i-lucide-newspaper",
    sitemap: "i-lucide-network",
    "table-columns": "i-lucide-columns-3",
    globe: "i-lucide-globe",
    wordpress: "i-lucide-pen-line",
    users: "i-lucide-users",
    "users-gear": "i-lucide-users-round",
    "user-pen": "i-lucide-user-pen",
    sliders: "i-lucide-sliders-horizontal",
    "file-arrow-down": "i-lucide-file-down",
    "chart-simple": "i-lucide-chart-column",
    h: "i-lucide-list-checks"
};

const betaPaths = ["/activity", "/admin", "/admin/user", "/admin/groups", "/admin/camp-settings", "/admin/exports", "/admin/google"];

export interface HqShortcut {
    title: string;
    icon: string;
    to: string;
    opensNewWindow: boolean;
}

export function lucideIcon(fontAwesomeName: string): string {
    return lucideIcons[fontAwesomeName] ?? "i-lucide-link";
}

export function toHqShortcut(entry: HQPanelEntryDto): HqShortcut {
    const isAbsolute = /^https?:\/\//i.test(entry.url);
    const inBeta = !entry.mvc && betaPaths.includes(entry.url);

    return {
        title: entry.title,
        icon: lucideIcon(entry.icon[entry.icon.length - 1] ?? ""),
        to: inBeta || isAbsolute ? entry.url : new URL(entry.url, window.location.origin).href,
        opensNewWindow: isAbsolute || entry.mvc
    };
}
