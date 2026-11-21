<template>
    <UDashboardPanel id="board-detail" :ui="{ body: 'p-0 sm:p-0 overflow-hidden' }">
        <template #header>
            <UDashboardNavbar :title="board?.title ?? 'Tavle'">
                <template #leading>
                    <UDashboardSidebarCollapse />
                    <UButton to="/board" color="neutral" variant="ghost" icon="i-lucide-arrow-left" aria-label="Tilbage til tavler" />
                </template>
                <template #trailing>
                    <UBadge v-if="board?.isArchived" label="Arkiveret" color="warning" variant="subtle" />
                    <UBadge v-if="board?.isDeveloperBoard" label="Udviklertavle" color="neutral" variant="outline" />
                </template>
                <template v-if="canManage" #right>
                    <UButton icon="i-lucide-plus" label="Kolonne" color="neutral" variant="outline" class="hidden sm:inline-flex" @click="openColumnCreate" />
                    <UDropdownMenu :items="boardMenu" :content="{ align: 'end' }">
                        <UButton icon="i-lucide-settings-2" label="Tavle" color="neutral" variant="outline" trailing-icon="i-lucide-chevron-down" />
                    </UDropdownMenu>
                </template>
            </UDashboardNavbar>

            <UDashboardToolbar v-if="board?.description || board?.tags?.length">
                <p v-if="board?.description" class="min-w-0 flex-1 truncate text-sm text-muted" :title="board.description">{{ board.description }}</p>
                <div v-if="board?.tags?.length" class="ms-auto flex items-center gap-2">
                    <span v-if="selectedTagId !== null" class="text-xs text-muted" role="status">{{ filteredCards.length }} kort matcher</span>
                    <USelect v-model="tagFilter" :items="tagItems" icon="i-lucide-tag" class="w-48" aria-label="Filtrer på tag" />
                </div>
            </UDashboardToolbar>
        </template>

        <template #body>
            <div v-if="loading && !board" class="flex h-full gap-4 p-4">
                <USkeleton v-for="index in 3" :key="index" class="h-80 w-72" />
            </div>

            <UEmpty v-else-if="!board" icon="i-lucide-columns-3" title="Tavlen blev ikke fundet." class="h-full" />

            <div v-else class="flex h-full flex-col bg-muted">
                <UAlert
                    v-if="selectedTagId !== null"
                    class="m-4 mb-0"
                    color="info"
                    variant="subtle"
                    icon="i-lucide-filter"
                    description="Fjern tagfilteret for at flytte kort og kolonner."
                    :actions="[{ label: 'Fjern filter', color: 'info', variant: 'outline', onClick: () => { tagFilter = allTags; } }]"
                />
                <UEmpty v-if="board.columns.length === 0" icon="i-lucide-columns-3" title="Tavlen har ingen kolonner endnu." :actions="canManage ? [{ label: 'Tilføj kolonne', icon: 'i-lucide-plus', onClick: openColumnCreate }] : []" class="flex-1" />
                <div v-else class="flex min-h-0 flex-1 items-start gap-4 overflow-x-auto p-4" role="region" aria-label="Tavlens kolonner" @dragover.self="clearDropTarget">
                    <BoardColumn
                        v-for="column in board.columns"
                        :key="column.id"
                        :column="column"
                        :cards="cardsFor(column.id)"
                        :can-manage="canManage && selectedTagId === null"
                        @open-card="openCard"
                        @add-card="openNewCard"
                        @edit-column="openColumnEdit"
                        @delete-column="confirmDeleteColumn"
                        @move-card="moveCard"
                        @reorder-column="reorderColumn"
                    />
                </div>
            </div>

            <BoardCardModal v-model:open="cardOpen" :board-id="boardId" :column-id="targetColumnId" :card="editingCard" :can-manage="canManage" />
            <BoardColumnModal v-model:open="columnOpen" :board-id="boardId" :column="editingColumn" />
            <BoardMembersModal v-model:open="membersOpen" :board-id="boardId" />
            <BoardPaletteModal v-model:open="colorsOpen" :board-id="boardId" mode="colors" />
            <BoardPaletteModal v-model:open="tagsOpen" :board-id="boardId" mode="tags" />

            <ConfirmModal
                v-model:open="deleteColumnOpen"
                title="Slet kolonne"
                :description="`Slet kolonnen ${columnToDelete?.title}? Kolonnen skal være tom.`"
                confirm-label="Slet"
                color="error"
                :loading="deletingColumn"
                @confirm="deleteColumn"
            />

            <UModal v-model:open="settingsOpen" title="Tavleindstillinger">
                <template #body>
                    <form id="board-settings-form" class="flex flex-col gap-4" @submit.prevent="saveSettings">
                        <UFormField label="Titel" required>
                            <UInput v-model="settings.title" class="w-full" />
                        </UFormField>
                        <UFormField label="Beskrivelse">
                            <UTextarea v-model="settings.description" :rows="3" class="w-full" />
                        </UFormField>
                        <UCheckbox v-model="settings.isArchived" label="Arkiveret" />
                        <UCheckbox v-if="isSysAdmin" v-model="settings.isDeveloperBoard" label="Udviklertavle" description="Kortene får et felt til links til pull requests." />
                    </form>
                </template>
                <template #footer>
                    <div class="flex w-full justify-end gap-2">
                        <UButton color="neutral" variant="outline" label="Annuller" @click="settingsOpen = false" />
                        <UButton type="submit" form="board-settings-form" label="Gem" :loading="savingSettings" :disabled="!settings.title.trim()" />
                    </div>
                </template>
            </UModal>
        </template>
    </UDashboardPanel>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, provide, ref, watch } from "vue";
