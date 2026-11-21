<template>
    <article
        class="board-card relative cursor-pointer rounded-md border border-default p-3 text-sm transition-colors focus-visible:outline-2 focus-visible:outline-primary"
        :class="[card.color ? '' : 'bg-elevated hover:bg-default', { 'opacity-45': isDragging, 'drop-before': dropIndicator === 'before', 'drop-after': dropIndicator === 'after' }]"
        :style="card.color ? { backgroundColor: `color-mix(in oklab, ${card.color} 30%, var(--ui-bg))` } : undefined"
        :draggable="canManage"
        :data-card-id="card.id"
        tabindex="0"
        @click="emit('open', card)"
        @keydown.enter="emit('open', card)"
        @dragstart="onDragStart"
    >
        <div class="flex items-start justify-between gap-2">
            <span class="font-mono text-xs text-muted">DS-{{ card.id }}</span>
            <span v-if="card.tags?.length" class="flex flex-wrap justify-end gap-1">
                <BoardTagBadge v-for="tag in card.tags" :key="tag.id" :tag="tag" />
            </span>
        </div>

        <p class="mt-1 font-medium" :class="isDoneColumn ? 'text-muted line-through' : 'text-highlighted'">{{ card.title }}</p>
        <p v-if="descriptionPreview" class="mt-1 line-clamp-2 text-xs text-muted">{{ descriptionPreview }}</p>

        <UBadge v-if="waitingLabel" class="mt-2" :label="waitingLabel" icon="i-lucide-clock" color="warning" variant="subtle" size="sm" />

        <div v-if="card.dueDate || Number(card.priority) !== 0 || card.assignees.length" class="mt-2 flex items-center gap-2 text-xs">
            <span v-if="card.dueDate" class="flex items-center gap-1" :class="isOverdue ? 'font-medium text-error' : 'text-muted'" :title="deadlineLabel">
                <UIcon name="i-lucide-calendar" class="size-3.5" />
                <span class="sr-only">{{ isOverdue ? 'Overskredet deadline:' : 'Deadline:' }}</span>
                {{ formattedDueDate }}
            </span>
            <UIcon
                v-if="Number(card.priority) !== 0"
                :name="Number(card.priority) > 0 ? 'i-lucide-arrow-up' : 'i-lucide-arrow-down'"
                class="size-4"
                :class="Number(card.priority) > 0 ? 'text-warning' : 'text-muted'"
                role="img"
                :aria-label="priorityLabel"
            />
            <UAvatarGroup v-if="card.assignees.length" size="2xs" :max="3" class="ms-auto">
                <UTooltip v-for="user in card.assignees" :key="user.id" :text="`${user.firstName} ${user.lastName}`">
                    <UAvatar
                        :src="user.profilePicture?.url"
                        :alt="`Ansvarlig: ${user.firstName} ${user.lastName}`"
                        :text="`${user.firstName?.[0] ?? ''}${user.lastName?.[0] ?? ''}`.toLocaleUpperCase('da-DK')"
                        draggable="false"
                    />
                </UTooltip>
            </UAvatarGroup>
        </div>
    </article>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from "vue";
import type { WorkItemDto } from "@/types";
import { htmlToPlainText } from "@/lib/util";
import { useBoardDrag } from "@/lib/board/boardDrag";
import BoardTagBadge from "@/Components/Board/BoardTagBadge.vue";

const props = defineProps<{
    card: WorkItemDto;
    canManage: boolean;
    isDoneColumn: boolean;
    dropIndicator: "before" | "after" | null;
}>();

const emit = defineEmits<{ open: [card: WorkItemDto] }>();

const drag = useBoardDrag();
const dragImageCaptured = ref(false);
const isDragging = computed(() => dragImageCaptured.value && drag.value?.kind === "card" && drag.value.id === Number(props.card.id));

const descriptionPreview = computed(() => htmlToPlainText(props.card.description));

const unfinishedDependencies = computed(() => (props.card.waitingOn ?? []).filter((dependency) => !dependency.isDone));
const waitingLabel = computed(() => {
    if (props.isDoneColumn || unfinishedDependencies.value.length === 0) return "";
    if (unfinishedDependencies.value.length === 1) return `Venter på DS-${unfinishedDependencies.value[0]!.id}`;

    return `Venter på ${unfinishedDependencies.value.length} kort`;
});

const today = ref(currentDate());
let dateRefresh: ReturnType<typeof setInterval> | undefined;
const shortDateFormat = new Intl.DateTimeFormat("da-DK", { day: "numeric", month: "short" });
const fullDateFormat = new Intl.DateTimeFormat("da-DK", { day: "numeric", month: "long", year: "numeric" });
const dueDate = computed(() => props.card.dueDate ? new Date(`${props.card.dueDate}T12:00:00`) : null);
const formattedDueDate = computed(() => dueDate.value ? shortDateFormat.format(dueDate.value) : "");
const isOverdue = computed(() => !props.isDoneColumn && !!props.card.dueDate && props.card.dueDate < today.value);
const deadlineLabel = computed(() => dueDate.value ? `${isOverdue.value ? "Overskredet deadline" : "Deadline"}: ${fullDateFormat.format(dueDate.value)}` : "");
const priorityLabel = computed(() => Number(props.card.priority) > 0 ? "Høj prioritet" : "Lav prioritet");

function currentDate() {
    return new Intl.DateTimeFormat("sv-SE", { timeZone: "Europe/Copenhagen" }).format(new Date());
}

onMounted(() => {
    dateRefresh = setInterval(() => { today.value = currentDate(); }, 60000);
});

onBeforeUnmount(() => clearInterval(dateRefresh));

function onDragStart(event: DragEvent) {
    if (!props.canManage) {
        event.preventDefault();
        return;
    }

    event.dataTransfer?.setData("text/plain", `card:${props.card.id}`);
    if (event.dataTransfer) event.dataTransfer.effectAllowed = "move";
    drag.value = { kind: "card", id: Number(props.card.id), target: null };
    dragImageCaptured.value = false;
    requestAnimationFrame(() => { dragImageCaptured.value = true; });
}
</script>

<style scoped>
.board-card.drop-before::before,
.board-card.drop-after::after {
    content: "";
    position: absolute;
    left: 0;
    right: 0;
    height: 3px;
    border-radius: 3px;
    background: var(--ui-primary);
}

.board-card.drop-before::before {
    top: -6px;
}

.board-card.drop-after::after {
    bottom: -6px;
}
</style>
