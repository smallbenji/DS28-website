<template>
    <BModal v-model="open" has-modal-card :width="1024" custom-content-class="board-card-modal-container" :can-cancel="['escape', 'outside']">
        <div v-if="open" class="modal-card board-card-modal">
            <header class="modal-card-head">
                <div class="board-card-context">{{ store.SELECTED_BOARD?.title ?? 'Tavle' }} / {{ isNew ? 'Nyt kort' : `DS-${card?.id}` }}</div>
                <div class="board-card-header-actions">
                    <BDropdown v-if="canManage && !isNew" position="is-bottom-left" aria-role="list">
                        <template #trigger>
                            <BButton size="is-small" class="is-ghost" aria-label="Korthandlinger" :disabled="saving">•••</BButton>
                        </template>
                        <BDropdownItem aria-role="listitem" class="has-text-danger" @click="remove">Slet kort</BDropdownItem>
                    </BDropdown>
                    <button type="button" class="delete" aria-label="Luk" @click="open = false" />
                </div>
            </header>

            <section class="modal-card-body">
                <template v-if="canManage">
                    <div class="board-card-columns">
                        <div class="board-card-main">
                            <BField class="board-card-title-field">
                                <BInput v-model="form.title" aria-label="Kortets titel" placeholder="Hvad skal gøres?" @keydown.enter="save" />
                            </BField>

                            <BField label="Beskrivelse" class="board-card-description-field">
                                <RichTextEditor v-model="form.description" />
                            </BField>
                        </div>

                        <div class="board-card-side">
                            <BField label="Deadline">
                                <BInput v-model="form.dueDate" type="date" />
                            </BField>

                            <BField label="Prioritet">
                                <BSelect v-model="form.priority" expanded>
                                    <option v-for="option in boardPriorityOptions" :key="option.value" :value="option.value">
                                        {{ option.label }}
                                    </option>
                                </BSelect>
                            </BField>

                            <section class="board-card-section">
                                <h3 class="board-card-heading">Ansvarlig</h3>

                                <div v-if="form.assigneeName" class="board-card-current">
                                    <span class="tag is-light">{{ form.assigneeName }}</span>
                                    <BButton size="is-small" class="is-ghost" aria-label="Fjern ansvarlig" title="Fjern ansvarlig" @click="clearAssignee">×</BButton>
                                </div>

                                <BButton size="is-small" class="is-ghost" :aria-expanded="assigneePickerOpen" @click="assigneePickerOpen = !assigneePickerOpen">
                                    {{ assigneePickerOpen ? 'Luk søgning' : form.assigneeName ? 'Skift ansvarlig' : 'Tilføj ansvarlig' }}
                                </BButton>
                                <div v-if="assigneePickerOpen" class="board-card-assignee-picker">
                                    <BField label="Søg efter bruger">
                                        <BInput
                                            v-model="assigneeSearch"
                                            icon="magnifying-glass"
                                            placeholder="Navn eller email..."
                                            :loading="assigneeSearching"
                                        />
                                    </BField>

                                    <div v-if="assigneeResults.length > 0" class="board-card-assignee-results">
                                        <div v-for="user in assigneeResults" :key="user.id" class="board-card-assignee-result">
                                            <div class="board-card-assignee-info">
                                                <div class="board-card-assignee-name">{{ user.firstName }} {{ user.lastName }}</div>
                                                <div class="board-card-assignee-email">{{ user.email }}</div>
                                            </div>
                                            <BButton size="is-small" type="is-primary" @click="selectAssignee(user)">Vælg</BButton>
                                        </div>
                                    </div>
                                    <p v-else-if="assigneeSearch && !assigneeSearching" class="has-text-grey is-size-7">
                                        Ingen brugere fundet.
                                    </p>
                                </div>
                            </section>

                            <section class="board-card-section">
                                <h3 class="board-card-heading">Stilling</h3>
                                <BSelect v-model="form.ownerPositionId" expanded>
                                    <option :value="null">Ingen stilling</option>
                                    <option v-for="position in orgPositions" :key="position.id" :value="position.id">
                                        {{ position.title }}
                                    </option>
                                </BSelect>
                            </section>

                            <section v-if="!isNew" class="board-card-section">
                                <h3 class="board-card-heading">Tavler og status</h3>

                                <ul class="board-card-placements">
                                    <li v-for="placement in placements" :key="placement.boardId" class="board-card-placement">
                                        <div class="board-card-placement-info">
                                            <span class="board-card-placement-board">
                                                {{ placement.boardTitle }}<span v-if="isCurrentBoard(placement)" class="board-card-placement-current"> · denne tavle</span>
                                            </span>
                                            <BDropdown position="is-top-left" aria-role="list" @active-change="loadStatusColumns(placement, $event)">
                                                <template #trigger>
                                                    <BButton size="is-small" class="board-card-status" :class="{ 'is-complete': placement.isDoneColumn }" :disabled="saving" :loading="statusLoading[placement.boardId]" :aria-label="`Skift status på ${placement.boardTitle}: ${placement.columnTitle}`">
                                                        <font-awesome-icon v-if="placement.isDoneColumn" icon="check" />
                                                        <span>{{ placement.columnTitle }}</span>
                                                        <i class="fas fa-chevron-down" aria-hidden="true"></i>
                                                    </BButton>
                                                </template>
                                                <BDropdownItem v-if="statusLoading[placement.boardId]" disabled>Henter statusser…</BDropdownItem>
                                                <BDropdownItem v-else-if="!statusColumns[placement.boardId]?.length" @click="loadStatusColumns(placement, true)">
                                                    Kunne ikke hente statusser. Prøv igen
                                                </BDropdownItem>
                                                <BDropdownItem v-for="column in statusColumns[placement.boardId] || []" :key="column.id" aria-role="listitem" :disabled="saving || String(column.id) === String(placement.columnId)" @click="changePlacementStatus(placement, column)">
                                                    <span class="board-card-status-option">
                                                        <span>{{ column.title }}</span>
                                                        <span v-if="column.isDoneColumn" class="board-card-status-done">Færdig</span>
                                                        <font-awesome-icon v-if="String(column.id) === String(placement.columnId)" icon="check" />
                                                    </span>
                                                </BDropdownItem>
                                            </BDropdown>
                                        </div>
                                        <BButton
                                            v-if="placements.length > 1"
                                            size="is-small"
                                            class="is-ghost"
                                            :disabled="saving"
                                            @click="confirmRemovePlacement(placement)"
                                        >
                                            Fjern
                                        </BButton>
                                    </li>
                                </ul>
                                <p class="board-card-status-help">Status og tavleplaceringer gemmes med det samme.</p>

                                <template v-if="availableBoards.length > 0">
                                    <BSelect v-model="placementBoardId" expanded>
                                        <option :value="null">Tilføj til tavle…</option>
                                        <option v-for="board in availableBoards" :key="board.id" :value="String(board.id)">
                                            {{ board.title }}
                                        </option>
                                    </BSelect>

                                    <BSelect
                                        v-if="placementBoardId !== null && placementColumns.length > 0"
                                        v-model="placementColumnId"
                                        expanded
                                        class="board-card-placement-column-select"
                                    >
                                        <option v-for="column in placementColumns" :key="column.id" :value="String(column.id)">
                                            {{ column.title }}
                                        </option>
                                    </BSelect>

                                    <BButton
                                        v-if="placementBoardId !== null && placementColumnId !== null"
                                        size="is-small"
                                        type="is-primary"
                                        :loading="saving"
                                        @click="addPlacement"
                                    >
                                        Tilføj til tavle
                                    </BButton>
                                </template>
                            </section>

                            <section v-if="!isNew && card && card.links.length" class="board-card-section">
                                <h3 class="board-card-heading">Koblinger</h3>
                                <ul class="board-card-links">
                                    <li v-for="link in card.links" :key="`${link.entityType}-${link.entityId}`">
                                        {{ link.entityType }} #{{ link.entityId }}
                                    </li>
                                </ul>
                            </section>
                        </div>
                    </div>
                </template>

                <template v-else>
                    <div class="board-card-columns">
                        <div class="board-card-main">
                            <h2 class="board-card-read-only-title">{{ card?.title }}</h2>
                            <h3 class="board-card-heading">Beskrivelse</h3>
                            <RichTextViewer v-if="card?.description" :html="card.description" />
                        </div>

                        <div class="board-card-side">
                            <dl class="board-card-read-only">
                                <div v-if="card?.dueDate">
                                    <dt>Deadline</dt>
                                    <dd>{{ card.dueDate }}</dd>
                                </div>
                                <div>
                                    <dt>Prioritet</dt>
                                    <dd>{{ boardPriorityLabel(card?.priority ?? 0) }}</dd>
                                </div>
                                <div v-if="card?.assigneeName">
                                    <dt>Ansvarlig</dt>
                                    <dd>{{ card.assigneeName }}</dd>
                                </div>
                                <div v-if="card?.ownerPositionTitle">
                                    <dt>Stilling</dt>
                                    <dd>{{ card.ownerPositionTitle }}</dd>
                                </div>
                                <div v-if="placements.length > 1">
                                    <dt>Tavler</dt>
                                    <dd>
                                        <ul class="board-card-placements board-card-placements-plain">
                                            <li v-for="placement in placements" :key="placement.boardId">
                                                {{ placement.boardTitle }} — {{ placement.columnTitle }}
                                            </li>
                                        </ul>
                                    </dd>
                                </div>
                                <div v-if="card?.links.length">
                                    <dt>Koblinger</dt>
                                    <dd>
                                        <ul class="board-card-links">
                                            <li v-for="link in card.links" :key="`${link.entityType}-${link.entityId}`">
                                                {{ link.entityType }} #{{ link.entityId }}
                                            </li>
                                        </ul>
                                    </dd>
                                </div>
                            </dl>
                        </div>
                    </div>
                </template>
            </section>

            <footer v-if="canManage" class="modal-card-foot">
                <div class="buttons">
                    <BButton class="is-ghost" :disabled="saving" @click="open = false">Annuller</BButton>
                    <BButton type="is-primary" :loading="saving" @click="save">{{ isNew ? 'Opret kort' : 'Gem ændringer' }}</BButton>
                </div>
            </footer>
        </div>
    </BModal>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, reactive, ref, watch } from 'vue';
