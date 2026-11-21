<template>
    <UDashboardPanel id="profile">
        <template #header>
            <UDashboardNavbar title="Min profil">
                <template #leading>
                    <UDashboardSidebarCollapse />
                </template>
            </UDashboardNavbar>
        </template>

        <template #body>
            <div v-if="Me" class="mx-auto flex w-full max-w-4xl flex-col gap-6">
                <section class="flex flex-wrap items-center gap-4">
                    <UserAvatar :user="Me" size="3xl" />
                    <div class="min-w-0 flex-1">
                        <h1 class="truncate text-2xl font-semibold text-highlighted">{{ Me.firstName }} {{ Me.lastName }}</h1>
                        <p class="flex items-center gap-1.5 text-sm text-muted">
                            <template v-if="Me.phone">
                                <UIcon name="i-lucide-phone" class="size-4" />
                                {{ Me.phone }}
                            </template>
                            <template v-else>Intet telefonnummer</template>
                        </p>
                    </div>
                    <div class="flex flex-wrap gap-2">
                        <ProfilePictureUpload :has-picture="!!Me.profilePicture" :disabled="updatingPicture" size="sm" @confirm="updatePicture" />
                        <UButton v-if="Me.profilePicture" color="error" variant="ghost" size="sm" label="Fjern" :disabled="updatingPicture" @click="removePicture" />
                    </div>
                </section>

                <div class="grid gap-6" :class="{ 'lg:grid-cols-2': hasNotificationOptions }">
                    <UCard>
                        <template #header>
                            <h2 class="font-semibold text-highlighted">Profil og sikkerhed</h2>
                        </template>
                        <dl class="flex flex-col divide-y divide-default text-sm">
                            <div class="flex items-center justify-between gap-3 py-2">
                                <dt class="text-muted">Navn</dt>
                                <dd class="flex items-center gap-2">
                                    <span>{{ Me.firstName }} {{ Me.lastName }}</span>
                                    <UButton color="neutral" variant="ghost" size="xs" icon="i-lucide-pencil" aria-label="Rediger navn" @click="openNameModal" />
                                </dd>
                            </div>
                            <div class="flex items-center justify-between gap-3 py-2">
                                <dt class="text-muted">Email</dt>
                                <dd class="truncate">{{ Me.email }}</dd>
                            </div>
                            <div class="flex items-center justify-between gap-3 py-2">
                                <dt class="text-muted">Telefon</dt>
                                <dd class="flex items-center gap-2">
                                    <span>{{ Me.phone || 'Intet telefonnummer' }}</span>
                                    <UButton color="neutral" variant="ghost" size="xs" icon="i-lucide-pencil" aria-label="Rediger telefon" @click="openPhoneModal" />
                                </dd>
                            </div>
                        </dl>
                        <template #footer>
                            <div class="flex flex-wrap items-center justify-between gap-2">
                                <p class="text-sm text-muted">Hold din konto sikker ved at skifte din adgangskode.</p>
                                <UButton color="neutral" variant="outline" icon="i-lucide-key-round" label="Skift adgangskode" @click="openPasswordModal" />
                            </div>
                        </template>
                    </UCard>

                    <UCard v-if="hasNotificationOptions">
                        <template #header>
                            <h2 class="font-semibold text-highlighted">Notifikationer</h2>
                        </template>
                        <div class="flex flex-col gap-3">
                            <USwitch
                                v-for="notification in notifications"
                                :key="notification.type"
                                :model-value="notification.subscribed"
                                :label="notificationLabels[notification.type] ?? notification.type"
                                :disabled="!notification.canSubscribe || updatingNotifications"
                                @update:model-value="toggleNotification(notification, $event)"
                            />
                        </div>
                    </UCard>
                </div>

                <UCard v-if="Me.roles.includes('McpUser')">
                    <template #header>
                        <h2 class="font-semibold text-highlighted">Forbind en AI-assistent</h2>
                        <p class="text-sm text-muted">Forbind din AI-assistent til DS28 via MCP. Assistenten får adgang til tavler på dine vegne med dine eksisterende rettigheder.</p>
                    </template>
                    <UFormField label="MCP-adresse">
                        <UFieldGroup class="w-full">
                            <UInput :model-value="mcpUrl" readonly class="w-full font-mono" @focus="selectInput" />
                            <UButton icon="i-lucide-copy" aria-label="Kopiér MCP-adresse" @click="copyMcpUrl" />
                        </UFieldGroup>
                    </UFormField>
                    <div class="mt-3 flex flex-wrap gap-2">
                        <UButton label="Tilføj til Claude" @click="mcpClient = 'Claude'" />
                        <UButton label="Tilføj til ChatGPT" @click="mcpClient = 'ChatGPT'" />
                    </div>
                    <p class="mt-3 text-sm text-muted">Du skal godkende forbindelsen og logge ind med din DS28-konto. Din konto skal have rollen McpUser.</p>
                </UCard>

                <UCard>
                    <template #header>
                        <div class="flex flex-wrap items-center gap-3">
                            <UIcon name="i-lucide-shield-check" class="size-6" :class="Status.twoFactorEnabled ? 'text-success' : 'text-muted'" />
                            <div class="min-w-0 flex-1">
                                <h2 class="font-semibold text-highlighted">Tofaktorautentificering</h2>
                                <p class="text-sm text-muted">
                                    <template v-if="Status.twoFactorEnabled">Du har {{ Status.recoveryCodesLeft }} recovery codes tilbage.</template>
                                    <template v-else>Ved at aktivere det, tilføjes et ekstra sikkerhedslag ved login med din autentificeringsapp.</template>
                                </p>
                            </div>
                            <UBadge :label="Status.twoFactorEnabled ? 'Aktiv' : 'Deaktiveret'" :color="Status.twoFactorEnabled ? 'success' : 'neutral'" variant="subtle" />
                        </div>
                    </template>

                    <template v-if="Status.twoFactorEnabled">
                        <div class="mb-3 flex items-center justify-between gap-2">
                            <h3 class="text-sm font-semibold text-highlighted">Dine passkeys</h3>
                            <UButton size="sm" icon="i-lucide-plus" label="Tilføj passkey" :loading="addingPasskey" @click="addPasskey" />
                        </div>
                        <ul v-if="passkeys.length" class="flex flex-col divide-y divide-default">
                            <li v-for="passkey in passkeys" :key="passkey.id" class="flex items-center gap-3 py-2">
                                <UIcon name="i-lucide-fingerprint" class="size-5 text-muted" />
                                <div class="min-w-0 flex-1">
                                    <p class="truncate text-sm font-medium">{{ passkey.name }}</p>
                                    <p class="text-xs text-muted">Tilføjet {{ formatDate(passkey.createdAt) }}</p>
                                </div>
                                <UButton color="error" variant="ghost" size="xs" icon="i-lucide-trash-2" :aria-label="`Fjern passkey ${passkey.name}`" @click="confirmDeletePasskey(passkey)" />
                            </li>
                        </ul>
                        <p v-else class="text-sm text-muted">Passkeys er ikke aktiveret.</p>
                    </template>

                    <template #footer>
                        <div class="flex flex-wrap gap-2">
                            <UButton v-if="!Status.twoFactorEnabled" icon="i-lucide-shield-check" label="Aktivér 2FA" @click="setupOpen = true" />
                            <template v-else>
                                <UButton color="neutral" variant="outline" icon="i-lucide-refresh-cw" label="Generér nye recovery codes" @click="regenerate" />
                                <UButton color="warning" variant="outline" icon="i-lucide-smartphone" label="Nulstil autentificeringsnøgle" @click="resetOpen = true" />
                                <UButton color="error" variant="outline" icon="i-lucide-shield-off" label="Deaktiver 2FA" @click="openDisable" />
                            </template>
                        </div>
                    </template>
                </UCard>

                <UCard>
                    <template #header>
                        <div class="flex flex-wrap items-center justify-between gap-2">
                            <div class="min-w-0">
                                <h2 class="font-semibold text-highlighted">Adgangstokens</h2>
                                <p class="text-sm text-muted">Adgangstokens bruges af værktøjer og scripts, så de kan læse og skrive i DS28 uden din adgangskode. Behandl dem som adgangskoder, og tilbagekald dem, du ikke bruger.</p>
                            </div>
                        </div>
                    </template>
                    <p v-if="tokens.length === 0" class="text-sm text-muted">Du har ingen aktive adgangstokens.</p>
                    <ul v-else class="flex flex-col divide-y divide-default">
                        <li v-for="token in tokens" :key="token.id" class="flex items-center gap-3 py-2">
                            <UIcon name="i-lucide-key-round" class="size-5 text-muted" />
                            <div class="min-w-0 flex-1">
                                <p class="truncate text-sm font-medium">{{ token.name }}</p>
                                <p class="text-xs text-muted">
                                    Oprettet {{ formatTokenDate(token.createdAt) }}
                                    <template v-if="token.expiresAt"> · udløber {{ formatTokenDate(token.expiresAt) }}</template>
                                </p>
                            </div>
                            <UButton color="error" variant="ghost" size="xs" label="Tilbagekald" @click="confirmRevokeToken(token)" />
                        </li>
                    </ul>
                    <template #footer>
                        <UButton icon="i-lucide-plus" label="Opret token" @click="openCreateToken" />
                    </template>
                </UCard>
            </div>

            <UModal v-model:open="nameOpen" title="Rediger navn">
                <template #body>
                    <form id="profile-name-form" class="grid gap-4 sm:grid-cols-2" @submit.prevent="updateName">
                        <UFormField label="Fornavn" required>
                            <UInput v-model="firstName" class="w-full" autofocus />
                        </UFormField>
                        <UFormField label="Efternavn" required>
                            <UInput v-model="lastName" class="w-full" />
                        </UFormField>
                    </form>
                </template>
                <template #footer>
                    <div class="flex w-full justify-end gap-2">
                        <UButton color="neutral" variant="outline" label="Annuller" @click="nameOpen = false" />
                        <UButton type="submit" form="profile-name-form" label="Gem" :loading="updatingName" />
                    </div>
                </template>
            </UModal>

            <UModal v-model:open="phoneOpen" title="Rediger telefon">
                <template #body>
                    <form id="profile-phone-form" @submit.prevent="updatePhone">
                        <UFormField label="Telefon">
                            <UInput v-model="phone" type="tel" class="w-full" autofocus />
                        </UFormField>
                    </form>
                </template>
                <template #footer>
                    <div class="flex w-full justify-end gap-2">
                        <UButton color="neutral" variant="outline" label="Annuller" @click="phoneOpen = false" />
                        <UButton type="submit" form="profile-phone-form" label="Gem" :loading="updatingPhone" />
                    </div>
                </template>
            </UModal>

            <UModal v-model:open="passwordOpen" title="Skift adgangskode">
                <template #body>
                    <form id="profile-password-form" class="flex flex-col gap-4" @submit.prevent="changePassword">
                        <UFormField label="Nuværende adgangskode" required>
                            <UInput v-model="oldPassword" :type="showPasswords ? 'text' : 'password'" autocomplete="current-password" class="w-full" autofocus />
                        </UFormField>
                        <UFormField label="Ny adgangskode" required>
                            <UInput v-model="newPassword" :type="showPasswords ? 'text' : 'password'" autocomplete="new-password" class="w-full" />
                        </UFormField>
                        <UFormField label="Gentag ny adgangskode" required :error="repeatPassword && newPassword !== repeatPassword ? 'Adgangskoderne er ikke ens' : undefined">
                            <UInput v-model="repeatPassword" :type="showPasswords ? 'text' : 'password'" autocomplete="new-password" class="w-full" />
                        </UFormField>
                        <UCheckbox v-model="showPasswords" label="Vis adgangskoder" />
                    </form>
                </template>
                <template #footer>
                    <div class="flex w-full justify-end gap-2">
                        <UButton color="neutral" variant="outline" label="Annuller" @click="passwordOpen = false" />
                        <UButton type="submit" form="profile-password-form" label="Gem" :loading="changingPassword" />
                    </div>
                </template>
            </UModal>

            <UModal v-model:open="mcpSetupOpen" :title="`Tilføj til ${mcpClient ?? ''}`">
                <template #body>
                    <p v-if="mcpClient === 'Claude'" class="mb-3 text-sm">Åbn Claude, gå til Connectors, og vælg Add custom connector. Angiv DS28 som navn og indsæt MCP-adressen nedenfor.</p>
                    <p v-else class="mb-3 text-sm">Åbn ChatGPTs indstillinger for apps, og opret en forbindelse til en egen MCP-server med navnet DS28 og adressen nedenfor. Opsætningen kan kræve udviklertilstand og tilladelse fra din workspace-administrator.</p>
                    <UFormField label="MCP-adresse">
                        <UFieldGroup class="w-full">
                            <UInput :model-value="mcpUrl" readonly class="w-full font-mono" @focus="selectInput" />
                            <UButton icon="i-lucide-copy" aria-label="Kopiér MCP-adresse" @click="copyMcpUrl" />
                        </UFieldGroup>
                    </UFormField>
                    <p class="mt-3 text-sm text-muted">Vælg OAuth, hvis du bliver spurgt om godkendelsesmetode, og log ind med din DS28-konto. Hvis klienten kræver et klient-id og en klienthemmelighed, skal en systemadministrator oprette en OAuth-klient til den pågældende assistent.</p>
                </template>
                <template #footer>
                    <div class="flex w-full flex-wrap justify-end gap-2">
                        <UButton color="neutral" variant="outline" label="Luk" @click="mcpClient = null" />
                        <UButton :to="mcpClient === 'Claude' ? 'https://claude.ai/settings/connectors' : 'https://chatgpt.com/#settings/Connectors'" target="_blank" rel="noopener noreferrer" trailing-icon="i-lucide-external-link" :label="`Åbn ${mcpClient ?? ''} (ny fane)`" />
                    </div>
                </template>
            </UModal>

            <TwoFactorSetupModal v-model:open="setupOpen" @enabled="notify.success('Tofaktorautentificering er aktiveret.')" />
            <RecoveryCodesModal v-model:open="recoveryCodesOpen" :codes="recoveryCodes" />

            <UModal v-model:open="createTokenOpen" title="Opret adgangstoken">
                <template #body>
                    <form id="profile-token-form" class="flex flex-col gap-4" @submit.prevent="createToken">
                        <UFormField label="Navn" required>
                            <UInput v-model="tokenName" placeholder="Fx MCP eller CI" maxlength="64" class="w-full" autofocus />
                        </UFormField>
                        <UFormField label="Udløber om">
                            <USelect v-model="tokenExpiryDays" :items="tokenExpiryItems" class="w-full" />
                        </UFormField>
                    </form>
                </template>
                <template #footer>
                    <div class="flex w-full justify-end gap-2">
                        <UButton color="neutral" variant="outline" label="Annuller" @click="createTokenOpen = false" />
                        <UButton type="submit" form="profile-token-form" label="Opret" :loading="creatingToken" />
                    </div>
                </template>
            </UModal>

            <UModal v-model:open="createdTokenOpen" title="Dit nye adgangstoken" :dismissible="false">
                <template #body>
                    <UAlert color="warning" variant="subtle" icon="i-lucide-triangle-alert" title="Kopiér token nu. Du kan ikke se det igen." description="Brug det i stedet for din adgangskode i værktøjet." />
                    <UFormField label="Token" class="mt-4">
                        <UFieldGroup class="w-full">
                            <UInput :model-value="createdToken" readonly class="w-full font-mono" aria-label="Token" @focus="selectInput" />
                            <UButton icon="i-lucide-copy" label="Kopiér" @click="copyToken" />
                        </UFieldGroup>
                    </UFormField>
                </template>
                <template #footer>
                    <div class="flex w-full justify-end">
                        <UButton label="Luk" @click="closeCreatedToken" />
                    </div>
                </template>
            </UModal>

            <UModal v-model:open="disableOpen" title="Deaktiver tofaktorautentificering" description="Indtast din nuværende adgangskode for at deaktivere tofaktorautentificering.">
                <template #body>
                    <form id="profile-disable-2fa-form" @submit.prevent="disable">
                        <UFormField label="Adgangskode" required>
                            <UInput v-model="disablePassword" type="password" autocomplete="current-password" class="w-full" autofocus />
                        </UFormField>
                    </form>
                </template>
                <template #footer>
                    <div class="flex w-full justify-end gap-2">
                        <UButton color="neutral" variant="outline" label="Annuller" @click="disableOpen = false" />
                        <UButton type="submit" form="profile-disable-2fa-form" color="error" label="Deaktiver" :loading="disabling" />
                    </div>
                </template>
            </UModal>

            <ConfirmModal
                v-model:open="resetOpen"
                title="Nulstil autentificeringsnøgle"
                description="Nulstilling af autentificeringsnøglen deaktiverer tofaktorautentificering, så du skal sætte den op igen med din autentificeringsapp."
                confirm-label="Nulstil og deaktiver"
                color="warning"
                :loading="resetting"
                @confirm="reset"
            />

            <ConfirmModal
                v-model:open="deletePasskeyOpen"
                title="Fjern passkey"
                :description="`Fjern passkeyen ${passkeyToDelete?.name}? Du kan ikke længere logge ind med den.`"
                confirm-label="Fjern"
                color="error"
                :loading="deletingPasskey"
                @confirm="deletePasskey"
            />

            <ConfirmModal
                v-model:open="revokeTokenOpen"
                title="Tilbagekald token"
                :description="`Tilbagekald ${tokenToRevoke?.name}? Værktøjer, der bruger det, mister adgangen med det samme.`"
                confirm-label="Tilbagekald"
                color="error"
                :loading="revokingToken"
                @confirm="revokeToken"
            />
        </template>
    </UDashboardPanel>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { storeToRefs } from "pinia";
