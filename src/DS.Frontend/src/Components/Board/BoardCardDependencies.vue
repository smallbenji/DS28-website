<template>
    <section class="board-card-dependencies" aria-labelledby="board-card-dependencies-heading">
        <div class="board-card-dependencies-header">
            <h3 id="board-card-dependencies-heading" class="board-card-dependencies-heading">Afhængigheder</h3>
            <BButton v-if="canManage" size="is-small" class="is-ghost" icon-left="plus" :aria-expanded="pickerOpen" :disabled="saving" @click="togglePicker">
                {{ pickerOpen ? 'Luk søgning' : 'Tilføj' }}
            </BButton>
        </div>

        <div v-if="pickerOpen" class="board-card-dependency-picker">
            <BField label="Kortet venter på">
                <BInput v-model="search" icon="magnifying-glass" placeholder="DS-nummer eller titel..." :loading="searching" />
            </BField>

            <ul v-if="results.length > 0" class="board-card-dependency-results">
                <li v-for="result in results" :key="result.id" class="board-card-dependency-result">
                    <div class="board-card-dependency-result-info">
                        <span class="is-family-monospace is-size-7 has-text-grey">DS-{{ result.id }}</span>
                        <span class="board-card-dependency-title">{{ result.title }}</span>
                        <span v-if="result.placements.length" class="is-size-7 has-text-grey">{{ placementSummary(result.placements) }}</span>
                    </div>
                    <BButton size="is-small" type="is-primary" :disabled="isLinked(result.id) || saving" @click="add(result.id)">
                        {{ isLinked(result.id) ? 'Tilføjet' : 'Tilføj' }}
                    </BButton>
                </li>
            </ul>
            <p v-else-if="search.trim() && !searching" class="has-text-grey is-size-7">Ingen kort fundet.</p>
        </div>

        <template v-for="group in groups" :key="group.key">
            <div v-if="group.items.length > 0" class="board-card-dependency-group">
                <div class="board-card-dependency-group-head">
                    <span>{{ group.label }}</span>
                    <span v-if="group.summary" class="has-text-grey">{{ group.summary }}</span>
                </div>
                <ul>
                    <li v-for="item in group.items" :key="item.id" class="board-card-dependency" :class="{ 'is-done': item.isDone }">
                        <span class="is-family-monospace is-size-7 has-text-grey">DS-{{ item.id }}</span>
                        <span class="board-card-dependency-title" :class="{ 'has-text-grey': !item.isVisible }">
                            {{ item.isVisible ? item.title : 'Kort på en tavle, du ikke har adgang til' }}
                        </span>
                        <span class="tag" :class="item.isDone ? 'is-success is-light' : 'is-warning is-light'">
                            <font-awesome-icon v-if="item.isDone" icon="check" class="mr-1" />
                            {{ statusLabel(item) }}
                        </span>
                        <BButton
                            v-if="canManage"
                            size="is-small"
                            class="is-ghost"
                            :aria-label="`Fjern afhængighed til DS-${item.id}`"
                            title="Fjern afhængighed"
                            :disabled="saving"
                            @click="confirmRemove(item)"
                        >
                            ×
                        </BButton>
                    </li>
                </ul>
            </div>
        </template>

        <p v-if="!hasDependencies && !pickerOpen" class="has-text-grey is-size-7">Kortet venter ikke på andre kort.</p>
    </section>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, ref, watch } from 'vue';
import { BButton, BField, BInput, useDialog, useToast } from 'buefy';
import BoardService from '@/Services/BoardService';
import { useBoardStore } from '@/Stores/BoardStore';
import { getErrorMessage } from '@/lib/api';
import type { WorkItemDependencyDto, WorkItemDto, WorkItemPlacementDto, WorkItemSearchResultDto } from '@/types';

const props = defineProps<{
    boardId: number;
    card: WorkItemDto;
    canManage: boolean;
}>();

const store = useBoardStore();
const boardService = new BoardService();
const Toast = useToast();
const Dialog = useDialog();

const pickerOpen = ref(false);
const search = ref('');
const searching = ref(false);
const saving = ref(false);
const results = ref<WorkItemSearchResultDto[]>([]);
let searchTimeout: ReturnType<typeof setTimeout> | null = null;

const liveCard = computed(() => store.SelectedBoard?.cards.find((card) => String(card.id) === String(props.card.id)) ?? props.card);
const waitingOn = computed(() => liveCard.value.waitingOn ?? []);
const blocking = computed(() => liveCard.value.blocking ?? []);
const hasDependencies = computed(() => waitingOn.value.length > 0 || blocking.value.length > 0);

