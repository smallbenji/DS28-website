<template>
    <section class="section">
        <div class="columns is-centered">
            <div class="column is-4-desktop is-6-tablet">

                <div class="card frame">
                    <header class="card-header">
                        <p class="card-header-title is-centered">Login</p>
                    </header>

                    <div class="card-content">
                        <div v-if="registered" class="notification is-success is-light py-2 px-4 my-3">
                            Din bruger er oprettet. Du kan nu logge ind.
                        </div>

                        <template v-if="step === 'login'">
                            <BField label="Email">
                                <BInput ref="emailInput" v-model="email" type="email" icon="envelope"
                                    autocomplete="username" @keyup.enter="login" />
                            </BField>

                            <BField label="Adgangskode">
                                <BInput v-model="password" type="password" icon="lock" password-reveal
                                    autocomplete="current-password" @keyup.enter="login" />
                            </BField>

                            <div v-if="error" class="notification is-danger is-light py-2 px-4 my-3">
                                {{ error }}
                            </div>

                            <BButton type="is-primary" expanded :loading="isSubmitting" :disabled="!canSubmitLogin"
                                icon-left="right-to-bracket" @click="login">
                                Log ind
                            </BButton>

                            <!-- <p class="has-text-centered mt-4">
                                Har du ingen bruger?
                                <router-link to="/register">Opret bruger</router-link>
                            </p> -->
                        </template>

                        <template v-else-if="step === 'twofactor'">
                            <p class="mb-4">
                                {{
                                    authenticatorAvailable
                                        ? "Indtast den 6-cifrede kode fra din autentificeringsapp."
                                        : "Login med din passkey."
                                }}
                            </p>

                            <BButton v-if="hasPasskeys" type="is-info is-light" expanded :loading="isSubmitting"
                                icon-left="fingerprint" class="mb-4" @click="verifyWithPasskey">
                                Login med Passkey
                            </BButton>

                            <template v-if="authenticatorAvailable">

                                <BField label="Autentificeringskode">
                                    <BInput ref="twoFactorInput" v-model="twoFactorCode" icon="mobile-screen-button"
                                        autocomplete="one-time-code" @keyup.enter="verifyTwoFactor" />
                                </BField>

                                <BCheckbox v-model="rememberMachine">
                                    Husk denne enhed
                                </BCheckbox>

                                <div v-if="error" class="notification is-danger is-light py-2 px-4 my-3">
                                    {{ error }}
                                </div>

                                <BButton type="is-primary" expanded :loading="isSubmitting"
                                    :disabled="!canSubmitTwoFactor" icon-left="shield-halved" @click="verifyTwoFactor">
                                    Verificér
                                </BButton>

                                <p class="has-text-centered mt-4">
                                    <a class="has-text-link" @click="goToRecovery">Log ind med en recovery code</a>
                                </p>
                            </template>
                        </template>

                        <template v-else>
                            <p class="mb-4">
                                Indtast en af dine recovery codes for at logge ind.
                            </p>

                            <BField label="Recovery code">
                                <BInput ref="recoveryInput" v-model="recoveryCode" icon="key"
                                    autocomplete="one-time-code" @keyup.enter="loginWithRecoveryCode" />
                            </BField>

                            <div v-if="error" class="notification is-danger is-light py-2 px-4 my-3">
                                {{ error }}
                            </div>

                            <BButton type="is-primary" expanded :loading="isSubmitting" :disabled="!canSubmitRecovery"
                                icon-left="right-to-bracket" @click="loginWithRecoveryCode">
                                Log ind
                            </BButton>

                            <p class="has-text-centered mt-4">
                                <a class="has-text-link" @click="goToTwoFactor">Tilbage til autentificeringskode</a>
                            </p>
                        </template>
                    </div>
                </div>

            </div>
        </div>
    </section>
</template>

<script lang="ts" setup>
import { startAssertion } from '@/lib/passkeys';
import AuthService from '@/Services/AuthService';
import { BButton, BCheckbox, BField, BInput } from 'buefy';
import { computed, nextTick, onMounted, ref, watch } from 'vue';
import { useRoute } from 'vue-router';

const route = useRoute();
const authService = new AuthService();

