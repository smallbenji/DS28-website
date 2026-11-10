<template>
    <section
        class="board-column"
        :class="{ 'is-done': column.isDoneColumn, 'is-dragging': dragging, 'drop-before': columnTarget && !columnTarget.after, 'drop-after': columnTarget?.after, 'drop-append': cardTarget }"
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

        <div ref="cardsContainer" class="board-column-cards">
            <BoardCard
                v-for="card in cards"
                :key="card.id"
                :card="card"
                :can-manage="canManage"
                :is-done-column="column.isDoneColumn"
                :drop-indicator="dropIndicatorFor(card)"
                @open="emit('openCard', $event)"
            />
            <p v-if="visibleCards.length === 0" class="board-column-empty" :class="{ 'is-drop-target': cardTarget }">
                {{ cardTarget ? 'Slip kortet her' : 'Ingen kort' }}
            </p>
        </div>

        <BButton v-if="canManage" size="is-small" class="board-column-add is-ghost" @click="emit('addCard', column.id)">
            <font-awesome-icon icon="plus" />
            <span>Tilføj kort</span>
        </BButton>
    </section>
</template>

<script setup lang="ts">
import { BButton } from 'buefy';
import { computed, ref } from 'vue';
import type { BoardColumnDto, WorkItemDto } from '@/types';
import BoardCard from './BoardCard.vue';
import { isSameDropTarget, useBoardDrag, type BoardDropTarget } from './boardDrag';

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
    moveCard: [payload: { cardId: number; columnId: number; position: number }];
    reorderColumn: [payload: { targetColumnId: number; draggedColumnId: number; after: boolean }];
}>();

const drag = useBoardDrag();
const cardsContainer = ref<HTMLElement | null>(null);
const columnId = computed(() => Number(props.column.id));
const dragging = computed(() => drag.value?.kind === 'column' && drag.value.id === columnId.value);
const draggedCardId = computed(() => drag.value?.kind === 'card' ? drag.value.id : null);
const visibleCards = computed(() => props.cards.filter((card) => Number(card.id) !== draggedCardId.value));

const columnTarget = computed(() => {
    const target = drag.value?.target;

    return target?.kind === 'column' && target.columnId === columnId.value ? target : null;
});

const cardTarget = computed(() => {
    const target = drag.value?.target;

    return target?.kind === 'card' && target.columnId === columnId.value ? target : null;
});

function dropIndicatorFor(card: WorkItemDto) {
    const target = cardTarget.value;
    if (!target) return null;

    const index = visibleCards.value.indexOf(card);
    if (index < 0) return null;
    if (index === target.index) return 'before';
    if (index === visibleCards.value.length - 1 && target.index === visibleCards.value.length) return 'after';

    return null;
}

function onColumnDragStart(event: DragEvent) {
    if (!props.canManage) {
        event.preventDefault();
        return;
    }

    event.dataTransfer?.setData('text/plain', `column:${props.column.id}`);
    if (event.dataTransfer) event.dataTransfer.effectAllowed = 'move';

    drag.value = { kind: 'column', id: columnId.value, target: null };
}

function cardInsertionIndex(clientY: number) {
    const elements = Array.from(cardsContainer.value?.querySelectorAll<HTMLElement>('[data-card-id]') ?? [])
        .filter((element) => Number(element.dataset.cardId) !== draggedCardId.value);
    const index = elements.findIndex((element) => {
        const bounds = element.getBoundingClientRect();

        return clientY < bounds.top + bounds.height / 2;
    });

    return index < 0 ? elements.length : index;
}

function cardDropTarget(clientY: number): BoardDropTarget | null {
    const index = cardInsertionIndex(clientY);
    const currentIndex = props.cards.findIndex((card) => Number(card.id) === draggedCardId.value);
    if (currentIndex === index) return null;

    return { kind: 'card', columnId: columnId.value, index };
}

function columnDropTarget(event: DragEvent): BoardDropTarget | null {
    if (dragging.value) return null;

    const bounds = (event.currentTarget as HTMLElement).getBoundingClientRect();

    return { kind: 'column', columnId: columnId.value, after: event.clientX > bounds.left + bounds.width / 2 };
}

function onDragOver(event: DragEvent) {
    if (!props.canManage || !drag.value) return;

    event.preventDefault();
    event.stopPropagation();
    if (event.dataTransfer) event.dataTransfer.dropEffect = 'move';

    const target = drag.value.kind === 'card' ? cardDropTarget(event.clientY) : columnDropTarget(event);
    if (!isSameDropTarget(drag.value.target, target)) drag.value.target = target;
}

function onDragLeave(event: DragEvent) {
    if ((event.currentTarget as HTMLElement).contains(event.relatedTarget as Node | null)) return;
    if ((columnTarget.value || cardTarget.value) && drag.value) drag.value.target = null;
}

function onDrop(event: DragEvent) {
    if (!props.canManage || !drag.value) return;

    event.preventDefault();
    event.stopPropagation();
    const { kind, id, target } = drag.value;
    drag.value = null;
    if (!target || target.columnId !== columnId.value) return;

    if (kind === 'card' && target.kind === 'card') {
        emit('moveCard', { cardId: id, columnId: columnId.value, position: target.index });
    } else if (kind === 'column' && target.kind === 'column') {
        emit('reorderColumn', { targetColumnId: columnId.value, draggedColumnId: id, after: target.after });
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
    border: 1px dashed transparent;
    border-radius: 4px;
}

.board-column-empty.is-drop-target {
    border-color: #3273dc;
    background: #e8f0fd;
    color: #2458a6;
    pointer-events: none;
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
