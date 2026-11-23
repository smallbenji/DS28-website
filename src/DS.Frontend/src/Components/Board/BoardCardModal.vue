<template>
    <UModal v-model:open="open" :dismissible="!saving" :close="false" :ui="{ content: 'sm:max-w-5xl', body: 'p-0 sm:p-0' }">
        <template #header>
            <div class="flex w-full flex-wrap items-center gap-2">
                <div class="flex min-w-0 flex-1 items-center gap-1.5 text-sm text-muted">
                    <span class="truncate">{{ store.SelectedBoard?.title ?? 'Tavle' }}</span>
                    <span aria-hidden="true">/</span>
                    <span class="font-mono text-highlighted">{{ isNew ? 'Nyt kort' : `DS-${card?.id}` }}</span>
                </div>

                <template v-if="currentPlacement">
                    <UDropdownMenu v-if="canManage" :items="statusItems(currentPlacement)" :content="{ align: 'end' }" @update:open="(value: boolean) => loadStatusColumns(currentPlacement!, value)">
                        <UButton
                            :color="currentPlacement.isDoneColumn ? 'success' : 'neutral'"
                            variant="subtle"
                            size="sm"
                            :icon="currentPlacement.isDoneColumn ? 'i-lucide-check' : 'i-lucide-circle-dot'"
                            :label="currentPlacement.columnTitle"
                            trailing-icon="i-lucide-chevron-down"
                            :disabled="saving"
                            :aria-label="`Status på ${currentPlacement.boardTitle}: ${currentPlacement.columnTitle}. Skift status`"
                        />
                    </UDropdownMenu>
                    <UBadge v-else :color="currentPlacement.isDoneColumn ? 'success' : 'neutral'" variant="subtle" :label="currentPlacement.columnTitle" />
                </template>

                <UDropdownMenu v-if="canManage && !isNew" :items="cardMenu" :content="{ align: 'end' }">
                    <UButton color="neutral" variant="ghost" size="sm" icon="i-lucide-ellipsis" aria-label="Korthandlinger" :disabled="saving" />
                </UDropdownMenu>
                <UButton color="neutral" variant="ghost" size="sm" icon="i-lucide-x" aria-label="Luk" :disabled="saving" @click="open = false" />
            </div>
        </template>

        <template #body>
            <div class="grid lg:grid-cols-[minmax(0,1fr)_18rem]">
                <div class="flex min-w-0 flex-col gap-6 p-4 sm:p-6">
                    <BoardTagInput v-if="canManage" ref="tagInput" v-model="selectedTags" :tags="availableTags" :disabled="saving" />
                    <div v-else-if="card?.tags.length" class="flex flex-wrap gap-1">
                        <BoardTagBadge v-for="tag in card.tags" :key="tag.id" :tag="tag" />
                    </div>

                    <UInput
                        v-if="canManage"
                        v-model="form.title"
                        size="xl"
                        variant="ghost"
                        placeholder="Hvad skal gøres?"
                        aria-label="Kortets titel"
                        class="w-full"
                        :ui="{ base: 'px-0 text-xl font-semibold' }"
                        @keydown.enter.prevent="save"
                    />
                    <h2 v-else class="text-xl font-semibold text-highlighted">{{ card?.title }}</h2>

                    <section aria-labelledby="card-description-heading">
                        <h3 id="card-description-heading" class="mb-2 text-sm font-semibold text-highlighted">Beskrivelse</h3>
                        <RichTextEditor v-if="canManage" v-model="form.description" placeholder="Tilføj en beskrivelse…" />
                        <RichTextViewer v-else-if="card?.description" :html="card.description" />
                        <p v-else class="text-xs text-muted">Ingen beskrivelse.</p>
                    </section>

                    <BoardCardEpic v-if="pullRequestCard" :key="`epic-${pullRequestCard.id}`" :board-id="boardId" :card="pullRequestCard" :can-manage="canManage" @open="openOtherCard" />

                    <BoardCardDependencies v-if="!isNew && card" :board-id="boardId" :card="card" :can-manage="canManage" />

                    <BoardCardPullRequests v-if="pullRequestCard" :key="pullRequestCard.id" :board-id="boardId" :card="pullRequestCard" :can-manage="canManage" />

                    <section v-if="!isNew && card && card.links.length" aria-labelledby="card-links-heading">
                        <h3 id="card-links-heading" class="mb-2 text-sm font-semibold text-highlighted">Koblinger</h3>
                        <ul class="flex flex-col gap-1 text-sm">
                            <li v-for="link in card.links" :key="`${link.entityType}-${link.entityId}`" class="flex items-center gap-2">
                                <UIcon name="i-lucide-link" class="size-4 text-muted" />
                                {{ link.entityType }} #{{ link.entityId }}
                            </li>
                        </ul>
                    </section>
                </div>

                <aside class="flex flex-col gap-5 border-t border-default bg-elevated/40 p-4 sm:p-6 lg:border-s lg:border-t-0" aria-label="Detaljer">
                    <BoardCardAssignees :key="liveCard?.id ?? 'new'" v-model:pending="form.assignees" :board-id="boardId" :card="liveCard" :can-manage="canManage" />

                    <UFormField label="Stilling">
                        <USelectMenu v-if="canManage" v-model="positionValue" :items="positionItems" value-key="value" size="sm" class="w-full" :disabled="saving" />
                        <p v-else class="text-sm" :class="{ 'text-muted': !positionTitle }">{{ positionTitle ?? 'Ingen stilling' }}</p>
                    </UFormField>

                    <UFormField label="Deadline" :help="dueDateHint || undefined">
                        <UInput v-if="canManage" v-model="form.dueDate" type="date" size="sm" class="w-full" :disabled="saving" />
                        <p v-else class="text-sm" :class="{ 'text-muted': !dueDateLabel }">{{ dueDateLabel || 'Ingen' }}</p>
                    </UFormField>

                    <UFormField label="Prioritet">
                        <USelect v-if="canManage" v-model="form.priority" :items="priorityItems" size="sm" class="w-full" :disabled="saving" />
                        <p v-else class="text-sm">{{ boardPriorityLabel(form.priority) }}</p>
                    </UFormField>

                    <UFormField label="Farve" :help="selectedColorLabel">
                        <div class="flex flex-wrap gap-1.5" role="group" aria-label="Kortets farve">
                            <button
                                v-for="color in cardColorOptions"
                                :key="color.key"
                                type="button"
                                class="flex size-7 items-center justify-center rounded-md border text-xs focus-visible:outline-2 focus-visible:outline-primary disabled:cursor-not-allowed"
                                :class="form.color === color.value ? 'border-inverted ring-2 ring-primary' : 'border-default'"
                                :style="{ backgroundColor: color.value ?? 'var(--ui-bg)', color: color.value ? readableTextColor(color.value) : undefined }"
                                :aria-label="color.label"
                                :title="color.label"
                                :aria-pressed="form.color === color.value"
                                :disabled="!canManage || saving"
                                @click="form.color = color.value"
                            >
                                <UIcon v-if="form.color === color.value" name="i-lucide-check" class="size-3.5" />
                                <UIcon v-else-if="color.value === null" name="i-lucide-ban" class="size-3.5 text-muted" />
                            </button>
                        </div>
                    </UFormField>

                    <section v-if="!isNew && (otherPlacements.length > 0 || (canManage && availableBoards.length > 0))" aria-labelledby="card-placements-heading">
                        <h3 id="card-placements-heading" class="mb-2 text-sm font-medium text-highlighted">Også på andre tavler</h3>
                        <ul class="flex flex-col gap-2">
                            <li v-for="placement in otherPlacements" :key="placement.boardId" class="flex items-center gap-2 text-sm">
                                <span class="min-w-0 flex-1 truncate">{{ placement.boardTitle }}</span>
                                <UDropdownMenu v-if="canManage" :items="statusItems(placement)" :content="{ align: 'end' }" @update:open="(value: boolean) => loadStatusColumns(placement, value)">
                                    <UButton :color="placement.isDoneColumn ? 'success' : 'neutral'" variant="subtle" size="xs" :label="placement.columnTitle" trailing-icon="i-lucide-chevron-down" :disabled="saving" :aria-label="`Status på ${placement.boardTitle}: ${placement.columnTitle}. Skift status`" />
                                </UDropdownMenu>
                                <UBadge v-else :color="placement.isDoneColumn ? 'success' : 'neutral'" variant="subtle" size="sm" :label="placement.columnTitle" />
                                <UButton v-if="canManage" color="neutral" variant="ghost" size="xs" icon="i-lucide-x" :aria-label="`Fjern fra ${placement.boardTitle}`" :disabled="saving" @click="confirmRemovePlacement(placement)" />
                            </li>
                        </ul>

                        <template v-if="canManage && availableBoards.length > 0">
                            <UButton v-if="!placementPickerOpen" class="mt-2" color="neutral" variant="ghost" size="xs" icon="i-lucide-plus" label="Tilføj til tavle" @click="placementPickerOpen = true" />
                            <div v-else class="mt-2 flex flex-col gap-2">
                                <USelectMenu v-model="placementBoardId" :items="placementBoardItems" value-key="value" placeholder="Vælg tavle…" size="sm" class="w-full" />
                                <USelect v-if="placementColumns.length" v-model="placementColumnId" :items="placementColumnItems" size="sm" class="w-full" />
                                <div class="flex gap-2">
                                    <UButton color="neutral" variant="outline" size="xs" label="Annuller" @click="closePlacementPicker" />
                                    <UButton size="xs" label="Tilføj til tavle" :disabled="!placementBoardId || !placementColumnId" :loading="saving" @click="addPlacement" />
                                </div>
                            </div>
                            <p class="mt-2 text-xs text-muted">Status og tavler gemmes med det samme.</p>
                        </template>
                    </section>
                </aside>
            </div>
        </template>

        <template v-if="canManage" #footer>
            <div class="flex w-full items-center justify-between gap-2">
                <span v-if="isDirty" class="flex items-center gap-1.5 text-xs text-warning">
                    <span class="size-2 rounded-full bg-warning" aria-hidden="true" />
                    Ugemte ændringer
                </span>
                <span v-else />
                <div class="flex gap-2">
                    <UButton color="neutral" variant="outline" label="Annuller" :disabled="saving" @click="open = false" />
                    <UButton :label="isNew ? 'Opret kort' : 'Gem ændringer'" :loading="saving" @click="save" />
                </div>
            </div>
        </template>
    </UModal>

    <ConfirmModal
        v-model:open="deleteOpen"
        title="Slet kort"
        :description="`Slet ${card?.title}?`"
        confirm-label="Slet"
        color="error"
        :loading="saving"
        @confirm="doRemove"
    />

    <ConfirmModal
        v-model:open="removePlacementOpen"
        title="Fjern fra tavle"
        :description="removePlacementDescription"
        confirm-label="Fjern"
        color="error"
        :loading="saving"
        @confirm="doRemovePlacement"
    />
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from "vue";
import type { DropdownMenuItem } from "@nuxt/ui";
import BoardService from "@/Services/BoardService";
import OrgService from "@/Services/OrgService";
import { useBoardStore } from "@/Stores/BoardStore";
import { htmlToPlainText } from "@/lib/util";
import { useNotify } from "@/lib/notify";
import { readableTextColor } from "@/lib/board/colorContrast";
import ConfirmModal from "@/Components/ConfirmModal.vue";
import RichTextEditor from "@/Components/RichText/RichTextEditor.vue";
import RichTextViewer from "@/Components/RichText/RichTextViewer.vue";
import BoardTagInput from "@/Components/Board/BoardTagInput.vue";
import BoardTagBadge from "@/Components/Board/BoardTagBadge.vue";
import BoardCardDependencies from "@/Components/Board/BoardCardDependencies.vue";
import BoardCardEpic from "@/Components/Board/BoardCardEpic.vue";
import BoardCardPullRequests from "@/Components/Board/BoardCardPullRequests.vue";
import BoardCardAssignees from "@/Components/Board/BoardCardAssignees.vue";
import {
    boardPriorityLabel,
    boardPriorityOptions,
    type BoardColumnDto,
    type BoardTagDto,
    type OrgPositionDto,
    type UserSummaryDto,
    type WorkItemDto,
    type WorkItemPlacementDto
} from "@/types";

