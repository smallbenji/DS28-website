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
            <UPinInput
                v-model="digits"
                :length="6"
                type="number"
                otp
                autofocus
                size="xl"
                :disabled="verifying"
                aria-label="Verificér kode"
                class="w-full justify-between"
                @complete="verify"
            />
        </UFormField>
        <UButton type="submit" :label="submitLabel" :loading="verifying" :disabled="!isComplete" block />
    </form>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useAccountStore } from "@/Stores/AccountStore";
import type { TwoFactorSetupDto } from "@/types";

withDefaults(defineProps<{ submitLabel?: string }>(), { submitLabel: "Verificér og aktivér" });
const emit = defineEmits<{ enabled: [recoveryCodes: string[]] }>();

const accountStore = useAccountStore();

const setup = ref<TwoFactorSetupDto | null>(null);
const digits = ref<number[]>([]);
const code = computed(() => digits.value.join(""));
const isComplete = computed(() => /^[0-9]{6}$/.test(code.value));
const verifying = ref(false);
const invalid = ref(false);
const qrUrl = `/api/v1/account/2fa/qr?ts=${Date.now()}`;

onMounted(async () => {
    setup.value = await accountStore.GET_SETUP();
});

async function verify() {
    if (!isComplete.value || verifying.value) return;

    verifying.value = true;
    invalid.value = false;
    try {
        const result = await accountStore.ENABLE_2FA(code.value);
        if (result) {
            emit("enabled", result.recoveryCodes);
        } else {
            invalid.value = true;
            digits.value = [];
        }
    } finally {
        verifying.value = false;
    }
}
</script>
