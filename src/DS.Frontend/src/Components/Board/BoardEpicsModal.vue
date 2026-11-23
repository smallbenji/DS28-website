<template>
    <UModal v-model:open="open" title="Epics" :description="summary" :ui="{ content: 'sm:max-w-2xl' }">
        <template #body>
            <UEmpty
                v-if="epics.length === 0"
                icon="i-lucide-layers"
                title="Tavlen har ingen epics endnu."
                description="Markér et kort som epic i kortdialogen for at samle kort under det."
            />

            <div v-else class="flex flex-col gap-5">
                <section v-for="group in groups" :key="group.key" :aria-labelledby="`epics-${group.key}`">
                    <h3 :id="`epics-${group.key}`" class="mb-2 text-sm font-semibold text-highlighted">{{ group.title }} ({{ group.epics.length }})</h3>

                    <ul class="flex flex-col gap-1.5">
                        <li v-for="epic in group.epics" :key="epic.card.id">
                            <button
                                type="button"
                                class="flex w-full flex-col gap-2 rounded-md border border-default bg-default px-3 py-2.5 text-start hover:bg-elevated focus-visible:outline-2 focus-visible:outline-primary"
                                :aria-label="`Åbn epic DS-${epic.card.id}: ${epic.card.title}`"
                                @click="emit('open', epic.card)"
                            >
                                <span class="flex w-full items-center gap-2">
                                    <UIcon name="i-lucide-layers" class="size-4 shrink-0 text-primary" />
                                    <span class="font-mono text-xs text-muted">DS-{{ epic.card.id }}</span>
                                    <span class="min-w-0 flex-1 truncate text-sm font-medium text-highlighted">{{ epic.card.title }}</span>
                                    <UBadge :label="epic.columnTitle" :icon="epic.isDone ? 'i-lucide-check' : undefined" :color="epic.isDone ? 'success' : 'neutral'" variant="subtle" size="sm" class="shrink-0" />
                                </span>

                                <span class="flex w-full items-center gap-3">
                                    <span class="min-w-0 flex-1">
                                        <span class="mb-1 flex items-center gap-1.5 text-xs" :class="epic.isComplete ? 'font-medium text-success' : 'text-muted'">
                                            <UIcon v-if="epic.isComplete" name="i-lucide-circle-check" class="size-3.5" />
                                            {{ progressLabel(epic) }}
                                        </span>
                                        <UProgress v-if="epic.total > 0" :model-value="epic.done" :max="epic.total" size="2xs" :color="epic.isComplete ? 'success' : 'primary'" />
                                    </span>

                                    <UAvatarGroup v-if="epic.card.assignees.length" size="2xs" :max="3" class="shrink-0">
                                        <UTooltip v-for="user in epic.card.assignees" :key="user.id" :text="`${user.firstName} ${user.lastName}`">
                                            <UAvatar
                                                :src="user.profilePicture?.url"
                                                :alt="`Ansvarlig: ${user.firstName} ${user.lastName}`"
                                                :text="`${user.firstName?.[0] ?? ''}${user.lastName?.[0] ?? ''}`.toLocaleUpperCase('da-DK')"
                                            />
                                        </UTooltip>
                                    </UAvatarGroup>
                                    <span v-else class="shrink-0 text-xs text-muted">Ingen ansvarlige</span>
                                </span>
                            </button>
                        </li>
                    </ul>
                </section>
            </div>
        </template>
    </UModal>
</template>

<script setup lang="ts">
import { computed } from "vue";
import { useBoardStore } from "@/Stores/BoardStore";
import type { WorkItemDto } from "@/types";

interface EpicOverviewItem {
    card: WorkItemDto;
    columnTitle: string;
    columnIndex: number;
    isDone: boolean;
    done: number;
    total: number;
    isComplete: boolean;
}

const open = defineModel<boolean>("open", { required: true });

const emit = defineEmits<{ open: [card: WorkItemDto] }>();

const store = useBoardStore();

const epics = computed<EpicOverviewItem[]>(() => {
    const columns = store.SelectedBoard?.columns ?? [];
    const cards = store.SelectedBoard?.cards ?? [];

    return cards
        .filter((card) => card.isEpic)
        .map((card) => {
            const columnIndex = columns.findIndex((column) => String(column.id) === String(card.columnId));
            const column = columns[columnIndex];
            const done = Number(card.epicProgress?.done ?? 0);
            const total = Number(card.epicProgress?.total ?? 0);

            return {
                card,
                columnTitle: column?.title ?? "Ukendt kolonne",
                columnIndex,
                isDone: column?.isDoneColumn ?? false,
                done,
                total,
                isComplete: total > 0 && done === total
            };
        })
        .sort((a, b) => a.columnIndex - b.columnIndex || Number(a.card.position) - Number(b.card.position));
});

const groups = computed(() => [
    { key: "open", title: "I arbejde", epics: epics.value.filter((epic) => !epic.isDone) },
    { key: "done", title: "Færdige", epics: epics.value.filter((epic) => epic.isDone) }
].filter((group) => group.epics.length > 0));

const summary = computed(() => {
    const openCount = epics.value.filter((epic) => !epic.isDone).length;

    return `${epics.value.length} epics, ${openCount} i arbejde`;
});

function progressLabel(epic: EpicOverviewItem) {
    if (epic.total === 0) return "Ingen kort i epic'en endnu";
    if (epic.isComplete && !epic.isDone) return `Alle ${epic.total} kort er færdige. Epic'en kan flyttes til færdig.`;
    if (epic.isComplete) return `Alle ${epic.total} kort er færdige`;

    return `${epic.done} af ${epic.total} færdige`;
}
</script>
