<template>
    <UCard>
        <template #header>
            <div class="flex items-center gap-2">
                <UIcon name="i-lucide-refresh-cw" class="size-5 text-muted" />
                <h2 class="font-semibold text-highlighted">Historisk tilmelding</h2>
            </div>
            <p class="text-sm text-muted">
                Synkroniserer historiske tilmeldinger fra Firebase som et baggrundsjob i workeren
                for det lejrår, der er valgt i opsætningen.
            </p>
        </template>

        <UButton icon="i-lucide-refresh-cw" label="Start synkronisering" :loading="syncing" @click="confirmOpen = true" />

        <ConfirmModal
            v-model:open="confirmOpen"
            title="Historisk tilmelding"
            description="Erstat årets historiske tilmeldinger med data fra Firebase?"
            confirm-label="Start"
            :loading="syncing"
            @confirm="startSync"
        />
    </UCard>
</template>

<script setup lang="ts">
import { ref } from "vue";
import AdminService from "@/Services/AdminService";
import { getErrorMessage } from "@/lib/api";
import { useNotify } from "@/lib/notify";
import ConfirmModal from "@/Components/ConfirmModal.vue";

const service = new AdminService();
const notify = useNotify();

const syncing = ref(false);
const confirmOpen = ref(false);

async function startSync() {
    if (syncing.value) return;

    syncing.value = true;
    try {
        await service.syncHistoricRegistration();
        notify.success("Synkroniseringen er sat i gang.");
    } catch (e) {
        notify.error(getErrorMessage(e, "Synkroniseringen kunne ikke startes."));
    } finally {
        syncing.value = false;
        confirmOpen.value = false;
    }
}
</script>
