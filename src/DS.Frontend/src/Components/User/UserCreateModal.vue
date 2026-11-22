<template>
    <UModal v-model:open="open" title="Tilføj ny bruger">
        <template #body>
            <form id="create-user-form" class="flex flex-col gap-4" @submit.prevent="create">
                <UFormField label="Brugernavn" help="Dannes ud fra for- og efternavn.">
                    <UInput :model-value="userName" disabled class="w-full" />
                </UFormField>
                <div class="grid gap-4 sm:grid-cols-2">
                    <UFormField label="Fornavn" required>
                        <UInput v-model="firstName" class="w-full" autofocus />
                    </UFormField>
                    <UFormField label="Efternavn" required>
                        <UInput v-model="lastName" class="w-full" />
                    </UFormField>
                </div>
                <UFormField label="Email">
                    <UInput v-model="email" type="email" class="w-full" />
                </UFormField>
                <UFormField label="Gruppe">
                    <USelectMenu v-model="groupId" :items="groupItems" value-key="value" class="w-full" />
                </UFormField>
            </form>
        </template>
        <template #footer>
            <div class="flex w-full justify-end gap-2">
                <UButton color="neutral" variant="outline" label="Annuller" @click="open = false" />
                <UButton type="submit" form="create-user-form" label="Opret bruger" :loading="saving" :disabled="!firstName.trim() || !lastName.trim()" />
            </div>
        </template>
    </UModal>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { useToast } from "@nuxt/ui/composables";
import { useUserStore } from "@/Stores/UserStore";
import { noGroup, useGroupOptions } from "@/lib/groupOptions";
import type { UserDto } from "@/types";

const open = defineModel<boolean>("open", { required: true });

const userStore = useUserStore();
const toast = useToast();
const { groupItems, findGroup } = useGroupOptions();

const firstName = ref("");
const lastName = ref("");
const email = ref("");
const groupId = ref(noGroup);
const saving = ref(false);

const userName = computed(() => `${firstName.value}${lastName.value}`.toLowerCase());

watch(open, (isOpen) => {
    if (!isOpen) return;

    firstName.value = "";
    lastName.value = "";
    email.value = "";
    groupId.value = noGroup;
});

async function create() {
    if (!firstName.value.trim() || !lastName.value.trim()) return;

    const user: UserDto = {
        id: "",
        userName: userName.value,
        firstName: firstName.value,
        lastName: lastName.value,
        email: email.value,
        phone: "",
        roles: [],
        positions: [],
        group: findGroup(groupId.value),
        lockoutEnd: null,
        lastLoginAt: null,
        lastActiveAt: null,
        profilePicture: null
    };

    saving.value = true;
    try {
        const ok = await userStore.CREATE_USER(user);
        if (ok) {
            toast.add({ title: "Brugeren er oprettet.", color: "success", icon: "i-lucide-circle-check" });
            open.value = false;
        } else {
            toast.add({ title: "Brugeren kunne ikke oprettes.", color: "error", icon: "i-lucide-circle-alert" });
        }
    } finally {
        saving.value = false;
    }
}
</script>
