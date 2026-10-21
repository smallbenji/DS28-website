<template>
    <section class="section">
        <div class="container">
            <div class="board-list-head">
                <h1 class="title is-4">Tavler</h1>
                <BButton type="is-primary" @click="openCreate">
                    <font-awesome-icon icon="plus" />
                    <span>Ny tavle</span>
                </BButton>
            </div>

            <p v-if="loading && boards.length === 0" role="status">Indlæser tavler…</p>

            <p v-else-if="boards.length === 0" class="has-text-grey">Ingen tavler endnu.</p>

            <div v-else class="board-grid">
                <router-link
                    v-for="board in boards"
                    :key="board.id"
                    :to="`/board/${board.id}`"
                    class="board-tile"
                    :style="tileStyle(board)"
                >
                    <span class="board-tile-icon">
                        <font-awesome-icon :icon="board.icon || 'table-columns'" />
                    </span>
                    <div class="board-tile-body">
                        <p class="board-tile-title">{{ board.title }}</p>
                        <p v-if="board.description" class="board-tile-description">{{ board.description }}</p>
                        <p class="board-tile-meta">
                            {{ board.cardCount }} kort
                            <template v-if="board.isArchived"> · Arkiveret</template>
                        </p>
                    </div>
                </router-link>
            </div>

            <BModal v-model="createOpen" has-modal-card>
                <div class="modal-card">
                    <header class="modal-card-head">
                        <p class="modal-card-title">Ny tavle</p>
                        <button type="button" class="delete" @click="createOpen = false" />
                    </header>
                    <section class="modal-card-body">
                        <BField label="Titel">
                            <BInput v-model="form.title" placeholder="Fx Aktiviteter 2028" />
                        </BField>
                        <BField label="Beskrivelse">
                            <BInput v-model="form.description" type="textarea" :rows="3" />
                        </BField>
                        <p class="help">Kun inviterede medlemmer samt systemadministratorer og lejrchefer har adgang. Du bliver selv medlem.</p>
                    </section>
                    <footer class="modal-card-foot">
                        <div class="buttons">
                            <BButton type="is-primary" :loading="saving" @click="createBoard">Opret</BButton>
                            <BButton @click="createOpen = false">Annuller</BButton>
                        </div>
                    </footer>
                </div>
            </BModal>
        </div>
    </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import { BButton, BField, BInput, BModal, useToast } from 'buefy';
import type { BoardSummaryDto } from '@ds28/shared-core/types';
import { useBoardStore } from '@ds28/shared-core/Stores/BoardStore';

const router = useRouter();
const store = useBoardStore();
const Toast = useToast();

const loading = ref(false);
const saving = ref(false);
const createOpen = ref(false);
const form = ref({ title: '', description: '' });

const boards = computed(() => store.Boards);

function tileStyle(board: BoardSummaryDto): Record<string, string> {
    return board.color ? { borderTop: `3px solid ${board.color}` } : {};
}

function openCreate() {
    form.value = { title: '', description: '' };
    createOpen.value = true;
}

async function createBoard() {
    if (saving.value) return;

    if (!form.value.title.trim()) {
        Toast.open({ message: 'Titlen må ikke være tom.', type: 'is-danger' });
        return;
    }

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
            Toast.open({ message: 'Tavlen er oprettet.', type: 'is-success' });
            createOpen.value = false;
            router.push(`/board/${id}`);
        } else {
            Toast.open({ message: 'Tavlen kunne ikke oprettes. Prøv en anden titel.', type: 'is-danger' });
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

<style scoped>
.board-list-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
    margin-bottom: 1.5rem;
}

.board-list-head .button svg {
    margin-right: 0.4rem;
}

.board-grid {
    display: grid;
    gap: 1rem;
    grid-template-columns: repeat(auto-fill, minmax(15rem, 1fr));
}

.board-tile {
    display: flex;
    gap: 0.9rem;
    align-items: flex-start;
    padding: 1rem;
    border: 1px solid hsl(0, 0%, 90%);
    border-radius: 8px;
    background: #fff;
    color: inherit;
    transition: box-shadow 0.15s ease;
}

.board-tile:hover {
    box-shadow: 0 2px 8px rgba(10, 10, 10, 0.12);
}

.board-tile-icon {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 2.5rem;
    height: 2.5rem;
    border-radius: 6px;
    background: hsl(0, 0%, 96%);
    color: rgba(0, 0, 0, 0.55);
    flex-shrink: 0;
    font-size: 1.15rem;
}

.board-tile-body {
    min-width: 0;
}

.board-tile-title {
    font-weight: 600;
    overflow-wrap: anywhere;
}

.board-tile-description {
    margin-top: 0.2rem;
    color: rgba(0, 0, 0, 0.6);
    font-size: 0.85rem;
    display: -webkit-box;
    -webkit-line-clamp: 2;
    -webkit-box-orient: vertical;
    overflow: hidden;
}

.board-tile-meta {
    margin-top: 0.4rem;
    color: rgba(0, 0, 0, 0.45);
    font-size: 0.8rem;
}
</style>
