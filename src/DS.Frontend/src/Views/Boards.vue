<template>
    <UDashboardPanel id="boards">
        <template #header>
            <UDashboardNavbar title="Tavler">
                <template #leading>
                    <UDashboardSidebarCollapse />
                </template>
                <template #right>
                    <UButton icon="i-lucide-plus" label="Ny tavle" @click="openCreate" />
                </template>
            </UDashboardNavbar>
        </template>

        <template #body>
            <div v-if="loading && boards.length === 0" class="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
                <USkeleton v-for="index in 6" :key="index" class="h-28" />
            </div>

            <UEmpty v-else-if="boards.length === 0" icon="i-lucide-columns-3" title="Ingen tavler endnu." :actions="[{ label: 'Ny tavle', icon: 'i-lucide-plus', onClick: openCreate }]" />

            <div v-else class="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-3">
                <div v-for="board in boards" :key="board.id" class="relative">
                    <UPageCard
                        :to="`/board/${board.id}`"
                        :title="board.title"
                        :description="board.description ?? undefined"
                        icon="i-lucide-columns-3"
                        variant="outline"
                        spotlight
                        class="h-full"
                        :class="{ 'opacity-60': board.isArchived }"
                    >
                        <template #footer>
                            <div class="flex items-center gap-2 text-xs text-muted">
                                <span>{{ board.cardCount }} kort</span>
                                <UBadge v-if="board.isArchived" label="Arkiveret" color="warning" variant="subtle" size="sm" />
                            </div>
                        </template>
                    </UPageCard>
                    <span v-if="board.color" class="pointer-events-none absolute inset-x-0 top-0 h-[3px] rounded-t-lg" :style="{ backgroundColor: board.color }" aria-hidden="true" />
                </div>
            </div>

            <UModal v-model:open="createOpen" title="Ny tavle" description="Kun inviterede medlemmer samt systemadministratorer og lejrchefer har adgang. Du bliver selv medlem.">
                <template #body>
                    <form id="board-create-form" class="flex flex-col gap-4" @submit.prevent="createBoard">
                        <UFormField label="Titel" required>
                            <UInput v-model="form.title" placeholder="Fx Aktiviteter 2028" class="w-full" autofocus />
                        </UFormField>
                        <UFormField label="Beskrivelse">
                            <UTextarea v-model="form.description" :rows="3" class="w-full" />
                        </UFormField>
                    </form>
                </template>
                <template #footer>
                    <div class="flex w-full justify-end gap-2">
                        <UButton color="neutral" variant="outline" label="Annuller" @click="createOpen = false" />
                        <UButton type="submit" form="board-create-form" label="Opret" :loading="saving" :disabled="!form.title.trim()" />
                    </div>
                </template>
            </UModal>
        </template>
    </UDashboardPanel>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRouter } from "vue-router";
import { useBoardStore } from "@/Stores/BoardStore";
import { useNotify } from "@/lib/notify";

const router = useRouter();
const store = useBoardStore();
const notify = useNotify();

const loading = ref(false);
const saving = ref(false);
const createOpen = ref(false);
const form = ref({ title: "", description: "" });

const boards = computed(() => store.Boards);

function openCreate() {
    form.value = { title: "", description: "" };
    createOpen.value = true;
}

async function createBoard() {
    if (saving.value || !form.value.title.trim()) return;

    saving.value = true;
    try {
        const id = await store.CREATE_BOARD({
            title: form.value.title.trim(),
            description: form.value.description.trim() ? form.value.description.trim() : null,
            icon: null,
            color: null,
            columns: []
        });

        if (id !== null) {
            notify.success("Tavlen er oprettet.");
            createOpen.value = false;
            await router.push(`/board/${id}`);
        } else {
            notify.error("Tavlen kunne ikke oprettes. Prøv en anden titel.");
        }
    } finally {
        saving.value = false;
    }
}

onMounted(async () => {
    loading.value = true;
    try {
        await store.GET_BOARDS();
    } finally {
        loading.value = false;
    }
});
</script>
