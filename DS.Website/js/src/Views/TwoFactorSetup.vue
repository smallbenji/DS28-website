<template>
    <div class="twofactor-setup">
        <div class="card frame">
            <header class="card-header">
                <p class="card-header-title is-centered">Tofaktorautentificering påkrævet</p>
            </header>

            <div class="card-content">
                <p class="mb-4">
                    Som SysAdmin skal du aktivere tofaktorautentificering, før du kan bruge applikationen.
                </p>

                <template v-if="setup && !enabled">
                    <div class="has-text-centered mb-3">
                        <img :src="qrUrl" alt="QR-kode til autentificeringsappen"
                            style="width: 200px; height: 200px;" />
                    </div>

                    <p class="has-text-centered is-family-monospace is-size-7 has-text-grey mb-4">
                        {{ setup.manualEntryKey }}
                    </p>

                    <BField label="Verificér kode">
                        <BInput ref="codeInput" v-model="code" placeholder="123456" autocomplete="one-time-code"
                            @keyup.enter="verify" />
                    </BField>

                    <p v-if="error" class="help is-danger">
                        Den indtastede kode var ikke gyldig.
                    </p>

                    <BButton type="is-primary" expanded :loading="isVerifying" @click="verify">
                        Aktivér tofaktorautentificering
                    </BButton>
                </template>

                <template v-if="setup && !enabled">
                    <p class="has-text-centered has-text-grey mb-3">
                        Har du en passkey? Du kan aktivere tofaktorautentificering med den i stedet for din
                        autentificeringsapp.
                    </p>

                    <p v-if="passkeyError" class="help is-danger has-text-centered mb-2">
                        {{ passkeyError }}
                    </p>

                    <BButton type="is-primary is-light" 
                            outlined
                            rounded
                            expanded 
                            :loading="isPasskeyVerifying" 
                            icon-left="fingerprint"
                            @click="enableWithPasskey">
                        Aktivér med passkey
                    </BButton>
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
                            <span v-for="recoveryCode in recoveryCodes" :key="recoveryCode"
                                class="tag is-warning is-light is-medium is-family-monospace">
                                {{ recoveryCode }}
                            </span>
                        </div>
                    </div>

                    <BButton type="is-success" expanded @click="finish">
                        Jeg har gemt koderne
                    </BButton>
                </template>

                <p v-else-if="!setup" class="has-text-grey">
                    Indlæser...
                </p>

                <hr />

                <p class="has-text-centered">
                    <a class="has-text-danger" @click="logout">Log ud</a>
                </p>
            </div>
        </div>
    </div>
</template>

<script lang="ts" setup>
import { useAccountStore } from '@/Stores/AccountStore';
import AuthService from '@/Services/AuthService';
import { BButton, BField, BInput } from 'buefy';
import { computed, nextTick, onMounted, ref, watch } from 'vue';
import { useRouter } from 'vue-router';
import type { TwoFactorSetupDto } from '@/types';
import { useMeStore } from '@/Stores/MeStore';

const router = useRouter();
const accountStore = useAccountStore();
const meStore = useMeStore();
const authService = new AuthService();

const setup = ref<TwoFactorSetupDto | null>(null);
const code = ref('');
const enabled = ref(false);
const recoveryCodes = ref<string[]>([]);
const isVerifying = ref(false);
const error = ref(false);
const isPasskeyVerifying = ref(false);
const passkeyError = ref('');


const codeInput = ref<InstanceType<typeof BInput> | null>(null);

const qrUrl = computed(() => `/api/v1/account/2fa/qr?ts=${Date.now()}`);

onMounted(async () => {
    setup.value = await accountStore.GET_SETUP();
});

watch(setup, () => {
    nextTick(() => codeInput.value?.focus());
});

const verify = async () => {
    if (!code.value.trim() || isVerifying.value) return;

    isVerifying.value = true;
    error.value = false;

    try {
        const result = await accountStore.ENABLE_2FA(code.value);
        if (result) {
            enabled.value = true;
            recoveryCodes.value = result.recoveryCodes;
        } else {
            error.value = true;
        }
    } finally {
        isVerifying.value = false;
    }
};

const finish = () => {
    router.push("/");
};

const logout = async () => {
    await authService.logout();
    window.location.href = "/login";
};

const enableWithPasskey = async () => {
    if (isPasskeyVerifying.value) return;
    isPasskeyVerifying.value = true;
    passkeyError.value = '';


    try {
        const result = await accountStore.CREATE_PASSKEY(meStore.Me.name);
        if (result) {
            enabled.value = true;
            recoveryCodes.value = result.recoveryCodes;
        } else {
            passkeyError.value = "Der skete en fejl under aktiveringen af din passkey."
        }
    } catch (e) {
        passkeyError.value = (e as DOMException)?.name === "NotAllowedError"
            ? "Aktivering blev afbrudt af brugeren."
            : 'Der skete en uventet fejl under aktiveringen af din passkey.'
    } finally {
        isPasskeyVerifying.value = false;
    }
}
</script>

<style scoped>
.twofactor-setup {
    flex: 1;
    min-height: 0;
    display: flex;
    overflow-y: auto;
    padding: 1rem;
}

.frame {
    min-width: 350px;
    margin: auto;
}
</style>
