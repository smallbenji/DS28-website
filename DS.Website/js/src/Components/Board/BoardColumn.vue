<template>
    <section
        class="board-column"
        :class="{ 'is-done': column.isDoneColumn, 'is-dragging': dragging, 'drop-before': columnTarget && !columnTarget.after, 'drop-after': columnTarget?.after, 'drop-append': appendTarget }"
        @dragover="onDragOver"
        @dragleave="onDragLeave"
        @drop="onDrop"
    >
        <header class="board-column-head">
            <button
                v-if="canManage"
                type="button"
                class="board-column-handle"
                draggable="true"
                :aria-label="`Flyt kolonnen ${column.title}`"
                @dragstart="onColumnDragStart"
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
            <div v-if="appendTarget" class="board-column-drop" role="status">Slip kortet her</div>
        </div>

        <BButton v-if="canManage" size="is-small" class="board-column-add is-ghost" @click="emit('addCard', column.id)">
            <font-awesome-icon icon="plus" />
            <span>Tilføj kort</span>
        </BButton>
    </section>
</template>

<script setup lang="ts">
import { BButton } from 'buefy';
import { computed } from 'vue';
import type { BoardColumnDto, WorkItemDto } from '@/types';
import BoardCard from './BoardCard.vue';
import { useBoardDrag } from './boardDrag';

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
    dropCard: [payload: { targetCardId: string; draggedId: string; after: boolean }];
    dropColumn: [payload: { columnId: number; draggedId: string }];
    reorderColumn: [payload: { targetColumnId: number; draggedColumnId: number; after: boolean }];
}>();

const drag = useBoardDrag();
const dragging = computed(() => drag.value?.kind === 'column' && drag.value.id === Number(props.column.id));
const columnTarget = computed(() => drag.value?.target?.kind === 'column' && drag.value.target.id === Number(props.column.id) ? drag.value.target : null);
const appendTarget = computed(() => drag.value?.target?.kind === 'append' && drag.value.target.id === Number(props.column.id));

function onColumnDragStart(event: DragEvent) {
    if (!props.canManage) {
        event.preventDefault();
        return;
    }

    event.dataTransfer?.setData('text/plain', `column:${props.column.id}`);
    if (event.dataTransfer) event.dataTransfer.effectAllowed = 'move';

    drag.value = { kind: 'column', id: Number(props.column.id), target: null };
}

function onDragOver(event: DragEvent) {
    if (!props.canManage || !drag.value) return;

    event.preventDefault();
    event.stopPropagation();
    if (event.dataTransfer) event.dataTransfer.dropEffect = 'move';
    if (dragging.value) {
        drag.value.target = null;
        return;
    }

    const bounds = (event.currentTarget as HTMLElement).getBoundingClientRect();
    drag.value.target = {
        kind: drag.value.kind === 'card' ? 'append' : 'column',
        id: Number(props.column.id),
        after: event.clientX > bounds.left + bounds.width / 2
    };
}

function onDragLeave(event: DragEvent) {
    if ((event.currentTarget as HTMLElement).contains(event.relatedTarget as Node | null)) return;
    if ((columnTarget.value || appendTarget.value) && drag.value) drag.value.target = null;
}

function onDrop(event: DragEvent) {
    if (!props.canManage || !drag.value) return;

    event.preventDefault();
    event.stopPropagation();
    const { kind, id, target } = drag.value;
    drag.value = null;
    if (!target || target.id !== Number(props.column.id)) return;

    if (kind === 'card' && target.kind === 'append') {
        emit('dropColumn', { columnId: props.column.id, draggedId: String(id) });
    } else if (kind === 'column' && target.kind === 'column') {
        emit('reorderColumn', { targetColumnId: props.column.id, draggedColumnId: id, after: target.after });
    }
}
</script>

<style scoped>
.board-column {
    position: relative;
    display: flex;
    flex-direction: column;
    flex: 0 0 18rem;
    width: 18rem;
    max-height: 100%;
    background: #f0f2f5;
    border: 1px solid transparent;
    border-radius: 8px;
    overflow: hidden;
}

.board-column.drop-before {
    border-left-color: #3273dc;
    box-shadow: inset 3px 0 #3273dc;
}

.board-column.drop-after {
    border-right-color: #3273dc;
    box-shadow: inset -3px 0 #3273dc;
}

.board-column.drop-append {
    border-color: #3273dc;
}

.board-column-drop {
    border-top: 4px solid #3273dc;
    padding: 0.4rem;
    color: #2458a6;
    background: #e8f0fd;
    border-radius: 3px;
    font-size: 0.8rem;
    text-align: center;
    pointer-events: none;
}

.board-column.is-done {
    background: #f0f2f5;
}

.board-column.is-done .board-column-title::before {
    content: '';
    display: inline-block;
    width: 0.45rem;
    height: 0.45rem;
    margin-right: 0.5rem;
    border-radius: 50%;
    background: #48a879;
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
    border-bottom: 0;
}

.board-column-title {
    font-weight: 600;
    font-size: 0.9rem;
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
    opacity: 0.35;
    transition: opacity 0.15s ease;
}

.board-column:hover .board-column-actions,
.board-column:focus-within .board-column-actions {
    opacity: 1;
}

@media (hover: none) {
    .board-column-actions {
        opacity: 1;
    }
}

.board-icon-button {
    border: none;
    background: transparent;
    color: rgba(0, 0, 0, 0.5);
    cursor: pointer;
    padding: 0.4rem;
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
    flex: 0 1 auto;
}

.board-column-empty {
    color: rgba(0, 0, 0, 0.4);
    font-size: 0.85rem;
    padding: 0.5rem 0.35rem;
}

.board-column-add {
    margin: 0 0.5rem 0.5rem;
    justify-content: flex-start;
    border: 0;
    box-shadow: none;
    background: transparent;
    color: #68717d;
    text-decoration: none;
    flex-shrink: 0;
}

.board-column-add:hover {
    background: #e5e8ed;
    color: #363636;
    text-decoration: none;
}

.board-column-handle:focus-visible,
.board-icon-button:focus-visible,
.board-column-add:focus-visible {
    outline: 2px solid hsl(217, 71%, 53%);
    outline-offset: 2px;
}

.board-column-add svg {
    margin-right: 0.35rem;
}
</style>
