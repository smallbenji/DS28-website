<template>
    <section class="section clients-page">
        <div class="clients-heading">
            <div>
                <h1 class="title is-4">OpenIddict-klienter</h1>
                <p>Administrér applikationer, der bruger HQ som loginudbyder.</p>
            </div>
            <div class="buttons">
                <BButton :loading="loading" :disabled="busy" @click="load">Genindlæs</BButton>
                <BButton type="is-primary" :disabled="busy || loading" @click="openCreate">Opret klient</BButton>
            </div>
        </div>

        <BNotification type="is-info" :closable="false">
            Kun authorization-code-flow understøttes. Brugeren skal have mindst én af klientens valgte
            loginrettigheder. Public-klienter skal bruge PKCE.
        </BNotification>
        <BNotification v-if="error" type="is-danger" :closable="false" role="alert">{{ error }}</BNotification>

        <BTable :data="clients" :loading="loading" :mobile-cards="true">
            <BTableColumn field="displayName" label="Navn" v-slot="props">{{ props.row.displayName || 'Uden navn' }}</BTableColumn>
            <BTableColumn field="clientId" label="Client ID" v-slot="props"><code>{{ props.row.clientId }}</code></BTableColumn>
            <BTableColumn field="clientType" label="Type" v-slot="props">{{ props.row.clientType }}</BTableColumn>
            <BTableColumn field="requirePkce" label="PKCE" v-slot="props">{{ props.row.requirePkce ? 'Påkrævet' : 'Valgfri' }}</BTableColumn>
            <BTableColumn label="Loginrettigheder (mindst én)" v-slot="props">
                <ul v-if="props.row.requiredRoles.length"><li v-for="role in props.row.requiredRoles" :key="role"><code>{{ role }}</code></li></ul>
                <span v-else class="has-text-danger">Ingen gyldige rettigheder – login afvist</span>
            </BTableColumn>
            <BTableColumn label="Redirect-URL'er" v-slot="props">
                <ul><li v-for="uri in props.row.redirectUris" :key="uri" class="client-uri">{{ uri }}</li></ul>
            </BTableColumn>
            <BTableColumn label="Handlinger" v-slot="props">
                <div class="buttons are-small">
                    <BButton :disabled="busy || loading" @click="openEdit(props.row)">Rediger</BButton>
                    <BButton v-if="props.row.clientType === 'confidential'" :disabled="busy || loading" @click="confirmRotate(props.row)">Nyt secret</BButton>
                    <BButton type="is-danger" outlined :disabled="busy || loading" @click="confirmDelete(props.row)">Slet</BButton>
                </div>
            </BTableColumn>
            <template #empty><p class="has-text-centered" role="status">{{ error ? 'Listen kunne ikke hentes. Prøv Genindlæs.' : loading ? 'Indlæser klienter…' : 'Der er ingen klienter endnu.' }}</p></template>
        </BTable>

        <BModal v-model="editorOpen" has-modal-card :can-cancel="busy ? false : ['escape', 'outside']">
            <form class="modal-card" @submit.prevent="submit">
                <header class="modal-card-head">
                    <p class="modal-card-title">{{ editing ? 'Rediger klient' : 'Opret klient' }}</p>
                    <button type="button" class="delete" aria-label="Luk" :disabled="busy" @click="editorOpen = false" />
                </header>
                <section class="modal-card-body">
                    <BNotification v-if="saveError" type="is-danger" :closable="false" role="alert">{{ saveError }}</BNotification>
                    <BField label="Navn"><BInput v-model="form.displayName" required maxlength="200" :disabled="busy" /></BField>
                    <BField label="Client ID" message="Unikt ID, som applikationen bruger ved login. Ændringer kræver opdatering i applikationen.">
                        <BInput v-model="form.clientId" required maxlength="100" :disabled="busy" />
                    </BField>
                    <BField label="Klienttype">
                        <BSelect v-model="form.clientType" expanded :disabled="busy" @update:model-value="onTypeChange">
                            <option value="confidential">Confidential – serverapplikation med secret</option>
                            <option value="public">Public – browserapplikation uden secret</option>
                        </BSelect>
                    </BField>
                    <BField label="Loginrettigheder" message="Vælg mindst én AppRoles-rettighed. Brugeren skal have mindst én af de valgte rettigheder; de tildeles ikke af klienten.">
                        <BTaginput
                            v-model="form.requiredRoles"
                            autocomplete
                            append-to-body
                            open-on-focus
                            :data="filteredRoles"
                            :allow-new="false"
                            :disabled="busy"
                            placeholder="Søg og vælg rettigheder…"
                            @typing="onRoleTyping"
                        />
                    </BField>
                    <BField label="Redirect-URL'er" message="Én præcis callback-URL pr. linje. HTTPS kræves, undtagen HTTP på loopback (fx localhost). Ingen wildcards eller fragmenter.">
                        <BInput v-model="redirectText" type="textarea" :rows="4" required :disabled="busy" />
                    </BField>
                    <BField label="Scopes" grouped group-multiline>
                        <BCheckbox :model-value="true" disabled>openid</BCheckbox>
                        <BCheckbox v-for="scope in editableScopes" :key="scope" v-model="form.scopes" :native-value="scope" :disabled="busy">{{ scope }}</BCheckbox>
                    </BField>
                    <BField>
                        <BCheckbox v-model="form.requirePkce" :disabled="busy || form.clientType === 'public'">Kræv PKCE</BCheckbox>
                    </BField>
                    <p class="is-size-7 has-text-grey">Nye confidential-klienter får et automatisk genereret secret. Eksisterende secrets bevares ved almindelig redigering. Øvrige avancerede indstillinger bevares.</p>
                </section>
                <footer class="modal-card-foot">
                    <div class="buttons">
                        <BButton type="is-primary" native-type="submit" :loading="busy">{{ editing ? 'Gem ændringer' : 'Opret klient' }}</BButton>
                        <BButton :disabled="busy" @click="editorOpen = false">Annuller</BButton>
                    </div>
                </footer>
            </form>
        </BModal>

        <BModal :model-value="!!secret" has-modal-card :can-cancel="false">
            <div class="modal-card">
                <header class="modal-card-head"><p class="modal-card-title">Gem client secret</p></header>
                <section class="modal-card-body">
                    <BNotification type="is-warning" :closable="false">Secret til <strong>{{ secretClientId }}</strong> vises kun nu. Gem det sikkert, og opdatér klientapplikationen.</BNotification>
                    <BField label="Client secret"><BInput :model-value="secret" readonly aria-label="Client secret" /></BField>
                    <BButton @click="copySecret">Kopiér secret</BButton>
                </section>
                <footer class="modal-card-foot"><BButton type="is-primary" @click="secret = ''">Jeg har gemt secret</BButton></footer>
            </div>
        </BModal>
    </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { BButton, BCheckbox, BField, BInput, BModal, BNotification, BSelect, BTable, BTableColumn, BTaginput, useDialog, useToast } from 'buefy';
