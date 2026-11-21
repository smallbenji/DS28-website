<template>
    <UDashboardPanel id="group-pre-signup">
        <template #header>
            <UDashboardNavbar title="Forhåndstilmelding">
                <template #leading>
                    <UDashboardSidebarCollapse />
                    <UButton to="/group" color="neutral" variant="ghost" icon="i-lucide-arrow-left" aria-label="Tilbage til gruppen" />
                </template>
            </UDashboardNavbar>
        </template>

        <template #body>
            <div class="mx-auto flex w-full max-w-2xl flex-col gap-4">
                <div v-if="loading" class="flex flex-col gap-4">
                    <USkeleton class="h-10 w-64" />
                    <USkeleton class="h-72" />
                </div>

                <UAlert
                    v-else-if="!loaded"
                    color="error"
                    variant="subtle"
                    icon="i-lucide-circle-alert"
                    :title="error || 'Forhåndstilmeldingen kunne ikke hentes.'"
                    :actions="[{ label: 'Prøv igen', color: 'error', variant: 'outline', onClick: load }]"
                />

                <template v-else>
                    <div>
                        <h1 class="text-xl font-semibold text-highlighted">{{ groupName }}</h1>
                        <p class="text-sm text-muted">Gruppenummer {{ groupId }}</p>
                    </div>

                    <UAlert v-if="!isOpen" color="warning" variant="subtle" icon="i-lucide-lock" title="Forhåndstilmeldingen er lukket" description="Du kan se de gemte deltagerantal, men ikke ændre dem." />
                    <UAlert v-if="error" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="error" />

                    <UCard>
                        <template #header>
                            <h2 class="font-semibold text-highlighted">Forventede deltagere</h2>
                            <p class="text-sm text-muted">Ret gruppens forventede deltagerantal i hver gren.</p>
                        </template>

                        <form id="pre-signup-form" class="grid grid-cols-1 gap-4 sm:grid-cols-2" @submit.prevent="save">
                            <UFormField v-for="field in fields" :key="field.key" :label="field.label" required>
                                <UInputNumber
                                    v-model="counts[field.key]"
                                    :min="0"
                                    :max="2147483647"
                                    :step="1"
                                    :disabled="saving || !isOpen"
                                    class="w-full"
                                />
                            </UFormField>
                        </form>

                        <template #footer>
                            <div class="flex items-center justify-between gap-4">
                                <span class="text-sm text-muted">Forventede deltagere i alt</span>
                                <span class="text-2xl font-semibold text-highlighted tabular-nums">{{ total }}</span>
                            </div>
                        </template>
                    </UCard>

                    <div v-if="isOpen" class="flex flex-wrap items-center justify-end gap-2">
                        <span v-if="dirty" class="me-auto flex items-center gap-1.5 text-xs text-warning">
                            <span class="size-2 rounded-full bg-warning" aria-hidden="true" />
                            Ugemte ændringer
                        </span>
                        <UButton color="neutral" variant="ghost" label="Fortryd ændringer" :disabled="!dirty || saving" @click="reset" />
                        <UButton type="submit" form="pre-signup-form" label="Gem ændringer" :loading="saving" :disabled="!valid || !dirty || saving" />
                    </div>
                </template>
            </div>
        </template>
    </UDashboardPanel>
</template>

<script setup lang="ts">
import { computed, onMounted, reactive, ref } from "vue";
import { getErrorMessage } from "@/lib/api";
import { useNotify } from "@/lib/notify";
import GroupPreSignupService, { type ParticipantCounts } from "@/Services/GroupPreSignupService";
import RegistrationSettingsService from "@/Services/RegistrationSettingsService";

const service = new GroupPreSignupService();
const settingsService = new RegistrationSettingsService();
const notify = useNotify();

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
const loading = ref(true);
const loaded = ref(false);
const saving = ref(false);
const error = ref("");
const groupName = ref("");
const groupId = ref("");
const counts = reactive<ParticipantCounts>({ beaver: 0, wolf: 0, junior: 0, trop: 0, senior: 0, rover: 0, leader: 0 });
const original = ref<ParticipantCounts>({ ...counts });

const valid = computed(() => fields.every(({ key }) => Number.isInteger(counts[key]) && counts[key] >= 0 && counts[key] <= 2147483647));
const dirty = computed(() => fields.some(({ key }) => counts[key] !== original.value[key]));
const total = computed(() => fields.reduce((sum, { key }) => sum + (Number(counts[key]) || 0), 0).toLocaleString("da-DK"));

async function load() {
    loading.value = true;
    error.value = "";
    try {
        const [data, settings] = await Promise.all([service.getOwn(), settingsService.get()]);
        isOpen.value = settings.isPreSignupOpen;
        groupName.value = data.groupName;
        groupId.value = data.groupId;
        Object.assign(counts, data.counts);
        original.value = { ...counts };
        loaded.value = true;
    } catch (cause) {
        error.value = getErrorMessage(cause, "Der opstod en fejl. Prøv igen.");
    } finally {
        loading.value = false;
    }
}

function reset() {
    Object.assign(counts, original.value);
    error.value = "";
}

async function save() {
    if (!isOpen.value || !loaded.value || !valid.value || !dirty.value || saving.value) return;

    saving.value = true;
    error.value = "";
    try {
        await service.updateOwn({ ...counts });
        original.value = { ...counts };
        notify.success("Forhåndstilmeldingen er gemt.");
    } catch (cause) {
        error.value = getErrorMessage(cause, "Der opstod en fejl. Prøv igen.");
    } finally {
        saving.value = false;
    }
}

onMounted(load);
</script>
