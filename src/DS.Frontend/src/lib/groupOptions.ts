import { computed } from "vue";
import { storeToRefs } from "pinia";
import { useGroupsStore } from "@/Stores/GroupsStore";
import type { GroupDto } from "@/types";

export const noGroup = "none";

export function useGroupOptions() {
    const { Groups } = storeToRefs(useGroupsStore());

    const groupItems = computed(() => [
        { label: "Ingen gruppe", value: noGroup },
        ...[...Groups.value.groups]
            .sort((a, b) => a.name.localeCompare(b.name, "da"))
            .map((group) => ({ label: group.name, value: String(group.id) }))
    ]);

    function findGroup(value: string): GroupDto | null {
        if (value === noGroup) return null;

        return Groups.value.groups.find((group) => String(group.id) === value) ?? null;
    }

    function groupValue(group: GroupDto | null): string {
        return group ? String(group.id) : noGroup;
    }

    return { groupItems, findGroup, groupValue };
}
