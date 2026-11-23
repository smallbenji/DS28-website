<template>
    <article
        class="board-card relative cursor-pointer rounded-md border border-default bg-elevated/60 pe-3 text-sm transition-colors hover:bg-default focus-visible:outline-2 focus-visible:outline-primary"
        :class="[card.color ? 'ps-4' : 'ps-3', compact ? 'py-2' : 'py-3', { 'opacity-45': isDragging, 'drop-before': dropIndicator === 'before', 'drop-after': dropIndicator === 'after' }]"
        :draggable="canManage"
        :data-card-id="card.id"
        tabindex="0"
        @click="emit('open', card)"
        @keydown.enter="emit('open', card)"
        @dragstart="onDragStart"
    >
        <span v-if="card.color" class="absolute inset-y-0 start-0 w-1 rounded-s-md" :style="{ backgroundColor: card.color }" aria-hidden="true" />

        <div class="flex flex-wrap items-center gap-1.5">
            <span class="font-mono text-xs text-muted">DS-{{ card.id }}</span>
            <UBadge v-if="isEpic" label="Epic" icon="i-lucide-layers" color="primary" variant="subtle" size="sm" />
            <UBadge
                v-else-if="epicLabel"
                :label="epicLabel"
                icon="i-lucide-layers"
                color="neutral"
                variant="outline"
                size="sm"
                class="max-w-40"
                :ui="{ label: 'truncate' }"
                :title="`Hører til epic: ${epicLabel}`"
            />
            <BoardTagBadge v-for="tag in card.tags" :key="tag.id" :tag="tag" />
        </div>

        <p class="mt-1 font-medium" :class="isDoneColumn ? 'text-muted line-through' : 'text-highlighted'">{{ card.title }}</p>
        <p v-if="descriptionPreview && !compact" class="mt-1 line-clamp-2 text-xs text-muted">{{ descriptionPreview }}</p>

        <div v-if="isEpic && epicProgress.total > 0" :class="compact ? 'mt-1.5' : 'mt-2'">
            <p class="flex items-center gap-1 text-xs" :class="isEpicComplete ? 'font-medium text-success' : 'text-muted'">
                <UIcon :name="isEpicComplete ? 'i-lucide-circle-check' : 'i-lucide-list-checks'" class="size-3.5" />
                {{ isEpicComplete ? `Alle ${epicProgress.total} færdige` : `${epicProgress.done} af ${epicProgress.total} færdige` }}
            </p>
            <UProgress v-if="!compact" class="mt-1" :model-value="epicProgress.done" :max="epicProgress.total" size="2xs" :color="isEpicComplete ? 'success' : 'primary'" />
        </div>
        <p v-else-if="isEpic && !compact" class="mt-2 text-xs text-muted">Ingen kort i epic'en endnu</p>

        <UBadge v-if="waitingLabel && !compact" class="mt-2" :label="waitingLabel" icon="i-lucide-link" color="warning" variant="subtle" size="sm" />

        <div v-if="card.dueDate || Number(card.priority) !== 0 || pullRequestCount || card.assignees.length" class="flex items-center gap-2.5 text-xs" :class="compact ? 'mt-1.5' : 'mt-2.5'">
            <span v-if="card.dueDate" class="flex items-center gap-1" :class="dueClass" :title="deadlineLabel">
                <UIcon name="i-lucide-calendar" class="size-3.5" />
                <span class="sr-only">{{ isOverdue ? 'Overskredet deadline:' : 'Deadline:' }}</span>
                {{ dueText }}
            </span>
            <span v-if="Number(card.priority) > 0" class="flex items-center gap-0.5 font-medium text-error" :title="priorityLabel">
                <UIcon name="i-lucide-arrow-up" class="size-3.5" />
                Høj
            </span>
            <span v-else-if="Number(card.priority) < 0" class="flex items-center gap-0.5 text-muted" :title="priorityLabel">
                <UIcon name="i-lucide-arrow-down" class="size-3.5" />
                Lav
            </span>
            <span v-if="pullRequestCount" class="flex items-center gap-1 text-muted" :title="pullRequestLabel">
                <UIcon name="i-lucide-git-pull-request" class="size-3.5" />
                <span class="sr-only">Pull requests:</span>
                {{ pullRequestCount }}
            </span>
            <span v-if="completedCount" class="flex items-center gap-1" :class="completedCount === card.assignees.length ? 'text-success' : 'text-muted'" :title="completionLabel">
                <UIcon name="i-lucide-circle-check" class="size-3.5" />
                <span class="sr-only">Klarmeldt:</span>
                {{ completedCount }}/{{ card.assignees.length }}
            </span>
            <UAvatarGroup v-if="card.assignees.length" size="2xs" :max="3" class="ms-auto">
                <UTooltip v-for="user in card.assignees" :key="user.id" :text="`${user.firstName} ${user.lastName}${user.completedAt ? ' (klarmeldt)' : ''}`">
                    <UAvatar
                        :src="user.profilePicture?.url"
                        :alt="`Ansvarlig: ${user.firstName} ${user.lastName}${user.completedAt ? ', klarmeldt' : ''}`"
                        :chip="user.completedAt ? { color: 'success', position: 'bottom-right' } : undefined"
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
import { useBoardCompact } from "@/lib/board/boardFilters";
import { useBoardStore } from "@/Stores/BoardStore";
import BoardTagBadge from "@/Components/Board/BoardTagBadge.vue";

