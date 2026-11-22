<template>
    <div class="flex min-w-0 flex-1 flex-wrap items-center gap-2 py-1">
        <UInput v-model="filters.search" icon="i-lucide-search" placeholder="Søg i kort eller DS-nummer" class="min-w-0 flex-1 sm:w-64 sm:flex-none" aria-label="Søg i kort" />

        <UButton
            icon="i-lucide-sliders-horizontal"
            label="Filtre"
            :color="filters.activeCount ? 'primary' : 'neutral'"
            :variant="filters.activeCount ? 'subtle' : 'outline'"
            class="sm:hidden"
            @click="drawerOpen = true"
        >
            <template v-if="filters.activeCount" #trailing>
                <UBadge :label="String(filters.activeCount)" size="sm" />
            </template>
        </UButton>

        <div class="hidden sm:contents">
            <UPopover :content="{ align: 'start' }">
                <UButton
                    icon="i-lucide-sliders-horizontal"
                    label="Filtre"
                    size="sm"
                    :color="filters.activeCount ? 'primary' : 'neutral'"
                    :variant="filters.activeCount ? 'subtle' : 'outline'"
                    class="rounded-full"
                >
                    <template #trailing>
                        <UBadge v-if="filters.activeCount" :label="String(filters.activeCount)" size="sm" />
                        <UIcon name="i-lucide-chevron-down" class="size-4" />
                    </template>
                </UButton>

                <template #content>
                    <div class="flex w-80 flex-col">
                        <div class="max-h-[70vh] overflow-y-auto p-4">
                            <BoardFilterPanel :filters="filters" :tags="tags" :cards="cards" size="sm" />
                        </div>
                        <div class="flex items-center justify-between gap-2 border-t border-default px-4 py-2">
                            <span class="text-xs text-muted">{{ visibleCount }} af {{ cards.length }} kort</span>
                            <UButton label="Ryd filtre" icon="i-lucide-x" size="sm" color="neutral" variant="ghost" :disabled="!filters.activeCount" @click="filters.clearFilters()" />
                        </div>
                    </div>
                </template>
            </UPopover>

            <button
                v-for="chip in activeChips"
                :key="chip.key"
                type="button"
                class="inline-flex items-center gap-1 rounded-full border border-default py-0.5 ps-2 pe-1.5 text-xs hover:bg-elevated focus-visible:outline-2 focus-visible:outline-primary"
                :aria-label="`Fjern filteret ${chip.label}`"
                @click="chip.remove()"
            >
                <BoardTagBadge v-if="chip.tag" :tag="chip.tag" />
                <template v-else>
                    <UIcon :name="chip.icon" class="size-3.5 text-muted" />
                    <span>{{ chip.label }}</span>
                </template>
                <UIcon name="i-lucide-x" class="size-3.5 text-muted" />
            </button>

            <UButton v-if="filters.activeCount" label="Ryd filtre" size="sm" color="neutral" variant="link" @click="filters.clearFilters()" />
        </div>

        <span v-if="filters.isFiltering" class="w-full text-xs text-muted sm:w-auto" role="status">{{ visibleCount }} af {{ cards.length }} kort</span>
    </div>

    <UFieldGroup class="ms-2 hidden sm:flex">
        <UTooltip text="Detaljeret">
            <UButton icon="i-lucide-rows-3" size="sm" color="neutral" :variant="compact ? 'outline' : 'subtle'" aria-label="Detaljeret visning" :aria-pressed="!compact" @click="compact = false" />
        </UTooltip>
        <UTooltip text="Kompakt">
            <UButton icon="i-lucide-rows-4" size="sm" color="neutral" :variant="compact ? 'subtle' : 'outline'" aria-label="Kompakt visning" :aria-pressed="compact" @click="compact = true" />
        </UTooltip>
    </UFieldGroup>

    <UDrawer v-model:open="drawerOpen" title="Filtre" :description="`${visibleCount} af ${cards.length} kort vises`">
        <template #body>
            <div class="flex flex-col gap-5">
                <BoardFilterPanel :filters="filters" :tags="tags" :cards="cards" />

                <div>
                    <p class="mb-2 text-sm font-semibold text-highlighted">Visning</p>
                    <UFieldGroup class="w-full">
                        <UButton label="Detaljeret" icon="i-lucide-rows-3" color="neutral" :variant="compact ? 'outline' : 'subtle'" block :aria-pressed="!compact" @click="compact = false" />
                        <UButton label="Kompakt" icon="i-lucide-rows-4" color="neutral" :variant="compact ? 'subtle' : 'outline'" block :aria-pressed="compact" @click="compact = true" />
                    </UFieldGroup>
                </div>
            </div>
        </template>
        <template #footer>
            <div class="flex gap-2">
                <UButton label="Ryd filtre" color="neutral" variant="outline" :disabled="!filters.activeCount" class="flex-1 justify-center" @click="filters.clearFilters()" />
                <UButton :label="`Vis ${visibleCount} kort`" class="flex-1 justify-center" @click="drawerOpen = false" />
            </div>
        </template>
    </UDrawer>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import type { BoardTagDto, WorkItemDto } from "@/types";
import { boardPriorityFilters, boardQuickFilters, boardUnassignedFilterId, type BoardFilters } from "@/lib/board/boardFilters";
import BoardTagBadge from "@/Components/Board/BoardTagBadge.vue";
import BoardFilterPanel from "@/Components/Board/BoardFilterPanel.vue";

const props = defineProps<{
    filters: BoardFilters;
    tags: BoardTagDto[];
    cards: WorkItemDto[];
    visibleCount: number;
}>();

const compact = defineModel<boolean>("compact", { default: false });
const drawerOpen = ref(false);

interface ActiveFilterChip {
    key: string;
    label: string;
    icon?: string;
    tag?: BoardTagDto;
    remove: () => void;
}

const assigneeNames = computed(() => {
    const names = new Map<string, string>();
    for (const card of props.cards) {
        for (const user of card.assignees) names.set(user.id, `${user.firstName} ${user.lastName}`);
    }

    return names;
});

const activeChips = computed<ActiveFilterChip[]>(() => {
    const filters = props.filters;

    const quick = boardQuickFilters
        .filter((filter) => filters.quick[filter.key])
        .map((filter) => ({ key: `quick-${filter.key}`, label: filter.label, icon: filter.icon, remove: () => { filters.quick[filter.key] = false; } }));

    const assignees = filters.assigneeIds.map((id) => ({
        key: `assignee-${id}`,
        label: id === boardUnassignedFilterId ? "Ingen ansvarlig" : assigneeNames.value.get(id) ?? "Ukendt",
        icon: id === boardUnassignedFilterId ? "i-lucide-user-x" : "i-lucide-user",
        remove: () => filters.toggleAssignee(id)
    }));

    const priorities = boardPriorityFilters
        .filter((priority) => filters.priorities.includes(priority.value))
        .map((priority) => ({ key: `priority-${priority.value}`, label: `${priority.label} prioritet`, icon: priority.icon, remove: () => filters.togglePriority(priority.value) }));

    const tags = props.tags
        .filter((tag) => filters.tagIds.includes(Number(tag.id)))
        .map((tag) => ({ key: `tag-${tag.id}`, label: tag.name, tag, remove: () => filters.toggleTag(Number(tag.id)) }));

    return [...quick, ...assignees, ...priorities, ...tags];
});
</script>
