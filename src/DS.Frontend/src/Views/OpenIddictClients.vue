<template>
    <UDashboardPanel id="openiddict-clients">
        <template #header>
            <UDashboardNavbar title="OpenIddict-klienter">
                <template #leading>
                    <UDashboardSidebarCollapse />
                    <UButton to="/admin" color="neutral" variant="ghost" icon="i-lucide-arrow-left" aria-label="Tilbage til admin" />
                </template>
                <template #right>
                    <UButton color="neutral" variant="ghost" icon="i-lucide-refresh-cw" aria-label="Genindlæs" :loading="loading" :disabled="busy" @click="load" />
                    <UButton icon="i-lucide-plus" label="Opret klient" :disabled="busy || loading" @click="openCreate" />
                </template>
            </UDashboardNavbar>
        </template>

        <template #body>
            <div class="mx-auto flex w-full max-w-4xl flex-col gap-4">
                <p class="text-sm text-muted">Administrér applikationer, der bruger HQ som loginudbyder.</p>

                <UAlert color="info" variant="subtle" icon="i-lucide-info" description="Kun authorization-code-flow understøttes. Brugeren skal have mindst én af klientens valgte loginrettigheder. Public-klienter skal bruge PKCE." />

                <UAlert v-if="error" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="error" />

                <template v-if="loading && clients.length === 0">
                    <USkeleton v-for="index in 2" :key="index" class="h-40" />
                </template>

                <UEmpty v-else-if="!error && clients.length === 0" icon="i-lucide-key-round" title="Der er ingen klienter endnu." :actions="[{ label: 'Opret klient', icon: 'i-lucide-plus', onClick: openCreate }]" />

                <UCard v-for="client in clients" :key="client.id">
                    <template #header>
                        <div class="flex flex-wrap items-start justify-between gap-3">
                            <div class="min-w-0">
                                <h2 class="font-semibold text-highlighted">{{ client.displayName || 'Uden navn' }}</h2>
                                <code class="text-sm text-muted">{{ client.clientId }}</code>
                            </div>
                            <div class="flex flex-wrap gap-1">
                                <UBadge :label="client.clientType === 'confidential' ? 'Confidential' : 'Public'" color="neutral" variant="outline" />
                                <UBadge :label="client.requirePkce ? 'PKCE påkrævet' : 'PKCE valgfri'" :color="client.requirePkce ? 'success' : 'warning'" variant="subtle" />
                            </div>
                        </div>
                    </template>

                    <dl class="grid gap-4 text-sm sm:grid-cols-2">
                        <div>
                            <dt class="mb-1 text-xs font-medium text-muted">Loginrettigheder (mindst én)</dt>
                            <dd v-if="client.requiredRoles.length" class="flex flex-wrap gap-1">
                                <UBadge v-for="role in client.requiredRoles" :key="role" :label="role" color="neutral" variant="subtle" class="font-mono" />
                            </dd>
                            <dd v-else class="flex items-center gap-1 text-error">
                                <UIcon name="i-lucide-circle-alert" class="size-4" />
                                Ingen gyldige rettigheder. Login afvises.
                            </dd>
                        </div>
                        <div class="min-w-0">
                            <dt class="mb-1 text-xs font-medium text-muted">Redirect-URL'er</dt>
                            <dd>
                                <ul class="flex flex-col gap-0.5">
                                    <li v-for="uri in client.redirectUris" :key="uri" class="break-all font-mono text-xs">{{ uri }}</li>
                                </ul>
                            </dd>
                        </div>
                        <div>
                            <dt class="mb-1 text-xs font-medium text-muted">Scopes</dt>
                            <dd class="font-mono text-xs">{{ client.scopes.join(', ') }}</dd>
                        </div>
                    </dl>

                    <template #footer>
                        <div class="flex flex-wrap justify-end gap-2">
                            <UButton color="error" variant="ghost" icon="i-lucide-trash-2" label="Slet" :disabled="busy || loading" @click="confirmDelete(client)" />
                            <UButton v-if="client.clientType === 'confidential'" color="warning" variant="outline" icon="i-lucide-key-round" label="Nyt secret" :disabled="busy || loading" @click="confirmRotate(client)" />
                            <UButton color="neutral" variant="outline" icon="i-lucide-pencil" label="Rediger" :disabled="busy || loading" @click="openEdit(client)" />
                        </div>
                    </template>
                </UCard>
            </div>

            <ClientEditorModal v-model:open="editorOpen" :client="editing" :available-roles="availableRoles" @saved="onSaved" />

            <ClientSecretModal :secret="secret" :client-id="secretClientId" @close="secret = ''" />

            <ConfirmModal
                v-model:open="rotateOpen"
                title="Generér nyt secret"
                description="Det gamle secret stopper med at virke med det samme. Klientapplikationen skal opdateres med det nye secret."
                confirm-label="Generér nyt secret"
                color="warning"
                :loading="busy"
                @confirm="rotate"
            />

            <ConfirmModal
                v-model:open="deleteOpen"
                title="Slet klient"
                :description="`${target?.displayName || target?.clientId} og klientens gemte tokens og autorisationer slettes. Applikationen kan derefter ikke bruge HQ-login. Handlingen kan ikke fortrydes.`"
                confirm-label="Slet klient"
                color="error"
                :loading="busy"
                @confirm="remove"
            />
        </template>
    </UDashboardPanel>
