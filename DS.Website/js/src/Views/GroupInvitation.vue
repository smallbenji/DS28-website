<template>
    <section class="section">
        <div class="columns is-centered"><div class="column is-6-tablet is-5-desktop">
            <div class="box">
                <h1 class="title is-4">Invitation til en gruppe</h1>
                <p v-if="loading" role="status">Henter invitation…</p>
                <BNotification v-if="error" type="is-danger" :closable="false" role="alert">{{ error }}</BNotification>
                <template v-if="invitation">
                    <p class="mb-4">Du er inviteret til <strong>{{ invitation.groupName }}</strong> som <strong>{{ invitation.email }}</strong>.</p>
                    <template v-if="invitation.existingUser && !me.ME.isAuthenticated">
                        <p class="mb-4">Log ind med den inviterede email for at acceptere invitationen.</p>
                        <BButton type="is-primary" @click="login">Log ind</BButton>
                    </template>
                    <template v-else-if="!invitation.existingUser && me.ME.isAuthenticated">
                        <p>Log ud, før du opretter en konto med den inviterede email.</p>
                        <BButton :disabled="busy" @click="logout">Log ud</BButton>
                    </template>
                    <form v-else @submit.prevent="accept">
                        <template v-if="!invitation.existingUser">
                            <BField label="Fornavn"><BInput v-model="firstName" autocomplete="given-name" required :disabled="busy" /></BField>
                            <BField label="Efternavn"><BInput v-model="lastName" autocomplete="family-name" required :disabled="busy" /></BField>
                            <BField label="Adgangskode" message="Mindst 4 tegn"><BInput v-model="password" type="password" autocomplete="new-password" minlength="4" required :disabled="busy" /></BField>
                            <BField label="Gentag adgangskode"><BInput v-model="repeatPassword" type="password" autocomplete="new-password" required :disabled="busy" /></BField>
                        </template>
                        <p v-else class="mb-4">Du er logget ind som {{ me.ME.name }}. Kontoen skal have den inviterede email og må ikke allerede tilhøre en gruppe.</p>
                        <BButton native-type="submit" type="is-primary" :loading="busy" :disabled="busy || !valid">{{ invitation.existingUser ? 'Accepter invitation' : 'Opret bruger og tilslut gruppen' }}</BButton>
                        <BButton v-if="invitation.existingUser" class="mt-3" :disabled="busy" @click="logout">Log ind med en anden konto</BButton>
                    </form>
                </template>
            </div>
        </div></div>
    </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { BButton, BField, BInput, BNotification } from 'buefy';
import axios from 'axios';
import { useMeStore } from '@/Stores/MeStore';
const route = useRoute(); const router = useRouter(); const me = useMeStore();
const invitation = ref<{ email: string; groupName: string; existingUser: boolean } | null>(null);
const firstName = ref(''); const lastName = ref(''); const password = ref(''); const repeatPassword = ref('');
const busy = ref(false); const loading = ref(true); const error = ref('');
const valid = computed(() => invitation.value?.existingUser || (firstName.value.trim() && lastName.value.trim() && password.value.length >= 4 && password.value === repeatPassword.value));
const endpoint = computed(() => `/api/v1/group-invitations/${encodeURIComponent(String(route.params.id))}`);
function fail(e: unknown) { error.value = axios.isAxiosError(e) && typeof e.response?.data === 'string' ? e.response.data : 'Der opstod en fejl. Prøv igen.'; }
function login() { router.push({ path: '/login', query: { returnUrl: route.fullPath } }); }
async function logout() {
    busy.value = true;
    try { await axios.post('/api/v1/auth/logout'); window.location.assign(`/login?returnUrl=${encodeURIComponent(route.fullPath)}`); }
    catch (e) { fail(e); busy.value = false; }
}
async function accept() {
    if (!valid.value || busy.value) return;
    busy.value = true; error.value = '';
    try {
        await axios.post(endpoint.value, { firstName: firstName.value.trim(), lastName: lastName.value.trim(), password: password.value });
        window.location.assign('/group');
    } catch (e) { fail(e); busy.value = false; }
}
onMounted(async () => {
    try { invitation.value = (await axios.get(endpoint.value)).data; }
    catch (e) { fail(e); } finally { loading.value = false; }
});
</script>
