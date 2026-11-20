<template>
    <UCard>
        <template #header>
            <h1 class="text-center text-lg font-semibold text-highlighted">Forhåndstilmelding for spejdergrupper</h1>
        </template>

        <div v-if="statusLoading" class="flex flex-col gap-3">
            <USkeleton v-for="index in 3" :key="index" class="h-10" />
        </div>

        <UAlert
            v-else-if="statusError"
            color="error"
            variant="subtle"
            icon="i-lucide-circle-alert"
            title="Tilmeldingsstatus kunne ikke hentes."
            :actions="[{ label: 'Prøv igen', color: 'error', variant: 'outline', onClick: loadStatus }]"
        />

        <UAlert v-else-if="!isOpen" color="warning" variant="subtle" icon="i-lucide-lock" title="Forhåndstilmeldingen er lukket." />

        <div v-else class="flex flex-col gap-6">
            <UStepper :model-value="step" :items="steps" disabled size="sm" />

            <form v-if="step === 0" class="flex flex-col gap-4" @submit.prevent="lookup">
                <p class="text-sm">Indtast jeres gruppenummer for at finde gruppen.</p>
                <UFormField label="Gruppenummer" required>
                    <UInput v-model="groupNumber" inputmode="numeric" pattern="[0-9]+" :disabled="busy" class="w-full" autofocus />
                </UFormField>
                <UButton type="submit" icon="i-lucide-search" label="Find gruppe" block :loading="busy" :disabled="!validGroupNumber || busy" />
            </form>

            <form v-else-if="step === 1" class="flex flex-col gap-4" @submit.prevent="next">
                <p class="text-sm"><strong>{{ group?.name }}</strong> · Gruppenummer {{ group?.id }} · {{ districtLabel(group?.district) }}</p>
                <p class="text-sm text-muted">Angiv det forventede antal deltagere i hver gren. Antallene er foreløbige.</p>
                <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
                    <UFormField v-for="field in fields" :key="field.key" :label="field.label" required>
                        <UInputNumber v-model="counts[field.key]" :min="0" :max="2147483647" :step="1" class="w-full" />
                    </UFormField>
                </div>
                <div class="flex items-center justify-between rounded-md bg-elevated px-3 py-2 text-sm">
                    <span class="text-muted">Forventede deltagere i alt</span>
                    <strong class="text-lg tabular-nums">{{ total }}</strong>
                </div>
                <div class="flex justify-between gap-2">
                    <UButton color="neutral" variant="outline" icon="i-lucide-arrow-left" label="Tilbage" @click="back" />
                    <UButton type="submit" trailing-icon="i-lucide-arrow-right" label="Fortsæt" :disabled="!validCounts" />
                </div>
            </form>

            <form v-else class="flex flex-col gap-4" @submit.prevent="submit">
                <p class="text-sm">Du opretter en bruger til <strong>{{ group?.name }}</strong> med {{ total }} forventede deltagere.</p>
                <div class="grid gap-4 sm:grid-cols-2">
                    <UFormField label="Fornavn" required>
                        <UInput v-model="firstName" autocomplete="given-name" :disabled="busy" class="w-full" />
                    </UFormField>
                    <UFormField label="Efternavn" required>
                        <UInput v-model="lastName" autocomplete="family-name" :disabled="busy" class="w-full" />
                    </UFormField>
                </div>
                <UFormField label="Email" required>
                    <UInput v-model="email" type="email" icon="i-lucide-mail" autocomplete="email" :disabled="busy" class="w-full" />
                </UFormField>
                <PasswordFields v-model:password="password" v-model:repeat-password="repeatPassword" :disabled="busy" />
                <p class="text-sm text-muted">Når du afslutter, gemmes forhåndstilmeldingen, og du bliver logget ind på gruppens side.</p>
                <div class="flex justify-between gap-2">
                    <UButton color="neutral" variant="outline" icon="i-lucide-arrow-left" label="Tilbage" :disabled="busy" @click="back" />
                    <UButton type="submit" icon="i-lucide-check" label="Afslut forhåndstilmelding" :loading="busy" :disabled="!canSubmit || busy" />
                </div>
            </form>
        </div>

        <UAlert v-if="error" class="mt-4" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="error" />

        <p class="mt-4 text-center text-sm">
            Har du allerede en bruger?
            <ULink :to="{ path: '/login', query: { returnUrl: '/beta/group' } }" class="text-primary">Log ind</ULink>
        </p>
    </UCard>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue";
