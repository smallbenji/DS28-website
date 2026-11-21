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
            <UButton
                v-for="filter in boardQuickFilters"
                :key="filter.key"
                :label="filter.label"
                :icon="filter.icon"
                size="sm"
                :color="filters.quick[filter.key] ? 'primary' : 'neutral'"
                :variant="filters.quick[filter.key] ? 'subtle' : 'outline'"
                :aria-pressed="filters.quick[filter.key]"
                class="rounded-full"
                @click="filters.quick[filter.key] = !filters.quick[filter.key]"
            />

            <template v-if="tags.length">
                <USeparator orientation="vertical" class="h-5" />
                <UDropdownMenu :items="tagMenu" :content="{ align: 'start' }">
                    <UButton
                        icon="i-lucide-tags"
                        label="Tags"
                        size="sm"
                        :color="filters.tagIds.length ? 'primary' : 'neutral'"
                        :variant="filters.tagIds.length ? 'subtle' : 'outline'"
                        class="rounded-full"
                    >
                        <template #trailing>
                            <UBadge v-if="filters.tagIds.length" :label="String(filters.tagIds.length)" size="sm" />
                            <UIcon name="i-lucide-chevron-down" class="size-4" />
                        </template>
                    </UButton>
                </UDropdownMenu>
                <button
                    v-for="tag in selectedTags"
                    :key="tag.id"
                    type="button"
                    class="inline-flex items-center gap-0.5 rounded focus-visible:outline-2 focus-visible:outline-primary"
                    :aria-label="`Fjern tagfilteret ${tag.name}`"
                    @click="filters.toggleTag(Number(tag.id))"
                >
                    <BoardTagBadge :tag="tag" />
                    <UIcon name="i-lucide-x" class="size-3.5 text-muted" />
                </button>
            </template>
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
                <div>
                    <p class="mb-2 text-sm font-semibold text-highlighted">Vis kun</p>
                    <div class="flex flex-wrap gap-2">
                        <UButton
                            v-for="filter in boardQuickFilters"
                            :key="filter.key"
                            :label="filter.label"
                            :icon="filter.icon"
                            :color="filters.quick[filter.key] ? 'primary' : 'neutral'"
                            :variant="filters.quick[filter.key] ? 'subtle' : 'outline'"
                            :aria-pressed="filters.quick[filter.key]"
                            class="rounded-full"
                            @click="filters.quick[filter.key] = !filters.quick[filter.key]"
                        />
                    </div>
                </div>

                <div v-if="tags.length">
                    <p class="mb-2 text-sm font-semibold text-highlighted">Tags</p>
                    <div class="flex flex-col divide-y divide-default rounded-md border border-default">
                        <label v-for="tag in tags" :key="tag.id" class="flex cursor-pointer items-center gap-3 px-3 py-2.5">
                            <UCheckbox :model-value="filters.tagIds.includes(Number(tag.id))" @update:model-value="filters.toggleTag(Number(tag.id))" />
                            <BoardTagBadge :tag="tag" />
                            <span class="ms-auto text-xs text-muted">{{ tagCount(tag.id) }} kort</span>
                        </label>
                    </div>
                </div>

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
import type { DropdownMenuItem } from "@nuxt/ui";
import type { BoardTagDto, WorkItemDto } from "@/types";
import { boardQuickFilters, type BoardFilters } from "@/lib/board/boardFilters";
import BoardTagBadge from "@/Components/Board/BoardTagBadge.vue";

const props = defineProps<{
    filters: BoardFilters;
    tags: BoardTagDto[];
    cards: WorkItemDto[];
    visibleCount: number;
}>();

const compact = defineModel<boolean>("compact", { default: false });
const drawerOpen = ref(false);

const selectedTags = computed(() => props.tags.filter((tag) => props.filters.tagIds.includes(Number(tag.id))));

function tagCount(tagId: number) {
    return props.cards.filter((card) => card.tags?.some((tag) => Number(tag.id) === Number(tagId))).length;
}

const tagMenu = computed<DropdownMenuItem[][]>(() => [
    props.tags.map((tag) => ({
        label: `${tag.name} (${tagCount(tag.id)})`,
        type: "checkbox" as const,
        checked: props.filters.tagIds.includes(Number(tag.id)),
        onUpdateChecked: () => props.filters.toggleTag(Number(tag.id)),
        onSelect: (event: Event) => event.preventDefault()
    })),
    [{ label: "Ryd tags", icon: "i-lucide-x", disabled: props.filters.tagIds.length === 0, onSelect: () => { props.filters.tagIds = []; } }]
]);
</script>