const groups = computed(() => [
    {
        key: 'waiting-on',
        label: 'Venter på',
        summary: `${waitingOn.value.filter((item) => item.isDone).length} af ${waitingOn.value.length} færdig`,
        items: waitingOn.value
    },
    {
        key: 'blocking',
        label: 'Blokerer',
        summary: '',
        items: blocking.value
    }
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
    search.value = '';
    results.value = [];
}

function isLinked(cardId: number) {
    return [...waitingOn.value, ...blocking.value].some((item) => String(item.id) === String(cardId));
}

function placementSummary(placements: WorkItemPlacementDto[]) {
    return placements.map((placement) => `${placement.boardTitle} - ${placement.columnTitle}`).join(', ');
}

function statusLabel(item: WorkItemDependencyDto) {
    if (item.isDone) return 'Færdig';

    return item.placements.find((placement) => !placement.isDoneColumn)?.columnTitle ?? 'Ikke færdig';
}

async function add(dependsOnId: number) {
    if (saving.value) return;

    saving.value = true;
    try {
        await boardService.addDependency(props.boardId, Number(props.card.id), Number(dependsOnId));
        await store.GET_BOARD(props.boardId);
        Toast.open({ message: `Kortet venter nu på DS-${dependsOnId}.`, type: 'is-success' });
        togglePicker();
    } catch (error) {
        Toast.open({ message: getErrorMessage(error, 'Afhængigheden kunne ikke tilføjes.'), type: 'is-danger' });
    } finally {
        saving.value = false;
    }
}

function confirmRemove(item: WorkItemDependencyDto) {
    Dialog.confirm({
        title: 'Fjern afhængighed',
        message: `Er du sikker på, at du vil fjerne afhængigheden mellem dette kort og DS-${item.id}? Kortene bevares.`,
        confirmText: 'Fjern',
        cancelText: 'Annuller',
        type: 'is-danger',
        onConfirm: () => {
            void remove(item.id);
        }
    });
}

async function remove(otherId: number) {
    saving.value = true;
    try {
        const success = await boardService.removeDependency(props.boardId, Number(props.card.id), Number(otherId));
        if (!success) {
            Toast.open({ message: 'Afhængigheden kunne ikke fjernes.', type: 'is-danger' });
            return;
        }

        await store.GET_BOARD(props.boardId);
        Toast.open({ message: 'Afhængigheden er fjernet.', type: 'is-success' });
    } finally {
        saving.value = false;
    }
}
</script>

<style scoped>
.board-card-dependencies {
    margin-top: 1.5rem;
}

.board-card-dependencies-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 0.5rem;
}

.board-card-dependencies-heading {
    font-size: 0.75rem;
    font-weight: 600;
    color: #66717e;
}

.board-card-dependency-picker {
    margin-bottom: 0.75rem;
}

.board-card-dependency-results {
    border-top: 1px solid rgba(0, 0, 0, 0.08);
    margin-top: 0.5rem;
}

.board-card-dependency-result {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    justify-content: space-between;
    gap: 0.5rem 0.75rem;
    padding: 0.5rem 0;
    border-bottom: 1px solid rgba(0, 0, 0, 0.05);
}

.board-card-dependency-result-info {
    display: flex;
    flex-direction: column;
    min-width: 0;
}

.board-card-dependency-group {
    border: 1px solid rgba(0, 0, 0, 0.1);
    border-radius: 10px;
    overflow: hidden;
    margin-bottom: 0.75rem;
}

.board-card-dependency-group-head {
    display: flex;
    justify-content: space-between;
    padding: 0.4rem 0.85rem;
    background: #f7f8fa;
    border-bottom: 1px solid rgba(0, 0, 0, 0.08);
    font-size: 0.75rem;
    font-weight: 600;
    color: #374151;
}

.board-card-dependency {
    display: grid;
    grid-template-columns: 4rem minmax(0, 1fr) auto auto;
    align-items: center;
    gap: 0.6rem;
    min-height: 2.75rem;
    padding: 0.25rem 0.5rem 0.25rem 0.85rem;
    border-bottom: 1px solid rgba(0, 0, 0, 0.05);
}

.board-card-dependency:last-child {
    border-bottom: none;
}

.board-card-dependency-title {
    font-size: 0.9rem;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.board-card-dependency.is-done .board-card-dependency-title {
    color: #6b7280;
}

@media screen and (max-width: 768px) {
    .board-card-dependency {
        grid-template-columns: minmax(0, 1fr) auto auto;
    }

    .board-card-dependency-title {
        grid-column: 1 / -1;
        grid-row: 2;
        white-space: normal;
    }
}
</style>