import { useAccountStore } from "@/Stores/AccountStore";
import { useMeStore } from "@/Stores/MeStore";
import NotificationService from "@/Services/NotificationService";
import { notificationLabels } from "@/lib/notifications";
import AccessTokenService from "@/Services/AccessTokenService";
import FileService from "@/Services/FileService";
import { formatDate } from "@/lib/util";
import { useNotify } from "@/lib/notify";
import ConfirmModal from "@/Components/ConfirmModal.vue";
import UserAvatar from "@/Components/User/UserAvatar.vue";
import ProfilePictureUpload from "@/Components/User/ProfilePictureUpload.vue";
import TwoFactorSetupModal from "@/Components/Account/TwoFactorSetupModal.vue";
import RecoveryCodesModal from "@/Components/Account/RecoveryCodesModal.vue";
import type { AccessTokenDto, NotificationPreferenceDto, PasskeyDto } from "@/types";

const accountStore = useAccountStore();
const meStore = useMeStore();
const notificationService = new NotificationService();
const accessTokenService = new AccessTokenService();
const fileService = new FileService();
const notify = useNotify();
const { Status } = storeToRefs(accountStore);
const { Me } = storeToRefs(meStore);

function selectInput(event?: Event) {
    if (event?.target instanceof HTMLInputElement) event.target.select();
}

