<template>
    <UDashboardPanel id="camp-settings">
        <template #header>
            <UDashboardNavbar title="Lejrindstillinger">
                <template #leading>
                    <UDashboardSidebarCollapse />
                    <UButton to="/admin" color="neutral" variant="ghost" icon="i-lucide-arrow-left" aria-label="Tilbage til admin" />
                </template>
            </UDashboardNavbar>
        </template>

        <template #body>
            <div class="mx-auto flex w-full max-w-2xl flex-col gap-4">
                <UAlert v-if="error" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="error" :actions="[{ label: 'Prøv igen', color: 'error', variant: 'outline', onClick: load }]" />

                <UCard v-if="canManageSignup" :ui="{ body: 'p-0 sm:p-0' }">
                    <template #header>
                        <h2 class="font-semibold text-highlighted">Tilmelding</h2>
                        <p class="text-sm text-muted">Ændringer træder i kraft med det samme.</p>
                    </template>

                    <div v-if="loading" class="flex flex-col gap-3 p-4 sm:p-6">
                        <USkeleton v-for="index in 2" :key="index" class="h-10" />
                    </div>

                    <ul v-else-if="settings" class="divide-y divide-default">
                        <li v-for="option in options" :key="option.key" class="flex items-center justify-between gap-4 px-4 py-4 sm:px-6">
                            <div class="min-w-0">
                                <p :id="`${option.key}-label`" class="flex items-center gap-2 font-medium text-highlighted">
                                    {{ option.label }}
                                    <UBadge :label="settings[option.key] ? 'Åben' : 'Lukket'" :color="settings[option.key] ? 'success' : 'neutral'" variant="subtle" size="sm" />
                                </p>
                                <p class="text-sm text-muted">{{ option.description }}</p>
                            </div>
                            <USwitch
                                :model-value="settings[option.key]"
                                :loading="saving === option.key"
                                :disabled="saving != null"
                                :aria-labelledby="`${option.key}-label`"
                                @update:model-value="(value: boolean) => update(option.key, value)"
                            />
                        </li>
                    </ul>
                </UCard>

                <GoogleDriveConnection v-if="hasRole('AdminAccess')" />

                <HistoricRegistrationSync v-if="hasRole('AdminAccess')" />
            </div>
        </template>
    </UDashboardPanel>
</template>

<script setup lang="ts">
import { onMounted, ref } from "vue";
import RegistrationSettingsService, { type RegistrationSettings } from "@/Services/RegistrationSettingsService";
import { useNotify } from "@/lib/notify";
import { useHasRole } from "@/lib/permissions";
import GoogleDriveConnection from "@/Components/GoogleDriveConnection.vue";
import HistoricRegistrationSync from "@/Components/HistoricRegistrationSync.vue";

type SettingKey = keyof RegistrationSettings;

const service = new RegistrationSettingsService();
const notify = useNotify();
const hasRole = useHasRole();
const canManageSignup = hasRole("PreSignupManage");

const options: { key: SettingKey; label: string; description: string }[] = [
    { key: "isPreSignupOpen", label: "Forhåndstilmelding", description: "Styrer, om grupperne kan forhåndstilmelde sig." },
    { key: "isSignupOpen", label: "Endelig tilmelding", description: "Styrer, om grupperne kan lave den endelige tilmelding." }
];

const settings = ref<RegistrationSettings | null>(null);
const loading = ref(true);
const saving = ref<SettingKey | null>(null);
const error = ref("");

async function load() {
    loading.value = true;
    error.value = "";
    try {
        settings.value = await service.get();
    } catch {
        error.value = "Tilmeldingsstatus kunne ikke hentes.";
    } finally {
        loading.value = false;
    }
}

async function update(key: SettingKey, value: boolean) {
    if (saving.value) return;

    const label = options.find((option) => option.key === key)?.label ?? "";

    saving.value = key;
    error.value = "";
    try {
        settings.value = await service.update({ [key]: value });
        notify.success(`${label} er ${settings.value[key] ? "åbnet" : "lukket"}.`);
    } catch {
        error.value = "Ændringen kunne ikke bekræftes. Kontrollér status, og prøv igen.";
        try {
            settings.value = await service.get();
        } catch {
            settings.value = null;
        }
    } finally {
        saving.value = null;
    }
}

onMounted(() => {
    if (canManageSignup) load();
});
</script>
