<template>
    <UModal v-model:open="open" title="Opret spejder">
        <template #body>
            <form id="create-scout-form" class="flex flex-col gap-4" @submit.prevent="create">
                <UFormField label="Navn" required>
                    <UInput v-model="name" placeholder="Spejderens navn" class="w-full" autofocus />
                </UFormField>
                <div class="grid gap-4 sm:grid-cols-2">
                    <UFormField label="Fødselsdato" required>
                        <UInput v-model="birthday" type="date" class="w-full" />
                    </UFormField>
                    <UFormField label="Køn">
                        <USelect v-model="gender" :items="genderItems" class="w-full" />
                    </UFormField>
                </div>
            </form>
        </template>
        <template #footer>
            <div class="flex w-full justify-end gap-2">
                <UButton color="neutral" variant="outline" label="Annuller" @click="open = false" />
                <UButton type="submit" form="create-scout-form" label="Opret spejder" :loading="saving" :disabled="!name.trim() || !birthday" />
            </div>
        </template>
    </UModal>
</template>

<script setup lang="ts">
import { ref, watch } from "vue";
import GroupsService from "@/Services/GroupsService";
import { useGroupsStore } from "@/Stores/GroupsStore";
import { useNotify } from "@/lib/notify";
import { genderItems } from "@/lib/scouts";

const props = defineProps<{ groupId: string }>();
const open = defineModel<boolean>("open", { required: true });

const groupsService = new GroupsService();
const groupsStore = useGroupsStore();
const notify = useNotify();

const name = ref("");
const birthday = ref("");
const gender = ref<"Male" | "Female">("Male");
const saving = ref(false);

watch(open, (isOpen) => {
    if (!isOpen) return;

    name.value = "";
    birthday.value = "";
    gender.value = "Male";
});

async function create() {
    if (!name.value.trim() || !birthday.value) return;

    saving.value = true;
    try {
        const birthdayIso = new Date(`${birthday.value}T00:00:00`).toISOString();
        const scout = await groupsService.createScout(props.groupId, name.value.trim(), birthdayIso, gender.value);
        notify.result(scout != null, "Spejderen er oprettet.", "Spejderen kunne ikke oprettes.");
        if (scout) {
            open.value = false;
            await groupsStore.GET_GROUPS();
        }
    } finally {
        saving.value = false;
    }
}
</script>
