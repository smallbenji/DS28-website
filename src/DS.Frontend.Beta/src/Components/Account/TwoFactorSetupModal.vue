<template>
    <UModal v-model:open="open" title="Aktivér tofaktorautentificering" :dismissible="recoveryCodes.length === 0" :close="recoveryCodes.length === 0">
        <template #body>
            <TwoFactorEnrollment v-if="recoveryCodes.length === 0" @enabled="onEnabled" />
            <template v-else>
                <UAlert class="mb-4" color="success" variant="subtle" icon="i-lucide-shield-check" title="Tofaktorautentificering er nu aktiveret." />
                <RecoveryCodesList :codes="recoveryCodes" />
            </template>
        </template>
        <template #footer>
            <div class="flex w-full justify-end">
                <UButton v-if="recoveryCodes.length" label="Jeg har gemt koderne" @click="open = false" />
                <UButton v-else color="neutral" variant="outline" label="Annuller" @click="open = false" />
            </div>
        </template>
    </UModal>
</template>

<script setup lang="ts">
import { ref, watch } from "vue";
import TwoFactorEnrollment from "@/Components/Account/TwoFactorEnrollment.vue";
import RecoveryCodesList from "@/Components/Account/RecoveryCodesList.vue";

const open = defineModel<boolean>("open", { required: true });
const emit = defineEmits<{ enabled: [] }>();

const recoveryCodes = ref<string[]>([]);

watch(open, (isOpen) => {
    if (isOpen) recoveryCodes.value = [];
});

function onEnabled(codes: string[]) {
    recoveryCodes.value = codes;
    emit("enabled");
}
</script>
