<template>
    <form class="flex flex-col gap-3" @submit.prevent="submit" @keydown.esc.stop="emit('cancel')">
        <p class="text-sm text-muted">Ny understilling under <span class="font-semibold text-highlighted">{{ parentTitle }}</span></p>
        <UFormField label="Titel" required>
            <UInput v-model="title" placeholder="Fx Økonomiansvarlig" class="w-full" autofocus />
        </UFormField>
        <UFormField label="Farve">
            <OrgColorSelect v-model="color" />
        </UFormField>
        <UFormField label="Grupper">
            <USelectMenu v-model="groups" :items="groupItems" value-key="value" multiple placeholder="Vælg grupper…" class="w-full" />
        </UFormField>
        <div class="flex justify-end gap-2">
            <UButton color="neutral" variant="ghost" label="Annuller" @click="emit('cancel')" />
            <UButton type="submit" icon="i-lucide-plus" label="Opret" :loading="loading" :disabled="loading || !title.trim()" />
        </div>
        <p class="text-xs text-muted">Personer tilføjes bagefter under fanen Personer.</p>
    </form>
</template>

<script setup lang="ts">
import { ref } from "vue";
import OrgColorSelect from "@/Components/Org/OrgColorSelect.vue";

const props = defineProps<{
    parentTitle: string;
    parentColor: string | null;
    groupItems: { label: string; value: string }[];
    loading: boolean;
}>();

const emit = defineEmits<{
    (e: "submit", data: { title: string; color: string | null; groups: string[] }): void;
    (e: "cancel"): void;
}>();

const title = ref("");
const color = ref<string | null>(props.parentColor);
const groups = ref<string[]>([]);

function submit() {
    if (!title.value.trim()) return;

    emit("submit", { title: title.value.trim(), color: color.value, groups: groups.value });
}
</script>
