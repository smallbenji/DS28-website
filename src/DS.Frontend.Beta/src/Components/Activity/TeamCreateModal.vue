<template>
    <UModal v-model:open="open" title="Opret team">
        <template #body>
            <form id="activity-team-create-form" @submit.prevent="create">
                <UFormField label="Navn" required>
                    <UInput v-model="name" placeholder="Teamets navn" class="w-full" autofocus />
                </UFormField>
            </form>
        </template>
        <template #footer>
            <div class="flex w-full justify-end gap-2">
                <UButton color="neutral" variant="outline" label="Annuller" @click="open = false" />
                <UButton type="submit" form="activity-team-create-form" label="Opret team" :loading="saving" :disabled="!name.trim()" />
            </div>
        </template>
    </UModal>
</template>

<script setup lang="ts">
import { ref, watch } from "vue";
import { useActivityStore } from "@/Stores/ActivityStore";
import { useNotify } from "@/lib/notify";

const open = defineModel<boolean>("open", { required: true });

const activityStore = useActivityStore();
const notify = useNotify();

const name = ref("");
const saving = ref(false);

watch(open, (isOpen) => {
    if (isOpen) name.value = "";
});

async function create() {
    if (!name.value.trim() || saving.value) return;

    saving.value = true;
    try {
        const ok = await activityStore.ADD_TEAM(name.value.trim());
        notify.result(ok, "Teamet er oprettet.", "Teamet kunne ikke oprettes.");
        if (ok) open.value = false;
    } finally {
        saving.value = false;
    }
}
</script>
