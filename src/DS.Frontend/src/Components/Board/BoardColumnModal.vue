<template>
    <UModal v-model:open="open" :title="column ? 'Rediger kolonne' : 'Ny kolonne'">
        <template #body>
            <form id="board-column-form" class="flex flex-col gap-4" @submit.prevent="save">
                <UFormField label="Titel" required>
                    <UInput v-model="title" placeholder="Fx I gang" class="w-full" autofocus />
                </UFormField>
                <UFormField label="WIP-grænse" help="Lad feltet stå tomt for ingen grænse." :error="wipError || undefined">
                    <UInput v-model="wipLimit" type="number" min="1" class="w-full" />
                </UFormField>
                <UCheckbox v-model="isDoneColumn" label="Færdig-kolonne" description="Kort i kolonnen tæller som færdige." />
            </form>
        </template>
        <template #footer>
            <div class="flex w-full justify-end gap-2">
                <UButton color="neutral" variant="outline" label="Annuller" @click="open = false" />
                <UButton type="submit" form="board-column-form" label="Gem" :loading="saving" :disabled="!title.trim() || !!wipError" />
            </div>
        </template>
    </UModal>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { useBoardStore } from "@/Stores/BoardStore";
import { useNotify } from "@/lib/notify";
import type { BoardColumnDto } from "@/types";

const props = defineProps<{ boardId: number; column: BoardColumnDto | null }>();
const open = defineModel<boolean>("open", { required: true });

const store = useBoardStore();
const notify = useNotify();

const title = ref("");
const wipLimit = ref("");
const isDoneColumn = ref(false);
const saving = ref(false);

const parsedWipLimit = computed(() => String(wipLimit.value ?? "").trim() ? Number(wipLimit.value) : null);
const wipError = computed(() => parsedWipLimit.value !== null && (!Number.isInteger(parsedWipLimit.value) || parsedWipLimit.value < 1)
    ? "WIP-grænsen skal være mindst 1."
    : "");

watch(open, (isOpen) => {
    if (!isOpen) return;

    title.value = props.column?.title ?? "";
    wipLimit.value = props.column?.wipLimit != null ? String(props.column.wipLimit) : "";
    isDoneColumn.value = props.column?.isDoneColumn ?? false;
});

async function save() {
    if (saving.value || !title.value.trim() || wipError.value) return;

    saving.value = true;
    try {
        const payload = { title: title.value.trim(), wipLimit: parsedWipLimit.value, isDoneColumn: isDoneColumn.value };
        const ok = props.column
            ? await store.UPDATE_COLUMN(props.boardId, Number(props.column.id), payload)
            : (await store.ADD_COLUMN(props.boardId, payload)) !== null;
        notify.result(ok, "Kolonnen er gemt.", "Kolonnen kunne ikke gemmes.");
        if (ok) open.value = false;
    } finally {
        saving.value = false;
    }
}
</script>