async function copyText(value: string, success: string, failure: string) {
    try {
        await navigator.clipboard.writeText(value);
        notify.success(success);
    } catch {
        notify.error(failure);
    }
}

const mcpUrl = new URL("/mcp", window.location.origin).href;
const mcpClient = ref<"Claude" | "ChatGPT" | null>(null);
const mcpSetupOpen = computed({
    get: () => mcpClient.value !== null,
    set: (open: boolean) => {
        if (!open) mcpClient.value = null;
    }
});

function copyMcpUrl() {
    void copyText(mcpUrl, "MCP-adressen er kopieret.", "Kunne ikke kopiere adressen. Markér og kopiér den fra feltet.");
}

const notifications = ref<NotificationPreferenceDto[]>([]);
const updatingNotifications = ref(false);
const hasNotificationOptions = computed(() => notifications.value.some((notification) => notification.canSubscribe));

const updatingPicture = ref(false);

async function updatePicture(file: File) {
    updatingPicture.value = true;
    try {
        const uploaded = await fileService.uploadProfilePicture(file);
        const ready = await fileService.waitUntilReady(uploaded.publicId);
        if (!ready) {
            notify.error("Billedet kunne ikke behandles. Prøv igen.");
            return;
        }

        const ok = await meStore.UPDATE_PROFILE_PICTURE(uploaded.publicId);
        notify.result(ok, "Dit profilbillede er opdateret.", "Der skete en fejl under opdatering af dit profilbillede.");
    } catch {
        notify.error("Der skete en fejl under upload af billedet.");
    } finally {
        updatingPicture.value = false;
    }
}

