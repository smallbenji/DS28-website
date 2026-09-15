<template>
    <div class="box pre-signup-toggle">
        <h2 class="title is-5">Forhåndstilmelding</h2>
        <p v-if="loading" role="status">Henter tilmeldingsstatus…</p>
        <template v-else-if="loaded">
            <BSwitch :key="switchVersion" :model-value="isOpen" :disabled="saving" @update:model-value="update">
                {{ isOpen ? 'Åben' : 'Lukket' }}
            </BSwitch>
            <p class="help mt-2">Ændringen gemmes med det samme og gælder for alle grupper.</p>
            <p class="help">Når der er lukket, kan grupper se deres tilmelding, men ikke oprette en ny eller ændre deltagerantal.</p>
        </template>
        <BNotification v-if="error" type="is-danger" :closable="false" class="mt-3" role="alert">{{ error }}</BNotification>
        <BButton v-if="!loaded && !loading" @click="load">Prøv igen</BButton>
        <p v-if="saved" class="help has-text-success" role="status">Indstillingen er gemt.</p>
    </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { BButton, BNotification, BSwitch } from 'buefy';
import RegistrationSettingsService from '@/Services/RegistrationSettingsService';

const service = new RegistrationSettingsService();
const isOpen = ref(false);
const switchVersion = ref(0);
const loaded = ref(false);
const loading = ref(true);
const saving = ref(false);
const saved = ref(false);
const error = ref('');

async function load() {
    loading.value = true;
    error.value = '';

    try {
        isOpen.value = (await service.get()).isPreSignupOpen;
        loaded.value = true;
    } catch {
        error.value = 'Tilmeldingsstatus kunne ikke hentes.';
    } finally {
        loading.value = false;
    }
}

async function update(value: unknown) {
    if (saving.value || typeof value !== 'boolean') return;
    saving.value = true;
    saved.value = false;
    error.value = '';

    try {
        isOpen.value = (await service.update(value)).isPreSignupOpen;
        saved.value = true;
    } catch {
        error.value = 'Ændringen kunne ikke bekræftes. Kontrollér status, og prøv igen.';
        loaded.value = false;
        try {
            isOpen.value = (await service.get()).isPreSignupOpen;
            loaded.value = true;
        } catch {
            // Keep the switch hidden until the persisted state can be retrieved.
        }
        switchVersion.value++;
    } finally {
        saving.value = false;
    }
}

onMounted(load);
</script>

<style scoped>
.pre-signup-toggle { max-width: 52rem; margin: 1rem auto; }
</style>
