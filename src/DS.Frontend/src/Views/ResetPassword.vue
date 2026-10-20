<template>
    <div class="card frame center" v-if="hasToken">
        <div class="card-header">
            <p class="card-header-title">
                Nulstil adgangskode
            </p>
        </div>
        <div class="card-content">
            <template v-if="!success">
                <BField label="Ny adgangskode">
                    <BInput type="password" v-model="password" />
                </BField>
                <BField label="Gentag adgangskode">
                    <BInput type="password" v-model="repeatPassword" />
                </BField>
                <p v-if="error" class="help is-danger">
                    Nulstillingen fejlede. Linket er muligvis udløbet eller allerede brugt.
                </p>
            </template>
            <p v-else>
                Din adgangskode er blevet nulstillet. Du kan nu logge ind med din nye adgangskode.
            </p>
        </div>
        <div class="card-footer">
            <p class="card-footer-item" v-if="!success">
                <BButton type="is-success" :disabled="!canSubmit" :loading="isSubmitting" @click="submit">
                    Nulstil adgangskode
                </BButton>
            </p>
            <p class="card-footer-item" v-else>
                <BButton type="is-primary" @click="goToLogin">
                    Gå til login
                </BButton>
            </p>
        </div>
    </div>
    <div class="center" v-else>
        Ugyldigt link
    </div>
</template>

<script lang="ts" setup>
import PasswordResetService from '@/Services/PasswordResetService';
import { BButton, BField, BInput } from 'buefy';
import { computed, ref } from 'vue';
import { useRoute } from 'vue-router';

const route = useRoute();
const passwordResetService = new PasswordResetService();

const password = ref('');
const repeatPassword = ref('');
const isSubmitting = ref(false);
const success = ref(false);
const error = ref(false);

const userId = computed(() => route.params.id?.toString() ?? '');
const token = computed(() => route.query.token?.toString() ?? '');
const hasToken = computed(() => userId.value !== '' && token.value !== '');

const canSubmit = computed(() => {
    if (password.value.length < 4) return false;
    if (password.value !== repeatPassword.value) return false;
    return true;
});

const submit = async () => {
    if (!canSubmit.value) return;

    isSubmitting.value = true;
    error.value = false;

    try {
        const ok = await passwordResetService.resetPassword({
            userId: userId.value,
            token: token.value,
            newPassword: password.value
        });

        if (ok) {
            success.value = true;
        } else {
            error.value = true;
        }
    } finally {
        isSubmitting.value = false;
    }
};

const goToLogin = () => {
    window.location.href = "/login";
};
</script>

<style>
.center {
    margin: auto;
}
.frame {
    min-width: 350px;
}
</style>
