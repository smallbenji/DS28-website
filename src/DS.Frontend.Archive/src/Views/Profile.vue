<template>
    <div v-if="Me" class="profile-scroll compact-scroll">
        <div class="profile-page">
            <section class="profile-header">
                <UserAvatar :user="Me" size="large" />

                <div class="profile-identity">
                    <p class="title is-4">{{ Me.firstName }} {{ Me.lastName }}</p>
                    <p class="subtitle is-6 has-text-grey">
                        <template v-if="Me.phone">
                            <BIcon icon="phone" size="is-small" />
                            {{ Me.phone }}
                        </template>
                        <template v-else>Intet telefonnummer</template>
                    </p>
                </div>

                <div class="profile-picture-actions">
                    <div class="buttons is-right">
                        <ProfilePictureUpload :has-picture="!!Me.profilePicture" :disabled="isUpdatingPicture"
                            @confirm="updatePicture" />
                        <BButton v-if="Me.profilePicture" type="is-danger is-light"
                            :disabled="isUpdatingPicture" @click="removePicture">
                            Fjern
                        </BButton>
                    </div>
                </div>
            </section>

            <div class="profile-grid">
                <div class="card" :class="{ 'profile-grid-wide': !hasNotificationOptions }">
                    <header class="card-header">
                        <p class="card-header-title">Profil og sikkerhed</p>
                    </header>
                    <div class="card-content">
                        <div class="profile-summary">
                            <div class="profile-summary-row">
                                <span class="has-text-grey">Navn</span>
                                <span class="profile-summary-value">
                                    <span>{{ Me.firstName }} {{ Me.lastName }}</span>
                                    <BButton size="is-small" type="is-primary is-light" icon-left="user-pen"
                                        @click="openNameModal">
                                        Rediger
                                    </BButton>
                                </span>
                            </div>
                            <div class="profile-summary-row">
                                <span class="has-text-grey">Email</span>
                                <span class="profile-summary-value">{{ Me.email }}</span>
                            </div>
                            <div class="profile-summary-row">
                                <span class="has-text-grey">Telefon</span>
                                <span class="profile-summary-value">
                                    <span>{{ Me.phone || 'Intet telefonnummer' }}</span>
                                    <BButton size="is-small" type="is-primary is-light" icon-left="phone"
                                        @click="openPhoneModal">
                                        Rediger
                                    </BButton>
                                </span>
                            </div>
                        </div>

                        <hr />

                        <p class="mb-2">
                            Hold din konto sikker ved at skifte din adgangskode.
                        </p>
                        <BButton icon-left="key" @click="openPasswordModal">
                            Skift adgangskode
                        </BButton>
                    </div>
                </div>

                <div v-if="hasNotificationOptions" class="card">
                    <header class="card-header">
                        <p class="card-header-title">Notifikationer</p>
                    </header>
                    <div class="card-content">
                        <BField v-for="notification in notifications" :key="notification.type">
                            <BCheckbox v-model="notification.subscribed"
                                :disabled="!notification.canSubscribe">
                                {{ notificationLabels[notification.type] ?? notification.type }}
                            </BCheckbox>
                        </BField>
                        <BButton type="is-primary" :loading="isUpdatingNotifications" @click="updateNotifications">
                            Gem notifikationer
                        </BButton>
                    </div>
                </div>

                <div v-if="Me.roles.includes('McpUser')" class="card profile-grid-wide">
                    <header class="card-header">
                        <p class="card-header-title">Forbind en AI-assistent</p>
                    </header>
                    <div class="card-content">
                        <p class="mb-3">Forbind din AI-assistent til DS28 via MCP. Assistenten får adgang til tavler på dine vegne med dine eksisterende rettigheder.</p>
                        <BField label="MCP-adresse">
                            <BInput :model-value="mcpUrl" readonly @focus="selectMcpUrl" />
                        </BField>
                        <div class="buttons">
                            <BButton type="is-primary" @click="mcpClient = 'Claude'">Tilføj til Claude</BButton>
                            <BButton type="is-primary" @click="mcpClient = 'ChatGPT'">Tilføj til ChatGPT</BButton>
                            <BButton icon-left="copy" @click="copyMcpUrl">Kopiér MCP-adresse</BButton>
                        </div>
                        <p class="has-text-grey">Du skal godkende forbindelsen og logge ind med din DS28-konto. Din konto skal have rollen McpUser.</p>
                    </div>
                </div>

                <div class="card profile-grid-wide">
                    <header class="card-header">
                        <p class="card-header-title">Tofaktorautentificering</p>
                    </header>
                    <div class="card-content">
                        <div class="tfa-status">
                            <BIcon icon="shield-halved" size="is-medium"
                                :class="Status.twoFactorEnabled ? 'has-text-success' : 'has-text-grey'" />
                            <div class="tfa-status-text">
                                <p class="has-text-weight-medium">
                                    Tofaktorautentificering er {{ Status.twoFactorEnabled ? 'aktiv' : 'deaktiveret' }}
                                </p>
                                <p class="has-text-grey">
                                    <template v-if="Status.twoFactorEnabled">
                                        Du har {{ Status.recoveryCodesLeft }} recovery codes tilbage.
                                    </template>
                                    <template v-else>
                                        Ved at aktivere det, tilføjes et ekstra sikkerhedslag ved login med din
                                        autentificeringsapp.
                                    </template>
                                </p>
                            </div>
                            <span class="tag" :class="Status.twoFactorEnabled ? 'is-success is-light' : 'is-light'">
                                {{ Status.twoFactorEnabled ? 'Aktiv' : 'Deaktiveret' }}
                            </span>
                        </div>

                        <hr />

                        <template v-if="Status.twoFactorEnabled && passkeys != null && passkeys.length > 0">
                            <div class="tfa-section-head">
                                <h3 class="title is-5">Dine passkeys</h3>
                                <BButton type="is-primary" size="is-small" icon-left="plus" @click="addPasskey">
                                    Tilføj passkey
                                </BButton>
                            </div>

                            <div class="tfa-passkeys">
                                <div v-for="passkey in passkeys" :key="passkey.id" class="tfa-passkey">
                                    <div class="tfa-passkey-info">
                                        <BIcon icon="key" />
                                        <div>
                                            <p class="has-text-weight-medium">{{ passkey.name }}</p>
                                            <p class="has-text-grey">Tilføjet {{ formatDate(passkey.createdAt) }}</p>
                                        </div>
                                    </div>
                                    <BButton type="is-danger is-light" size="is-small" icon-left="trash"
                                        @click="deletePasskey(passkey.id)">
                                        Fjern
                                    </BButton>
                                </div>
                            </div>
                        </template>
                        <div v-else-if="Status.twoFactorEnabled" class="tfa-passkey-row">
                            <p class="has-text-grey">Passkeys er ikke aktiveret.</p>
                            <BButton size="is-small" type="is-primary is-light" icon-left="plus"
                                @click="addPasskey">
                                Tilføj passkey
                            </BButton>
                        </div>

                        <hr />

                        <div class="buttons">
                            <BButton v-if="!Status.twoFactorEnabled" type="is-primary" icon-left="shield-halved"
                                @click="openSetup">
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

                <div class="card profile-grid-wide">
                    <header class="card-header">
                        <p class="card-header-title">Adgangstokens</p>
                    </header>
                    <div class="card-content">
                        <p class="mb-3">
                            Adgangstokens bruges af værktøjer og scripts, så de kan læse og skrive i DS28 uden din
                            adgangskode. Behandl dem som adgangskoder, og tilbagekald dem, du ikke bruger.
                        </p>

                        <p v-if="tokens.length === 0" class="has-text-grey mb-3">
                            Du har ingen aktive adgangstokens.
                        </p>
                        <div v-for="token in tokens" :key="token.id" class="tfa-passkey">
                            <div class="tfa-passkey-info">
                                <BIcon icon="key" />
                                <div>
                                    <p class="has-text-weight-medium">{{ token.name }}</p>
                                    <p class="has-text-grey">
                                        Oprettet {{ formatTokenDate(token.createdAt) }}
                                        <template v-if="token.expiresAt">
                                            · udløber {{ formatTokenDate(token.expiresAt) }}
                                        </template>
                                    </p>
                                </div>
                            </div>
                            <BButton type="is-danger is-light" size="is-small" icon-left="trash"
                                @click="revokeToken(token.id)">
                                Tilbagekald
                            </BButton>
                        </div>

                        <div class="buttons mt-3">
                            <BButton type="is-primary" icon-left="plus" @click="openCreateToken">
                                Opret token
                            </BButton>
                        </div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <BModal v-model="showName" has-modal-card>
        <div class="modal-card">
            <header class="modal-card-head">
                <p class="modal-card-title">Rediger navn</p>
            </header>
            <section class="modal-card-body">
                <BField label="Fornavn">
                    <BInput v-model="firstName" />
                </BField>
                <BField label="Efternavn">
                    <BInput v-model="lastName" />
                </BField>
            </section>
            <footer class="modal-card-foot">
                <div class="buttons">
                    <BButton type="is-primary" :loading="isUpdatingName" @click="updateName">
                        Gem
                    </BButton>
                    <BButton @click="showName = false">Annuller</BButton>
                </div>
            </footer>
        </div>
    </BModal>

    <BModal v-model="showPhone" has-modal-card>
        <div class="modal-card">
            <header class="modal-card-head">
                <p class="modal-card-title">Rediger telefon</p>
            </header>
            <section class="modal-card-body">
                <BField label="Telefon">
                    <BInput v-model="phone" type="tel" />
                </BField>
            </section>
            <footer class="modal-card-foot">
                <div class="buttons">
                    <BButton type="is-primary" :loading="isUpdatingPhone" @click="updatePhone">
                        Gem
                    </BButton>
                    <BButton @click="showPhone = false">Annuller</BButton>
                </div>
            </footer>
        </div>
    </BModal>

    <BModal v-model="showPassword" has-modal-card>
        <div class="modal-card">
            <header class="modal-card-head">
                <p class="modal-card-title">Skift adgangskode</p>
            </header>
            <section class="modal-card-body">
                <BField label="Nuværende adgangskode">
                    <BInput v-model="oldPassword" type="password" password-reveal />
                </BField>
                <BField label="Ny adgangskode">
                    <BInput v-model="newPassword" type="password" password-reveal />
                </BField>
                <BField label="Gentag ny adgangskode">
                    <BInput v-model="repeatPassword" type="password" password-reveal />
                </BField>
            </section>
            <footer class="modal-card-foot">
                <div class="buttons">
                    <BButton type="is-primary" :loading="isChangingPassword" @click="changePassword">
                        Gem
                    </BButton>
                    <BButton @click="showPassword = false">Annuller</BButton>
                </div>
            </footer>
        </div>
    </BModal>

    <BModal v-model="mcpSetupOpen" has-modal-card>
        <div class="modal-card">
            <header class="modal-card-head">
                <p class="modal-card-title">Tilføj til {{ mcpClient }}</p>
            </header>
            <section class="modal-card-body">
                <p class="mb-3" v-if="mcpClient === 'Claude'">Åbn Claude, gå til Connectors, og vælg Add custom connector. Angiv DS28 som navn og indsæt MCP-adressen nedenfor.</p>
                <p class="mb-3" v-else>Åbn ChatGPTs indstillinger for apps, og opret en forbindelse til en egen MCP-server med navnet DS28 og adressen nedenfor. Opsætningen kan kræve udviklertilstand og tilladelse fra din workspace-administrator.</p>
                <BField label="MCP-adresse">
                    <BInput :model-value="mcpUrl" readonly @focus="selectMcpUrl" />
                </BField>
                <p>Vælg OAuth, hvis du bliver spurgt om godkendelsesmetode, og log ind med din DS28-konto. Hvis klienten kræver et klient-id og en klienthemmelighed, skal en systemadministrator oprette en OAuth-klient til den pågældende assistent.</p>
            </section>
            <footer class="modal-card-foot">
                <div class="buttons">
                    <BButton type="is-primary" tag="a" :href="mcpClient === 'Claude' ? 'https://claude.ai/settings/connectors' : 'https://chatgpt.com/#settings/Connectors'" target="_blank" rel="noopener noreferrer">Åbn {{ mcpClient }} (ny fane)</BButton>
                    <BButton icon-left="copy" @click="copyMcpUrl">Kopiér MCP-adresse</BButton>
                    <BButton @click="mcpClient = null">Luk</BButton>
                </div>
            </footer>
        </div>
    </BModal>

    <TwoFactorSetupModal v-model:active="showSetup" @enabled="onEnabled" />
    <RecoveryCodesModal v-model:active="showRecoveryCodes" :codes="recoveryCodes" />

    <BModal v-model="showCreateToken" has-modal-card>
        <div class="modal-card">
            <header class="modal-card-head">
                <p class="modal-card-title">Opret adgangstoken</p>
            </header>
            <section class="modal-card-body">
                <BField label="Navn">
                    <BInput v-model="tokenName" placeholder="Fx MCP eller CI" maxlength="64" />
                </BField>
                <BField label="Udløber om">
                    <BSelect v-model="tokenExpiryDays" expanded>
                        <option :value="30">30 dage</option>
                        <option :value="90">90 dage</option>
                        <option :value="365">1 år</option>
                    </BSelect>
                </BField>
            </section>
            <footer class="modal-card-foot">
                <div class="buttons">
                    <BButton type="is-primary" :loading="isCreatingToken" @click="createToken">
                        Opret
                    </BButton>
                    <BButton @click="showCreateToken = false">Annuller</BButton>
                </div>
            </footer>
        </div>
    </BModal>

    <BModal v-model="showCreatedToken" has-modal-card>
        <div class="modal-card">
            <header class="modal-card-head">
                <p class="modal-card-title">Dit nye adgangstoken</p>
            </header>
            <section class="modal-card-body">
                <p class="mb-3">
                    Kopiér token nu. Du kan ikke se det igen. Brug det i stedet for din adgangskode i værktøjet.
                </p>
                <BField label="Token">
                    <BInput :model-value="createdToken" readonly @focus="selectToken" />
                </BField>
            </section>
            <footer class="modal-card-foot">
                <div class="buttons">
                    <BButton type="is-primary" icon-left="copy" @click="copyToken">Kopiér</BButton>
                    <BButton @click="showCreatedToken = false">Luk</BButton>
                </div>
            </footer>
        </div>
    </BModal>

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
import { BButton, BField, BIcon, BInput, BModal, BSelect, useToast, BCheckbox } from 'buefy';
import { computed, onMounted, ref } from 'vue';
import { storeToRefs } from 'pinia';
import TwoFactorSetupModal from '@/Components/Account/TwoFactorSetupModal.vue';
import RecoveryCodesModal from '@/Components/Account/RecoveryCodesModal.vue';
import UserAvatar from '@/Components/User/UserAvatar.vue';
import ProfilePictureUpload from '@/Components/User/ProfilePictureUpload.vue';
import NotificationService from '@/Services/NotificationService';
import AccessTokenService from '@/Services/AccessTokenService';
import FileService from '@/Services/FileService';
import type { AccessTokenDto, NotificationPreferenceDto, PasskeyDto } from '@/types';
import { formatDate } from '@/lib/util';

