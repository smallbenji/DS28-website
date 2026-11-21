<template>
    <UCard>
        <template #header>
            <h1 class="text-center text-lg font-semibold text-highlighted">Log ind</h1>
        </template>

        <UAlert v-if="registered" class="mb-4" color="success" variant="subtle" icon="i-lucide-circle-check" title="Din bruger er oprettet. Du kan nu logge ind." />

        <form v-if="step === 'login'" class="flex flex-col gap-4" @submit.prevent="login">
            <UFormField label="Email" required>
                <UInput ref="emailInput" v-model="email" type="email" icon="i-lucide-mail" autocomplete="username" class="w-full" />
            </UFormField>

            <UFormField label="Adgangskode" required>
                <UInput
                    v-model="password"
                    :type="showPassword ? 'text' : 'password'"
                    icon="i-lucide-lock"
                    autocomplete="current-password"
                    class="w-full"
                    :ui="{ trailing: 'pe-1' }"
                >
                    <template #trailing>
                        <UButton
                            color="neutral"
                            variant="link"
                            size="sm"
                            :icon="showPassword ? 'i-lucide-eye-off' : 'i-lucide-eye'"
                            :aria-label="showPassword ? 'Skjul adgangskode' : 'Vis adgangskode'"
                            :aria-pressed="showPassword"
                            @click="showPassword = !showPassword"
                        />
                    </template>
                </UInput>
            </UFormField>

            <UAlert v-if="error" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="error" />

            <UButton type="submit" icon="i-lucide-log-in" label="Log ind" block :loading="isSubmitting" :disabled="!canSubmitLogin" />

            <p class="text-center text-sm">
                <ULink to="/forgot-password" class="text-primary">Glemt adgangskode</ULink>
            </p>
        </form>

        <div v-else-if="step === 'twofactor'" class="flex flex-col gap-4">
            <p class="text-sm">
                {{ authenticatorAvailable ? "Indtast den 6-cifrede kode fra din autentificeringsapp." : "Log ind med din passkey." }}
            </p>

            <UButton
                v-if="hasPasskeys"
                color="neutral"
                variant="outline"
                icon="i-lucide-fingerprint"
                label="Log ind med passkey"
                block
                :loading="isSubmitting"
                @click="verifyWithPasskey"
            />

            <form v-if="authenticatorAvailable" class="flex flex-col gap-4" @submit.prevent="verifyTwoFactor">
                <UFormField label="Autentificeringskode" required>
                    <UPinInput
                        v-model="twoFactorDigits"
                        :length="6"
                        type="number"
                        otp
                        autofocus
                        size="xl"
                        :disabled="isSubmitting"
                        aria-label="Autentificeringskode"
                        class="w-full justify-between"
                        @complete="verifyTwoFactor"
                    />
                </UFormField>

                <UCheckbox v-model="rememberMachine" label="Husk denne enhed" />

                <UAlert v-if="error" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="error" />

                <UButton type="submit" icon="i-lucide-shield-check" label="Verificér" block :loading="isSubmitting" :disabled="!canSubmitTwoFactor" />

                <p class="text-center text-sm">
                    <UButton variant="link" label="Log ind med en recovery code" @click="goToRecovery" />
                </p>
            </form>

            <UAlert v-else-if="error" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="error" />
        </div>

        <form v-else class="flex flex-col gap-4" @submit.prevent="loginWithRecoveryCode">
            <p class="text-sm">Indtast en af dine recovery codes for at logge ind.</p>

            <UFormField label="Recovery code" required>
                <UInput ref="recoveryInput" v-model="recoveryCode" icon="i-lucide-key-round" autocomplete="one-time-code" class="w-full" />
            </UFormField>

            <UAlert v-if="error" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="error" />

            <UButton type="submit" icon="i-lucide-log-in" label="Log ind" block :loading="isSubmitting" :disabled="!canSubmitRecovery" />

            <p class="text-center text-sm">
                <UButton variant="link" label="Tilbage til autentificeringskode" @click="goToTwoFactor" />
            </p>
        </form>
    </UCard>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, ref, watch } from "vue";