import { useRoute } from "vue-router";
import type { HubConnection } from "@microsoft/signalr";
import type { DropdownMenuItem } from "@nuxt/ui";
import { useBoardStore } from "@/Stores/BoardStore";
import { useMeStore } from "@/Stores/MeStore";
import { useNotify } from "@/lib/notify";
import { boardDragKey, type BoardDragState } from "@/lib/board/boardDrag";
import ConfirmModal from "@/Components/ConfirmModal.vue";
import BoardColumn from "@/Components/Board/BoardColumn.vue";
import BoardCardModal from "@/Components/Board/BoardCardModal.vue";
import BoardColumnModal from "@/Components/Board/BoardColumnModal.vue";
import BoardMembersModal from "@/Components/Board/BoardMembersModal.vue";
import BoardPaletteModal from "@/Components/Board/BoardPaletteModal.vue";
import type { BoardColumnDto, WorkItemDto } from "@/types";

const route = useRoute();
const store = useBoardStore();
const meStore = useMeStore();
const notify = useNotify();

const boardId = Number(route.params.id);
const isSysAdmin = computed(() => meStore.Me?.roles.includes("SysAdmin") ?? false);
const loading = ref(true);

const drag = ref<BoardDragState | null>(null);
provide(boardDragKey, drag);

function clearDrag() {
    drag.value = null;
}

function clearDropTarget() {
    if (drag.value) drag.value.target = null;
}

const board = computed(() => store.SelectedBoard && Number(store.SelectedBoard.id) === boardId ? store.SelectedBoard : null);
const canManage = computed(() => board.value !== null);

const cardOpen = ref(false);
const editingCard = ref<WorkItemDto | null>(null);
const targetColumnId = ref(0);
const columnOpen = ref(false);
const editingColumn = ref<BoardColumnDto | null>(null);
const membersOpen = ref(false);
const colorsOpen = ref(false);
const tagsOpen = ref(false);
const settingsOpen = ref(false);
const savingSettings = ref(false);
const settings = ref({ title: "", description: "", isArchived: false, isDeveloperBoard: false });

const boardMenu = computed<DropdownMenuItem[][]>(() => [
    [
        { label: "Ny kolonne", icon: "i-lucide-plus", onSelect: openColumnCreate },
        { label: "Medlemmer og invitation", icon: "i-lucide-users", onSelect: () => { membersOpen.value = true; } },
        { label: "Farver", icon: "i-lucide-palette", onSelect: () => { colorsOpen.value = true; } },
        { label: "Tags", icon: "i-lucide-tags", onSelect: () => { tagsOpen.value = true; } }
    ],
    [
        { label: "Indstillinger", icon: "i-lucide-settings", onSelect: openSettings }
    ]
]);

const allTags = "all";
const tagFilter = ref(allTags);
const selectedTagId = computed(() => tagFilter.value === allTags ? null : Number(tagFilter.value));
const tagItems = computed(() => [
    { label: "Alle kort", value: allTags },
    ...(board.value?.tags ?? []).map((tag) => ({ label: tag.name, value: String(tag.id) }))
]);

watch(() => board.value?.tags, (tags) => {
    if (selectedTagId.value !== null && !tags?.some((tag) => Number(tag.id) === selectedTagId.value)) tagFilter.value = allTags;
});

const filteredCards = computed(() => (board.value?.cards ?? [])
    .filter((card) => selectedTagId.value === null || card.tags?.some((tag) => Number(tag.id) === selectedTagId.value)));

function cardsFor(columnId: number): WorkItemDto[] {
    return filteredCards.value.filter((card) => String(card.columnId) === String(columnId));
}

function openCard(card: WorkItemDto) {
    editingCard.value = card;
    targetColumnId.value = Number(card.columnId);
    cardOpen.value = true;
}

function openNewCard(columnId: number) {
    editingCard.value = null;
    targetColumnId.value = Number(columnId);
    cardOpen.value = true;
}

