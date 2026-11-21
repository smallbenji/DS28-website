<template>
    <div class="flex min-h-svh items-center justify-center bg-muted p-4">
        <UCard class="w-full max-w-md">
            <template #header>
                <div class="flex items-center gap-2">
                    <UIcon name="i-lucide-shield-check" class="size-5 text-primary" />
                    <h1 class="font-semibold text-highlighted">Tofaktorautentificering påkrævet</h1>
                </div>
                <p class="mt-1 text-sm text-muted">Som SysAdmin skal du aktivere tofaktorautentificering, før du kan bruge applikationen.</p>
            </template>

            <template v-if="recoveryCodes.length === 0">
                <TwoFactorEnrollment submit-label="Aktivér tofaktorautentificering" @enabled="onEnabled" />

                <USeparator label="eller" class="my-5" />

                <div class="flex flex-col gap-2">
                    <p class="text-center text-sm text-muted">Har du en passkey? Du kan aktivere tofaktorautentificering med den i stedet for din autentificeringsapp.</p>
                    <UAlert v-if="passkeyError" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="passkeyError" />
                    <UButton color="neutral" variant="outline" icon="i-lucide-fingerprint" label="Aktivér med passkey" :loading="passkeyVerifying" block @click="enableWithPasskey" />
                </div>
            </template>

            <template v-else>
                <UAlert class="mb-4" color="success" variant="subtle" icon="i-lucide-shield-check" title="Tofaktorautentificering er nu aktiveret." />
                <RecoveryCodesList :codes="recoveryCodes" />
                <UButton class="mt-4" color="success" label="Jeg har gemt koderne" block @click="finish" />
            </template>

            <template #footer>
                <div class="flex justify-center">
                    <UButton color="error" variant="link" label="Log ud" @click="logout" />
                </div>
            </template>
        </UCard>
    </div>
</template>

<script setup lang="ts">
import { ref } from "vue";
import { useRouter } from "vue-router";
import { useAccountStore } from "@/Stores/AccountStore";
import { useMeStore } from "@/Stores/MeStore";
import AuthService from "@/Services/AuthService";
import TwoFactorEnrollment from "@/Components/Account/TwoFactorEnrollment.vue";
import RecoveryCodesList from "@/Components/Account/RecoveryCodesList.vue";

const router = useRouter();
const accountStore = useAccountStore();
const meStore = useMeStore();
const authService = new AuthService();

const recoveryCodes = ref<string[]>([]);
const passkeyVerifying = ref(false);
const passkeyError = ref("");

function onEnabled(codes: string[]) {
    recoveryCodes.value = codes;
}

async function enableWithPasskey() {
    if (passkeyVerifying.value || !meStore.Me) return;

    passkeyVerifying.value = true;
    passkeyError.value = "";
    try {
        const result = await accountStore.CREATE_PASSKEY(meStore.Me.name);
        if (result?.recoveryCodes.length) recoveryCodes.value = result.recoveryCodes;
        else if (result) await finish();
        else passkeyError.value = "Der skete en fejl under aktiveringen af din passkey.";
    } catch (cause) {
        passkeyError.value = (cause as DOMException)?.name === "NotAllowedError"
            ? "Aktivering blev afbrudt af brugeren."
            : "Der skete en uventet fejl under aktiveringen af din passkey.";
    } finally {
        passkeyVerifying.value = false;
    }
}

async function finish() {
    await meStore.GET_ME();
    await router.push("/");
}

async function logout() {
    await authService.logout();
    window.location.href = "/beta/login";
}
</script>
