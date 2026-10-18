<template>
    <div class="board-detail">
        <header class="board-detail-head">
            <div class="board-detail-info">
                <h1 class="board-detail-title">{{ board?.title ?? 'Tavle' }}</h1>
                <p v-if="board?.description" class="board-detail-description">{{ board.description }}</p>
                <p v-if="board?.isArchived" class="tag is-warning is-light">Arkiveret</p>
            </div>
            <div v-if="canManage" class="board-detail-actions">
                <BButton size="is-small" class="is-ghost" @click="membersOpen = true">Medlemmer og invitation</BButton>
                <BButton size="is-small" class="is-ghost" @click="openColumnCreate">
                    <font-awesome-icon icon="plus" />
                    <span>Kolonne</span>
                </BButton>
                <BButton size="is-small" class="is-ghost" @click="openSettings">Indstillinger</BButton>
            </div>
        </header>

        <p v-if="loading && !board" role="status">Indlæser tavle…</p>

        <p v-else-if="!board" class="has-text-grey">Tavlen blev ikke fundet.</p>

        <div v-else class="board-columns" role="region" aria-label="Tavlens kolonner" @drop.stop.prevent="onContainerDrop">
            <BoardColumn
                v-for="column in board.columns"
                :key="column.id"
                :column="column"
                :cards="cardsFor(column.id)"
                :can-manage="canManage"
                @open-card="openCard"
                @add-card="openNewCard"
                @edit-column="openColumnEdit"
                @delete-column="confirmDeleteColumn"
                @drop-card="handleDropOnCard"
                @drop-column="handleDropOnColumn"
                @reorder-column="handleReorderColumn"
            />
        </div>

        <BoardCardModal
            v-model="cardDrawerOpen"
            :board-id="boardId"
            :column-id="targetColumnId"
            :card="editingCard"
            :can-manage="canManage"
        />

        <BoardColumnDrawer
            v-model="columnDrawerOpen"
            :board-id="boardId"
            :column="editingColumn"
        />

        <BModal v-model="settingsOpen" has-modal-card>
            <div class="modal-card">
                <header class="modal-card-head">
                    <p class="modal-card-title">Tavleindstillinger</p>
                    <button type="button" class="delete" @click="settingsOpen = false" />
                </header>
                <section class="modal-card-body">
                    <BField label="Titel">
                        <BInput v-model="settings.title" />
                    </BField>
                    <BField label="Beskrivelse">
                        <BInput v-model="settings.description" type="textarea" :rows="3" />
                    </BField>
                    <BCheckbox v-model="settings.isArchived">Arkiveret</BCheckbox>
                </section>
                <footer class="modal-card-foot">
                    <div class="buttons">
                        <BButton type="is-primary" :loading="savingSettings" @click="saveSettings">Gem</BButton>
                        <BButton @click="settingsOpen = false">Annuller</BButton>
                    </div>
                </footer>
            </div>
        </BModal>
        <BoardMembersModal v-model="membersOpen" :board-id="boardId" />
    </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute } from 'vue-router';
import { BButton, BCheckbox, BField, BInput, BModal, useDialog, useToast } from 'buefy';
import type { BoardColumnDto, WorkItemDto } from '@/types';
import { useBoardStore } from '@/Stores/BoardStore';
import BoardMembersModal from '@/Components/Board/BoardMembersModal.vue';
import BoardColumn from '@/Components/Board/BoardColumn.vue';
import BoardCardModal from '@/Components/Board/BoardCardModal.vue';
import BoardColumnDrawer from '@/Components/Board/BoardColumnDrawer.vue';

const route = useRoute();
const store = useBoardStore();
const Toast = useToast();
const Dialog = useDialog();

const boardId = Number(route.params.id);
const loading = ref(true);

const cardDrawerOpen = ref(false);
const editingCard = ref<WorkItemDto | null>(null);
const targetColumnId = ref(0);

const columnDrawerOpen = ref(false);
const editingColumn = ref<BoardColumnDto | null>(null);

const settingsOpen = ref(false);
const membersOpen = ref(false);
const savingSettings = ref(false);
const settings = ref({ title: '', description: '', isArchived: false });

const board = computed(() => store.SELECTED_BOARD);
const canManage = computed(() => board.value !== null);

function cardsFor(columnId: number): WorkItemDto[] {
    return (board.value?.cards ?? []).filter((card) => String(card.columnId) === String(columnId));
}

function openCard(card: WorkItemDto) {
    editingCard.value = card;
    targetColumnId.value = Number(card.columnId);
    cardDrawerOpen.value = true;
}

function openNewCard(columnId: number) {
    editingCard.value = null;
    targetColumnId.value = Number(columnId);
    cardDrawerOpen.value = true;
}

function openColumnCreate() {
    editingColumn.value = null;
    columnDrawerOpen.value = true;
}

function openColumnEdit(column: BoardColumnDto) {
    editingColumn.value = column;
    columnDrawerOpen.value = true;
}

function confirmDeleteColumn(column: BoardColumnDto) {
    Dialog.confirm({
        title: 'Slet kolonne',
        message: `Er du sikker på, at du vil slette kolonnen "${column.title}"?`,
        confirmText: 'Slet',
        cancelText: 'Annuller',
        type: 'is-danger',
        onConfirm: () => {
            void doDeleteColumn(column);
        }
    });
}