function selectToken(event?: Event) {
    if (event?.target instanceof HTMLInputElement) event.target.select();
}

const Toast = useToast();
const accountStore = useAccountStore();
const meStore = useMeStore();
const notificationService = new NotificationService();
const accessTokenService = new AccessTokenService();
const fileService = new FileService();
const { Status } = storeToRefs(accountStore);
const { Me } = storeToRefs(meStore);
const mcpUrl = new URL('/mcp', window.location.origin).href;
const mcpClient = ref<'Claude' | 'ChatGPT' | null>(null);
const mcpSetupOpen = computed({
    get: () => mcpClient.value !== null,
    set: (open: boolean) => {
        if (!open) mcpClient.value = null;
    }
});

const selectMcpUrl = (event?: Event) => {
    if (event?.target instanceof HTMLInputElement) event.target.select();
};

const copyMcpUrl = async () => {
    try {
        await navigator.clipboard.writeText(mcpUrl);
        Toast.open({ message: 'MCP-adressen er kopieret', type: 'is-success' });
    } catch {
        Toast.open({ message: 'Kunne ikke kopiere adressen. Markér og kopiér den fra feltet.', type: 'is-danger' });
    }
};

const notificationLabels: Record<string, string> = {
    NewUser: 'Ny bruger oprettet'
};