async function removePicture() {
    updatingPicture.value = true;
    try {
        const ok = await meStore.UPDATE_PROFILE_PICTURE(null);
        notify.result(ok, "Dit profilbillede er fjernet.", "Der skete en fejl under fjernelse af dit profilbillede.");
    } finally {
        updatingPicture.value = false;
    }
}

onMounted(() => {
    void accountStore.GET_STATUS();
    passkeys.value = Me.value?.passkeys ?? [];
    void loadNotifications();
    void loadTokens();
});

async function loadNotifications() {
    try {
        notifications.value = await notificationService.getPreferences();
    } catch {
        notifications.value = [];
    }
}

async function toggleNotification(notification: NotificationPreferenceDto, subscribed: boolean) {
    notification.subscribed = subscribed;
    updatingNotifications.value = true;
    try {
        const types = notifications.value
            .filter((item) => item.subscribed && item.canSubscribe)
            .map((item) => item.type);
        await notificationService.updatePreferences(types);
        notify.success(subscribed ? "Notifikationen er slået til." : "Notifikationen er slået fra.");
    } catch {
        notification.subscribed = !subscribed;
        notify.error("Notifikationen kunne ikke gemmes.");
    } finally {
        updatingNotifications.value = false;
    }
}

const nameOpen = ref(false);
const firstName = ref("");
const lastName = ref("");
const updatingName = ref(false);