</template>

<script setup lang="ts">
import { onMounted, ref } from "vue";
import OpenIddictService from "@/Services/OpenIddictService";
import { getErrorMessage } from "@/lib/api";
import { useNotify } from "@/lib/notify";
import ConfirmModal from "@/Components/ConfirmModal.vue";
import ClientEditorModal from "@/Components/OpenIddict/ClientEditorModal.vue";
import ClientSecretModal from "@/Components/OpenIddict/ClientSecretModal.vue";
import type { OpenIddictClientDto, OpenIddictClientSavedDto } from "@/types";

const service = new OpenIddictService();
const notify = useNotify();

const clients = ref<OpenIddictClientDto[]>([]);
const availableRoles = ref<string[]>([]);
const loading = ref(false);
const busy = ref(false);
const error = ref("");

const editorOpen = ref(false);
const editing = ref<OpenIddictClientDto | null>(null);

const secret = ref("");
const secretClientId = ref("");

const target = ref<OpenIddictClientDto | null>(null);
const rotateOpen = ref(false);
const deleteOpen = ref(false);

async function load() {
    if (loading.value) return;

    loading.value = true;
    error.value = "";
    try {
        const [loadedClients, roles] = await Promise.all([service.getClients(), service.getAvailableRoles()]);
        clients.value = loadedClients;
        availableRoles.value = roles;
    } catch (exception) {
        clients.value = [];
        error.value = getErrorMessage(exception, "Klientlisten kunne ikke hentes. Kontrollér din adgang, og prøv igen.");
    } finally {
        loading.value = false;
    }
}

function openCreate() {
    editing.value = null;
    editorOpen.value = true;
}

function openEdit(client: OpenIddictClientDto) {
    editing.value = client;
    editorOpen.value = true;
}

async function onSaved(result: OpenIddictClientSavedDto) {
    if (result.clientSecret) showSecret(result.client.clientId, result.clientSecret);
    await load();
}

function showSecret(clientId: string, value: string) {
    secretClientId.value = clientId;
    secret.value = value;
}

function confirmRotate(client: OpenIddictClientDto) {
    target.value = client;
    rotateOpen.value = true;
}

async function rotate() {
    const client = target.value;
    if (busy.value || !client) return;

    busy.value = true;
    error.value = "";
    try {
        showSecret(client.clientId, await service.rotateSecret(client.id));
    } catch (exception) {
        error.value = getErrorMessage(exception, "Secret kunne ikke roteres. Prøv igen.");
    } finally {
        rotateOpen.value = false;
        busy.value = false;
    }
}

function confirmDelete(client: OpenIddictClientDto) {
    target.value = client;
    deleteOpen.value = true;
}

async function remove() {
    const client = target.value;
    if (busy.value || !client) return;

    busy.value = true;
    error.value = "";
    try {
        await service.delete(client.id);
        notify.success("Klienten er slettet.");
        await load();
    } catch (exception) {
        error.value = getErrorMessage(exception, "Klienten kunne ikke slettes. Prøv igen.");
    } finally {
        deleteOpen.value = false;
        busy.value = false;
    }
}

onMounted(load);
</script>