const props = defineProps<{
    card: WorkItemDto;
    canManage: boolean;
    isDoneColumn: boolean;
    dropIndicator: "before" | "after" | null;
}>();

const emit = defineEmits<{ open: [card: WorkItemDto] }>();

const drag = useBoardDrag();
const store = useBoardStore();
const compact = useBoardCompact();
const dragImageCaptured = ref(false);
const isDragging = computed(() => dragImageCaptured.value && drag.value?.kind === "card" && drag.value.id === Number(props.card.id));

const isDeveloperBoard = computed(() => store.SelectedBoard?.isDeveloperBoard ?? false);
const isEpic = computed(() => isDeveloperBoard.value && props.card.isEpic);
const epicLabel = computed(() => {
    const epicCardId = props.card.epicCardId;
    if (!isDeveloperBoard.value || !epicCardId) return "";

    return store.SelectedBoard?.cards.find((card) => String(card.id) === String(epicCardId))?.title ?? `DS-${epicCardId}`;
});
const epicProgress = computed(() => props.card.epicProgress ?? { done: 0, total: 0 });
const isEpicComplete = computed(() => epicProgress.value.total > 0 && epicProgress.value.done === epicProgress.value.total);

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
const daysUntilDue = computed(() => props.card.dueDate ? Math.round((Date.parse(props.card.dueDate) - Date.parse(today.value)) / 86400000) : null);
const isDueSoon = computed(() => !props.isDoneColumn && daysUntilDue.value !== null && daysUntilDue.value >= 0 && daysUntilDue.value <= 1);
const dueText = computed(() => {
    const days = daysUntilDue.value;
    if (days === null || props.isDoneColumn) return formattedDueDate.value;
    if (days < 0) return days === -1 ? "1 dag over" : `${-days} dage over`;
    if (days === 0) return "I dag";
    if (days === 1) return "I morgen";

    return formattedDueDate.value;
});
const dueClass = computed(() => {
    if (isOverdue.value) return "font-medium text-error";
    if (isDueSoon.value) return "font-medium text-warning";

    return "text-muted";
});
const pullRequestCount = computed(() => props.card.pullRequests?.length ?? 0);
const pullRequestLabel = computed(() => pullRequestCount.value === 1 ? "1 pull request" : `${pullRequestCount.value} pull requests`);
const completedCount = computed(() => props.card.assignees.filter((user) => user.completedAt).length);
const completionLabel = computed(() => `${completedCount.value} af ${props.card.assignees.length} ansvarlige har klarmeldt`);

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
