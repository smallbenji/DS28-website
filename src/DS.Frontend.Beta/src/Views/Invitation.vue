<template>
    <UCard v-if="loading">
        <div class="flex flex-col gap-3">
            <USkeleton v-for="index in 4" :key="index" class="h-10" />
        </div>
    </UCard>

    <UCard v-else-if="invitation && !invitation.used">
        <template #header>
            <h1 class="text-center text-lg font-semibold text-highlighted">Brugeroprettelse</h1>
        </template>

        <form class="flex flex-col gap-4" @submit.prevent="useInvitation">
            <UFormField label="Email">
                <UInput :model-value="invitation.email" type="email" icon="i-lucide-mail" disabled class="w-full" />
            </UFormField>
            <div class="grid gap-4 sm:grid-cols-2">
                <UFormField label="Fornavn" required>
                    <UInput v-model="firstName" autocomplete="given-name" :disabled="busy" class="w-full" />
                </UFormField>
                <UFormField label="Efternavn" required>
                    <UInput v-model="lastName" autocomplete="family-name" :disabled="busy" class="w-full" />
                </UFormField>
            </div>

            <PasswordFields v-model:password="password" v-model:repeat-password="repeatPassword" :disabled="busy" />

            <UAlert v-if="failed" color="error" variant="subtle" icon="i-lucide-circle-alert" title="Kontoen kunne ikke oprettes. Prøv igen." />

            <UButton type="submit" icon="i-lucide-user-plus" label="Opret konto" block :loading="busy" :disabled="!canComplete || busy" />
        </form>
    </UCard>

    <UCard v-else>
        <UEmpty
            :icon="invitation?.used ? 'i-lucide-mail-check' : 'i-lucide-mail-x'"
            :title="invitation?.used ? 'Invitationen er brugt' : 'Ingen invitation fundet'"
            :actions="[{ label: 'Gå til login', to: '/login' }]"
        />
    </UCard>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRoute } from "vue-router";
import InvitationService from "@/Services/InvitationService";
import PasswordFields from "@/Components/Auth/PasswordFields.vue";
import type { UserInvitationDto } from "@/types";

const route = useRoute();
const invitationService = new InvitationService();

const invitation = ref<UserInvitationDto | null>(null);
const loading = ref(true);
const busy = ref(false);
const failed = ref(false);
const firstName = ref("");
const lastName = ref("");
const password = ref("");
const repeatPassword = ref("");

const invitationId = computed(() => route.params.id?.toString() ?? "");

const canComplete = computed(() => password.value.length >= 4
    && password.value === repeatPassword.value
    && firstName.value.trim() !== ""
    && lastName.value.trim() !== "");

onMounted(async () => {
    try {
        if (invitationId.value) invitation.value = await invitationService.getInvitation(invitationId.value);
    } finally {
        loading.value = false;
    }
});

async function useInvitation() {
    if (!canComplete.value || busy.value) return;

    busy.value = true;
    failed.value = false;
    try {
        const ok = await invitationService.useInvitation(invitationId.value, {
            firstName: firstName.value,
            lastName: lastName.value,
            password: password.value
        });

        if (ok) {
            window.location.assign("/beta/");
            return;
        }

        failed.value = true;
    } finally {
        busy.value = false;
    }
}
</script>
