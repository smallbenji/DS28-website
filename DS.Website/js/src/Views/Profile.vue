<template>
    <Back icon="user" title="Min profil" />

    <div class="profile-scroll compact-scroll">
        <section class="section">
        <div class="columns is-centered">
            <div class="column is-6">

                <div class="card">
                    <header class="card-header">
                        <p class="card-header-title">Navn</p>
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
                        <BButton type="is-primary" :loading="isUpdatingName" @click="updateName">
                            Opdater navn
                        </BButton>
                    </div>
                </div>

                <div class="card">
                    <header class="card-header">
                        <p class="card-header-title">Skift adgangskode</p>
                    </header>
                    <div class="card-content">
                        <BField label="Nuværende adgangskode">
                            <BInput v-model="oldPassword" type="password" password-reveal />
                        </BField>
                        <BField label="Ny adgangskode">
                            <BInput v-model="newPassword" type="password" password-reveal />
                        </BField>
                        <BField label="Gentag ny adgangskode">
                            <BInput v-model="repeatPassword" type="password" password-reveal />
                        </BField>
                        <BButton type="is-primary" :loading="isChangingPassword" @click="changePassword">
                            Opdater adgangskode
                        </BButton>
                    </div>
                </div>

                <div class="card">
                    <header class="card-header">
                        <p class="card-header-title">Tofaktorautentificering</p>
                    </header>
                    <div class="card-content">
                        <div v-if="Status.twoFactorEnabled" class="notification is-success is-light">
                            Tofaktorautentificering er <strong>aktiv</strong>.
                            Du har <strong>{{ Status.recoveryCodesLeft }}</strong> recovery codes tilbage.
                        </div>
                        <div v-else class="notification is-info is-light">
                            Tofaktorautentificering er <strong>deaktiveret</strong>.
                            Ved at aktivere det, tilføjes et ekstra sikkerhedslag ved login med din autentificeringsapp.
                        </div>

                        <div class="buttons">
                            <BButton v-if="!Status.twoFactorEnabled" type="is-primary" icon-left="shield-halved" @click="openSetup">
                                Aktivér 2FA
                            </BButton>
                            <template v-else>
                                <BButton icon-left="rotate" @click="regenerate">
                                    Generér nye recovery codes
                                </BButton>
                                <BButton type="is-warning" icon-left="mobile-screen" @click="openReset">
                                    Nulstil autentificeringsnøgle
                                </BButton>
                                <BButton type="is-danger is-light" icon-left="shield-halved" @click="openDisable">
                                    Deaktiver 2FA
                                </BButton>
                            </template>
                        </div>
                    </div>
                </div>

            </div>
        </div>
        </section>
    </div>

    <TwoFactorSetupModal v-model:active="showSetup" @enabled="onEnabled" />
    <RecoveryCodesModal v-model:active="showRecoveryCodes" :codes="recoveryCodes" />

    <BModal v-model="showDisable" has-modal-card>
        <div class="modal-card">
            <header class="modal-card-head">
                <p class="modal-card-title">Deaktiver tofaktorautentificering</p>
            </header>
            <section class="modal-card-body">
                <p class="mb-3">
                    Indtast din nuværende adgangskode for at deaktivere tofaktorautentificering.
                </p>
                <BField label="Adgangskode">
                    <BInput v-model="disablePassword" type="password" password-reveal />
                </BField>
            </section>
            <footer class="modal-card-foot">
                <div class="buttons">
                    <BButton type="is-danger" :loading="isDisabling" @click="disable">
                        Deaktiver
                    </BButton>
                    <BButton @click="showDisable = false">Annuller</BButton>
                </div>
            </footer>
        </div>
    </BModal>

    <BModal v-model="showReset" has-modal-card>
        <div class="modal-card">
            <header class="modal-card-head">
                <p class="modal-card-title">Nulstil autentificeringsnøgle</p>
            </header>
            <section class="modal-card-body">
                <p>
                    Nulstilling af autentificeringsnøglen deaktiverer tofaktorautentificering,
                    så du skal sætte den op igen med din autentificeringsapp.
                </p>
            </section>
            <footer class="modal-card-foot">
                <div class="buttons">
                    <BButton type="is-warning" :loading="isResetting" @click="reset">
                        Nulstil og deaktiver
                    </BButton>
                    <BButton @click="showReset = false">Annuller</BButton>
                </div>
            </footer>
        </div>
    </BModal>