function openNameModal() {
    firstName.value = Me.value?.firstName ?? "";
    lastName.value = Me.value?.lastName ?? "";
    nameOpen.value = true;
}

async function updateName() {
    if (!firstName.value.trim() || !lastName.value.trim()) {
        notify.error("Fornavn og efternavn skal udfyldes.");
        return;
    }

    updatingName.value = true;
    try {
        const ok = await accountStore.UPDATE_NAME(firstName.value, lastName.value);
        notify.result(ok, "Dit navn er opdateret.", "Der skete en fejl under opdatering af dit navn.");
        if (ok) {
            await meStore.GET_ME();
            nameOpen.value = false;
        }
    } finally {
        updatingName.value = false;
    }
}

const phoneOpen = ref(false);
const phone = ref("");
const updatingPhone = ref(false);

function openPhoneModal() {
    phone.value = Me.value?.phone ?? "";
    phoneOpen.value = true;
}

async function updatePhone() {
    updatingPhone.value = true;
    try {
        const ok = await meStore.UPDATE_PHONE(phone.value);
        notify.result(ok, "Dit telefonnummer er opdateret.", "Der skete en fejl under opdatering af dit telefonnummer.");
        if (ok) phoneOpen.value = false;
    } finally {
        updatingPhone.value = false;
    }
}

