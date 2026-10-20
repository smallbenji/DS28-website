<template>
    <BModal :model-value="active" @update:model-value="$emit('update:active', $event)" has-modal-card>
        <div class="modal-card">
            <header class="modal-card-head">
                <p class="modal-card-title">Aktivér tofaktorautentificering</p>
                <button type="button" class="delete" @click="close" />
            </header>
            <section class="modal-card-body">
                <template v-if="setup && !enabled">
                    <p class="mb-3">
                        Scan QR-koden med din autentificeringsapp
                        (fx Google Authenticator, Microsoft Authenticator eller 1Password).
                        Kan du ikke scanne, kan du indtaste nøglen manuelt.
                    </p>

                    <div class="has-text-centered mb-3">
                        <img :src="qrUrl" alt="QR-kode til autentificeringsappen" style="width: 200px; height: 200px;" />
                    </div>

                    <p class="has-text-centered is-family-monospace is-size-7 has-text-grey mb-4">
                        {{ setup.manualEntryKey }}
                    </p>

                    <BField label="Verificér kode">
                        <BInput v-model="code" placeholder="123456" autocomplete="one-time-code" />
                    </BField>

                    <p v-if="error" class="help is-danger">
                        Den indtastede kode var ikke gyldig.
                    </p>
                </template>

                <template v-else-if="enabled && recoveryCodes.length">
                    <div class="notification is-success is-light">
                        Tofaktorautentificering er nu aktiveret.
                    </div>
                    <div class="notification is-warning is-light">
                        <p class="mb-2">
                            Gem disse recovery codes et sikkert sted. De vises kun én gang og kan bruges
                            til at logge ind, hvis du mister adgang til din autentificeringsapp.
                        </p>
                        <div class="tags">
                            <span v-for="code in recoveryCodes" :key="code" class="tag is-warning is-light is-medium is-family-monospace">
                                {{ code }}
                            </span>
                        </div>
                    </div>
                </template>

                <p v-else-if="!setup" class="has-text-grey">
                    Indlæser...
                </p>
            </section>
            <footer class="modal-card-foot">
                <div class="buttons">
                    <template v-if="!enabled">
                        <BButton type="is-primary" :loading="isVerifying" @click="verify">
                            Verificér og aktivér
                        </BButton>
                        <BButton @click="close">Annuller</BButton>
                    </template>
                    <BButton v-else type="is-primary" @click="finish">
                        Jeg har gemt koderne
                    </BButton>
                </div>
            </footer>
        </div>
    </BModal>
</template>
<script setup lang="ts">
import { useAccountStore } from '@/Stores/AccountStore';
import { BButton, BField, BInput, BModal, useToast } from 'buefy';
import { computed, ref, watch } from 'vue';
import type { TwoFactorSetupDto } from '@/types';

const props = defineProps<{
    active: boolean;
}>();

const emit = defineEmits<{
    (e: 'update:active', value: boolean): void;
    (e: 'enabled'): void;
}>();

const Toast = useToast();
const accountStore = useAccountStore();

const setup = ref<TwoFactorSetupDto | null>(null);
const code = ref('');
const enabled = ref(false);
const recoveryCodes = ref<string[]>([]);
const isVerifying = ref(false);
const error = ref(false);

const qrUrl = computed(() => `/api/v1/account/2fa/qr?ts=${Date.now()}`);

watch(() => props.active, (isActive) => {
    if (isActive) {
        setup.value = null;
        code.value = '';
        enabled.value = false;
        recoveryCodes.value = [];
        error.value = false;

        accountStore.GET_SETUP().then((data) => {
            setup.value = data;
        });
    }
});

const verify = async () => {
    if (!code.value.trim()) {
        Toast.open({
            message: 'Indtast venligst koden',
            type: 'is-warning'
        });
        return;
    }

    isVerifying.value = true;
    error.value = false;

    try {
        const result = await accountStore.ENABLE_2FA(code.value);
        if (result) {
            enabled.value = true;
            recoveryCodes.value = result.recoveryCodes;
            emit('enabled');
        } else {
            error.value = true;
        }
    } finally {
        isVerifying.value = false;
    }
};

const close = () => {
    emit('update:active', false);
};

const finish = () => {
    close();
};
</script>
<style lang="scss">
.modal-card {
    .modal-card-head,
    .modal-card-title,
    .modal-card-body,
    .modal-card-foot {
        color: #363636;
    }
}
</style>
