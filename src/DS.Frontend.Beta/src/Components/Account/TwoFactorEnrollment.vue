<template>
    <div v-if="!setup" class="flex flex-col items-center gap-3 py-6">
        <USkeleton class="size-48" />
        <p class="text-sm text-muted">Indlæser…</p>
    </div>

    <form v-else class="flex flex-col gap-4" @submit.prevent="verify">
        <p class="text-sm">
            Scan QR-koden med din autentificeringsapp (fx Google Authenticator, Microsoft Authenticator eller 1Password).
            Kan du ikke scanne, kan du indtaste nøglen manuelt.
        </p>
        <div class="flex justify-center">
            <img :src="qrUrl" alt="QR-kode til autentificeringsappen" class="size-48 rounded-md bg-white p-2" />
        </div>
        <p class="text-center font-mono text-xs break-all text-muted">{{ setup.manualEntryKey }}</p>
        <UFormField label="Verificér kode" :error="invalid ? 'Den indtastede kode var ikke gyldig.' : undefined">
            <UInput ref="codeInput" v-model="code" placeholder="123456" autocomplete="one-time-code" inputmode="numeric" class="w-full" />
        </UFormField>
        <UButton type="submit" :label="submitLabel" :loading="verifying" :disabled="!code.trim()" block />
    </form>
</template>

<script setup lang="ts">
import { nextTick, onMounted, ref } from "vue";
import { useAccountStore } from "@/Stores/AccountStore";
import type { TwoFactorSetupDto } from "@/types";

withDefaults(defineProps<{ submitLabel?: string }>(), { submitLabel: "Verificér og aktivér" });
const emit = defineEmits<{ enabled: [recoveryCodes: string[]] }>();

const accountStore = useAccountStore();

const setup = ref<TwoFactorSetupDto | null>(null);
const code = ref("");
const verifying = ref(false);
const invalid = ref(false);
const codeInput = ref<{ inputRef?: HTMLInputElement } | null>(null);
const qrUrl = `/api/v1/account/2fa/qr?ts=${Date.now()}`;

onMounted(async () => {
    setup.value = await accountStore.GET_SETUP();
    await nextTick();
    codeInput.value?.inputRef?.focus();
});

async function verify() {
    if (!code.value.trim() || verifying.value) return;

    verifying.value = true;
    invalid.value = false;
    try {
        const result = await accountStore.ENABLE_2FA(code.value);
        if (result) emit("enabled", result.recoveryCodes);
        else invalid.value = true;
    } finally {
        verifying.value = false;
    }
}
</script>