const passwordOpen = ref(false);
const oldPassword = ref("");
const newPassword = ref("");
const repeatPassword = ref("");
const showPasswords = ref(false);
const changingPassword = ref(false);

function openPasswordModal() {
    oldPassword.value = "";
    newPassword.value = "";
    repeatPassword.value = "";
    showPasswords.value = false;
    passwordOpen.value = true;
}

async function changePassword() {
    if (newPassword.value !== repeatPassword.value) {
        notify.error("Adgangskoderne er ikke ens.");
        return;
    }

    changingPassword.value = true;
    try {
        const ok = await accountStore.CHANGE_PASSWORD(oldPassword.value, newPassword.value);
        notify.result(ok, "Din adgangskode er opdateret.", "Der skete en fejl. Tjek din nuværende adgangskode.");
        if (ok) {
            oldPassword.value = "";
            newPassword.value = "";
            repeatPassword.value = "";
            passwordOpen.value = false;
        }
    } finally {
        changingPassword.value = false;
    }
}

const setupOpen = ref(false);
const recoveryCodesOpen = ref(false);
const recoveryCodes = ref<string[]>([]);

function showRecoveryCodes(codes: string[]) {
    recoveryCodes.value = codes;
    recoveryCodesOpen.value = true;
}

async function regenerate() {
    const result = await accountStore.GENERATE_RECOVERY_CODES();
    if (result) showRecoveryCodes(result.recoveryCodes);
    else notify.error("Der skete en fejl under generering af recovery codes.");
}

const disableOpen = ref(false);
const disablePassword = ref("");
const disabling = ref(false);

function openDisable() {
    disablePassword.value = "";
    disableOpen.value = true;
}

async function disable() {
    if (!disablePassword.value) {
        notify.error("Indtast venligst din adgangskode.");
        return;
    }

    disabling.value = true;
    try {
        const errorMessage = await accountStore.DISABLE_2FA(disablePassword.value);
        if (errorMessage === null) {
            notify.success("Tofaktorautentificering er deaktiveret.");
            disableOpen.value = false;
        } else {
            notify.error(errorMessage);
        }
    } finally {
        disabling.value = false;
    }
}

const resetOpen = ref(false);
const resetting = ref(false);

async function reset() {
    resetting.value = true;
    try {
        const ok = await accountStore.RESET_AUTHENTICATOR();
        notify.result(ok, "Autentificeringsnøglen er nulstillet.", "Der skete en fejl.");
        if (ok) resetOpen.value = false;
    } finally {
        resetting.value = false;
    }
}