const props = defineProps<{ boardId: number; columnId: number; card: WorkItemDto | null; canManage: boolean }>();
const open = defineModel<boolean>("open", { required: true });
const emit = defineEmits<{ openCard: [card: WorkItemDto] }>();

const store = useBoardStore();
const boardService = new BoardService();
const orgService = new OrgService();
const notify = useNotify();

const saving = ref(false);
const selectedTags = ref<BoardTagDto[]>([]);
const tagInput = ref<InstanceType<typeof BoardTagInput>>();
const orgPositions = ref<OrgPositionDto[]>([]);
const placementPickerOpen = ref(false);
const placementBoardId = ref<string | undefined>();
const placementColumnId = ref<string | undefined>();
const placementColumns = ref<BoardColumnDto[]>([]);
const statusColumns = ref<Record<number, BoardColumnDto[]>>({});
const statusLoading = ref<Record<number, boolean>>({});
const savedSnapshot = ref("");
const deleteOpen = ref(false);
const removePlacementOpen = ref(false);
const placementToRemove = ref<WorkItemPlacementDto | null>(null);

const form = reactive({
    title: "",
    description: "",
    color: null as string | null,
    dueDate: "",
    priority: "0",
    assignees: [] as UserSummaryDto[],
    ownerPositionId: null as number | null
});

