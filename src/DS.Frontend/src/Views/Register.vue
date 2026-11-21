<template>
    <UCard>
        <template #header>
            <h1 class="text-center text-lg font-semibold text-highlighted">Opret bruger</h1>
        </template>

        <form class="flex flex-col gap-4" @submit.prevent="submit">
            <div class="grid gap-4 sm:grid-cols-2">
                <UFormField label="Fornavn" required>
                    <UInput v-model="firstName" autocomplete="given-name" class="w-full" />
                </UFormField>
                <UFormField label="Efternavn" required>
                    <UInput v-model="lastName" autocomplete="family-name" class="w-full" />
                </UFormField>
            </div>

            <UFormField label="Email" required>
                <UInput v-model="email" type="email" icon="i-lucide-mail" autocomplete="email" class="w-full" />
            </UFormField>

            <PasswordFields v-model:password="password" v-model:repeat-password="repeatPassword" :disabled="isSubmitting" />

            <UAlert v-if="error" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="error" />

            <UButton type="submit" icon="i-lucide-user-plus" label="Opret bruger" block :loading="isSubmitting" :disabled="!canSubmit" />

            <p class="text-center text-sm">
                Har du allerede en bruger?
                <ULink to="/login" class="text-primary">Log ind</ULink>
            </p>
        </form>
    </UCard>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import { useRouter } from "vue-router";
import AuthService from "@/Services/AuthService";
import PasswordFields from "@/Components/Auth/PasswordFields.vue";

const router = useRouter();
const authService = new AuthService();

const firstName = ref("");
const lastName = ref("");
const email = ref("");
const password = ref("");
const repeatPassword = ref("");
const isSubmitting = ref(false);
const error = ref("");

const canSubmit = computed(() => firstName.value.trim() !== ""
    && lastName.value.trim() !== ""
    && email.value.trim() !== ""
    && password.value.length >= 4
    && password.value === repeatPassword.value);

async function submit() {
    if (!canSubmit.value || isSubmitting.value) return;

    isSubmitting.value = true;
    error.value = "";
    try {
        const errorMessage = await authService.register({
            firstName: firstName.value,
            lastName: lastName.value,
            email: email.value,
            password: password.value
        });

        if (errorMessage) {
            error.value = errorMessage;
            return;
        }

        await router.push({ path: "/login", query: { registered: "1" } });
    } finally {
        isSubmitting.value = false;
    }
}
</script>