import { BButton, BDropdown, BDropdownItem, BField, BInput, BModal, BSelect, useDialog, useToast } from 'buefy';
import BoardService from '@/Services/BoardService';
import OrgService from '@/Services/OrgService';
import { boardPriorityLabel, boardPriorityOptions, type BoardColumnDto, type OrgPositionDto, type UserDto, type WorkItemDto, type WorkItemPlacementDto } from '@/types';
import { useBoardStore } from '@/Stores/BoardStore';
import RichTextEditor from '@/Components/RichText/RichTextEditor.vue';
import RichTextViewer from '@/Components/RichText/RichTextViewer.vue';
import { htmlToPlainText } from '@/lib/util';

const props = defineProps<{
    modelValue: boolean;
    boardId: number;
    columnId: number;
    card: WorkItemDto | null;
    canManage: boolean;
}>();

const emit = defineEmits<{
    'update:modelValue': [value: boolean];
    saved: [];
}>();

const store = useBoardStore();
const boardService = new BoardService();
const orgService = new OrgService();
const Toast = useToast();
const Dialog = useDialog();

const saving = ref(false);
const assigneeSearch = ref('');
const assigneePickerOpen = ref(false);
const assigneeSearching = ref(false);
const assigneeResults = ref<UserDto[]>([]);
const orgPositions = ref<OrgPositionDto[]>([]);
const placementBoardId = ref<string | null>(null);
const placementColumnId = ref<string | null>(null);
const placementColumns = ref<BoardColumnDto[]>([]);
const statusColumns = ref<Record<number, BoardColumnDto[]>>({});
const statusLoading = ref<Record<number, boolean>>({});
let searchTimeout: ReturnType<typeof setTimeout> | null = null;