const notifications = ref<NotificationPreferenceDto[]>([]);
const isUpdatingNotifications = ref(false);
const hasNotificationOptions = computed(() => notifications.value.some((notification) => notification.canSubscribe));

const firstName = ref('');
const lastName = ref('');
const phone = ref('');
const isUpdatingName = ref(false);
const passkeys = ref<PasskeyDto[] | null>(null);

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

const isUpdatingPicture = ref(false);

const tokens = ref<AccessTokenDto[]>([]);
const showCreateToken = ref(false);
const showCreatedToken = ref(false);
const isCreatingToken = ref(false);
const tokenName = ref('');
const tokenExpiryDays = ref(90);
const createdToken = ref('');

const showName = ref(false);
const showPhone = ref(false);
const isUpdatingPhone = ref(false);
const showPassword = ref(false);

const updatePicture = async (file: File) => {
    isUpdatingPicture.value = true;

    try {
        const uploaded = await fileService.uploadProfilePicture(file);
        const ready = await fileService.waitUntilReady(uploaded.publicId);

        if (!ready) {
            Toast.open({
                message: 'Billedet kunne ikke behandles. Prøv igen.',
                type: 'is-danger'
            });
            return;
        }

        const ok = await meStore.UPDATE_PROFILE_PICTURE(uploaded.publicId);
        Toast.open({
            message: ok
                ? 'Dit profilbillede er blevet opdateret!'
                : 'Der skete en fejl under opdatering af dit profilbillede',
            type: ok ? 'is-success' : 'is-danger'
        });
    } catch {
        Toast.open({
            message: 'Der skete en fejl under upload af billedet',
            type: 'is-danger'
        });
    } finally {
        isUpdatingPicture.value = false;
    }
};