const isNew = computed(() => props.card === null);
const availableTags = computed(() => String(store.SelectedBoard?.id) === String(props.boardId) ? store.SelectedBoard?.tags ?? [] : []);

const liveCard = computed(() => {
    if (!props.card) return null;

    const board = store.SelectedBoard;
    const live = board && Number(board.id) === props.boardId ? board.cards.find((card) => card.id === props.card?.id) : undefined;
    return live ?? props.card;
});

const pullRequestCard = computed(() => {
    const board = store.SelectedBoard;
    if (!board?.isDeveloperBoard || Number(board.id) !== props.boardId) return undefined;

    return board.cards.find((card) => card.id === props.card?.id);
});

const priorityItems = boardPriorityOptions.map((option) => ({ label: option.label, value: option.value }));

const noPosition = "none";
const positionItems = computed(() => [
    { label: "Ingen stilling", value: noPosition },
    ...orgPositions.value.map((position) => ({ label: position.title, value: String(position.id) }))
]);
const positionValue = computed({
    get: () => form.ownerPositionId === null ? noPosition : String(form.ownerPositionId),
    set: (value: string) => {
        form.ownerPositionId = value === noPosition ? null : Number(value);
    }
});
const positionTitle = computed(() => {
    if (form.ownerPositionId === null) return null;

    return orgPositions.value.find((position) => position.id === form.ownerPositionId)?.title
        ?? (form.ownerPositionId === props.card?.ownerPositionId ? props.card?.ownerPositionTitle : null)
        ?? null;
});

