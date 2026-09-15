<template>
    <section class="section">
        <div class="columns is-centered">
            <div class="column is-8-tablet is-6-desktop">
                <div class="card">
                    <header class="card-header">
                        <h1 class="card-header-title">Forhåndstilmelding for spejdergrupper</h1>
                    </header>
                    <div class="card-content">
                        <p v-if="statusLoading" role="status">Henter tilmeldingsstatus…</p>
                        <BNotification v-else-if="!isOpen && !statusError" type="is-warning" :closable="false">
                            Forhåndstilmeldingen er lukket.
                        </BNotification>
                        <BNotification v-if="statusError" type="is-danger" :closable="false">
                            Tilmeldingsstatus kunne ikke hentes.
                            <BButton @click="loadStatus">Prøv igen</BButton>
                        </BNotification>
                        <BSteps v-if="!statusLoading && isOpen && !statusError" v-model="step" :has-navigation="false" :animated="false">
                            <BStepItem label="Find gruppe" :clickable="false">
                                <form @submit.prevent="lookup">
                                    <p class="mb-4">Indtast jeres gruppenummer for at finde gruppen.</p>
                                    <BField label="Gruppenummer">
                                        <BInput v-model="groupNumber" required inputmode="numeric" pattern="[0-9]+" :disabled="busy" />
                                    </BField>
                                    <BButton native-type="submit" type="is-primary" :loading="busy" :disabled="!validGroupNumber || busy">Find gruppe</BButton>
                                </form>
                            </BStepItem>
                            <BStepItem label="Deltagere" :clickable="false">
                                <form @submit.prevent="next">
                                    <p class="mb-4"><strong>{{ group?.name }}</strong> · Gruppenummer {{ group?.id }} · {{ group?.district }}</p>
                                    <p class="mb-4">Angiv det forventede antal deltagere i hver gren. Antallene er foreløbige.</p>
                                    <div class="columns is-multiline">
                                        <div v-for="field in fields" :key="field.key" class="column is-half">
                                            <BField :label="field.label">
                                                <BInput v-model.number="counts[field.key]" type="number" min="0" max="2147483647" step="1" required />
                                            </BField>
                                        </div>
                                    </div>
                                    <p class="mb-4">Forventede deltagere i alt: <strong>{{ total }}</strong></p>
                                    <div class="buttons">
                                        <BButton @click="back">Tilbage</BButton>
                                        <BButton native-type="submit" type="is-primary" :disabled="!validCounts">Fortsæt</BButton>
                                    </div>
                                </form>
                            </BStepItem>
                            <BStepItem label="Opret bruger" :clickable="false">
                                <form @submit.prevent="submit">
                                    <p class="mb-4">Du opretter en bruger til <strong>{{ group?.name }}</strong> med {{ total }} forventede deltagere.</p>
                                    <div class="columns">
                                        <div class="column">
                                            <BField label="Fornavn"><BInput v-model="firstName" autocomplete="given-name" required :disabled="busy" /></BField>
                                        </div>
                                        <div class="column">
                                            <BField label="Efternavn"><BInput v-model="lastName" autocomplete="family-name" required :disabled="busy" /></BField>
                                        </div>
                                    </div>
                                    <BField label="Email"><BInput v-model="email" type="email" autocomplete="email" required :disabled="busy" /></BField>
                                    <BField label="Adgangskode" message="Mindst 4 tegn.">
                                        <BInput v-model="password" type="password" autocomplete="new-password" password-reveal minlength="4" required :disabled="busy" />
                                    </BField>
                                    <BField label="Gentag adgangskode" :message="repeatPassword && password !== repeatPassword ? 'Adgangskoderne skal være ens.' : ''">
                                        <BInput v-model="repeatPassword" type="password" autocomplete="new-password" password-reveal required :disabled="busy" />
                                    </BField>
                                    <p class="my-4">Når du afslutter, gemmes forhåndstilmeldingen, og du bliver logget ind på gruppens side.</p>
                                    <div class="buttons">
                                        <BButton :disabled="busy" @click="back">Tilbage</BButton>
                                        <BButton native-type="submit" type="is-primary" :loading="busy" :disabled="!canSubmit || busy">Afslut forhåndstilmelding</BButton>
                                    </div>
                                </form>
                            </BStepItem>
                        </BSteps>
                        <div v-if="error" role="alert" class="notification is-danger is-light mt-4">{{ error }}</div>
                        <p class="has-text-centered mt-4">Har du allerede en bruger? <router-link to="/login?returnUrl=/group">Log ind</router-link></p>
                    </div>
                </div>
            </div>
        </div>
    </section>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from 'vue';