const form = reactive({
    title: '',
    description: '',
    dueDate: '',
    priority: '0',
    assigneeUserId: null as string | null,
    assigneeName: null as string | null,
    ownerPositionId: null as number | null
});

const open = computed({
    get: () => props.modelValue,
    set: (value: boolean) => emit('update:modelValue', value)
});

const isNew = computed(() => props.card === null);

const placements = computed<WorkItemPlacementDto[]>(() => {
    const cardId = props.card?.id;
    if (cardId == null) return [];

    const live = store.SELECTED_BOARD?.cards.find((card) => String(card.id) === String(cardId));
    return live?.placements ?? props.card?.placements ?? [];
});

const availableBoards = computed(() =>
    store.BOARDS.filter((board) => !board.isArchived && !placements.value.some((placement) => String(placement.boardId) === String(board.id)))
);

watch(
    () => [props.modelValue, props.card] as const,
    () => {
        if (!props.modelValue) return;

        form.title = props.card?.title ?? '';
        form.description = props.card?.description ?? '';
        form.dueDate = props.card?.dueDate ?? '';
        form.priority = String(props.card?.priority ?? 0);
        form.assigneeUserId = props.card?.assigneeUserId ?? null;
        form.assigneeName = props.card?.assigneeName ?? null;
        form.ownerPositionId = props.card?.ownerPositionId ?? null;

        assigneePickerOpen.value = false;
        assigneeSearch.value = '';
        assigneeResults.value = [];
        assigneeSearching.value = false;

        placementBoardId.value = null;
        placementColumnId.value = null;
        placementColumns.value = [];
        statusColumns.value = {};

        void loadOrgPositions();
        void loadBoards();
    },
    { immediate: true }
);