const cardColorOptions = computed(() => {
    const options: { key: string; value: string | null; label: string }[] = [
        { key: "none", value: null, label: "Ingen farve" },
        ...(store.SelectedBoard?.colors ?? []).map((color) => ({ key: String(color.id), value: color.color, label: color.name }))
    ];
    if (form.color && !options.some((color) => color.value === form.color)) {
        options.push({ key: "current", value: form.color, label: "Nuværende farve (uden for tavlens palette)" });
    }

    return options;
});
const selectedColorLabel = computed(() => cardColorOptions.value.find((color) => color.value === form.color)?.label ?? "Ingen farve");

const dueDate = computed(() => form.dueDate ? new Date(`${form.dueDate}T12:00:00`) : null);
const dueDateLabel = computed(() => dueDate.value ? new Intl.DateTimeFormat("da-DK", { day: "numeric", month: "short", year: "numeric" }).format(dueDate.value) : "");
const dueDateHint = computed(() => {
    if (!dueDate.value) return "";

    const today = new Date(`${new Intl.DateTimeFormat("sv-SE", { timeZone: "Europe/Copenhagen" }).format(new Date())}T12:00:00`);
    const days = Math.round((dueDate.value.getTime() - today.getTime()) / 86400000);
    if (days === 0) return "I dag";
    if (days > 0) return `Om ${days} ${days === 1 ? "dag" : "dage"}`;

    return `${-days} ${days === -1 ? "dag" : "dage"} siden`;
});