import { BButton, BField, BInput, BSteps, BStepItem, BNotification } from 'buefy';
import RegistrationSettingsService from '@/Services/RegistrationSettingsService';
import axios from 'axios';
import GroupPreSignupService, { type ParticipantCounts, type SignupGroup } from '@/Services/GroupPreSignupService';

const service = new GroupPreSignupService();
const settingsService = new RegistrationSettingsService();
const isOpen = ref(false);
const statusLoading = ref(true);
const statusError = ref(false);

async function loadStatus() {
    statusLoading.value = true;
    statusError.value = false;
    try {
        isOpen.value = (await settingsService.get()).isPreSignupOpen;
    } catch {
        statusError.value = true;
        isOpen.value = false;
    } finally {
        statusLoading.value = false;
    }
}

onMounted(loadStatus);
const step = ref(0);
const busy = ref(false);
const error = ref('');
const groupNumber = ref('');
const group = ref<SignupGroup | null>(null);
const firstName = ref('');
const lastName = ref('');
const email = ref('');
const password = ref('');
const repeatPassword = ref('');
const counts = reactive<ParticipantCounts>({ beaver: 0, wolf: 0, junior: 0, trop: 0, senior: 0, rover: 0, leader: 0 });
const fields: { key: keyof ParticipantCounts; label: string }[] = [
    { key: 'beaver', label: 'Bævere' }, { key: 'wolf', label: 'Ulve' },
    { key: 'junior', label: 'Juniorer' }, { key: 'trop', label: 'Tropsspejdere' },
    { key: 'senior', label: 'Seniorer' }, { key: 'rover', label: 'Rovere' },
    { key: 'leader', label: 'Ledere' }
];
const validGroupNumber = computed(() => /^\d+$/.test(groupNumber.value) && Number(groupNumber.value) > 0 && Number(groupNumber.value) <= 2147483647);
const validCounts = computed(() => Object.values(counts).every(n => Number.isInteger(n) && n >= 0 && n <= 2147483647));
const total = computed(() => Object.values(counts).reduce((sum, n) => sum + Number(n), 0));
const canSubmit = computed(() => group.value && validCounts.value && firstName.value.trim() && lastName.value.trim() && email.value.trim() && password.value.length >= 4 && password.value === repeatPassword.value);

function showError(cause: unknown) {
    error.value = axios.isAxiosError(cause) && typeof cause.response?.data === 'string'
        ? cause.response.data : 'Der opstod en fejl. Prøv igen. Hvis tilmeldingen allerede blev gemt, kan du logge ind med din nye bruger.';
}

async function lookup() {
    if (!isOpen.value || !validGroupNumber.value || busy.value) return;
    busy.value = true;
    error.value = '';
    group.value = null;
    try {
        group.value = await service.lookup(String(Number(groupNumber.value)));
        step.value = 1;
    } catch (cause) { showError(cause); }
    finally { busy.value = false; }
}

function back() { error.value = ''; step.value--; }
function next() { if (validCounts.value && group.value) { error.value = ''; step.value = 2; } }

async function submit() {
    if (!isOpen.value || !canSubmit.value || !group.value || busy.value) return;
    busy.value = true;
    error.value = '';
    try {
        await service.create({ ...counts, groupId: group.value.id, firstName: firstName.value.trim(), lastName: lastName.value.trim(), email: email.value.trim(), password: password.value });
        // Reload authenticated state and land on the group's own page.
        window.location.assign('/group');
    } catch (cause) { showError(cause); busy.value = false; }
}
</script>