const passkeys = ref<PasskeyDto[]>([]);
const addingPasskey = ref(false);
const deletePasskeyOpen = ref(false);
const passkeyToDelete = ref<PasskeyDto | null>(null);
const deletingPasskey = ref(false);

async function addPasskey() {
    if (!meStore.Me || addingPasskey.value) return;

    addingPasskey.value = true;
    try {
        const result = await accountStore.CREATE_PASSKEY(meStore.Me.name);
        if (!result) {
            notify.error("Passkeyen blev ikke tilføjet.");
            return;
        }

        passkeys.value = accountStore.Passkeys;
        notify.success("Passkeyen er tilføjet.");
        if (result.recoveryCodes?.length) showRecoveryCodes(result.recoveryCodes);
    } catch (cause) {
        notify.error((cause as DOMException)?.name === "NotAllowedError"
            ? "Aktivering blev afbrudt af brugeren."
            : "Der skete en uventet fejl under aktiveringen af din passkey.");
    } finally {
        addingPasskey.value = false;
    }
}

function confirmDeletePasskey(passkey: PasskeyDto) {
    if (passkeys.value.length <= 1 && !Status.value.hasEnabledAuthenticator) {
        notify.error("Du kan ikke slette din sidste passkey.");
        return;
    }

    passkeyToDelete.value = passkey;
    deletePasskeyOpen.value = true;
}

async function deletePasskey() {
    const passkey = passkeyToDelete.value;
    if (!passkey) return;

    deletingPasskey.value = true;
    try {
        const error = await accountStore.REMOVE_PASSKEY(passkey.id);
        if (error) {
            notify.error("Der skete en fejl ved fjernelsen af din passkey.");
            return;
        }

        passkeys.value = accountStore.Passkeys;
        meStore.$patch({ Me: { passkeys: passkeys.value } });
        deletePasskeyOpen.value = false;
        notify.success("Passkeyen er fjernet.");
    } finally {
        deletingPasskey.value = false;
    }
}

const tokens = ref<AccessTokenDto[]>([]);
const tokenExpiryItems = [
    { label: "30 dage", value: 30 },
    { label: "90 dage", value: 90 },
    { label: "1 år", value: 365 }
];
const createTokenOpen = ref(false);
const createdTokenOpen = ref(false);
const creatingToken = ref(false);
const tokenName = ref("");
const tokenExpiryDays = ref(90);
const createdToken = ref("");
const revokeTokenOpen = ref(false);
const tokenToRevoke = ref<AccessTokenDto | null>(null);
const revokingToken = ref(false);

async function loadTokens() {
    tokens.value = await accessTokenService.getTokens();
}

function formatTokenDate(value: string) {
    return new Date(value).toLocaleDateString("da-DK");
}

function openCreateToken() {
    tokenName.value = "";
    tokenExpiryDays.value = 90;
    createTokenOpen.value = true;
}

async function createToken() {
    if (!tokenName.value.trim()) {
        notify.error("Giv tokenet et navn.");
        return;
    }

    creatingToken.value = true;
    try {
        const created = await accessTokenService.createToken(tokenName.value, tokenExpiryDays.value);
        createdToken.value = created.token;
        createTokenOpen.value = false;
        createdTokenOpen.value = true;
        await loadTokens();
    } catch {
        notify.error("Der skete en fejl under oprettelse af tokenet.");
    } finally {
        creatingToken.value = false;
    }
}

function copyToken() {
    void copyText(createdToken.value, "Tokenet er kopieret.", "Tokenet kunne ikke kopieres. Markér og kopiér det fra feltet.");
}

function closeCreatedToken() {
    createdTokenOpen.value = false;
    createdToken.value = "";
}

function confirmRevokeToken(token: AccessTokenDto) {
    tokenToRevoke.value = token;
    revokeTokenOpen.value = true;
}

async function revokeToken() {
    const token = tokenToRevoke.value;
    if (!token) return;

    revokingToken.value = true;
    try {
        await accessTokenService.revokeToken(token.id);
        await loadTokens();
        revokeTokenOpen.value = false;
        notify.success("Tokenet er tilbagekaldt.");
    } catch {
        notify.error("Der skete en fejl under tilbagekaldelse af tokenet.");
    } finally {
        revokingToken.value = false;
    }
}
</script>