const placements = computed<WorkItemPlacementDto[]>(() => {
    const cardId = props.card?.id;
    if (cardId == null) return [];

    const live = store.SelectedBoard?.cards.find((card) => String(card.id) === String(cardId));

    return live?.placements ?? props.card?.placements ?? [];
});
const currentPlacement = computed(() => placements.value.find((placement) => isCurrentBoard(placement)) ?? null);
const otherPlacements = computed(() => placements.value.filter((placement) => !isCurrentBoard(placement)));
const availableBoards = computed(() => store.Boards.filter((board) => !board.isArchived
    && !placements.value.some((placement) => String(placement.boardId) === String(board.id))));
const placementBoardItems = computed(() => availableBoards.value.map((board) => ({ label: board.title, value: String(board.id) })));
const placementColumnItems = computed(() => placementColumns.value.map((column) => ({ label: column.title, value: String(column.id) })));

const cardMenu = computed<DropdownMenuItem[][]>(() => [
    ...(currentPlacement.value && placements.value.length > 1
        ? [[{ label: "Fjern fra denne tavle", icon: "i-lucide-log-out", onSelect: () => confirmRemovePlacement(currentPlacement.value!) }]]
        : []),
    [{ label: "Slet kort", icon: "i-lucide-trash-2", color: "error" as const, onSelect: () => { deleteOpen.value = true; } }]
]);

const removePlacementDescription = computed(() => {
    const placement = placementToRemove.value;
    if (!placement || !props.card) return "";

    return isCurrentBoard(placement)
        ? `Fjern ${props.card.title} fra denne tavle? Kortet bevares på de øvrige tavler.`
        : `Fjern ${props.card.title} fra tavlen ${placement.boardTitle}?`;
});

const isDirty = computed(() => savedSnapshot.value !== "" && snapshot() !== savedSnapshot.value);

watch(() => [open.value, props.card, props.boardId] as const, () => {
    if (!open.value) return;

    selectedTags.value = (props.card?.tags ?? []).filter((tag) => availableTags.value.some((option) => Number(option.id) === Number(tag.id)));
    form.title = props.card?.title ?? "";
    form.description = props.card?.description ?? "";
    form.color = props.card?.color ?? null;
    form.dueDate = props.card?.dueDate ?? "";
    form.priority = String(props.card?.priority ?? 0);
    form.assignees = [...(props.card?.assignees ?? [])];
    form.ownerPositionId = props.card?.ownerPositionId ?? null;
    savedSnapshot.value = snapshot();

    closePlacementPicker();
    statusColumns.value = {};

    void loadOrgPositions();
    void loadBoards();
}, { immediate: true });

watch(placementBoardId, () => {
    void loadPlacementColumns();
});


function snapshot() {
    return JSON.stringify({
        title: form.title,
        description: htmlToPlainText(form.description) ? form.description : "",
        color: form.color,
        dueDate: form.dueDate,
        priority: form.priority,
        assignees: form.assignees.map((user) => user.id),
        ownerPositionId: form.ownerPositionId,
        tags: selectedTags.value.map((tag) => `${tag.id}:${tag.name}`)
    });
}

function openOtherCard(card: WorkItemDto) {
    if (saving.value) return;
    if (isDirty.value) {
        notify.error("Gem eller annuller dine ændringer, før du åbner et andet kort.");
        return;
    }

    emit("openCard", card);
}

function isCurrentBoard(placement: WorkItemPlacementDto) {
    return String(placement.boardId) === String(props.boardId);
}

async function loadOrgPositions() {
    if (!props.canManage || orgPositions.value.length > 0) return;

    orgPositions.value = await orgService.getPositions();
}

async function loadBoards() {
    if (!props.canManage || store.Boards.length > 0) return;

    await store.GET_BOARDS();
}

async function loadPlacementColumns() {
    placementColumnId.value = undefined;
    placementColumns.value = [];
    if (!placementBoardId.value) return;

    const columns = await boardService.getColumns(Number(placementBoardId.value));
    placementColumns.value = columns;
    if (columns[0]) placementColumnId.value = String(columns[0].id);
}

