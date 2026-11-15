<template>
    <BModal v-model="open" has-modal-card>
        <div class="modal-card board-tags-modal">
            <header class="modal-card-head">
                <p class="modal-card-title">Tavlens tags</p>
                <button type="button" class="delete" aria-label="Luk" @click="open = false" />
            </header>
            <section class="modal-card-body">
                <p class="mb-4">Opret tags til fx udgivelser. Tags gælder kun på denne tavle og kan sættes på flere kort.</p>
                <p v-if="error" class="notification is-danger is-light" role="alert">{{ error }}</p>
                <form class="mb-5" @submit.prevent="save">
                    <BField :label="editingId === null ? 'Nyt tag' : 'Rediger tag'">
                        <BInput v-model="name" placeholder="Fx v0.2.13" maxlength="64" expanded :disabled="saving" aria-label="Taggets navn" />
                        <BInput v-model="color" type="color" :disabled="saving || !useColor" aria-label="Vælg farve" />
                    </BField>
                    <BCheckbox v-model="useColor" :disabled="saving" class="mb-3">Brug farve</BCheckbox>
                    <div class="buttons">
                        <BButton native-type="submit" type="is-primary" :loading="saving" :disabled="!name.trim()">
                            {{ editingId === null ? 'Tilføj tag' : 'Gem tag' }}
                        </BButton>
                        <BButton v-if="editingId !== null" :disabled="saving" @click="resetForm">Annuller redigering</BButton>
                    </div>
                </form>
                <p v-if="tags.length === 0" class="has-text-grey">Ingen tags. Tilføj et tag ovenfor.</p>
                <div v-for="option in tags" :key="option.id" class="palette-row">
                    <span class="palette-swatch" :style="{ backgroundColor: option.color ?? 'transparent' }" aria-hidden="true" />
                    <span class="palette-name">{{ option.name }}</span>
                    <BButton size="is-small" :disabled="saving" :aria-label="`Rediger ${option.name}`" @click="edit(option)">Rediger</BButton>
                    <BButton size="is-small" type="is-danger" outlined :disabled="saving" :aria-label="`Slet ${option.name}`" @click="confirmDelete(option)">Slet</BButton>
                </div>
            </section>
            <footer class="modal-card-foot">
                <p class="help">Tags gemmes med det samme.</p>
                <BButton @click="open = false">Luk</BButton>
            </footer>
        </div>
    </BModal>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { BButton, BCheckbox, BField, BInput, BModal, useDialog } from 'buefy';
import BoardService from '@/Services/BoardService';
import { useBoardStore } from '@/Stores/BoardStore';
import type { BoardTagDto } from '@/types';

const props = defineProps<{ boardId: number }>();
const open = defineModel<boolean>({ default: false });
const store = useBoardStore();
const service = new BoardService();
const Dialog = useDialog();
const tags = computed(() => store.SelectedBoard?.tags ?? []);
const name = ref('');
const color = ref('#dbeafe');
const useColor = ref(false);
const editingId = ref<number | null>(null);
const saving = ref(false);
const error = ref('');

watch(open, (value) => {
    if (value) {
        resetForm();
        error.value = '';
    }
});

function resetForm() {
    name.value = '';
    color.value = '#dbeafe';
    useColor.value = false;
    editingId.value = null;
}

function edit(option: BoardTagDto) {
    name.value = option.name;
    color.value = option.color ?? '#dbeafe';
    useColor.value = option.color != null;
    editingId.value = option.id;
    error.value = '';
}

async function save() {
    if (saving.value || !name.value.trim()) return;
    saving.value = true;
    error.value = '';
    try {
        const data = { name: name.value.trim(), color: useColor.value ? color.value : null };
        if (editingId.value === null) {
            await service.createTag(props.boardId, data);
        } else {
            await service.updateTag(props.boardId, editingId.value, data);
        }
        await store.GET_BOARD(props.boardId);
        resetForm();
    } catch {
        error.value = 'Tagget kunne ikke gemmes. Kontrollér, at navnet er entydigt, og prøv igen.';
    } finally {
        saving.value = false;
    }
}

function confirmDelete(option: BoardTagDto) {
    Dialog.confirm({
        title: 'Slet tag',
        message: 'Vil du slette tagget? Det fjernes fra alle kort på denne tavle.',
        confirmText: 'Slet',
        cancelText: 'Annuller',
        type: 'is-danger',
        onConfirm: () => { void remove(option.id); }
    });
}

async function remove(id: number) {
    if (saving.value) return;
    saving.value = true;
    error.value = '';
    try {
        await service.deleteTag(props.boardId, id);
        await store.GET_BOARD(props.boardId);
        if (editingId.value === id) resetForm();
    } catch {
        error.value = 'Tagget kunne ikke slettes. Prøv igen.';
    } finally {
        saving.value = false;
    }
}
</script>

<style scoped>
.board-tags-modal {
    width: min(40rem, calc(100vw - 2rem));
    margin: 0 auto;
}
.board-tags-modal .modal-card-foot {
    flex-wrap: wrap;
    gap: 0.5rem;
}

.palette-row {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.5rem 0;
    border-bottom: 1px solid #eee;
}

.palette-name {
    flex: 1;
    min-width: 0;
    overflow-wrap: anywhere;
}

.palette-swatch {
    width: 1.5rem;
    height: 1.5rem;
    flex-shrink: 0;
    border: 1px solid #cfd5dd;
    border-radius: 50%;
}
</style>