const removePicture = async () => {
    isUpdatingPicture.value = true;

    try {
        const ok = await meStore.UPDATE_PROFILE_PICTURE(null);
        Toast.open({
            message: ok
                ? 'Dit profilbillede er blevet fjernet!'
                : 'Der skete en fejl under fjernelse af dit profilbillede',
            type: ok ? 'is-success' : 'is-danger'
        });
    } finally {
        isUpdatingPicture.value = false;
    }
};

onMounted(() => {
    accountStore.GET_STATUS();
    firstName.value = Me.value?.firstName ?? '';
    lastName.value = Me.value?.lastName ?? '';
    phone.value = Me.value?.phone ?? '';
    passkeys.value = Me.value?.passkeys ?? null;
    loadNotifications();
    loadTokens();
});

const loadTokens = async () => {
    tokens.value = await accessTokenService.getTokens();
};

const openCreateToken = () => {
    tokenName.value = '';
    tokenExpiryDays.value = 90;
    showCreateToken.value = true;
};

const createToken = async () => {
    if (!tokenName.value.trim()) {
        Toast.open({
            message: 'Giv tokenet et navn.',
            type: 'is-warning'
        });
        return;
    }

    isCreatingToken.value = true;

    try {
        const created = await accessTokenService.createToken(tokenName.value, tokenExpiryDays.value);
        createdToken.value = created.token;
        showCreateToken.value = false;
        showCreatedToken.value = true;
        await loadTokens();
    } catch {
        Toast.open({
            message: 'Der skete en fejl under oprettelse af tokenet',
            type: 'is-danger'
        });
    } finally {
        isCreatingToken.value = false;
    }
};

