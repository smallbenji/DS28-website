<template>
    <section class="section">
        <div class="container">
            <BNotification v-if="error" type="is-danger" :closable="false" role="alert">{{ error }}</BNotification>
            <p v-if="loading" role="status">Henter gruppens brugere…</p>
            <template v-if="group">
                <h2 class="title is-4">{{ group.name }}</h2>
                <div class="box">
                    <h3 class="title is-5">Inviter til gruppen</h3>
                    <p class="mb-4">Alle medlemmer kan invitere og fjerne andre medlemmer. Opret et personligt link og del det med den inviterede. Der sendes ikke automatisk en email.</p>
                    <form @submit.prevent="invite">
                        <BField label="Email"><BInput v-model="email" type="email" required :disabled="busy" /></BField>
                        <BButton native-type="submit" type="is-primary" :loading="busy" :disabled="busy || !email.trim()">Opret invitation</BButton>
                    </form>
                    <BNotification v-if="link" type="is-success" :closable="false" class="mt-4">
                        <BField label="Invitationslink – kopiér og del med den inviterede"><BInput :model-value="link" readonly /></BField>
                    </BNotification>
                </div>
                <div class="box">
                    <h3 class="title is-5">Medlemmer</h3>
                    <BTable :data="group.users" :mobile-cards="true">
                        <BTableColumn field="firstName" label="Navn" v-slot="props">{{ props.row.firstName }} {{ props.row.lastName }}</BTableColumn>
                        <BTableColumn field="email" label="Email" v-slot="props">{{ props.row.email }}</BTableColumn>
                        <BTableColumn label="Handling" v-slot="props">
                            <span v-if="props.row.id === me.ME.id">Dig</span>
                            <BButton v-else type="is-danger is-light" size="is-small" :disabled="busy" @click="removing = props.row">Fjern fra gruppen</BButton>
                        </BTableColumn>
                    </BTable>
                    <BNotification v-if="removing" :closable="false" class="mt-4">
                        <p class="mb-3">Fjern {{ removing.firstName }} {{ removing.lastName }} fra gruppen? Brugerens konto bevares, men adgangen til gruppen ophører.</p>
                        <div class="buttons">
                            <BButton type="is-danger" :disabled="busy" :loading="busy" @click="remove">Fjern medlem</BButton>
                            <BButton :disabled="busy" @click="removing = null">Annuller</BButton>
                        </div>
                    </BNotification>
                </div>
                <div class="box">
                    <h3 class="title is-5">Afventende invitationer</h3>
                    <p v-if="!invitations.length">Ingen afventende invitationer.</p>
                    <div v-for="invitation in invitations" :key="invitation.invitationId" class="invitation-row">
                        <span>{{ invitation.email }}</span>
                        <div class="buttons">
                            <BButton size="is-small" :disabled="busy" @click="showLink(invitation.invitationId)">Vis link</BButton>
                            <BButton size="is-small" type="is-danger is-light" :disabled="busy" @click="revoke(invitation.invitationId)">Annuller invitation</BButton>
                        </div>
                    </div>
                </div>
            </template>
            <BButton v-if="!loading && !group" @click="load">Prøv igen</BButton>
            <BButton :disabled="busy" @click="router.push('/group')">Tilbage til gruppen</BButton>
        </div>
    </section>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import { BButton, BField, BInput, BNotification, BTable, BTableColumn } from 'buefy';
import axios from 'axios';
import type { GroupDto } from '@/types';
import { useMeStore } from '@/Stores/MeStore';
const router = useRouter();
const me = useMeStore();
const group = ref<GroupDto | null>(null);
const invitations = ref<{ invitationId: string; email: string }[]>([]);
const removing = ref<GroupDto['users'][number] | null>(null);
const email = ref('');
const link = ref('');
const error = ref('');
const busy = ref(false);
const loading = ref(true);
function fail(e: unknown) { error.value = axios.isAxiosError(e) && typeof e.response?.data === 'string' ? e.response.data : 'Handlingen kunne ikke gennemføres. Kontrollér din adgang til gruppen, og prøv igen.'; }
function showLink(id: string) { link.value = `${window.location.origin}/group-invitation/${id}`; }
async function refresh() {
    const [members, pending] = await Promise.all([axios.get<GroupDto>('/api/v1/group'), axios.get('/api/v1/group/members/invitations')]);
    group.value = members.data; invitations.value = pending.data;
}
async function load() {
    loading.value = true; error.value = '';
    try { await refresh(); } catch (e) { fail(e); } finally { loading.value = false; }
}
async function invite() {
    if (busy.value) return;
    busy.value = true; error.value = ''; link.value = '';
    try {
        const { data } = await axios.post('/api/v1/group/members/invitations', { email: email.value.trim() });
        link.value = new URL(data.path, window.location.origin).href;
        email.value = ''; await refresh();
    } catch (e) { fail(e); } finally { busy.value = false; }
}
async function remove() {
    if (!removing.value || busy.value) return;
    busy.value = true; error.value = '';
    try { await axios.delete(`/api/v1/group/members/${encodeURIComponent(removing.value.id)}`); removing.value = null; await refresh(); }
    catch (e) { fail(e); } finally { busy.value = false; }
}
async function revoke(id: string) {
    if (busy.value) return;
    busy.value = true; error.value = '';
    try { await axios.delete(`/api/v1/group/members/invitations/${id}`); link.value = ''; await refresh(); }
    catch (e) { fail(e); } finally { busy.value = false; }
}
onMounted(load);
</script>

<style scoped>
.invitation-row { display: flex; flex-wrap: wrap; justify-content: space-between; align-items: center; gap: 1rem; margin-top: 1rem; }
</style>
