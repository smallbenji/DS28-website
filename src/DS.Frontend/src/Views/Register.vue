<template>
    <section class="section">
        <div class="columns is-centered">
            <div class="column is-5-desktop is-7-tablet">

                <div class="card frame">
                    <header class="card-header">
                        <p class="card-header-title is-centered">Opret bruger</p>
                    </header>

                    <div class="card-content">
                        <div class="columns">
                            <div class="column">
                                <BField label="Fornavn">
                                    <BInput v-model="firstName" />
                                </BField>
                            </div>
                            <div class="column">
                                <BField label="Efternavn">
                                    <BInput v-model="lastName" />
                                </BField>
                            </div>
                        </div>

                        <BField label="Email">
                            <BInput v-model="email" type="email" icon="envelope" autocomplete="email" @keyup.enter="submit" />
                        </BField>

                        <BField label="Adgangskode">
                            <BInput v-model="password" type="password" icon="lock" password-reveal autocomplete="new-password" @keyup.enter="submit" />
                        </BField>

                        <BField label="Gentag adgangskode">
                            <BInput v-model="repeatPassword" type="password" icon="lock" password-reveal autocomplete="new-password" @keyup.enter="submit" />
                        </BField>

                        <div v-if="error" class="notification is-danger is-light py-2 px-4 my-3">
                            {{ error }}
                        </div>

                        <BButton type="is-primary" expanded :loading="isSubmitting" :disabled="!canSubmit" icon-left="user-plus" @click="submit">
                            Opret bruger
                        </BButton>

                        <p class="has-text-centered mt-4">
                            Har du allerede en bruger?
                            <router-link to="/login">Log ind</router-link>
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
import { computed, ref } from 'vue';
import { useRouter } from 'vue-router';

const router = useRouter();
const authService = new AuthService();

const firstName = ref('');
const lastName = ref('');
const email = ref('');
const password = ref('');
const repeatPassword = ref('');

const isSubmitting = ref(false);
const error = ref('');

const canSubmit = computed(() => {
    if (firstName.value.trim() === '') return false;
    if (lastName.value.trim() === '') return false;
    if (email.value.trim() === '') return false;
    if (password.value.length < 4) return false;
    if (password.value !== repeatPassword.value) return false;
    return true;
});

const submit = async () => {
    if (!canSubmit.value || isSubmitting.value) return;

    isSubmitting.value = true;
    error.value = '';

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

        router.push({ path: '/login', query: { registered: '1' } });
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