watch(placementBoardId, () => {
    void loadPlacementColumns();
});

watch(assigneeSearch, (value) => {
    if (searchTimeout) clearTimeout(searchTimeout);

    if (!value.trim()) {
        assigneeResults.value = [];
        assigneeSearching.value = false;
        return;
    }

    assigneeSearching.value = true;
    searchTimeout = setTimeout(async () => {
        assigneeResults.value = await boardService.searchAssignees(value.trim());
        assigneeSearching.value = false;
    }, 300);
});

onBeforeUnmount(() => {
    if (searchTimeout) clearTimeout(searchTimeout);
});

async function loadOrgPositions() {
    if (!props.canManage || orgPositions.value.length > 0) return;

    orgPositions.value = await orgService.getPositions();
}

async function loadBoards() {
    if (!props.canManage || store.BOARDS.length > 0) return;

    await store.GET_BOARDS();
}

async function loadPlacementColumns() {
    placementColumnId.value = null;
    placementColumns.value = [];

    if (placementBoardId.value === null) return;

    const columns = await boardService.getColumns(Number(placementBoardId.value));
    placementColumns.value = columns;

    const first = columns[0];
    if (first) placementColumnId.value = String(first.id);
}

function isCurrentBoard(placement: WorkItemPlacementDto) {
    return String(placement.boardId) === String(props.boardId);
}

async function loadStatusColumns(placement: WorkItemPlacementDto, active: boolean) {
    if (!active || statusLoading.value[placement.boardId]) return;

    statusLoading.value[placement.boardId] = true;
    try {
        statusColumns.value[placement.boardId] = await boardService.getColumns(Number(placement.boardId));
    } finally {
        statusLoading.value[placement.boardId] = false;
    }
}

async function changePlacementStatus(placement: WorkItemPlacementDto, column: BoardColumnDto) {
    if (!props.card || saving.value || String(placement.columnId) === String(column.id)) return;

    saving.value = true;
    try {
        const success = await boardService.moveCard(Number(placement.boardId), Number(props.card.id), {
            columnId: Number(column.id),
            position: 2147483647
        });
        if (!success) {
            Toast.open({ message: 'Status kunne ikke ændres. Prøv igen.', type: 'is-danger' });
            return;
        }

        await store.GET_BOARD(props.boardId);
        Toast.open({ message: `Status på ${placement.boardTitle} er ændret til ${column.title}.`, type: 'is-success' });
    } finally {
        saving.value = false;
    }
}

async function addPlacement() {
    if (!props.card || placementBoardId.value === null || placementColumnId.value === null) return;

    saving.value = true;
    try {
        const success = await store.ADD_PLACEMENT(
            props.boardId,
            Number(placementBoardId.value),
            Number(props.card.id),
            Number(placementColumnId.value)
        );

        if (success) {
            Toast.open({ message: 'Kortet er tilføjet til tavlen.', type: 'is-success' });
            placementBoardId.value = null;
            placementColumnId.value = null;
            placementColumns.value = [];
        } else {
            Toast.open({ message: 'Kortet kunne ikke tilføjes til tavlen.', type: 'is-danger' });
        }
    } finally {
        saving.value = false;
    }
}