async function doDeleteColumn(column: BoardColumnDto) {
    const success = await store.DELETE_COLUMN(boardId, Number(column.id));
    Toast.open({
        message: success
            ? 'Kolonnen er slettet.'
            : 'Kolonnen kunne ikke slettes. Den må ikke indeholde kort.',
        type: success ? 'is-success' : 'is-danger'
    });
}

function openSettings() {
    settings.value = {
        title: board.value?.title ?? '',
        description: board.value?.description ?? '',
        isArchived: board.value?.isArchived ?? false
    };
    settingsOpen.value = true;
}

async function saveSettings() {
    if (savingSettings.value) return;

    if (!settings.value.title.trim()) {
        Toast.open({ message: 'Titlen må ikke være tom.', type: 'is-danger' });
        return;
    }

    savingSettings.value = true;
    try {
        const success = await store.UPDATE_BOARD(boardId, {
            title: settings.value.title.trim(),
            description: settings.value.description.trim() ? settings.value.description.trim() : null,
            icon: board.value?.icon ?? null,
            color: board.value?.color ?? null,
            isArchived: settings.value.isArchived
        });

        if (success) {
            Toast.open({ message: 'Tavlen er gemt.', type: 'is-success' });
            settingsOpen.value = false;
        } else {
            Toast.open({ message: 'Tavlen kunne ikke gemmes.', type: 'is-danger' });
        }
    } finally {
        savingSettings.value = false;
    }
}

function handleDropOnCard(payload: { targetCardId: string; draggedId: string }) {
    if (!payload.draggedId || payload.targetCardId === payload.draggedId) return;

    const target = board.value?.cards.find((card) => String(card.id) === String(payload.targetCardId));
    if (!target) return;

    const list = cardsFor(target.columnId).filter((card) => String(card.id) !== String(payload.draggedId));
    const index = list.findIndex((card) => String(card.id) === String(payload.targetCardId));
    void moveCard(payload.draggedId, target.columnId, index < 0 ? list.length : index);
}

function handleDropOnColumn(payload: { columnId: number; draggedId: string }) {
    if (!payload.draggedId) return;

    const list = cardsFor(payload.columnId).filter((card) => String(card.id) !== String(payload.draggedId));
    void moveCard(payload.draggedId, payload.columnId, list.length);
}

function handleReorderColumn(payload: { targetColumnId: number; draggedColumnId: number }) {
    const order = columnOrder();
    const from = order.indexOf(Number(payload.draggedColumnId));
    const to = order.indexOf(Number(payload.targetColumnId));
    if (from < 0 || to < 0 || from === to) return;

    const next = order.filter((columnId) => columnId !== order[from]);
    next.splice(from < to ? to - 1 : to, 0, order[from]);

    void reorderColumns(next);
}

function onContainerDrop(event: DragEvent) {
    const payload = event.dataTransfer?.getData('text/plain') ?? '';
    if (!payload.startsWith('column:')) return;

    const draggedColumnId = Number(payload.slice('column:'.length));
    if (!draggedColumnId) return;

    const order = columnOrder();
    if (!order.includes(draggedColumnId) || order.indexOf(draggedColumnId) === order.length - 1) return;

    void reorderColumns([...order.filter((columnId) => columnId !== draggedColumnId), draggedColumnId]);
}

function columnOrder(): number[] {
    return (board.value?.columns ?? []).map((column) => Number(column.id));
}

async function reorderColumns(order: number[]) {
    const success = await store.REORDER_COLUMNS(boardId, order);
    if (!success) {
        Toast.open({ message: 'Kolonnerne kunne ikke ombyttes.', type: 'is-danger' });
    }
}

async function moveCard(cardId: string, columnId: number, position: number) {
    const success = await store.MOVE_CARD(boardId, Number(cardId), Number(columnId), position);
    if (!success) {
        Toast.open({ message: 'Kortet kunne ikke flyttes.', type: 'is-danger' });
    }
}

onMounted(async () => {
    loading.value = true;
    try {
        await store.GET_BOARD(boardId);
    } finally {
        loading.value = false;
    }
});
</script>

<style scoped>
.board-detail {
    display: flex;
    flex-direction: column;
    flex: 1;
    min-height: 0;
    padding: 1rem;
    background: #fafbfc;
    border-radius: 8px;
    margin: 0 0.5rem 0.5rem;
}

.board-detail-head {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 1rem;
    flex-wrap: wrap;
    margin-bottom: 1.25rem;
}

.board-detail-title {
    font-size: 1.5rem;
    font-weight: 600;
    overflow-wrap: anywhere;
}

.board-detail-description {
    margin-top: 0.25rem;
    color: rgba(0, 0, 0, 0.6);
}

.board-detail-actions {
    display: flex;
    gap: 0.5rem;
}

.board-detail-actions .button {
    color: #68717d;
    text-decoration: none;
    box-shadow: none;
}

.board-detail-actions .button:hover {
    background: #edf0f3;
    color: #363636;
}

.board-detail-actions .button svg {
    margin-right: 0.35rem;
}

.board-columns {
    display: flex;
    gap: 1rem;
    align-items: flex-start;
    overflow-x: auto;
    padding-bottom: 0.5rem;
    flex: 1;
    min-height: 0;
}

.board-detail :deep(.b-sidebar) {
    --bulma-sidebar-width: 32rem;
}
</style>
