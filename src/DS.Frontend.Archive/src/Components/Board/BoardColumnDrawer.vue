<template>
    <BSidebar v-model="open" right fullheight overlay type="is-light" :can-cancel="['escape', 'outside']">
        <div class="board-column-drawer">
            <header class="board-column-drawer-head">
                <p class="board-column-drawer-title">{{ isNew ? 'Ny kolonne' : 'Rediger kolonne' }}</p>
                <button type="button" class="delete" aria-label="Luk" @click="open = false" />
            </header>

            <BField label="Titel">
                <BInput v-model="form.title" placeholder="Fx I gang" @keydown.enter="save" />
            </BField>

            <BField label="WIP-grænse" message="Lad feltet stå tomt for ingen grænse">
                <BInput v-model="form.wipLimit" type="number" min="1" />
            </BField>

            <BCheckbox v-model="form.isDoneColumn">Færdig-kolonne</BCheckbox>

            <div class="board-column-drawer-actions">
                <BButton type="is-primary" :loading="saving" @click="save">Gem</BButton>
            </div>
        </div>
    </BSidebar>
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from 'vue';
import { BButton, BCheckbox, BField, BInput, BSidebar, useToast } from 'buefy';
import type { BoardColumnDto } from '@/types';
import { useBoardStore } from '@/Stores/BoardStore';

const props = defineProps<{
    modelValue: boolean;
    boardId: number;
    column: BoardColumnDto | null;
}>();

const emit = defineEmits<{
    'update:modelValue': [value: boolean];
    saved: [];
}>();

const store = useBoardStore();
const Toast = useToast();

const saving = ref(false);
const form = reactive({
    title: '',
    wipLimit: '',
    isDoneColumn: false
});

const open = computed({
    get: () => props.modelValue,
    set: (value: boolean) => emit('update:modelValue', value)
});

const isNew = computed(() => props.column === null);

watch(
    () => [props.modelValue, props.column] as const,
    () => {
        if (!props.modelValue) return;

        form.title = props.column?.title ?? '';
        form.wipLimit = props.column?.wipLimit != null ? String(props.column.wipLimit) : '';
        form.isDoneColumn = props.column?.isDoneColumn ?? false;
    },
    { immediate: true }
);

async function save() {
    if (saving.value) return;

    if (!form.title.trim()) {
        Toast.open({ message: 'Titlen må ikke være tom.', type: 'is-danger' });
        return;
    }

    const wipLimit = form.wipLimit.trim() ? Number(form.wipLimit) : null;
    if (wipLimit !== null && (!Number.isInteger(wipLimit) || wipLimit < 1)) {
        Toast.open({ message: 'WIP-grænsen skal være mindst 1.', type: 'is-danger' });
        return;
    }

    saving.value = true;
    try {
        const payload = {
            title: form.title.trim(),
            wipLimit,
            isDoneColumn: form.isDoneColumn
        };

        const success = isNew.value
            ? (await store.ADD_COLUMN(props.boardId, payload)) !== null
            : await store.UPDATE_COLUMN(props.boardId, Number(props.column!.id), payload);

        if (success) {
            Toast.open({ message: 'Kolonnen er gemt.', type: 'is-success' });
            emit('saved');
            open.value = false;
        } else {
            Toast.open({ message: 'Kolonnen kunne ikke gemmes.', type: 'is-danger' });
        }
    } finally {
        saving.value = false;
    }
}
</script>

<style scoped>
.board-column-drawer {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
    padding: 1.25rem;
}

.board-column-drawer-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
    margin-bottom: 0.5rem;
}

.board-column-drawer-title {
    font-size: 1.15rem;
    font-weight: 600;
}

.board-column-drawer-actions {
    display: flex;
    gap: 0.5rem;
    margin-top: 1rem;
}
</style>
