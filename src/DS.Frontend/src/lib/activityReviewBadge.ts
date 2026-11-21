import type { ActivityReviewStatus } from "@/types";

type BadgeColor = "neutral" | "info" | "warning" | "success";

const badgeColors: Record<ActivityReviewStatus, BadgeColor> = {
    Draft: "neutral",
    Submitted: "info",
    ChangesRequested: "warning",
    Approved: "success"
};

export function activityReviewBadgeColor(status: ActivityReviewStatus): BadgeColor {
    return badgeColors[status] ?? "neutral";
}
