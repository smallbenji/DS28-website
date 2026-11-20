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
    "chart-simple": "i-lucide-chart-column"
};

const betaPaths = ["/activity"];

export interface HqShortcut {
    title: string;
    icon: string;
    to: string;
    opensNewWindow: boolean;
}

export function toHqShortcut(entry: HQPanelEntryDto): HqShortcut {
    const iconName = entry.icon[entry.icon.length - 1] ?? "";
    const opensNewWindow = /^https?:\/\//i.test(entry.url);
    const inBeta = betaPaths.includes(entry.url);

    return {
        title: entry.title,
        icon: lucideIcons[iconName] ?? "i-lucide-link",
        to: inBeta || opensNewWindow ? entry.url : new URL(entry.url, window.location.origin).href,
        opensNewWindow
    };
}
