<template>
    <UModal v-model:open="open" title="Opret ny gruppe">
        <template #body>
            <form id="create-group-form" class="flex flex-col gap-4" @submit.prevent="create">
                <UFormField label="ID" required>
                    <UInput v-model="id" inputmode="numeric" placeholder="F.eks. 1" class="w-full" autofocus />
                </UFormField>
                <UFormField label="Navn" required>
                    <UInput v-model="name" placeholder="Gruppens navn" class="w-full" />
                </UFormField>
                <UFormField label="Distrikt">
                    <USelect v-model="district" :items="districtItems" class="w-full" />
                </UFormField>
            </form>
        </template>
        <template #footer>
            <div class="flex w-full justify-end gap-2">
                <UButton color="neutral" variant="outline" label="Annuller" @click="open = false" />
                <UButton type="submit" form="create-group-form" label="Opret gruppe" :loading="saving" :disabled="!id.trim() || !name.trim()" />
            </div>
        </template>
    </UModal>
</template>

<script setup lang="ts">
import { ref, watch } from "vue";
import { useGroupsStore } from "@/Stores/GroupsStore";
import { useNotify } from "@/lib/notify";
import { districtItems } from "@/lib/scouts";
import { District } from "@/types";

const open = defineModel<boolean>("open", { required: true });

const groupsStore = useGroupsStore();
const notify = useNotify();

const id = ref("");
const name = ref("");
const district = ref<District>(District.DANEHOF);
const saving = ref(false);

watch(open, (isOpen) => {
    if (!isOpen) return;

    id.value = "";
    name.value = "";
    district.value = District.DANEHOF;
});

async function create() {
    if (!id.value.trim() || !name.value.trim()) return;

    saving.value = true;
    try {
        const ok = await groupsStore.CREATE_GROUP({
            id: id.value.trim(),
            name: name.value.trim(),
            district: district.value,
            patrols: [],
            scouts: [],
            users: []
        });
        notify.result(ok, "Gruppen er oprettet.", "Gruppen kunne ikke oprettes.");
        if (ok) open.value = false;
    } finally {
        saving.value = false;
    }
}
</script>
