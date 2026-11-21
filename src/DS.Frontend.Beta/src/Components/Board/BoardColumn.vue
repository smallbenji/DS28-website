<template>
    <section
        class="board-column flex max-h-full w-72 shrink-0 flex-col rounded-lg border-t-4 bg-default shadow-sm ring ring-default transition-opacity"
        :class="[column.isDoneColumn ? 'border-t-success' : 'border-t-(--ui-border-accented)', { 'opacity-50': dragging, 'drop-before': columnTarget && !columnTarget.after, 'drop-after': columnTarget?.after }]"
        :aria-label="`Kolonne ${column.title}`"
        @dragover="onDragOver"
        @dragleave="onDragLeave"
        @drop="onDrop"
    >
        <header class="flex items-center gap-1 border-b border-default px-2 py-2">
            <button
                v-if="canManage"
                type="button"
                class="cursor-grab rounded p-1 text-muted hover:text-highlighted focus-visible:outline-2 focus-visible:outline-primary"
                draggable="true"
                :aria-label="`Flyt kolonnen ${column.title}`"
                @dragstart="onColumnDragStart"
            >
                <UIcon name="i-lucide-grip-vertical" class="size-4" />
            </button>
            <h2 class="min-w-0 flex-1 truncate font-semibold text-highlighted">{{ column.title }}</h2>
            <UBadge
                :label="column.wipLimit ? `${cards.length} / ${column.wipLimit}` : String(cards.length)"
                color="neutral"
                variant="subtle"
                size="sm"
            />
            <template v-if="canManage">
                <UButton color="neutral" variant="ghost" size="xs" icon="i-lucide-pencil" :aria-label="`Rediger kolonnen ${column.title}`" @click="emit('editColumn', column)" />
                <UButton color="neutral" variant="ghost" size="xs" icon="i-lucide-trash-2" :aria-label="`Slet kolonnen ${column.title}`" @click="emit('deleteColumn', column)" />
            </template>
        </header>

        <div ref="cardsContainer" class="flex min-h-16 flex-1 flex-col gap-2 overflow-y-auto p-2">
            <BoardCard
                v-for="card in cards"
                :key="card.id"
                :card="card"
                :can-manage="canManage"
                :is-done-column="column.isDoneColumn"
                :drop-indicator="dropIndicatorFor(card)"
                @open="emit('openCard', $event)"
            />
            <p
                v-if="visibleCards.length === 0"
                class="rounded-md border border-dashed py-4 text-center text-xs"
                :class="cardTarget ? 'border-primary text-primary' : 'border-default text-muted'"
            >
                {{ cardTarget ? 'Slip kortet her' : 'Ingen kort' }}
            </p>
        </div>

        <div v-if="canManage" class="border-t border-default p-2">
            <UButton color="neutral" variant="ghost" size="sm" icon="i-lucide-plus" label="Tilføj kort" block class="justify-start" @click="emit('addCard', column.id)" />
        </div>
    </section>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import type { BoardColumnDto, WorkItemDto } from "@/types";
import { isSameDropTarget, useBoardDrag, type BoardDropTarget } from "@/lib/board/boardDrag";
import BoardCard from "@/Components/Board/BoardCard.vue";

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
const dragging = computed(() => drag.value?.kind === "column" && drag.value.id === columnId.value);
const draggedCardId = computed(() => drag.value?.kind === "card" ? drag.value.id : null);
const visibleCards = computed(() => props.cards.filter((card) => Number(card.id) !== draggedCardId.value));

const columnTarget = computed(() => {
    const target = drag.value?.target;

    return target?.kind === "column" && target.columnId === columnId.value ? target : null;
});

const cardTarget = computed(() => {
    const target = drag.value?.target;

    return target?.kind === "card" && target.columnId === columnId.value ? target : null;
});

function dropIndicatorFor(card: WorkItemDto) {
    const target = cardTarget.value;
    if (!target) return null;

    const index = visibleCards.value.indexOf(card);
    if (index < 0) return null;
    if (index === target.index) return "before";
    if (index === visibleCards.value.length - 1 && target.index === visibleCards.value.length) return "after";

    return null;
}

function onColumnDragStart(event: DragEvent) {
    if (!props.canManage) {
        event.preventDefault();
        return;
    }

    event.dataTransfer?.setData("text/plain", `column:${props.column.id}`);
    if (event.dataTransfer) event.dataTransfer.effectAllowed = "move";
    drag.value = { kind: "column", id: columnId.value, target: null };
}

function cardInsertionIndex(clientY: number) {
    const elements = Array.from(cardsContainer.value?.querySelectorAll<HTMLElement>("[data-card-id]") ?? [])
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

    return { kind: "card", columnId: columnId.value, index };
}

function columnDropTarget(event: DragEvent): BoardDropTarget | null {
    if (dragging.value) return null;

    const bounds = (event.currentTarget as HTMLElement).getBoundingClientRect();

    return { kind: "column", columnId: columnId.value, after: event.clientX > bounds.left + bounds.width / 2 };
}

function onDragOver(event: DragEvent) {
    if (!props.canManage || !drag.value) return;

    event.preventDefault();
    event.stopPropagation();
    if (event.dataTransfer) event.dataTransfer.dropEffect = "move";

    const target = drag.value.kind === "card" ? cardDropTarget(event.clientY) : columnDropTarget(event);
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

    if (kind === "card" && target.kind === "card") {
        emit("moveCard", { cardId: id, columnId: columnId.value, position: target.index });
    } else if (kind === "column" && target.kind === "column") {
        emit("reorderColumn", { targetColumnId: columnId.value, draggedColumnId: id, after: target.after });
    }
}
</script>

<style scoped>
.board-column {
    position: relative;
}

.board-column.drop-before::before,
.board-column.drop-after::after {
    content: "";
    position: absolute;
    top: 0;
    bottom: 0;
    width: 3px;
    border-radius: 3px;
    background: var(--ui-primary);
}

.board-column.drop-before::before {
    left: -8px;
}

.board-column.drop-after::after {
    right: -8px;
}
</style>
