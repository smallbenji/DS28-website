<template>
    <section
        class="board-column"
        :class="{ 'is-done': column.isDoneColumn, 'is-dragging': dragging }"
        @dragover.prevent
        @drop.stop.prevent="onDrop"
    >
        <header class="board-column-head">
            <button
                v-if="canManage"
                type="button"
                class="board-column-handle"
                draggable="true"
                :aria-label="`Flyt kolonnen ${column.title}`"
                @dragstart="onColumnDragStart"
                @dragend="dragging = false"
            >
                <font-awesome-icon icon="grip-vertical" />
            </button>
            <p class="board-column-title">{{ column.title }}</p>
            <span class="board-column-count">
                {{ cards.length }}<template v-if="column.wipLimit"> / {{ column.wipLimit }}</template>
            </span>
            <div v-if="canManage" class="board-column-actions">
                <button
                    type="button"
                    class="board-icon-button"
                    :aria-label="`Rediger kolonnen ${column.title}`"
                    @click="emit('editColumn', column)"
                >
                    <font-awesome-icon icon="pen" />
                </button>
                <button
                    type="button"
                    class="board-icon-button"
                    :aria-label="`Slet kolonnen ${column.title}`"
                    @click="emit('deleteColumn', column)"
                >
                    <font-awesome-icon icon="trash" />
                </button>
            </div>
        </header>

        <div class="board-column-cards">
            <BoardCard
                v-for="card in cards"
                :key="card.id"
                :card="card"
                :can-manage="canManage"
                :is-done-column="column.isDoneColumn"
                @open="emit('openCard', $event)"
                @drop-card="emit('dropCard', $event)"
            />
            <p v-if="cards.length === 0" class="board-column-empty">Ingen kort</p>
        </div>

        <BButton v-if="canManage" size="is-small" class="board-column-add" @click="emit('addCard', column.id)">
            <font-awesome-icon icon="plus" />
            <span>Tilføj kort</span>
        </BButton>
    </section>
</template>

<script setup lang="ts">
import { BButton } from 'buefy';
import { ref } from 'vue';
import type { BoardColumnDto, WorkItemDto } from '@/types';
import BoardCard from './BoardCard.vue';

const props = defineProps<{
    column: BoardColumnDto;
    cards: WorkItemDto[];
    canManage: boolean;
}>();

const emit = defineEmits<{
    openCard: [card: WorkItemDto];
    addCard: [columnId: number];
    editColumn: [column: BoardColumnDto];
    deleteColumn: [column: BoardColumnDto];
    dropCard: [payload: { targetCardId: string; draggedId: string }];
    dropColumn: [payload: { columnId: number; draggedId: string }];
    reorderColumn: [payload: { targetColumnId: number; draggedColumnId: number }];
}>();

const dragging = ref(false);

function onColumnDragStart(event: DragEvent) {
    event.dataTransfer?.setData('text/plain', `column:${props.column.id}`);
    if (event.dataTransfer) event.dataTransfer.effectAllowed = 'move';

    dragging.value = true;
}

function onDrop(event: DragEvent) {
    const payload = event.dataTransfer?.getData('text/plain') ?? '';
    if (!payload) return;

    if (payload.startsWith('card:')) {
        const draggedId = payload.slice('card:'.length);
        if (!draggedId) return;

        emit('dropColumn', { columnId: props.column.id, draggedId });
        return;
    }

    if (!payload.startsWith('column:')) return;

    const draggedColumnId = Number(payload.slice('column:'.length));
    if (!draggedColumnId || draggedColumnId === Number(props.column.id)) return;

    emit('reorderColumn', { targetColumnId: props.column.id, draggedColumnId });
}
</script>

<style scoped>
.board-column {
    display: flex;
    flex-direction: column;
    flex: 0 0 18rem;
    width: 18rem;
    max-height: 100%;
    background: hsl(0, 0%, 96%);
    border: 1px solid hsl(0, 0%, 90%);
    border-radius: 8px;
    overflow: hidden;
}

.board-column.is-done {
    background: hsl(141, 53%, 96%);
    border-color: hsl(141, 53%, 85%);
}

.board-column.is-dragging {
    opacity: 0.45;
}

.board-column-handle {
    border: none;
    background: transparent;
    color: rgba(0, 0, 0, 0.35);
    cursor: grab;
    padding: 0.25rem 0.15rem;
    border-radius: 4px;
    line-height: 1;
}

.board-column-handle:hover {
    background: rgba(0, 0, 0, 0.06);
    color: rgba(0, 0, 0, 0.6);
}

.board-column-head {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.6rem 0.75rem;
    border-bottom: 1px solid hsl(0, 0%, 90%);
}

.board-column-title {
    font-weight: 600;
    overflow-wrap: anywhere;
}

.board-column-count {
    color: rgba(0, 0, 0, 0.5);
    font-size: 0.8rem;
}

.board-column-actions {
    margin-left: auto;
    display: flex;
    gap: 0.15rem;
}

.board-icon-button {
    border: none;
    background: transparent;
    color: rgba(0, 0, 0, 0.5);
    cursor: pointer;
    padding: 0.25rem;
    border-radius: 4px;
    line-height: 1;
}

.board-icon-button:hover {
    background: rgba(0, 0, 0, 0.06);
    color: rgba(0, 0, 0, 0.75);
}

.board-column-cards {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    padding: 0.5rem;
    overflow-y: auto;
    min-height: 3rem;
    flex: 1;
}

.board-column-empty {
    color: rgba(0, 0, 0, 0.4);
    font-size: 0.85rem;
    text-align: center;
    padding: 0.75rem 0;
}

.board-column-add {
    margin: 0 0.5rem 0.5rem;
}

.board-column-add svg {
    margin-right: 0.35rem;
}
</style>
