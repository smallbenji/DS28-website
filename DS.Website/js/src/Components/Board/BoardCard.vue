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
        <p class="board-card-number">DS-{{ card.id }}</p>
        <p class="board-card-title">{{ card.title }}</p>
        <p v-if="descriptionPreview" class="board-card-description">{{ descriptionPreview }}</p>

        <div v-if="card.dueDate || Number(card.priority) !== 0 || card.assigneeName" class="board-card-meta">
            <span v-if="card.dueDate" class="board-card-due" :class="{ 'is-overdue': isOverdue }" :title="deadlineLabel">
                <font-awesome-icon icon="calendar-day" aria-hidden="true" />
                <span class="is-sr-only">{{ isOverdue ? 'Overskredet deadline:' : 'Deadline:' }}</span>
                {{ formattedDueDate }}
            </span>
            <span v-if="Number(card.priority) !== 0" class="board-card-priority" :class="{ 'is-high': Number(card.priority) > 0 }" :title="priorityLabel" role="img" :aria-label="priorityLabel">
                <i class="fas" :class="Number(card.priority) > 0 ? 'fa-arrow-up' : 'fa-arrow-down'" aria-hidden="true"></i>
            </span>
            <BTooltip v-if="card.assigneeName" class="board-card-assignee" :label="card.assigneeName" :triggers="['hover', 'focus']" position="is-left">
                <span class="board-card-avatar" tabindex="0" role="img" :aria-label="`Ansvarlig: ${card.assigneeName}`">
                    <img v-if="assigneePictureUrl && !assigneePictureFailed" :src="assigneePictureUrl" alt="" loading="lazy" draggable="false" @error="assigneePictureFailed = true" />
                    <span v-else aria-hidden="true">{{ assigneeInitials }}</span>
                </span>
            </BTooltip>
        </div>
    </article>
</template>

<script setup lang="ts">
import { computed, onMounted, onBeforeUnmount, ref, watch } from 'vue';
import { BTooltip } from 'buefy';
import type { WorkItemDto } from '@/types';
import { htmlToPlainText } from '@/lib/util';

const props = defineProps<{
    card: WorkItemDto;
    canManage: boolean;
    isDoneColumn: boolean;
}>();

const descriptionPreview = computed(() => htmlToPlainText(props.card.description));

const today = ref(currentDate());
let dateRefresh: ReturnType<typeof setInterval> | undefined;
const shortDateFormat = new Intl.DateTimeFormat('da-DK', { day: 'numeric', month: 'short' });
const fullDateFormat = new Intl.DateTimeFormat('da-DK', { day: 'numeric', month: 'long', year: 'numeric' });
const dueDate = computed(() => props.card.dueDate ? new Date(`${props.card.dueDate}T12:00:00`) : null);
const formattedDueDate = computed(() => dueDate.value ? shortDateFormat.format(dueDate.value) : '');
const isOverdue = computed(() => !props.isDoneColumn && !!props.card.dueDate && props.card.dueDate < today.value);
const deadlineLabel = computed(() => dueDate.value ? `${isOverdue.value ? 'Overskredet deadline' : 'Deadline'}: ${fullDateFormat.format(dueDate.value)}` : '');
const priorityLabel = computed(() => Number(props.card.priority) > 0 ? 'Høj prioritet' : 'Lav prioritet');
const assigneePictureUrl = computed(() => props.card.assigneeProfilePicture?.url ?? null);
const assigneePictureFailed = ref(false);
watch(assigneePictureUrl, () => { assigneePictureFailed.value = false; });
const assigneeInitials = computed(() => {
    const names = props.card.assigneeName?.trim().split(/\s+/) ?? [];
    return [names[0]?.[0], names.length > 1 ? names[names.length - 1]?.[0] : ''].join('').toLocaleUpperCase('da-DK');
});

function currentDate() {
    return new Intl.DateTimeFormat('sv-SE', { timeZone: 'Europe/Copenhagen' }).format(new Date());
}

onMounted(() => {
    dateRefresh = setInterval(() => { today.value = currentDate(); }, 60000);
});

onBeforeUnmount(() => {
    clearInterval(dateRefresh);
});


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
    border: 1px solid #e8ebef;
    border-radius: 6px;
    padding: 0.85rem 0.9rem;
    cursor: pointer;
    box-shadow: none;
    transition: border-color 0.15s ease, box-shadow 0.15s ease;
}

.board-card:hover {
    border-color: #cfd5dd;
    box-shadow: 0 2px 4px rgba(10, 10, 10, 0.04);
}

.board-card:focus-visible {
    outline: 2px solid hsl(217, 71%, 53%);
    outline-offset: 1px;
}

.board-card.is-done .board-card-title {
    text-decoration: line-through;
    color: rgba(0, 0, 0, 0.45);
}

.board-card-number {
    margin-bottom: 0.25rem;
    color: #68717d;
    font-size: 0.75rem;
}

.board-card-title {
    font-weight: 500;
    font-size: 0.95rem;
    line-height: 1.45;
    overflow-wrap: anywhere;
}

.board-card-description {
    margin-top: 0.25rem;
    color: #7b818a;
    font-size: 0.85rem;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
    overflow-wrap: anywhere;
}

.board-card-meta {
    display: flex;
    flex-wrap: nowrap;
    align-items: center;
    gap: 0.65rem;
    margin-top: 0.75rem;
    font-size: 0.75rem;
    color: rgba(0, 0, 0, 0.55);
}

.board-card-due {
    display: inline-flex;
    align-items: center;
    gap: 0.25rem;
}

.board-card-due.is-overdue {
    color: #b83c47;
}

.board-card-priority {
    display: inline-flex;
    align-items: center;
    color: #8b929c;
    font-size: 0.7rem;
}

.board-card-priority.is-high {
    color: #a77b28;
}

.board-card-avatar {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 1.5rem;
    height: 1.5rem;
    border-radius: 50%;
    overflow: hidden;
    background: #edf0f5;
    color: #5c687a;
    font-size: 0.6rem;
    font-weight: 600;
}

.board-card-avatar img {
    width: 100%;
    height: 100%;
    object-fit: cover;
}

.board-card-avatar:focus-visible {
    outline: 2px solid hsl(217, 71%, 53%);
    outline-offset: 2px;
}

.board-card-assignee {
    margin-left: auto;
}
</style>