function closePlacementPicker() {
    placementPickerOpen.value = false;
    placementBoardId.value = undefined;
    placementColumnId.value = undefined;
    placementColumns.value = [];
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

function statusItems(placement: WorkItemPlacementDto): DropdownMenuItem[] {
    if (statusLoading.value[placement.boardId]) return [{ label: "Henter statusser…", disabled: true }];

    const columns = statusColumns.value[placement.boardId];
    if (!columns?.length) return [{ label: "Kunne ikke hente statusser. Prøv igen", onSelect: (event: Event) => { event.preventDefault(); void loadStatusColumns(placement, true); } }];

    return columns.map((column) => {
        const isCurrent = String(column.id) === String(placement.columnId);

        return {
            label: column.title,
            icon: isCurrent ? "i-lucide-check" : undefined,
            suffix: column.isDoneColumn ? "Færdig" : undefined,
            disabled: saving.value || isCurrent,
            onSelect: () => { void changePlacementStatus(placement, column); }
        };
    });
}

async function changePlacementStatus(placement: WorkItemPlacementDto, column: BoardColumnDto) {
    if (!props.card || saving.value || String(placement.columnId) === String(column.id)) return;

    saving.value = true;
    try {
        const ok = await boardService.moveCard(Number(placement.boardId), Number(props.card.id), { columnId: Number(column.id), position: 2147483647 });
        if (!ok) {
            notify.error("Status kunne ikke ændres. Prøv igen.");
            return;
        }

        await store.GET_BOARD(props.boardId);
        notify.success(`Status på ${placement.boardTitle} er ændret til ${column.title}.`);
    } finally {
        saving.value = false;
    }
}

async function addPlacement() {
    if (!props.card || !placementBoardId.value || !placementColumnId.value) return;

    saving.value = true;
    try {
        const ok = await store.ADD_PLACEMENT(props.boardId, Number(placementBoardId.value), Number(props.card.id), Number(placementColumnId.value));
        notify.result(ok, "Kortet er tilføjet til tavlen.", "Kortet kunne ikke tilføjes til tavlen.");
        if (ok) closePlacementPicker();
    } finally {
        saving.value = false;
    }
}

function confirmRemovePlacement(placement: WorkItemPlacementDto) {
    placementToRemove.value = placement;
    removePlacementOpen.value = true;
}

async function doRemovePlacement() {
    const placement = placementToRemove.value;
    if (!props.card || !placement) return;

    saving.value = true;
    try {
        const ok = await store.REMOVE_PLACEMENT(props.boardId, Number(placement.boardId), Number(props.card.id));
        if (!ok) {
            notify.error("Kortet kunne ikke fjernes fra tavlen.");
            return;
        }

        removePlacementOpen.value = false;
        if (isCurrentBoard(placement)) {
            notify.success("Kortet er fjernet fra tavlen.");
            open.value = false;
        } else {
            notify.success(`Kortet er fjernet fra ${placement.boardTitle}.`);
        }
    } finally {
        saving.value = false;
    }
}

async function save() {
    if (saving.value) return;
    if (tagInput.value && !tagInput.value.commitPending()) return;

    if (!form.title.trim()) {
        notify.error("Titlen må ikke være tom.");
        return;
    }

    saving.value = true;
    try {
        const base = {
            tagIds: selectedTags.value.filter((tag) => tag.id > 0).map((tag) => Number(tag.id)),
            newTagNames: selectedTags.value.filter((tag) => tag.id < 0).map((tag) => tag.name),
            title: form.title.trim(),
            description: htmlToPlainText(form.description) ? form.description : null,
            color: form.color,
            ownerPositionId: form.ownerPositionId,
            dueDate: form.dueDate ? form.dueDate : null,
            priority: Number(form.priority)
        };

        const ok = isNew.value
            ? (await store.ADD_CARD(props.boardId, { ...base, assigneeUserIds: form.assignees.map((user) => user.id), columnId: props.columnId, links: [] })) !== null
            : await store.UPDATE_CARD(props.boardId, Number(props.card!.id), base);

        notify.result(ok, "Kortet er gemt.", "Kortet kunne ikke gemmes.");
        if (ok) open.value = false;
    } finally {
        saving.value = false;
    }
}

async function doRemove() {
    if (!props.card) return;

    saving.value = true;
    try {
        const ok = await store.REMOVE_CARD(props.boardId, Number(props.card.id));
        notify.result(ok, "Kortet er slettet.", "Kortet kunne ikke slettes.");
        if (ok) {
            deleteOpen.value = false;
            open.value = false;
        }
    } finally {
        saving.value = false;
    }
}
</script>