const step = ref<'login' | 'twofactor' | 'recovery'>('login');

const emailInput = ref<InstanceType<typeof BInput> | null>(null);
const twoFactorInput = ref<InstanceType<typeof BInput> | null>(null);
const recoveryInput = ref<InstanceType<typeof BInput> | null>(null);

const email = ref('');
const userId = ref('');
const password = ref('');
const twoFactorCode = ref('');
const recoveryCode = ref('');
const rememberMachine = ref(false);

const isSubmitting = ref(false);
const error = ref('');

const registered = computed(() => route.query.registered === '1');

const returnUrl = computed(() => {
    const value = route.query.ReturnUrl ?? route.query.returnUrl;
    return typeof value === 'string' && value.length > 0 ? value : '/';
});

const canSubmitLogin = computed(() => email.value.trim() !== '' && password.value.length > 0);
const canSubmitTwoFactor = computed(() => twoFactorCode.value.trim() !== '');
const canSubmitRecovery = computed(() => recoveryCode.value.trim() !== '');

const hasPasskeys = ref(false);
const authenticatorAvailable = ref(true);

watch(step, () => {
    nextTick(() => {
        if (step.value === 'login') {
            emailInput.value?.focus();
        } else if (step.value === 'twofactor') {
            twoFactorInput.value?.focus();
        } else {
            recoveryInput.value?.focus();
        }
    });
});

onMounted(() => {
    emailInput.value?.focus();
});

const login = async () => {
    if (!canSubmitLogin.value || isSubmitting.value) return;

    isSubmitting.value = true;
    error.value = '';

    try {
        const result = await authService.login({
            email: email.value,
            password: password.value,
            returnUrl: returnUrl.value
        });

        if (result.error) {
            error.value = result.error;
            return;
        }

            if (result.requiresTwoFactor) {
                hasPasskeys.value = result.passkeysAvailable ?? false
                authenticatorAvailable.value = result.hasAuthenticator ?? true
                userId.value = result.userId ?? '';
                step.value = 'twofactor';
                return;
            }

        window.location.assign(result.returnUrl || '/');
    } finally {
        isSubmitting.value = false;
    }
};

const verifyTwoFactor = async () => {
    if (!canSubmitTwoFactor.value || isSubmitting.value) return;

    isSubmitting.value = true;
    error.value = '';

    try {
        const result = await authService.twoFactorLogin({
            twoFactorCode: twoFactorCode.value,
            rememberMachine: rememberMachine.value,
            returnUrl: returnUrl.value
        });

        if (result.error) {
            error.value = result.error;
            return;
        }

        window.location.assign(result.returnUrl || '/');
    } finally {
        isSubmitting.value = false;
    }
};

const verifyWithPasskey = async () => {
    if (isSubmitting.value) return;
    isSubmitting.value = true;
    error.value = '';

    try {
        const options = await authService.passkeyOptions();
        const credentialJson = await startAssertion(options.optionsJson);
        
        const result = await authService.passkeyVerify({
            credentialJson,
            rememberMachine: rememberMachine.value,
            returnUrl: returnUrl.value,
            userId: userId.value
        });

        if (result.error) {
            error.value = result.error;
            return;
        }

        window.location.assign(result.returnUrl || "/");
    } catch (err) {
        error.value = (err as DOMException)?.name === "NotAllowedError"
            ? "Login med passkey blev afbrudt"
            : "Der skete en uvented fejl ved login"
    } finally {
        isSubmitting.value = false
    }
}

const loginWithRecoveryCode = async () => {
    if (!canSubmitRecovery.value || isSubmitting.value) return;

    isSubmitting.value = true;
    error.value = '';

    try {
        const result = await authService.recoveryCodeLogin({
            recoveryCode: recoveryCode.value,
            returnUrl: returnUrl.value
        });

        if (result.error) {
            error.value = result.error;
            return;
        }

        window.location.assign(result.returnUrl || '/');
    } finally {
        isSubmitting.value = false;
    }
};

const goToRecovery = () => {
    error.value = '';
    step.value = 'recovery';
};

const goToTwoFactor = () => {
    error.value = '';
    step.value = 'twofactor';
};
</script>

<style scoped>
.frame {
    min-width: 350px;
}
</style>