import RegistrationSettingsService from "@/Services/RegistrationSettingsService";
import GroupPreSignupService, { type ParticipantCounts, type SignupGroup } from "@/Services/GroupPreSignupService";
import { getErrorMessage } from "@/lib/api";
import PasswordFields from "@/Components/Auth/PasswordFields.vue";
import { districtLabel } from "@/types";

const service = new GroupPreSignupService();
const settingsService = new RegistrationSettingsService();

const steps = [
    { title: "Find gruppe", icon: "i-lucide-search" },
    { title: "Deltagere", icon: "i-lucide-users" },
    { title: "Opret bruger", icon: "i-lucide-user-plus" }
];

const fields: { key: keyof ParticipantCounts; label: string }[] = [
    { key: "beaver", label: "Bævere" },
    { key: "wolf", label: "Ulve" },
    { key: "junior", label: "Juniorer" },
    { key: "trop", label: "Tropsspejdere" },
    { key: "senior", label: "Seniorer" },
    { key: "rover", label: "Rovere" },
    { key: "leader", label: "Ledere" }
];

const isOpen = ref(false);
const statusLoading = ref(true);
const statusError = ref(false);
const step = ref(0);
const busy = ref(false);
const error = ref("");
const groupNumber = ref("");
const group = ref<SignupGroup | null>(null);
const firstName = ref("");
const lastName = ref("");
const email = ref("");
const password = ref("");
const repeatPassword = ref("");
const counts = reactive<ParticipantCounts>({ beaver: 0, wolf: 0, junior: 0, trop: 0, senior: 0, rover: 0, leader: 0 });

const validGroupNumber = computed(() => /^\d+$/.test(groupNumber.value) && Number(groupNumber.value) > 0 && Number(groupNumber.value) <= 2147483647);
const validCounts = computed(() => Object.values(counts).every((value) => Number.isInteger(value) && value >= 0 && value <= 2147483647));
const total = computed(() => Object.values(counts).reduce((sum, value) => sum + (Number(value) || 0), 0));
const canSubmit = computed(() => !!group.value
    && validCounts.value
    && firstName.value.trim() !== ""
    && lastName.value.trim() !== ""
    && email.value.trim() !== ""
    && password.value.length >= 4
    && password.value === repeatPassword.value);

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

function showError(cause: unknown) {
    error.value = getErrorMessage(cause, "Der opstod en fejl. Prøv igen. Hvis tilmeldingen allerede blev gemt, kan du logge ind med din nye bruger.");
}

async function lookup() {
    if (!isOpen.value || !validGroupNumber.value || busy.value) return;

    busy.value = true;
    error.value = "";
    group.value = null;
    try {
        group.value = await service.lookup(String(Number(groupNumber.value)));
        if (group.value.counts) {
            for (const [key, value] of Object.entries(group.value.counts)) {
                if (key in counts) counts[key as keyof ParticipantCounts] = Number(value);
            }
        }
        step.value = 1;
    } catch (cause) {
        showError(cause);
    } finally {
        busy.value = false;
    }
}

function back() {
    error.value = "";
    step.value--;
}

function next() {
    if (!validCounts.value || !group.value) return;

    error.value = "";
    step.value = 2;
}

async function submit() {
    if (!isOpen.value || !canSubmit.value || !group.value || busy.value) return;

    busy.value = true;
    error.value = "";
    try {
        await service.create({
            ...counts,
            groupId: group.value.id,
            firstName: firstName.value.trim(),
            lastName: lastName.value.trim(),
            email: email.value.trim(),
            password: password.value
        });
        window.location.assign("/beta/group");
    } catch (cause) {
        showError(cause);
        busy.value = false;
    }
}

onMounted(loadStatus);
</script>