function openColumnCreate() {
    editingColumn.value = null;
    columnOpen.value = true;
}

function openColumnEdit(column: BoardColumnDto) {
    editingColumn.value = column;
    columnOpen.value = true;
}

const deleteColumnOpen = ref(false);
const columnToDelete = ref<BoardColumnDto | null>(null);
const deletingColumn = ref(false);

function confirmDeleteColumn(column: BoardColumnDto) {
    columnToDelete.value = column;
    deleteColumnOpen.value = true;
}

async function deleteColumn() {
    if (!columnToDelete.value) return;

    deletingColumn.value = true;
    try {
        const ok = await store.DELETE_COLUMN(boardId, Number(columnToDelete.value.id));
        notify.result(ok, "Kolonnen er slettet.", "Kolonnen kunne ikke slettes. Den må ikke indeholde kort.");
        if (ok) deleteColumnOpen.value = false;
    } finally {
        deletingColumn.value = false;
    }
}

function openSettings() {
    settings.value = {
        title: board.value?.title ?? "",
        description: board.value?.description ?? "",
        isArchived: board.value?.isArchived ?? false,
        isDeveloperBoard: board.value?.isDeveloperBoard ?? false
    };
    settingsOpen.value = true;
}

async function saveSettings() {
    if (savingSettings.value || !settings.value.title.trim()) return;

    savingSettings.value = true;
    try {
        const ok = await store.UPDATE_BOARD(boardId, {
            title: settings.value.title.trim(),
            description: settings.value.description.trim() ? settings.value.description.trim() : null,
            icon: board.value?.icon ?? null,
            color: board.value?.color ?? null,
            isArchived: settings.value.isArchived,
            isDeveloperBoard: isSysAdmin.value ? settings.value.isDeveloperBoard : undefined
        });
        notify.result(ok, "Tavlen er gemt.", "Tavlen kunne ikke gemmes.");
        if (ok) settingsOpen.value = false;
    } finally {
        savingSettings.value = false;
    }
}

async function moveCard(payload: { cardId: number; columnId: number; position: number }) {
    const ok = await store.MOVE_CARD(boardId, payload.cardId, Number(payload.columnId), payload.position);
    if (!ok) notify.error("Kortet kunne ikke flyttes.");
}

async function reorderColumn(payload: { targetColumnId: number; draggedColumnId: number; after: boolean }) {
    const order = (board.value?.columns ?? []).map((column) => Number(column.id));
    const from = order.indexOf(Number(payload.draggedColumnId));
    const to = order.indexOf(Number(payload.targetColumnId));
    if (from < 0 || to < 0 || from === to) return;

    const next = order.filter((columnId) => columnId !== order[from]);
    const targetIndex = next.indexOf(Number(payload.targetColumnId));
    next.splice(targetIndex + Number(payload.after), 0, order[from]!);
    if (next.every((columnId, index) => columnId === order[index])) return;

    const ok = await store.REORDER_COLUMNS(boardId, next);
    if (!ok) notify.error("Kolonnerne kunne ikke ombyttes.");
}

let liveConnection: HubConnection | null = null;
let liveReloadTimer: ReturnType<typeof setTimeout> | null = null;

function scheduleLiveReload() {
    if (liveReloadTimer) clearTimeout(liveReloadTimer);

    liveReloadTimer = setTimeout(() => {
        liveReloadTimer = null;
        if (drag.value) {
            scheduleLiveReload();
            return;
        }

        void store.GET_BOARD(boardId);
    }, 250);
}

async function connectLiveUpdates() {
    const { HubConnectionBuilder } = await import("@microsoft/signalr");

    const connection = new HubConnectionBuilder()
        .withUrl("/api/v1/boards/hub")
        .withAutomaticReconnect()
        .build();

    connection.on("boardChanged", (changedBoardId: number) => {
        if (Number(changedBoardId) === boardId) scheduleLiveReload();
    });
    connection.onreconnected(() => {
        void connection.invoke("JoinBoard", boardId);
    });

    liveConnection = connection;

    try {
        await connection.start();
        await connection.invoke("JoinBoard", boardId);
    } catch {
        liveConnection = null;
    }
}

onMounted(async () => {
    window.addEventListener("dragend", clearDrag);
    window.addEventListener("drop", clearDrag);
    window.addEventListener("blur", clearDrag);

    loading.value = true;
    try {
        await store.GET_BOARD(boardId);
    } finally {
        loading.value = false;
    }

    void connectLiveUpdates();
});

onBeforeUnmount(() => {
    window.removeEventListener("dragend", clearDrag);
    window.removeEventListener("drop", clearDrag);
    window.removeEventListener("blur", clearDrag);
    if (liveReloadTimer) clearTimeout(liveReloadTimer);
    void liveConnection?.stop();
});
</script>