function confirmRemovePlacement(placement: WorkItemPlacementDto) {
    if (!props.card) return;

    Dialog.confirm({
        title: 'Fjern fra tavle',
        message: isCurrentBoard(placement)
            ? `Er du sikker på, at du vil fjerne "${props.card.title}" fra denne tavle? Kortet bevares på de øvrige tavler.`
            : `Er du sikker på, at du vil fjerne "${props.card.title}" fra tavlen "${placement.boardTitle}"?`,
        confirmText: 'Fjern',
        cancelText: 'Annuller',
        type: 'is-danger',
        onConfirm: () => {
            void doRemovePlacement(placement);
        }
    });
}

async function doRemovePlacement(placement: WorkItemPlacementDto) {
    if (!props.card) return;

    saving.value = true;
    try {
        const success = await store.REMOVE_PLACEMENT(props.boardId, Number(placement.boardId), Number(props.card.id));
        if (!success) {
            Toast.open({ message: 'Kortet kunne ikke fjernes fra tavlen.', type: 'is-danger' });
            return;
        }

        if (isCurrentBoard(placement)) {
            Toast.open({ message: 'Kortet er fjernet fra tavlen.', type: 'is-success' });
            emit('saved');
            open.value = false;
        } else {
            Toast.open({ message: `Kortet er fjernet fra "${placement.boardTitle}".`, type: 'is-success' });
        }
    } finally {
        saving.value = false;
    }
}

function selectAssignee(user: UserDto) {
    assigneePickerOpen.value = false;
    form.assigneeUserId = user.id;
    form.assigneeName = `${user.firstName} ${user.lastName}`.trim();
    assigneeSearch.value = '';
    assigneeResults.value = [];
}

function clearAssignee() {
    form.assigneeUserId = null;
    form.assigneeName = null;
}

async function save() {
    if (saving.value) return;

    if (!form.title.trim()) {
        Toast.open({ message: 'Titlen må ikke være tom.', type: 'is-danger' });
        return;
    }

    saving.value = true;
    try {
        const base = {
            title: form.title.trim(),
            description: htmlToPlainText(form.description) ? form.description : null,
            assigneeUserId: form.assigneeUserId,
            ownerPositionId: form.ownerPositionId,
            dueDate: form.dueDate ? form.dueDate : null,
            priority: Number(form.priority)
        };

        const success = isNew.value
            ? (await store.ADD_CARD(props.boardId, {
                ...base,
                columnId: props.columnId,
                links: []
            })) !== null
            : await store.UPDATE_CARD(props.boardId, Number(props.card!.id), base);

        if (success) {
            Toast.open({ message: 'Kortet er gemt.', type: 'is-success' });
            emit('saved');
            open.value = false;
        } else {
            Toast.open({ message: 'Kortet kunne ikke gemmes.', type: 'is-danger' });
        }
    } finally {
        saving.value = false;
    }
}

function remove() {
    if (!props.card) return;

    Dialog.confirm({
        title: 'Slet kort',
        message: `Er du sikker på, at du vil slette "${props.card.title}"?`,
        confirmText: 'Slet',
        cancelText: 'Annuller',
        type: 'is-danger',
        onConfirm: () => {
            void doRemove();
        }
    });
}

async function doRemove() {
    if (!props.card) return;

    saving.value = true;
    try {
        const success = await store.REMOVE_CARD(props.boardId, Number(props.card.id));
        if (success) {
            Toast.open({ message: 'Kortet er slettet.', type: 'is-success' });
            emit('saved');
            open.value = false;
        } else {
            Toast.open({ message: 'Kortet kunne ikke slettes.', type: 'is-danger' });
        }
    } finally {
        saving.value = false;
    }
}
</script>

<style scoped>
.board-card-modal {
    width: 100%;
    max-width: 64rem;
    max-height: calc(100vh - 3rem);
}

