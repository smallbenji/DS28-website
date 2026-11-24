<template>
    <UDashboardPanel id="board-card">
        <BoardCardDetail
            v-if="board && card"
            variant="page"
            :board-id="boardId"
            :column-id="Number(card.columnId)"
            :card="card"
            :can-manage="canManage"
            @removed="router.push(boardPath)"
            @open-card="(other) => router.push(`${boardPath}/card/${other.id}`)"
        />

        <template v-else>
            <UDashboardNavbar :title="loading ? 'Kort' : 'Kortet blev ikke fundet'">
                <template #leading>
                    <UDashboardSidebarCollapse />
                    <UButton :to="board ? boardPath : '/board'" color="neutral" variant="ghost" icon="i-lucide-arrow-left" :aria-label="board ? 'Tilbage til tavlen' : 'Tilbage til tavler'" />
                </template>
            </UDashboardNavbar>

            <div v-if="loading" class="flex flex-col gap-4 p-4 sm:p-8">
                <USkeleton class="h-8 w-2/3" />
                <USkeleton class="h-40 w-full max-w-4xl" />
            </div>
            <UEmpty
                v-else
                icon="i-lucide-square-kanban"
                :title="board ? 'Kortet findes ikke på denne tavle.' : 'Tavlen blev ikke fundet.'"
                :actions="[board ? { label: 'Gå til tavlen', to: boardPath } : { label: 'Gå til tavler', to: '/board' }]"
                class="flex-1"
            />
        </template>
    </UDashboardPanel>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref, watchEffect } from "vue";
import { useRoute, useRouter } from "vue-router";
import { useBoardStore } from "@/Stores/BoardStore";
import { useBoardLiveUpdates } from "@/lib/board/boardLiveUpdates";
import BoardCardDetail from "@/Components/Board/BoardCardDetail.vue";

const route = useRoute();
const router = useRouter();
const store = useBoardStore();

const boardId = Number(route.params.id);
const boardPath = `/board/${boardId}`;
const loading = ref(true);

const board = computed(() => store.SelectedBoard && Number(store.SelectedBoard.id) === boardId ? store.SelectedBoard : null);
const canManage = computed(() => board.value !== null);
const card = computed(() => board.value?.cards.find((item) => String(item.id) === String(route.params.cardId)) ?? null);

let liveReloadTimer: ReturnType<typeof setTimeout> | null = null;

function scheduleLiveReload() {
    if (liveReloadTimer) clearTimeout(liveReloadTimer);

    liveReloadTimer = setTimeout(() => {
        liveReloadTimer = null;
        void store.GET_BOARD(boardId);
    }, 250);
}

const liveUpdates = useBoardLiveUpdates(boardId, scheduleLiveReload);

watchEffect(() => {
    if (card.value) document.title = `DS-${card.value.id} ${card.value.title} · DS28 HQ`;
});

onMounted(async () => {
    loading.value = true;
    try {
        await store.GET_BOARD(boardId);
    } finally {
        loading.value = false;
    }

    void liveUpdates.connect();
});

onBeforeUnmount(() => {
    if (liveReloadTimer) clearTimeout(liveReloadTimer);
});
</script>