import OpenIddictService from '@/Services/OpenIddictService';
import { getErrorMessage } from '@/lib/api';
import type { OpenIddictClientDto, OpenIddictClientUpdateDto } from '@/types';

const service = new OpenIddictService();
const Dialog = useDialog();
const Toast = useToast();
const editableScopes = ['profile', 'email', 'roles'];
const clients = ref<OpenIddictClientDto[]>([]);
const availableRoles = ref<string[]>([]);
const roleQuery = ref('');
const filteredRoles = computed(() => availableRoles.value.filter(role => role.toLowerCase().includes(roleQuery.value.toLowerCase())));
const loading = ref(false);
const busy = ref(false);
const error = ref('');
const saveError = ref('');
const editorOpen = ref(false);
const editing = ref<OpenIddictClientDto | null>(null);
const form = ref<OpenIddictClientUpdateDto>(emptyForm());
const redirectText = ref('');
const secret = ref('');
const secretClientId = ref('');

function emptyForm(): OpenIddictClientUpdateDto {
    return { clientId: '', displayName: '', clientType: 'confidential', redirectUris: [], scopes: ['openid'], requiredRoles: [], requirePkce: true };
}

async function load() {
    if (loading.value) return;
    loading.value = true;
    error.value = '';
    try {
        const [loadedClients, roles] = await Promise.all([service.getClients(), service.getAvailableRoles()]);
        clients.value = loadedClients;
        availableRoles.value = roles;
    } catch (exception) {
        clients.value = [];
        error.value = getErrorMessage(exception, 'Klientlisten kunne ikke hentes. Kontrollér din adgang, og prøv igen.');
    } finally {
        loading.value = false;
    }
}

function openCreate() {
    editing.value = null;
    form.value = emptyForm();
    redirectText.value = '';
    saveError.value = '';
    roleQuery.value = '';
    editorOpen.value = true;
}