.board-card-columns {
    display: grid;
    grid-template-columns: minmax(0, 1fr) 16rem;
    gap: 0;
    align-items: stretch;
}

.board-card-main,
.board-card-side {
    min-width: 0;
}

@media screen and (max-width: 768px) {
    .board-card-columns {
        grid-template-columns: 1fr;
    }
}

.board-card-section {
    margin-top: 1.25rem;
}

.board-card-heading {
    font-size: 0.85rem;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.03em;
    color: rgba(0, 0, 0, 0.55);
    margin-bottom: 0.5rem;
}

.board-card-current {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 0.75rem;
}

.board-card-assignee-results {
    border-top: 1px solid rgba(0, 0, 0, 0.08);
    margin-top: 0.5rem;
    padding-top: 0.25rem;
}

.board-card-assignee-result {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    justify-content: space-between;
    gap: 0.5rem 0.75rem;
    padding: 0.5rem 0;
    border-bottom: 1px solid rgba(0, 0, 0, 0.05);
}

.board-card-assignee-result:last-child {
    border-bottom: none;
}

.board-card-assignee-info {
    min-width: 0;
}

.board-card-assignee-name {
    font-weight: 600;
    font-size: 0.9rem;
}

.board-card-assignee-email {
    font-size: 0.8rem;
    color: #6b7280;
    word-break: break-word;
}

.board-card-read-only-title {
    font-size: 1.15rem;
    font-weight: 600;
    line-height: 1.3;
    overflow-wrap: anywhere;
    margin-bottom: 0.75rem;
}

.board-card-read-only {
    margin: 0;
}

.board-card-read-only > div {
    margin-bottom: 1rem;
}

.board-card-read-only dt {
    font-size: 0.8rem;
    font-weight: 600;
    text-transform: uppercase;
    letter-spacing: 0.03em;
    color: rgba(0, 0, 0, 0.55);
}

.board-card-read-only dd {
    margin: 0.25rem 0 0;
    white-space: pre-wrap;
    overflow-wrap: anywhere;
}

.board-card-links {
    list-style: disc;
    padding-left: 1.2rem;
}

.board-card-modal .modal-card-head {
    padding: 0.85rem 1.75rem;
    background: #fff;
    border-bottom: 1px solid #edf0f2;
    box-shadow: none;
    gap: 1rem;
}

.board-card-context {
    min-width: 0;
    color: #737b86;
    font-size: 0.8rem;
    overflow-wrap: anywhere;
}

.board-card-header-actions {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    margin-left: auto;
    flex-shrink: 0;
}

.board-card-modal .modal-card-body {
    padding: 0;
}

.board-card-main {
    padding: 1.75rem;
    min-height: 24rem;
}

.board-card-side {
    padding: 1.75rem 1.25rem;
    background: #fafbfc;
    border-left: 1px solid #f1f3f5;
}

.board-card-title-field :deep(.input) {
    height: auto;
    padding: 0.25rem 0;
    border-color: transparent;
    box-shadow: none;
    background: transparent;
    font-size: 1.5rem;
    font-weight: 600;
}

.board-card-title-field :deep(.input:hover) {
    border-bottom-color: #dce2e7;
}

.board-card-title-field :deep(.input:focus) {
    border-bottom-color: var(--bulma-primary);
}

.board-card-read-only-title {
    font-size: 1.5rem;
}

.board-card-description-field {
    margin-top: 1.25rem;
}

.board-card-modal :deep(.label),
.board-card-heading,
.board-card-read-only dt {
    font-size: 0.75rem;
    font-weight: 600;
    color: #66717e;
    text-transform: none;
    letter-spacing: normal;
}

.board-card-description-field :deep(.rich-text-editor) {
    border-color: transparent;
    background: transparent;
    transition: border-color 0.15s ease;
}

.board-card-description-field :deep(.rich-text-toolbar) {
    display: none;
    background: transparent;
    border-bottom: 0;
    padding: 0.25rem 0;
}

.board-card-description-field :deep(.tiptap) {
    min-height: 12rem;
    padding: 0.75rem 0;
}

.board-card-description-field :deep(.rich-text-editor:focus-within) {
    border-color: #e5e9ed;
}

