<template>
    <section class="section">
        <div class="container">
            <div class="box google-card">
                <div class="google-main">
                    <p class="title is-5 google-title">
                        <font-awesome-icon icon="globe" />
                        Google Drev
                    </p>

                    <p v-if="loading" role="status">Indlæser status…</p>

                    <template v-else>
                        <p v-if="!status.configured" class="google-description">
                            Google-integrationen er ikke konfigureret. Sæt
                            <code>DS__GoogleClientId</code>, <code>DS__GoogleClientSecret</code>,
                            <code>DS__GoogleRedirectUri</code> og <code>DS__GoogleDriveBaseFolderId</code>,
                            og genstart appen.
                        </p>

                        <p v-else-if="status.connected" class="google-description">
                            Forbundet som <strong>{{ status.accountEmail }}</strong>. Dokumenter til
                            aktiviteter oprettes i denne brugers Google Drev.
                        </p>

                        <p v-else class="google-description">
                            Google Drev er ikke forbundet endnu. Forbind en konto for at kunne oprette
                            dokumenter til aktiviteter.
                        </p>
                    </template>
                </div>

                <div class="google-actions">
                    <BButton
                        v-if="!loading && status.configured && !status.connected"
                        type="is-primary"
                        icon-left="plus"
                        @click="connect"
                    >
                        Forbind Google Drev
                    </BButton>

                    <BButton
                        v-if="!loading && status.connected"
                        type="is-danger"
                        outlined
                        icon-left="trash"
                        :loading="disconnecting"
                        @click="disconnect"
                    >
                        Afbryd forbindelse
                    </BButton>
                </div>
            </div>
        </div>
    </section>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { useRoute, useRouter } from 'vue-router';
import { BButton, useToast } from 'buefy';
import GoogleService from '@/Services/GoogleService';
import type { GoogleConnectionStatusDto } from '@/types';

const googleService = new GoogleService();
const Toast = useToast();
const route = useRoute();
const router = useRouter();

const status = ref<GoogleConnectionStatusDto>({ configured: false, connected: false, accountEmail: null });
const loading = ref(true);
const disconnecting = ref(false);

function connect() {
    window.location.href = '/api/v1/google/connect';
}

async function disconnect() {
    if (disconnecting.value) return;

    disconnecting.value = true;
    try {
        await googleService.disconnect();
        status.value = { ...status.value, connected: false, accountEmail: null };
        Toast.open({ message: 'Forbindelsen til Google Drev er afbrudt', type: 'is-success' });
    } catch {
        Toast.open({ message: 'Forbindelsen kunne ikke afbrydes', type: 'is-danger' });
    } finally {
        disconnecting.value = false;
    }
}

async function load() {
    loading.value = true;
    try {
        status.value = await googleService.getStatus();
    } catch {
        Toast.open({ message: 'Status for Google Drev kunne ikke hentes', type: 'is-danger' });
    } finally {
        loading.value = false;
    }
}

onMounted(async () => {
    await load();

    if (route.query.connected) {
        Toast.open({ message: 'Google Drev er forbundet', type: 'is-success' });
    } else if (route.query.error) {
        Toast.open({ message: 'Google Drev kunne ikke forbindes. Prøv igen.', type: 'is-danger' });
    }

    if (route.query.connected || route.query.error) {
        router.replace({ path: route.path });
    }
});
</script>

<style scoped>
.google-card {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1.5rem;
    flex-wrap: wrap;
}

.google-main {
    flex: 1 1 20rem;
    min-width: 0;
}

.google-title {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-bottom: 0.35rem;
}

.google-title svg {
    color: rgba(0, 0, 0, 0.4);
}

.google-description {
    color: rgba(0, 0, 0, 0.6);
}

.google-actions {
    display: flex;
    gap: 0.75rem;
}
</style>
