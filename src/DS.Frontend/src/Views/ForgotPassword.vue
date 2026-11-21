<template>
    <UCard>
        <template #header>
            <h1 class="text-center text-lg font-semibold text-highlighted">Glemt adgangskode</h1>
        </template>

        <form v-if="!submitted" class="flex flex-col gap-4" @submit.prevent="submit">
            <p class="text-sm">Skriv den email, du er oprettet med, så sender vi dig et link til at vælge en ny adgangskode.</p>

            <UFormField label="Email" required>
                <UInput ref="emailInput" v-model="email" type="email" icon="i-lucide-mail" autocomplete="username" class="w-full" />
            </UFormField>

            <UAlert v-if="error" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="error" />

            <UButton type="submit" icon="i-lucide-mail" label="Send link" block :loading="isSubmitting" :disabled="!canSubmit" />
        </form>

        <UAlert
            v-else
            color="success"
            variant="subtle"
            icon="i-lucide-mail-check"
            :description="`Hvis ${email} er oprettet, har vi sendt et link til at vælge en ny adgangskode. Tjek din spam-mappe, hvis du ikke ser det.`"
        />

        <p class="mt-4 text-center text-sm">
            <ULink to="/login" class="text-primary">Tilbage til login</ULink>
        </p>
    </UCard>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import AuthService from "@/Services/AuthService";

const authService = new AuthService();

const emailInput = ref<{ inputRef?: HTMLInputElement } | null>(null);
const email = ref("");
const isSubmitting = ref(false);
const submitted = ref(false);
const error = ref("");

const canSubmit = computed(() => email.value.trim() !== "");

onMounted(() => {
    emailInput.value?.inputRef?.focus();
});

async function submit() {
    if (!canSubmit.value || isSubmitting.value) return;

    isSubmitting.value = true;
    error.value = "";
    try {
        const errorMessage = await authService.forgotPassword({ email: email.value });

        if (errorMessage) {
            error.value = errorMessage;
            return;
        }

        submitted.value = true;
    } finally {
        isSubmitting.value = false;
    }
}
</script>
