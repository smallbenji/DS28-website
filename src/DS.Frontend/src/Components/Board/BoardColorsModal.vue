<template>
    <BModal v-model="open" has-modal-card>
        <div class="modal-card">
            <header class="modal-card-head">
                <p class="modal-card-title">Tavlens kortfarver</p>
                <button type="button" class="delete" aria-label="Luk" @click="open = false" />
            </header>
            <section class="modal-card-body">
                <p class="mb-4">Opret navngivne farver, som kan vælges på tavlens kort. Farver på eksisterende kort ændres ikke, når paletten redigeres eller en farve slettes. Kortets farve er fælles på tværs af tavler.</p>
                <p v-if="error" class="notification is-danger is-light" role="alert">{{ error }}</p>
                <form class="mb-5" @submit.prevent="save">
                    <BField :label="editingId === null ? 'Ny farve' : 'Rediger farve'">
                        <BInput v-model="name" placeholder="Fx Haster" maxlength="64" expanded :disabled="saving" aria-label="Farvens navn" />
                        <BInput v-model="color" type="color" :disabled="saving" aria-label="Vælg farve" />
                    </BField>
                    <div class="buttons">
                        <BButton native-type="submit" type="is-primary" :loading="saving" :disabled="!name.trim()">
                            {{ editingId === null ? 'Tilføj farve' : 'Gem farve' }}
                        </BButton>
                        <BButton v-if="editingId !== null" :disabled="saving" @click="resetForm">Annuller redigering</BButton>
                    </div>
                </form>
                <p v-if="colors.length === 0" class="has-text-grey">Ingen farver i paletten. Tilføj en farve ovenfor.</p>
                <div v-for="option in colors" :key="option.id" class="palette-row">
                    <span class="palette-swatch" :style="{ backgroundColor: option.color }" aria-hidden="true" />
                    <span class="palette-name">{{ option.name }}</span>
                    <BButton size="is-small" :disabled="saving" :aria-label="`Rediger ${option.name}`" @click="edit(option)">Rediger</BButton>
                    <BButton size="is-small" type="is-danger" outlined :disabled="saving" :aria-label="`Slet ${option.name}`" @click="confirmDelete(option)">Slet</BButton>
                </div>
            </section>
            <footer class="modal-card-foot">
                <p class="help">Paletten gemmes, når du tilføjer, gemmer eller sletter en farve.</p>
                <BButton @click="open = false">Luk</BButton>
            </footer>
        </div>
    </BModal>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue';
import { BButton, BField, BInput, BModal, useDialog } from 'buefy';
import BoardService from '@/Services/BoardService';
import { useBoardStore } from '@/Stores/BoardStore';
import type { BoardColorDto } from '@/types';

const props = defineProps<{ boardId: number }>();
const open = defineModel<boolean>({ default: false });
const store = useBoardStore();
const service = new BoardService();
const Dialog = useDialog();
const colors = computed(() => store.SelectedBoard?.colors ?? []);
const name = ref('');
const color = ref('#dbeafe');
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
    editingId.value = null;
}

function edit(option: BoardColorDto) {
    name.value = option.name;
    color.value = option.color;
    editingId.value = option.id;
    error.value = '';
}

async function save() {
    if (saving.value || !name.value.trim()) return;
    saving.value = true;
    error.value = '';
    try {
        const data = { name: name.value.trim(), color: color.value };
        if (editingId.value === null) {
            await service.createColor(props.boardId, data);
        } else {
            await service.updateColor(props.boardId, editingId.value, data);
        }
        await store.GET_BOARD(props.boardId);
        resetForm();
    } catch {
        error.value = 'Farven kunne ikke gemmes. Prøv igen.';
    } finally {
        saving.value = false;
    }
}

function confirmDelete(option: BoardColorDto) {
    Dialog.confirm({
        title: 'Slet farve',
        message: 'Vil du slette farven fra tavlens palette? Eksisterende kort beholder deres farve.',
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
        await service.deleteColor(props.boardId, id);
        await store.GET_BOARD(props.boardId);
        if (editingId.value === id) resetForm();
    } catch {
        error.value = 'Farven kunne ikke slettes. Prøv igen.';
    } finally {
        saving.value = false;
    }
}
</script>

<style scoped>
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