</template>
<script lang="ts" setup>
import { useAccountStore } from '@/Stores/AccountStore';
import { useMeStore } from '@/Stores/MeStore';
import { BButton, BField, BInput, BModal, useToast } from 'buefy';
import { onMounted, ref } from 'vue';
import { storeToRefs } from 'pinia';
import Back from '@/Components/Back.vue';
import TwoFactorSetupModal from '@/Components/Account/TwoFactorSetupModal.vue';
import RecoveryCodesModal from '@/Components/Account/RecoveryCodesModal.vue';

const Toast = useToast();
const accountStore = useAccountStore();
const meStore = useMeStore();
const { Status } = storeToRefs(accountStore);
const { Me } = storeToRefs(meStore);

const firstName = ref('');
const lastName = ref('');
const isUpdatingName = ref(false);

const oldPassword = ref('');
const newPassword = ref('');
const repeatPassword = ref('');
const isChangingPassword = ref(false);

const showSetup = ref(false);
const showRecoveryCodes = ref(false);
const recoveryCodes = ref<string[]>([]);

const showDisable = ref(false);
const disablePassword = ref('');
const isDisabling = ref(false);

const showReset = ref(false);
const isResetting = ref(false);

onMounted(() => {
    accountStore.GET_STATUS();
    firstName.value = Me.value.firstName ?? '';
    lastName.value = Me.value.lastName ?? '';
});

const updateName = async () => {
    if (!firstName.value.trim() || !lastName.value.trim()) {
        Toast.open({
            message: 'Fornavn og efternavn skal udfyldes',
            type: 'is-warning'
        });
        return;
    }

    isUpdatingName.value = true;

    try {
        const ok = await accountStore.UPDATE_NAME(firstName.value, lastName.value);
        if (ok) {
            Toast.open({
                message: 'Dit navn er blevet opdateret!',
                type: 'is-success'
            });
            await meStore.GET_ME();
        } else {
            Toast.open({
                message: 'Der skete en fejl under opdatering af dit navn',
                type: 'is-danger'
            });
        }
    } finally {
        isUpdatingName.value = false;
    }
};

const changePassword = async () => {
    if (newPassword.value !== repeatPassword.value) {
        Toast.open({
            message: 'Adgangskoderne er ikke ens',
            type: 'is-warning'
        });
        return;
    }

    isChangingPassword.value = true;

    try {
        const ok = await accountStore.CHANGE_PASSWORD(oldPassword.value, newPassword.value);
        if (ok) {
            Toast.open({
                message: 'Din adgangskode er blevet opdateret!',
                type: 'is-success'
            });
            oldPassword.value = '';
            newPassword.value = '';
            repeatPassword.value = '';
        } else {
            Toast.open({
                message: 'Der skete en fejl. Tjek din nuværende adgangskode.',
                type: 'is-danger'
            });
        }
    } finally {
        isChangingPassword.value = false;
    }
};

const openSetup = () => {
    showSetup.value = true;
};

const onEnabled = () => {
    Toast.open({
        message: 'Tofaktorautentificering er aktiveret!',
        type: 'is-success'
    });
};

const regenerate = async () => {
    const result = await accountStore.GENERATE_RECOVERY_CODES();
    if (result) {
        recoveryCodes.value = result.recoveryCodes;
        showRecoveryCodes.value = true;
    } else {
        Toast.open({
            message: 'Der skete en fejl under generering af recovery codes',
            type: 'is-danger'
        });
    }
};

const openDisable = () => {
    disablePassword.value = '';
    showDisable.value = true;
};

const disable = async () => {
    if (!disablePassword.value) {
        Toast.open({
            message: 'Indtast venligst din adgangskode',
            type: 'is-warning'
        });
        return;
    }

    isDisabling.value = true;

    try {
        const ok = await accountStore.DISABLE_2FA(disablePassword.value);
        if (ok) {
            Toast.open({
                message: 'Tofaktorautentificering er deaktiveret',
                type: 'is-success'
            });
            showDisable.value = false;
        } else {
            Toast.open({
                message: 'Forkert adgangskode',
                type: 'is-danger'
            });
        }
    } finally {
        isDisabling.value = false;
    }
};

const openReset = () => {
    showReset.value = true;
};

const reset = async () => {
    isResetting.value = true;

    try {
        const ok = await accountStore.RESET_AUTHENTICATOR();
        if (ok) {
            Toast.open({
                message: 'Autentificeringsnøgle er nulstillet',
                type: 'is-success'
            });
            showReset.value = false;
        } else {
            Toast.open({
                message: 'Der skete en fejl',
                type: 'is-danger'
            });
        }
    } finally {
        isResetting.value = false;
    }
};
</script>
<style lang="scss">
.card {
    margin-bottom: 1rem;
}

.profile-scroll {
    flex: 1;
    min-height: 0;
    overflow-y: auto;
}
</style>
