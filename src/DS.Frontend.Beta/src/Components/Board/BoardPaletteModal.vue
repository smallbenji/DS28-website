<template>
    <UModal v-model:open="open" :title="text.title" :description="text.description">
        <template #body>
            <UAlert v-if="error" class="mb-4" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="error" />

            <form class="mb-5 flex flex-col gap-3" @submit.prevent="save">
                <UFormField :label="editingId === null ? text.newLabel : text.editLabel">
                    <UFieldGroup class="w-full">
                        <UInput v-model="name" :placeholder="text.placeholder" maxlength="64" :disabled="saving" class="w-full" :aria-label="text.nameLabel" />
                        <input
                            v-model="color"
                            type="color"
                            class="h-8 w-12 cursor-pointer rounded-e-md border border-default bg-default p-1 disabled:cursor-not-allowed disabled:opacity-50"
                            :disabled="saving || (mode === 'tags' && !useColor)"
                            aria-label="Vælg farve"
                        />
                    </UFieldGroup>
                </UFormField>
                <UCheckbox v-if="mode === 'tags'" v-model="useColor" label="Brug farve" :disabled="saving" />
                <div class="flex gap-2">
                    <UButton type="submit" :label="editingId === null ? text.addButton : text.saveButton" :loading="saving" :disabled="!name.trim()" />
                    <UButton v-if="editingId !== null" color="neutral" variant="outline" label="Annuller redigering" :disabled="saving" @click="resetForm" />
                </div>
            </form>

            <p v-if="entries.length === 0" class="text-sm text-muted">{{ text.empty }}</p>
            <ul v-else class="flex flex-col divide-y divide-default">
                <li v-for="entry in entries" :key="entry.id" class="flex items-center gap-3 py-2">
                    <span class="size-5 shrink-0 rounded border border-default" :style="{ backgroundColor: entry.color ?? 'transparent' }" aria-hidden="true" />
                    <span class="min-w-0 flex-1 truncate text-sm">{{ entry.name }}</span>
                    <UButton color="neutral" variant="ghost" size="xs" icon="i-lucide-pencil" :aria-label="`Rediger ${entry.name}`" :disabled="saving" @click="edit(entry)" />
                    <UButton color="error" variant="ghost" size="xs" icon="i-lucide-trash-2" :aria-label="`Slet ${entry.name}`" :disabled="saving" @click="confirmDelete(entry)" />
                </li>
            </ul>
        </template>
        <template #footer>
            <div class="flex w-full items-center justify-between gap-2">
                <p class="text-xs text-muted">{{ text.footer }}</p>
                <UButton color="neutral" variant="outline" label="Luk" @click="open = false" />
            </div>
        </template>
    </UModal>

    <ConfirmModal
        v-model:open="deleteOpen"
        :title="text.deleteTitle"
        :description="text.deleteDescription"
        confirm-label="Slet"
        color="error"
        :loading="saving"
        @confirm="remove"
    />
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import BoardService from "@/Services/BoardService";
import { useBoardStore } from "@/Stores/BoardStore";
import ConfirmModal from "@/Components/ConfirmModal.vue";

type PaletteEntry = { id: number; name: string; color: string | null };

const props = defineProps<{ boardId: number; mode: "colors" | "tags" }>();
const open = defineModel<boolean>("open", { required: true });

const store = useBoardStore();
const service = new BoardService();

const texts = {
    colors: {
        title: "Tavlens kortfarver",
        description: "Navngivne farver, som kan vælges på tavlens kort. Farver på eksisterende kort ændres ikke, når paletten redigeres, eller en farve slettes. Kortets farve er fælles på tværs af tavler.",
        newLabel: "Ny farve",
        editLabel: "Rediger farve",
        nameLabel: "Farvens navn",
        placeholder: "Fx Haster",
        addButton: "Tilføj farve",
        saveButton: "Gem farve",
        empty: "Ingen farver i paletten. Tilføj en farve ovenfor.",
        footer: "Paletten gemmes, når du tilføjer, gemmer eller sletter en farve.",
        deleteTitle: "Slet farve",
        deleteDescription: "Vil du slette farven fra tavlens palette? Eksisterende kort beholder deres farve.",
        saveError: "Farven kunne ikke gemmes. Prøv igen.",
        deleteError: "Farven kunne ikke slettes. Prøv igen."
    },
    tags: {
        title: "Tavlens tags",
        description: "Tags til fx udgivelser. Tags gælder kun på denne tavle og kan sættes på flere kort.",
        newLabel: "Nyt tag",
        editLabel: "Rediger tag",
        nameLabel: "Taggets navn",
        placeholder: "Fx v0.2.13",
        addButton: "Tilføj tag",
        saveButton: "Gem tag",
        empty: "Ingen tags. Tilføj et tag ovenfor.",
        footer: "Tags gemmes med det samme.",
        deleteTitle: "Slet tag",
        deleteDescription: "Vil du slette tagget? Det fjernes fra alle kort på denne tavle.",
        saveError: "Tagget kunne ikke gemmes. Kontrollér, at navnet er entydigt, og prøv igen.",
        deleteError: "Tagget kunne ikke slettes. Prøv igen."
    }
};

const text = computed(() => texts[props.mode]);
const entries = computed<PaletteEntry[]>(() => props.mode === "colors" ? store.SelectedBoard?.colors ?? [] : store.SelectedBoard?.tags ?? []);

const defaultColor = "#dbeafe";
const name = ref("");
const color = ref(defaultColor);
const useColor = ref(false);
const editingId = ref<number | null>(null);
const saving = ref(false);
const error = ref("");
const deleteOpen = ref(false);
const deleteId = ref<number | null>(null);

watch(open, (isOpen) => {
    if (!isOpen) return;

    resetForm();
    error.value = "";
});

function resetForm() {
    name.value = "";
    color.value = defaultColor;
    useColor.value = false;
    editingId.value = null;
}

function edit(entry: PaletteEntry) {
    name.value = entry.name;
    color.value = entry.color ?? defaultColor;
    useColor.value = entry.color != null;
    editingId.value = entry.id;
    error.value = "";
}

async function save() {
    if (saving.value || !name.value.trim()) return;

    saving.value = true;
    error.value = "";
    try {
        if (props.mode === "colors") {
            const data = { name: name.value.trim(), color: color.value };
            if (editingId.value === null) await service.createColor(props.boardId, data);
            else await service.updateColor(props.boardId, editingId.value, data);
        } else {
            const data = { name: name.value.trim(), color: useColor.value ? color.value : null };
            if (editingId.value === null) await service.createTag(props.boardId, data);
            else await service.updateTag(props.boardId, editingId.value, data);
        }
        await store.GET_BOARD(props.boardId);
        resetForm();
    } catch {
        error.value = text.value.saveError;
    } finally {
        saving.value = false;
    }
}

function confirmDelete(entry: PaletteEntry) {
    deleteId.value = entry.id;
    deleteOpen.value = true;
}

async function remove() {
    const id = deleteId.value;
    if (saving.value || id === null) return;

    saving.value = true;
    error.value = "";
    try {
        if (props.mode === "colors") await service.deleteColor(props.boardId, id);
        else await service.deleteTag(props.boardId, id);
        await store.GET_BOARD(props.boardId);
        if (editingId.value === id) resetForm();
    } catch {
        error.value = text.value.deleteError;
    } finally {
        deleteOpen.value = false;
        saving.value = false;
    }
}
</script>
