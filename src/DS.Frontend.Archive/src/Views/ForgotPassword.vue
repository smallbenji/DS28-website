<template>
    <section class="section">
        <div class="columns is-centered">
            <div class="column is-4-desktop is-6-tablet">

                <div class="card frame">
                    <header class="card-header">
                        <p class="card-header-title is-centered">Glemt adgangskode</p>
                    </header>

                    <div class="card-content">
                        <template v-if="!submitted">
                            <p class="mb-4">
                                Skriv den email du er oprettet med, så sender vi dig et link til at vælge en ny
                                adgangskode.
                            </p>

                            <BField label="Email">
                                <BInput ref="emailInput" v-model="email" type="email" icon="envelope"
                                    autocomplete="username" @keyup.enter="submit" />
                            </BField>

                            <div v-if="error" class="notification is-danger is-light py-2 px-4 my-3">
                                {{ error }}
                            </div>

                            <BButton type="is-primary" expanded :loading="isSubmitting" :disabled="!canSubmit"
                                icon-left="envelope" @click="submit">
                                Send link
                            </BButton>
                        </template>

                        <template v-else>
                            <div class="notification is-success is-light py-2 px-4 my-3">
                                Hvis {{ email }} er oprettet, har vi sendt et link til at vælge en ny adgangskode.
                                Tjek din spam-mappe, hvis du ikke ser det.
                            </div>
                        </template>

                        <p class="has-text-centered mt-4">
                            <router-link to="/login">
                                Tilbage til login
                            </router-link>
                        </p>
                    </div>
                </div>

            </div>
        </div>
    </section>
</template>

<script lang="ts" setup>
import AuthService from '@/Services/AuthService';
import { BButton, BField, BInput } from 'buefy';
import { computed, onMounted, ref } from 'vue';

const authService = new AuthService();

const emailInput = ref<InstanceType<typeof BInput> | null>(null);

const email = ref('');
const isSubmitting = ref(false);
const submitted = ref(false);
const error = ref('');

const canSubmit = computed(() => email.value.trim() !== '');

onMounted(() => {
    emailInput.value?.focus();
});

const submit = async () => {
    if (!canSubmit.value || isSubmitting.value) return;

    isSubmitting.value = true;
    error.value = '';

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
};
</script>

<style scoped>
.frame {
    min-width: 350px;
}
</style>
