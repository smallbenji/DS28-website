<template>
    <UCard>
        <template #header>
            <div class="flex items-center gap-2">
                <UIcon name="i-lucide-hard-drive" class="size-5 text-muted" />
                <h2 class="font-semibold text-highlighted">Google Drev</h2>
                <UBadge v-if="!loading && status" :label="statusLabel" :color="statusColor" variant="subtle" size="sm" />
            </div>
            <p class="text-sm text-muted">Dokumenter til aktiviteter oprettes i den forbundne kontos Google Drev.</p>
        </template>

        <USkeleton v-if="loading" class="h-10" />

        <UAlert v-else-if="!status" color="error" variant="subtle" icon="i-lucide-circle-alert" title="Status for Google Drev kunne ikke hentes." :actions="[{ label: 'Prøv igen', color: 'error', variant: 'outline', onClick: load }]" />

        <div v-else-if="!status.configured" class="text-sm text-muted">
            Integrationen er ikke konfigureret. Sæt
            <code>DS__GoogleClientId</code>, <code>DS__GoogleClientSecret</code>,
            <code>DS__GoogleRedirectUri</code> og <code>DS__GoogleDriveBaseFolderId</code>,
            og genstart appen.
        </div>

        <div v-else class="flex flex-wrap items-center justify-between gap-4">
            <p v-if="status.connected" class="text-sm">
                Forbundet som <strong>{{ status.accountEmail }}</strong>.
            </p>
            <p v-else class="text-sm text-muted">Ikke forbundet. Forbind en konto for at kunne oprette dokumenter til aktiviteter.</p>

            <UButton v-if="status.connected" color="error" variant="outline" icon="i-lucide-unlink" label="Afbryd forbindelse" @click="disconnectOpen = true" />
            <UButton v-else icon="i-lucide-link" label="Forbind Google Drev" @click="connect" />
        </div>

        <ConfirmModal
            v-model:open="disconnectOpen"
            title="Afbryd forbindelse til Google Drev"
            description="Der kan ikke oprettes dokumenter til aktiviteter, før en konto er forbundet igen. Eksisterende dokumenter bliver liggende i Google Drev."
            confirm-label="Afbryd forbindelse"
            color="error"
            :loading="disconnecting"
            @confirm="disconnect"
        />
    </UCard>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import GoogleService from "@/Services/GoogleService";
import { useNotify } from "@/lib/notify";
import ConfirmModal from "@/Components/ConfirmModal.vue";
import type { GoogleConnectionStatusDto } from "@/types";

const googleService = new GoogleService();
const notify = useNotify();
const route = useRoute();
const router = useRouter();

const status = ref<GoogleConnectionStatusDto | null>(null);
const loading = ref(true);
const disconnectOpen = ref(false);
const disconnecting = ref(false);

const statusLabel = computed(() => {
    if (!status.value?.configured) return "Ikke konfigureret";

    return status.value.connected ? "Forbundet" : "Ikke forbundet";
});

const statusColor = computed(() => status.value?.connected ? "success" : "neutral");

function connect() {
    window.location.href = "/api/v1/google/connect";
}

async function disconnect() {
    if (disconnecting.value || !status.value) return;

    disconnecting.value = true;
    try {
        await googleService.disconnect();
        status.value = { ...status.value, connected: false, accountEmail: null };
        disconnectOpen.value = false;
        notify.success("Forbindelsen til Google Drev er afbrudt.");
    } catch {
        notify.error("Forbindelsen kunne ikke afbrydes.");
    } finally {
        disconnecting.value = false;
    }
}

async function load() {
    loading.value = true;
    try {
        status.value = await googleService.getStatus();
    } catch {
        status.value = null;
    } finally {
        loading.value = false;
    }
}

onMounted(async () => {
    await load();

    if (route.query.connected) {
        notify.success("Google Drev er forbundet.");
    } else if (route.query.error) {
        notify.error("Google Drev kunne ikke forbindes. Prøv igen.");
    }

    if (route.query.connected || route.query.error) {
        router.replace({ path: route.path });
    }
});
</script>