import { useRoute } from "vue-router";
import { startAssertion } from "@/lib/passkeys";
import AuthService from "@/Services/AuthService";

type FocusableInput = { inputRef?: HTMLInputElement } | null;

const route = useRoute();
const authService = new AuthService();

const step = ref<"login" | "twofactor" | "recovery">("login");

const emailInput = ref<FocusableInput>(null);
const recoveryInput = ref<FocusableInput>(null);

const email = ref("");
const userId = ref("");
const password = ref("");
const showPassword = ref(false);
const twoFactorDigits = ref<number[]>([]);
const twoFactorCode = computed(() => twoFactorDigits.value.join(""));
const recoveryCode = ref("");
const rememberMachine = ref(false);

const isSubmitting = ref(false);
const error = ref("");

const hasPasskeys = ref(false);
const authenticatorAvailable = ref(true);

const registered = computed(() => route.query.registered === "1");

// The backend only honours local paths (starting with "/" but not "//") and falls back to "/".
const returnUrl = computed(() => {
    const value = route.query.ReturnUrl ?? route.query.returnUrl;
    return typeof value === "string" && value.length > 0 ? value : "/beta/";
});

const canSubmitLogin = computed(() => email.value.trim() !== "" && password.value.length > 0);
const canSubmitTwoFactor = computed(() => /^[0-9]{6}$/.test(twoFactorCode.value));
const canSubmitRecovery = computed(() => recoveryCode.value.trim() !== "");

watch(step, () => {
    nextTick(() => {
        const target = step.value === "login" ? emailInput.value : step.value === "recovery" ? recoveryInput.value : null;
        target?.inputRef?.focus();
    });
});

onMounted(() => {
    emailInput.value?.inputRef?.focus();
});

async function login() {
    if (!canSubmitLogin.value || isSubmitting.value) return;

    isSubmitting.value = true;
    error.value = "";
    try {
        const result = await authService.login({ email: email.value, password: password.value, returnUrl: returnUrl.value });

        if (result.error) {
            error.value = result.error;
            return;
        }

        if (result.requiresTwoFactor) {
            hasPasskeys.value = result.passkeysAvailable ?? false;
            authenticatorAvailable.value = result.hasAuthenticator ?? true;
            userId.value = result.userId ?? "";
            step.value = "twofactor";
            return;
        }

        window.location.assign(result.returnUrl || "/beta/");
    } finally {
        isSubmitting.value = false;
    }
}

async function verifyTwoFactor() {
    if (!canSubmitTwoFactor.value || isSubmitting.value) return;

    isSubmitting.value = true;
    error.value = "";
    try {
        const result = await authService.twoFactorLogin({
            twoFactorCode: twoFactorCode.value,
            rememberMachine: rememberMachine.value,
            returnUrl: returnUrl.value
        });

        if (result.error) {
            error.value = result.error;
            twoFactorDigits.value = [];
            return;
        }

        window.location.assign(result.returnUrl || "/beta/");
    } finally {
        isSubmitting.value = false;
    }
}

async function verifyWithPasskey() {
    if (isSubmitting.value) return;

    isSubmitting.value = true;
    error.value = "";
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

        window.location.assign(result.returnUrl || "/beta/");
    } catch (cause) {
        error.value = (cause as DOMException)?.name === "NotAllowedError"
            ? "Login med passkey blev afbrudt."
            : "Der skete en uventet fejl ved login.";
    } finally {
        isSubmitting.value = false;
    }
}

async function loginWithRecoveryCode() {
    if (!canSubmitRecovery.value || isSubmitting.value) return;

    isSubmitting.value = true;
    error.value = "";
    try {
        const result = await authService.recoveryCodeLogin({ recoveryCode: recoveryCode.value, returnUrl: returnUrl.value });

        if (result.error) {
            error.value = result.error;
            return;
        }

        window.location.assign(result.returnUrl || "/beta/");
    } finally {
        isSubmitting.value = false;
    }
}

function goToRecovery() {
    error.value = "";
    step.value = "recovery";
}

function goToTwoFactor() {
    error.value = "";
    step.value = "twofactor";
}
</script>