function openEdit(client: OpenIddictClientDto) {
    editing.value = client;
    form.value = {
        clientId: client.clientId,
        displayName: client.displayName ?? '',
        clientType: client.clientType,
        redirectUris: [...client.redirectUris],
        scopes: [...client.scopes],
        requiredRoles: [...client.requiredRoles],
        requirePkce: client.requirePkce || client.clientType === 'public'
    };
    redirectText.value = client.redirectUris.join('\n');
    saveError.value = '';
    roleQuery.value = '';
    editorOpen.value = true;
}

function onTypeChange() {
    if (form.value.clientType === 'public') form.value.requirePkce = true;
}

function onRoleTyping(value: string | number | undefined) {
    roleQuery.value = value == null ? '' : String(value);
}

function submit() {
    if (busy.value) return;
    if (form.value.requiredRoles.length === 0) {
        saveError.value = 'Vælg mindst én loginrettighed.';
        return;
    }
    if (editing.value && editing.value.clientType !== form.value.clientType) {
        Dialog.confirm({
            title: 'Skift klienttype',
            message: 'Skift af klienttype ændrer kravene til klientens login. Et eksisterende secret fjernes ved skift til public; et nyt genereres ved skift til confidential.',
            confirmText: 'Skift og gem',
            cancelText: 'Annuller',
            onConfirm: () => { void save(); }
        });
        return;
    }
    void save();
}

async function save() {
    if (busy.value) return;
    busy.value = true;
    saveError.value = '';
    const data = {
        ...form.value,
        clientId: form.value.clientId.trim(),
        displayName: form.value.displayName.trim(),
        redirectUris: [...new Set(redirectText.value.split(/\r?\n/).map(value => value.trim()).filter(Boolean))]
    };
    try {
        const result = editing.value ? await service.update(editing.value.id, data) : await service.create(data);
        editorOpen.value = false;
        if (result.clientSecret) showSecret(result.client.clientId, result.clientSecret);
        Toast.open({ message: 'Klienten er gemt.', type: 'is-success' });
        await load();
    } catch (exception) {
        saveError.value = getErrorMessage(exception, 'Klienten kunne ikke gemmes. Prøv igen.');
    } finally {
        busy.value = false;
    }
}

function showSecret(clientId: string, value: string) {
    secretClientId.value = clientId;
    secret.value = value;
}

function confirmRotate(client: OpenIddictClientDto) {
    Dialog.confirm({
        title: 'Generér nyt secret',
        message: 'Det gamle secret stopper med at virke med det samme. Klientapplikationen skal opdateres med det nye secret.',
        confirmText: 'Generér nyt secret',
        cancelText: 'Annuller',
        type: 'is-warning',
        onConfirm: () => { void rotate(client); }
    });
}

async function rotate(client: OpenIddictClientDto) {
    if (busy.value) return;
    busy.value = true;
    error.value = '';
    try {
        showSecret(client.clientId, await service.rotateSecret(client.id));
    } catch (exception) {
        error.value = getErrorMessage(exception, 'Secret kunne ikke roteres. Prøv igen.');
    } finally {
        busy.value = false;
    }
}

function confirmDelete(client: OpenIddictClientDto) {
    Dialog.confirm({
        title: 'Slet klient',
        message: 'Klienten og dens gemte tokens og autorisationer slettes. Applikationen kan derefter ikke bruge HQ-login. Handlingen kan ikke fortrydes.',
        confirmText: 'Slet klient',
        cancelText: 'Annuller',
        type: 'is-danger',
        onConfirm: () => { void remove(client); }
    });
}

async function remove(client: OpenIddictClientDto) {
    if (busy.value) return;
    busy.value = true;
    error.value = '';
    try {
        await service.delete(client.id);
        Toast.open({ message: 'Klienten er slettet.', type: 'is-success' });
        await load();
    } catch (exception) {
        error.value = getErrorMessage(exception, 'Klienten kunne ikke slettes. Prøv igen.');
    } finally {
        busy.value = false;
    }
}

async function copySecret() {
    try {
        await navigator.clipboard.writeText(secret.value);
        Toast.open({ message: 'Secret er kopieret.', type: 'is-success' });
    } catch {
        Toast.open({ message: 'Kopiér secret manuelt fra feltet.', type: 'is-warning' });
    }
}

onMounted(load);
</script>

<style scoped>
.clients-heading {
    display: flex;
    justify-content: space-between;
    align-items: flex-start;
    flex-wrap: wrap;
    gap: 1rem;
    margin-bottom: 1.5rem;
}

.client-uri {
    overflow-wrap: anywhere;
    max-width: 24rem;
    font-size: 0.85rem;
}

.modal-card {
    max-width: calc(100vw - 2rem);
}
</style>
