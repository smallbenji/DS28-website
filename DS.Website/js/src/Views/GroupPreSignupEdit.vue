<template>
    <section class="section">
        <div class="columns is-centered">
            <div class="column is-8-tablet is-6-desktop">
                <div class="card">
                    <header class="card-header">
                        <h2 class="card-header-title">Rediger forhåndstilmelding</h2>
                    </header>
                    <template v-if="loading">
                        <div class="card-content">
                            <p role="status">Henter forhåndstilmeldingen…</p>
                        </div>
                    </template>
                    <form v-else-if="loaded" @submit.prevent="save">
                        <div class="card-content">
                            <BNotification v-if="!isOpen" type="is-warning" :closable="false">
                                Forhåndstilmeldingen er lukket. Du kan se de gemte deltagerantal, men ikke ændre dem.
                            </BNotification>
                            <BNotification v-if="error" type="is-danger" :closable="false" role="alert">{{ error }}</BNotification>
                            <BNotification v-if="saved && !dirty" type="is-success" :closable="false" role="status">Forhåndstilmeldingen er gemt.</BNotification>
                            <p class="mb-4"><strong>{{ groupName }}</strong> · Gruppenummer {{ groupId }}</p>
                            <p class="mb-4">Ret gruppens forventede deltagerantal i hver gren.</p>
                            <div class="columns">
                                <div class="column is-half">
                                    <BField v-for="field in leftFields" :key="field.key" :label="field.label">
                                        <BInput v-model.number="counts[field.key]" type="number" min="0" max="2147483647"
                                            step="1" required :disabled="saving || !isOpen" />
                                    </BField>
                                </div>
                                <div class="column is-half">
                                    <BField v-for="field in rightFields" :key="field.key" :label="field.label">
                                        <BInput v-model.number="counts[field.key]" type="number" min="0" max="2147483647"
                                            step="1" required :disabled="saving || !isOpen" />
                                    </BField>
                                </div>
                            </div>
                            <BNotification :closable="false">
                                Forventede deltagere i alt: <strong>{{ total }}</strong>
                            </BNotification>
                        </div>
                        <footer class="card-footer px-5 py-4 is-align-items-center is-justify-content-space-between">
                            <BButton :disabled="saving" @click="router.push('/group')">Tilbage til gruppen</BButton>
                            <div v-if="isOpen" class="buttons mb-0">
                                <BButton :disabled="!dirty || saving" @click="reset">Fortryd ændringer</BButton>
                                <BButton native-type="submit" type="is-primary" :loading="saving" :disabled="!valid || !dirty || saving">Gem ændringer</BButton>
                            </div>
                        </footer>
                    </form>
                    <template v-else>
                        <div class="card-content">
                            <BNotification v-if="error" type="is-danger" :closable="false" role="alert">{{ error }}</BNotification>
                            <BButton class="mb-4" @click="load">Prøv igen</BButton>
                        </div>
                        <footer class="card-footer px-5 py-4">
                            <BButton :disabled="saving" @click="router.push('/group')">Tilbage til gruppen</BButton>
                        </footer>
                    </template>
                </div>
            </div>
        </div>
    </section>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import { useRouter } from 'vue-router';
import { BButton, BField, BInput, BNotification } from 'buefy';
import axios from 'axios';
import GroupPreSignupService, { type ParticipantCounts } from '@/Services/GroupPreSignupService';
import RegistrationSettingsService from '@/Services/RegistrationSettingsService';

const service = new GroupPreSignupService();
const settingsService = new RegistrationSettingsService();
const isOpen = ref(false);
const router = useRouter();
const loading = ref(true);
const loaded = ref(false);
const saving = ref(false);
const saved = ref(false);
const error = ref('');
const groupName = ref('');
const groupId = ref('');
const counts = reactive<ParticipantCounts>({ beaver: 0, wolf: 0, junior: 0, trop: 0, senior: 0, rover: 0, leader: 0 });
const original = ref<ParticipantCounts>({ ...counts });
const leftFields: { key: keyof ParticipantCounts; label: string }[] = [
    { key: 'beaver', label: 'Bævere' },
    { key: 'wolf', label: 'Ulve' },
    { key: 'junior', label: 'Juniorer' },
    { key: 'leader', label: 'Ledere' }
];
const rightFields: { key: keyof ParticipantCounts; label: string }[] = [
    { key: 'trop', label: 'Tropsspejdere' },
    { key: 'senior', label: 'Seniorer' },
    { key: 'rover', label: 'Rovere' }
];
const fields = [...leftFields, ...rightFields];
const valid = computed(() => fields.every(({ key }) => Number.isInteger(counts[key]) && counts[key] >= 0 && counts[key] <= 2147483647));
const dirty = computed(() => fields.some(({ key }) => counts[key] !== original.value[key]));
const total = computed(() => fields.reduce((sum, { key }) => sum + Number(counts[key]), 0));

function showError(cause: unknown) {
    error.value = axios.isAxiosError(cause) && typeof cause.response?.data === 'string'
        ? cause.response.data : 'Der opstod en fejl. Prøv igen.';
}

async function load() {
    loading.value = true;
    error.value = '';
    try {
        const [data, settings] = await Promise.all([service.getOwn(), settingsService.get()]);
        isOpen.value = settings.isPreSignupOpen;
        groupName.value = data.groupName;
        groupId.value = data.groupId;
        Object.assign(counts, data.counts);
        original.value = { ...counts };
        loaded.value = true;
    } catch (cause) { showError(cause); }
    finally { loading.value = false; }
}

function reset() {
    Object.assign(counts, original.value);
    error.value = '';
    saved.value = false;
}

async function save() {
    if (!isOpen.value || !loaded.value || !valid.value || !dirty.value || saving.value) return;
    saving.value = true;
    saved.value = false;
    error.value = '';
    try {
        await service.updateOwn({ ...counts });
        original.value = { ...counts };
        saved.value = true;
    } catch (cause) { showError(cause); }
    finally { saving.value = false; }
}

onMounted(load);
</script>
