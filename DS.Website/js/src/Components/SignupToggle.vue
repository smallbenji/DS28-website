<template>
    <div class="signup-toggle">
        <p>Endelig tilmelding</p>
        <BSwitch :key="switchVersion" :model-value="isOpen" :disabled="saving" @update:model-value="update">
            <!-- {{ isOpen ? 'Åben' : 'Lukket' }} -->
        </BSwitch>
    </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue';
import { BSwitch } from 'buefy';
import RegistrationSettingsService from '@/Services/RegistrationSettingsService';

const service = new RegistrationSettingsService();
const isOpen = ref(false);
const switchVersion = ref(0);
const saving = ref(false);
const error = ref('');

async function load() {
    error.value = '';

    try {
        isOpen.value = (await service.get()).isSignupOpen;
    } catch {
        error.value = 'Tilmeldingsstatus kunne ikke hentes.';
    }
}

async function update(value: unknown) {
    if (saving.value || typeof value !== 'boolean') return;
    saving.value = true;
    error.value = '';

    try {
        isOpen.value = (await service.update({ isSignupOpen: value })).isSignupOpen;
    } catch {
        error.value = 'Ændringen kunne ikke bekræftes. Kontrollér status, og prøv igen.';
        try {
            isOpen.value = (await service.get()).isSignupOpen;
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
.signup-toggle {
    margin: 1rem auto;
    display: flex;
    gap: 1rem;
}
</style>
