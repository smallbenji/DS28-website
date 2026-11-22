<template>
    <div class="flex flex-col gap-5">
        <div>
            <p class="mb-2 text-sm font-semibold text-highlighted">Vis kun</p>
            <div class="flex flex-wrap gap-2">
                <UButton
                    v-for="filter in boardQuickFilters"
                    :key="filter.key"
                    :label="filter.label"
                    :icon="filter.icon"
                    :size="size"
                    :color="filters.quick[filter.key] ? 'primary' : 'neutral'"
                    :variant="filters.quick[filter.key] ? 'subtle' : 'outline'"
                    :aria-pressed="filters.quick[filter.key]"
                    class="rounded-full"
                    @click="filters.quick[filter.key] = !filters.quick[filter.key]"
                />
            </div>
        </div>

        <div>
            <p class="mb-2 text-sm font-semibold text-highlighted">Prioritet</p>
            <div class="flex flex-wrap gap-2">
                <UButton
                    v-for="priority in boardPriorityFilters"
                    :key="priority.value"
                    :label="priority.label"
                    :icon="priority.icon"
                    :size="size"
                    :color="filters.priorities.includes(priority.value) ? 'primary' : 'neutral'"
                    :variant="filters.priorities.includes(priority.value) ? 'subtle' : 'outline'"
                    :aria-pressed="filters.priorities.includes(priority.value)"
                    class="rounded-full"
                    @click="filters.togglePriority(priority.value)"
                >
                    <template #trailing>
                        <span class="text-xs text-muted">{{ priorityCount(priority.value) }}</span>
                    </template>
                </UButton>
            </div>
        </div>

        <div>
            <p class="mb-2 text-sm font-semibold text-highlighted">Ansvarlige</p>
            <div class="flex max-h-64 flex-col divide-y divide-default overflow-y-auto rounded-md border border-default">
                <label v-for="assignee in assigneeOptions" :key="assignee.id" class="flex cursor-pointer items-center gap-3 px-3 py-2">
                    <UCheckbox :model-value="filters.assigneeIds.includes(assignee.id)" @update:model-value="filters.toggleAssignee(assignee.id)" />
                    <UAvatar v-if="assignee.user" :src="assignee.user.profilePicture?.url" :text="initials(assignee.user)" size="2xs" />
                    <UIcon v-else name="i-lucide-user-x" class="size-5 text-muted" />
                    <span class="truncate text-sm">{{ assignee.label }}</span>
                    <span class="ms-auto shrink-0 text-xs text-muted">{{ assignee.count }} kort</span>
                </label>
            </div>
        </div>

        <div v-if="tags.length">
            <p class="mb-2 text-sm font-semibold text-highlighted">Tags</p>
            <div class="flex max-h-64 flex-col divide-y divide-default overflow-y-auto rounded-md border border-default">
                <label v-for="tag in tags" :key="tag.id" class="flex cursor-pointer items-center gap-3 px-3 py-2">
                    <UCheckbox :model-value="filters.tagIds.includes(Number(tag.id))" @update:model-value="filters.toggleTag(Number(tag.id))" />
                    <BoardTagBadge :tag="tag" />
                    <span class="ms-auto shrink-0 text-xs text-muted">{{ tagCount(tag.id) }} kort</span>
                </label>
            </div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed } from "vue";
import type { BoardTagDto, UserSummaryDto, WorkItemDto } from "@/types";
import { boardPriorityFilters, boardQuickFilters, boardUnassignedFilterId, type BoardFilters } from "@/lib/board/boardFilters";
import BoardTagBadge from "@/Components/Board/BoardTagBadge.vue";

const props = withDefaults(defineProps<{
    filters: BoardFilters;
    tags: BoardTagDto[];
    cards: WorkItemDto[];
    size?: "sm" | "md";
}>(), { size: "md" });

function initials(user: UserSummaryDto) {
    return `${user.firstName.charAt(0)}${user.lastName.charAt(0)}`.toUpperCase();
}

function tagCount(tagId: number) {
    return props.cards.filter((card) => card.tags?.some((tag) => Number(tag.id) === Number(tagId))).length;
}

function priorityCount(priority: number) {
    return props.cards.filter((card) => Number(card.priority) === priority).length;
}

const assigneeOptions = computed(() => {
    const users = new Map<string, { user: UserSummaryDto; count: number }>();
    for (const card of props.cards) {
        for (const user of card.assignees) {
            const entry = users.get(user.id);
            if (entry) entry.count++;
            else users.set(user.id, { user, count: 1 });
        }
    }

    const people = [...users.values()]
        .map(({ user, count }) => ({
            id: user.id,
            user,
            count,
            label: user.id === props.filters.currentUserId ? `${user.firstName} ${user.lastName} (dig)` : `${user.firstName} ${user.lastName}`
        }))
        .sort((a, b) => Number(b.id === props.filters.currentUserId) - Number(a.id === props.filters.currentUserId) || a.label.localeCompare(b.label, "da"));

    const unassigned = {
        id: boardUnassignedFilterId,
        user: null,
        count: props.cards.filter((card) => !card.assignees.length).length,
        label: "Ingen ansvarlig"
    };

    return [...people, unassigned];
});
</script>
