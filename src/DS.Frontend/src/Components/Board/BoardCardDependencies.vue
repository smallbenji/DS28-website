<template>
    <section aria-labelledby="card-dependencies-heading">
        <div class="mb-2 flex items-center justify-end gap-2">
            <h3 id="card-dependencies-heading" class="sr-only">Afhængigheder</h3>
            <UButton v-if="canManage" color="neutral" variant="ghost" size="xs" :icon="pickerOpen ? 'i-lucide-x' : 'i-lucide-plus'" :label="pickerOpen ? 'Luk søgning' : 'Tilføj'" :aria-expanded="pickerOpen" :disabled="saving" @click="togglePicker" />
        </div>

        <div v-if="pickerOpen" class="mb-3 rounded-md border border-default p-3">
            <UFormField label="Kortet venter på">
                <UInput v-model="search" icon="i-lucide-search" placeholder="DS-nummer eller titel…" :loading="searching" class="w-full" autofocus />
            </UFormField>
            <ul v-if="results.length" class="mt-2 flex flex-col divide-y divide-default">
                <li v-for="result in results" :key="result.id" class="flex items-center justify-between gap-2 py-2">
                    <span class="min-w-0 text-sm">
                        <span class="font-mono text-xs text-muted">DS-{{ result.id }}</span>
                        <span class="ms-1">{{ result.title }}</span>
                        <span v-if="result.placements.length" class="block truncate text-xs text-muted">{{ placementSummary(result.placements) }}</span>
                    </span>
                    <UButton size="xs" :label="isLinked(result.id) ? 'Tilføjet' : 'Tilføj'" :disabled="isLinked(result.id) || saving" @click="add(result.id)" />
                </li>
            </ul>
            <p v-else-if="search.trim() && !searching" class="mt-2 text-xs text-muted">Ingen kort fundet.</p>
        </div>

        <template v-for="group in groups" :key="group.key">
            <div v-if="group.items.length" class="mb-3">
                <div class="mb-1 flex items-center justify-between text-xs font-medium text-muted">
                    <span>{{ group.label }}</span>
                    <span v-if="group.summary">{{ group.summary }}</span>
                </div>
                <ul class="flex flex-col gap-1">
                    <li v-for="item in group.items" :key="item.id" class="flex items-center gap-2 rounded-md bg-elevated px-2 py-1.5 text-sm">
                        <span class="font-mono text-xs text-muted">DS-{{ item.id }}</span>
                        <span class="min-w-0 flex-1 truncate" :class="{ 'text-muted': !item.isVisible, 'line-through': item.isDone }">
                            {{ item.isVisible ? item.title : 'Kort på en tavle, du ikke har adgang til' }}
                        </span>
                        <UBadge :label="statusLabel(item)" :icon="item.isDone ? 'i-lucide-check' : undefined" :color="item.isDone ? 'success' : 'warning'" variant="subtle" size="sm" />
                        <UButton v-if="canManage" color="neutral" variant="ghost" size="xs" icon="i-lucide-x" :aria-label="`Fjern afhængighed til DS-${item.id}`" :disabled="saving" @click="confirmRemove(item)" />
                    </li>
                </ul>
            </div>
        </template>

        <p v-if="!hasDependencies && !pickerOpen" class="text-xs text-muted">Kortet venter ikke på andre kort.</p>

        <ConfirmModal
            v-model:open="removeOpen"
            title="Fjern afhængighed"
            :description="`Fjern afhængigheden mellem dette kort og DS-${toRemove?.id}? Kortene bevares.`"
            confirm-label="Fjern"
            color="error"
            :loading="saving"
            @confirm="remove"
        />
    </section>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from "vue";
import BoardService from "@/Services/BoardService";
import { useBoardStore } from "@/Stores/BoardStore";
import { getErrorMessage } from "@/lib/api";
import { useNotify } from "@/lib/notify";
import ConfirmModal from "@/Components/ConfirmModal.vue";
import type { WorkItemDependencyDto, WorkItemDto, WorkItemPlacementDto, WorkItemSearchResultDto } from "@/types";

const props = defineProps<{ boardId: number; card: WorkItemDto; canManage: boolean }>();

const store = useBoardStore();
const boardService = new BoardService();
const notify = useNotify();

const pickerOpen = ref(false);
const search = ref("");
const searching = ref(false);
const saving = ref(false);
const results = ref<WorkItemSearchResultDto[]>([]);
const removeOpen = ref(false);
const toRemove = ref<WorkItemDependencyDto | null>(null);
let searchTimeout: ReturnType<typeof setTimeout> | null = null;

const liveCard = computed(() => store.SelectedBoard?.cards.find((card) => String(card.id) === String(props.card.id)) ?? props.card);
const waitingOn = computed(() => liveCard.value.waitingOn ?? []);
const blocking = computed(() => liveCard.value.blocking ?? []);
const hasDependencies = computed(() => waitingOn.value.length > 0 || blocking.value.length > 0);

const groups = computed(() => [
    { key: "waiting-on", label: "Venter på", summary: `${waitingOn.value.filter((item) => item.isDone).length} af ${waitingOn.value.length} færdig`, items: waitingOn.value },
    { key: "blocking", label: "Blokerer", summary: "", items: blocking.value }
]);

watch(search, (value) => {
    if (searchTimeout) clearTimeout(searchTimeout);

    if (!value.trim()) {
        results.value = [];
        searching.value = false;
        return;
    }

    searching.value = true;
    searchTimeout = setTimeout(async () => {
        const found = await boardService.searchCards(value.trim());
        results.value = found.filter((result) => String(result.id) !== String(props.card.id));
        searching.value = false;
    }, 300);
});

onBeforeUnmount(() => {
    if (searchTimeout) clearTimeout(searchTimeout);
});

function togglePicker() {
    pickerOpen.value = !pickerOpen.value;
    search.value = "";
    results.value = [];
}

function isLinked(cardId: number) {
    return [...waitingOn.value, ...blocking.value].some((item) => String(item.id) === String(cardId));
}

function placementSummary(placements: WorkItemPlacementDto[]) {
    return placements.map((placement) => `${placement.boardTitle}: ${placement.columnTitle}`).join(", ");
}

function statusLabel(item: WorkItemDependencyDto) {
    if (item.isDone) return "Færdig";

    return item.placements.find((placement) => !placement.isDoneColumn)?.columnTitle ?? "Ikke færdig";
}

async function add(dependsOnId: number) {
    if (saving.value) return;

    saving.value = true;
    try {
        await boardService.addDependency(props.boardId, Number(props.card.id), Number(dependsOnId));
        await store.GET_BOARD(props.boardId);
        notify.success(`Kortet venter nu på DS-${dependsOnId}.`);
        togglePicker();
    } catch (error) {
        notify.error(getErrorMessage(error, "Afhængigheden kunne ikke tilføjes."));
    } finally {
        saving.value = false;
    }
}

function confirmRemove(item: WorkItemDependencyDto) {
    toRemove.value = item;
    removeOpen.value = true;
}

async function remove() {
    if (!toRemove.value) return;

    saving.value = true;
    try {
        const ok = await boardService.removeDependency(props.boardId, Number(props.card.id), Number(toRemove.value.id));
        if (ok) await store.GET_BOARD(props.boardId);
        notify.result(ok, "Afhængigheden er fjernet.", "Afhængigheden kunne ikke fjernes.");
        if (ok) removeOpen.value = false;
    } finally {
        saving.value = false;
    }
}
</script>
