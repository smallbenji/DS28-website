<template>
    <article
        class="board-card"
        :class="{ 'is-done': isDoneColumn }"
        :draggable="canManage"
        tabindex="0"
        @click="emit('open', card)"
        @keydown.enter="emit('open', card)"
        @dragstart="onDragStart"
        @dragover.prevent
        @drop.stop.prevent="onDrop"
    >
        <p class="board-card-title">{{ card.title }}</p>
        <p v-if="descriptionPreview" class="board-card-description">{{ descriptionPreview }}</p>

        <div class="board-card-meta">
            <span v-if="card.dueDate" class="board-card-due">
                <font-awesome-icon icon="calendar-day" />
                {{ card.dueDate }}
            </span>
            <span v-if="Number(card.priority) > 0" class="tag is-warning is-light">Høj</span>
            <span v-else-if="Number(card.priority) < 0" class="tag is-light">Lav</span>
            <span v-if="card.assigneeName" class="board-card-assignee">{{ card.assigneeName }}</span>
        </div>
    </article>
</template>

<script setup lang="ts">
import { computed } from 'vue';
import type { WorkItemDto } from '@/types';
import { htmlToPlainText } from '@/lib/util';

const props = defineProps<{
    card: WorkItemDto;
    canManage: boolean;
    isDoneColumn: boolean;
}>();

const descriptionPreview = computed(() => htmlToPlainText(props.card.description));

const emit = defineEmits<{
    open: [card: WorkItemDto];
    dropCard: [payload: { targetCardId: string; draggedId: string }];
}>();

function onDragStart(event: DragEvent) {
    if (!props.canManage) {
        event.preventDefault();
        return;
    }

    event.dataTransfer?.setData('text/plain', `card:${props.card.id}`);
    if (event.dataTransfer) event.dataTransfer.effectAllowed = 'move';
}

function onDrop(event: DragEvent) {
    const payload = event.dataTransfer?.getData('text/plain') ?? '';
    if (!payload.startsWith('card:')) return;

    const draggedId = payload.slice('card:'.length);
    if (!draggedId || draggedId === String(props.card.id)) return;

    emit('dropCard', { targetCardId: String(props.card.id), draggedId });
}
</script>

<style scoped>
.board-card {
    background: #fff;
    border: 1px solid hsl(0, 0%, 90%);
    border-radius: 6px;
    padding: 0.6rem 0.7rem;
    cursor: pointer;
    box-shadow: 0 1px 2px rgba(10, 10, 10, 0.06);
    transition: box-shadow 0.15s ease, transform 0.15s ease;
}

.board-card:hover {
    box-shadow: 0 2px 6px rgba(10, 10, 10, 0.12);
}

.board-card:focus-visible {
    outline: 2px solid hsl(217, 71%, 53%);
    outline-offset: 1px;
}

.board-card.is-done .board-card-title {
    text-decoration: line-through;
    color: rgba(0, 0, 0, 0.45);
}

.board-card-title {
    font-weight: 600;
    line-height: 1.3;
    overflow-wrap: anywhere;
}

.board-card-description {
    margin-top: 0.25rem;
    color: rgba(0, 0, 0, 0.6);
    font-size: 0.85rem;
    display: -webkit-box;
    -webkit-line-clamp: 3;
    -webkit-box-orient: vertical;
    overflow: hidden;
    overflow-wrap: anywhere;
}

.board-card-meta {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 0.35rem;
    margin-top: 0.5rem;
    font-size: 0.75rem;
    color: rgba(0, 0, 0, 0.55);
}

.board-card-due {
    display: inline-flex;
    align-items: center;
    gap: 0.25rem;
}

.board-card-assignee {
    margin-left: auto;
}
</style>