const revokeToken = async (id: number) => {
    try {
        await accessTokenService.revokeToken(id);
        await loadTokens();
        Toast.open({
            message: 'Tokenet er tilbagekaldt!',
            type: 'is-success'
        });
    } catch {
        Toast.open({
            message: 'Der skete en fejl under tilbagekaldelse af tokenet',
            type: 'is-danger'
        });
    }
};

const copyToken = async () => {
    await navigator.clipboard.writeText(createdToken.value);
    Toast.open({
        message: 'Tokenet er kopieret',
        type: 'is-success'
    });
};

const formatTokenDate = (value: string) => new Date(value).toLocaleDateString('da-DK');

const loadNotifications = async () => {
    try {
        notifications.value = await notificationService.getPreferences();
    } catch {
        notifications.value = [];
    }
};

const updateNotifications = async () => {
    isUpdatingNotifications.value = true;

    try {
        const types = notifications.value
            .filter((notification) => notification.subscribed && notification.canSubscribe)
            .map((notification) => notification.type);

        await notificationService.updatePreferences(types);

        Toast.open({
            message: 'Dine notifikationer er blevet opdateret!',
            type: 'is-success'
        });
    } catch {
        Toast.open({
            message: 'Der skete en fejl under opdatering af dine notifikationer',
            type: 'is-danger'
        });
    } finally {
        isUpdatingNotifications.value = false;
    }
};

const openNameModal = () => {
    firstName.value = Me.value?.firstName ?? '';
    lastName.value = Me.value?.lastName ?? '';
    showName.value = true;
};

const openPhoneModal = () => {
    phone.value = Me.value?.phone ?? '';
    showPhone.value = true;
};

const openPasswordModal = () => {
    oldPassword.value = '';
    newPassword.value = '';
    repeatPassword.value = '';
    showPassword.value = true;
};

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
            showName.value = false;
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

const updatePhone = async () => {
    isUpdatingPhone.value = true;

    try {
        const ok = await meStore.UPDATE_PHONE(phone.value);
        if (ok) {
            Toast.open({
                message: 'Dit telefonnummer er blevet opdateret!',
                type: 'is-success'
            });
            showPhone.value = false;
        } else {
            Toast.open({
                message: 'Der skete en fejl under opdatering af dit telefonnummer',
                type: 'is-danger'
            });
        }
    } finally {
        isUpdatingPhone.value = false;
    }
};

