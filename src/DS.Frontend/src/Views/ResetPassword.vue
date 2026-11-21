<template>
    <UCard v-if="hasToken">
        <template #header>
            <h1 class="text-center text-lg font-semibold text-highlighted">Nulstil adgangskode</h1>
        </template>

        <form v-if="!success" class="flex flex-col gap-4" @submit.prevent="submit">
            <PasswordFields v-model:password="password" v-model:repeat-password="repeatPassword" label="Ny adgangskode" :disabled="isSubmitting" />

            <UAlert v-if="error" color="error" variant="subtle" icon="i-lucide-circle-alert" title="Nulstillingen fejlede. Linket er muligvis udløbet eller allerede brugt." />

            <UButton type="submit" icon="i-lucide-key-round" label="Nulstil adgangskode" block :loading="isSubmitting" :disabled="!canSubmit" />
        </form>

        <div v-else class="flex flex-col gap-4">
            <UAlert color="success" variant="subtle" icon="i-lucide-circle-check" title="Din adgangskode er nulstillet. Du kan nu logge ind med din nye adgangskode." />
            <UButton to="/login" icon="i-lucide-log-in" label="Gå til login" block />
        </div>
    </UCard>

    <UCard v-else>
        <UEmpty icon="i-lucide-link-2-off" title="Ugyldigt link" description="Linket til nulstilling af adgangskode mangler oplysninger." />
    </UCard>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import { useRoute } from "vue-router";
import PasswordResetService from "@/Services/PasswordResetService";
import PasswordFields from "@/Components/Auth/PasswordFields.vue";

const route = useRoute();
const passwordResetService = new PasswordResetService();

const password = ref("");
const repeatPassword = ref("");
const isSubmitting = ref(false);
const success = ref(false);
const error = ref(false);

const userId = computed(() => route.params.id?.toString() ?? "");
const token = computed(() => route.query.token?.toString() ?? "");
const hasToken = computed(() => userId.value !== "" && token.value !== "");

const canSubmit = computed(() => password.value.length >= 4 && password.value === repeatPassword.value);

async function submit() {
    if (!canSubmit.value || isSubmitting.value) return;

    isSubmitting.value = true;
    error.value = false;
    try {
        const ok = await passwordResetService.resetPassword({ userId: userId.value, token: token.value, newPassword: password.value });
        if (ok) success.value = true;
        else error.value = true;
    } finally {
        isSubmitting.value = false;
    }
}
</script>