.board-card-description-field :deep(.rich-text-editor:focus-within .tiptap) {
    padding-left: 0.75rem;
    padding-right: 0.75rem;
}

.board-card-description-field :deep(.rich-text-editor:focus-within .rich-text-toolbar) {
    display: flex;
}

.board-card-side :deep(.input),
.board-card-side :deep(.select select) {
    border-color: transparent;
    background: transparent;
    box-shadow: none;
    font-size: 0.85rem;
    border-radius: 5px;
}

.board-card-side :deep(.input:hover),
.board-card-side :deep(.select select:hover) {
    background: #f0f2f5;
}

.board-card-side :deep(.input:focus),
.board-card-side :deep(.select select:focus) {
    border-color: var(--bulma-primary);
    background: #fff;
}

.board-card-side :deep(.button.is-ghost) {
    color: #66717e;
    padding-left: 0.5rem;
    padding-right: 0.5rem;
    text-decoration: none;
}

.board-card-side :deep(.button.is-ghost:hover) {
    background: #edf0f2;
    color: #363636;
}

.board-card-side :deep(.field:not(:last-child)) {
    margin-bottom: 1.25rem;
}

.board-card-assignee-picker {
    margin-top: 0.75rem;
}

.board-card-placements {
    list-style: none;
    margin: 0 0 0.75rem;
    padding: 0;
}

.board-card-placements-plain {
    list-style: disc;
    padding-left: 1.2rem;
    margin-bottom: 0;
}

.board-card-placement {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    justify-content: space-between;
    gap: 0.35rem 0.5rem;
    padding: 0.4rem 0;
    border-bottom: 0;
}

.board-card-placement:last-child {
    border-bottom: none;
}

.board-card-placement-info {
    min-width: 0;
    display: flex;
    flex-direction: column;
    gap: 0.3rem;
}

.board-card-placement-board {
    font-size: 0.85rem;
    font-weight: 600;
    overflow-wrap: anywhere;
}

.board-card-placement-current {
    font-weight: 400;
    color: #6b7280;
}

.board-card-placement-column {
    font-size: 0.78rem;
    color: #6b7280;
}

.board-card-placement-column svg {
    margin-right: 0.2rem;
    color: #48c774;
}

.board-card-side :deep(.board-card-status) {
    height: auto;
    min-height: 1.8rem;
    padding: 0.25rem 0.5rem;
    border: 0;
    background: #edf0f4;
    color: #596575;
    box-shadow: none;
    white-space: normal;
    text-align: left;
}

.board-card-side :deep(.board-card-status.is-complete) {
    background: #e8f5ed;
    color: #287b4e;
}

.board-card-status :deep(.icon),
.board-card-status svg {
    margin-right: 0.35rem;
}

.board-card-status .fa-chevron-down {
    margin-left: 0.5rem;
    font-size: 0.6rem;
}

.board-card-status-option {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    white-space: normal;
}

.board-card-status-done {
    color: #287b4e;
    font-size: 0.7rem;
    margin-left: auto;
}

.board-card-status-help {
    margin: 0 0 1rem;
    font-size: 0.72rem;
    color: #737b86;
}

.board-card-placement-column-select {
    margin-top: 0.5rem;
}

.board-card-modal .modal-card-foot {
    justify-content: flex-end;
    padding: 0.85rem 1.75rem;
    background: #fff;
    border-top: 0;
}

.board-card-modal .modal-card-foot .buttons {
    margin: 0;
}

.board-card-modal .modal-card-foot .button {
    margin-bottom: 0;
}

@media screen and (max-width: 768px) {
    .board-card-modal {
        max-height: calc(100dvh - 1rem);
        max-width: calc(100vw - 1rem);
        margin: 0 auto;
    }

    .board-card-main {
        min-height: 0;
        padding: 1.25rem;
    }

    .board-card-side {
        padding: 1.25rem;
        border-left: none;
        border-top: 1px solid #edf0f2;
    }

    .board-card-modal .modal-card-head,
    .board-card-modal .modal-card-foot {
        padding: 0.75rem 1.25rem;
    }
}
</style>

<style>
.board-card-modal-container {
    width: calc(100vw - 2.5rem);
}
</style>