const addPasskey = async () => {
    if (!meStore.Me) return;

    try {
        const result = await accountStore.CREATE_PASSKEY(meStore.Me.name);
        if (result) {
            recoveryCodes.value = result.recoveryCodes;
        } else {
        }
    } catch (e) {
        return Toast.open({
            message: (e as DOMException)?.name === "NotAllowedError"
            ? "Aktivering blev afbrudt af brugeren."
            : 'Der skete en uventet fejl under aktiveringen af din passkey.',
            type: "is-warning"
        })
    }
}

const deletePasskey = async (passkey: string) => {
    if(!passkeys.value) return;
    if(passkeys.value?.length <= 1 && !Status.value.hasEnabledAuthenticator) {
        return Toast.open({
            message: "Du kan ikke slette din sidste passkey.",
            type: "is-danger"
        })
    }

    const passkeyIndex = passkeys.value.findIndex((p) => p.id === passkey);
    if(passkeyIndex === -1) {
        return Toast.open({
            message: "Kunne ikke finde passkey",
            type: "is-warning"
        })
    }

    const error = await accountStore.REMOVE_PASSKEY(passkey);
    if(error) {
        return Toast.open({
            message: "Der skete en fejl ved fjenelsen af din passkey",
            type: "is-warning"
        })
    }

    const newPasskeyList = passkeys.value.filter((_, i) => i !== passkeyIndex);

    meStore.$patch({
        Me: {
            passkeys: newPasskeyList
        }
    })
}

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
            showPassword.value = false;
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
        const errorMessage = await accountStore.DISABLE_2FA(disablePassword.value);
        if (errorMessage === null) {
            Toast.open({
                message: 'Tofaktorautentificering er deaktiveret',
                type: 'is-success'
            });
            showDisable.value = false;
        } else {
            Toast.open({
                message: errorMessage,
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
<style lang="scss" scoped>
.profile-scroll {
    flex: 1;
    min-height: 0;
    overflow-y: auto;
}

.profile-page {
    max-width: 64rem;
    margin: 0 auto;
    padding: 0 1rem 1rem;
}

.profile-header {
    display: flex;
    align-items: center;
    gap: 1rem;
    background-color: #fff;
    border-radius: 10px;
    box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
    padding: 1.25rem;
    margin-bottom: 1rem;

    .title,
    .subtitle {
        margin-bottom: 0.25rem;
    }
}

.profile-identity {
    flex: 1;
    min-width: 0;
}

.profile-picture-actions {
    text-align: right;
}

.profile-grid {
    display: grid;
    gap: 1rem;
    grid-template-columns: minmax(0, 2fr) minmax(0, 1fr);
}

.profile-grid-wide {
    grid-column: 1 / -1;
}

.card {
    margin-bottom: 0;
}

.profile-summary {
    margin-bottom: 1rem;
}

.profile-summary-row {
    display: flex;
    justify-content: space-between;
    gap: 1rem;
    padding: 0.5rem 0;
    border-bottom: 1px solid rgba(0, 0, 0, 0.08);

    &:last-child {
        border-bottom: none;
    }
}

.profile-summary-value {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    text-align: right;
}

.tfa-status {
    display: flex;
    align-items: center;
    gap: 1rem;
}

.tfa-status-text {
    flex: 1;
    min-width: 0;

    p {
        margin-bottom: 0.25rem;

        &:last-child {
            margin-bottom: 0;
        }
    }
}

.tfa-section-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
    margin-bottom: 0.75rem;

    .title {
        margin-bottom: 0;
    }
}

.tfa-passkeys {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.tfa-passkey-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;

    p {
        margin-bottom: 0;
    }
}

.tfa-passkey {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
    padding: 0.75rem;
    border: 1px solid rgba(0, 0, 0, 0.08);
    border-radius: 8px;

    &:hover {
        background-color: #f7f8fa;
    }
}

.tfa-passkey-info {
    display: flex;
    align-items: center;
    gap: 0.75rem;

    > i {
        color: #6b7280;
    }

    p {
        margin-bottom: 0.1rem;

        &:last-child {
            margin-bottom: 0;
        }
    }
}

@media (max-width: 768px) {
    .profile-grid {
        grid-template-columns: minmax(0, 1fr);
    }

    .profile-header {
        flex-direction: column;
        align-items: flex-start;
    }

    .profile-picture-actions {
        text-align: left;
    }
}
</style>
