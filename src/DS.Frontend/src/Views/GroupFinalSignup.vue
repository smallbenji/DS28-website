<template>
    <UDashboardPanel id="group-final-signup">
        <template #header>
            <UDashboardNavbar title="Endelig tilmelding">
                <template #leading>
                    <UDashboardSidebarCollapse />
                    <UButton to="/group" color="neutral" variant="ghost" icon="i-lucide-arrow-left" aria-label="Tilbage til gruppen" />
                </template>
            </UDashboardNavbar>
        </template>

        <template #body>
            <div class="mx-auto flex w-full max-w-5xl flex-col gap-4">
                <div v-if="loading" class="flex flex-col gap-6">
                    <USkeleton class="h-24" />
                    <USkeleton class="h-72" />
                </div>

                <UEmpty
                    v-else-if="loadFailed"
                    icon="i-lucide-circle-alert"
                    title="Gruppen kunne ikke hentes"
                    description="Der opstod en fejl. Prøv igen om lidt."
                    :actions="[{ label: 'Prøv igen', icon: 'i-lucide-refresh-cw', onClick: load }]"
                />

                <UEmpty
                    v-else-if="!hasGroup"
                    icon="i-lucide-users-round"
                    title="Du er ikke tilknyttet en gruppe"
                    description="Kontakt lejrens ledelse, hvis du skal tilknyttes en spejdergruppe."
                />

                <template v-else>
                    <div>
                        <h1 class="text-xl font-semibold text-highlighted">{{ Group.name }}</h1>
                        <p class="text-sm text-muted">Gruppenummer {{ Group.id }}</p>
                    </div>

                    <UAlert v-if="error" color="error" variant="subtle" icon="i-lucide-circle-alert" :title="error" />

                    <UAlert
                        v-if="campDaysLoadFailed"
                        color="error"
                        variant="subtle"
                        icon="i-lucide-circle-alert"
                        title="Lejrdagene kunne ikke hentes."
                        description="Spejdernes dage kan ikke vises eller ændres lige nu."
                        :actions="[{ label: 'Prøv igen', color: 'error', variant: 'outline', onClick: loadCampDays }]"
                    />

                    <UAlert
                        v-else-if="!hasCampDays"
                        color="info"
                        variant="subtle"
                        icon="i-lucide-calendar-clock"
                        title="Lejrens dage er ikke fastlagt endnu"
                        description="Du kan vælge, hvilke dage spejderne deltager, når lejrens ledelse har oprettet lejrdagene."
                    />

                    <template v-else>
                        <UAlert
                            v-if="missingScouts.length"
                            color="warning"
                            variant="subtle"
                            icon="i-lucide-calendar-x"
                            :title="missingTitle"
                            :description="missingNames"
                        >
                            <template #actions>
                                <div class="flex flex-wrap gap-2">
                                    <UButton
                                        color="warning"
                                        variant="outline"
                                        size="sm"
                                        :icon="showOnlyMissing ? 'i-lucide-list' : 'i-lucide-filter'"
                                        :label="showOnlyMissing ? 'Vis alle spejdere' : 'Vis kun dem'"
                                        @click="onlyMissing = !onlyMissing"
                                    />
                                    <UDropdownMenu :items="quickFillItems" :content="{ align: 'start' }">
                                        <UButton color="warning" size="sm" icon="i-lucide-wand-sparkles" trailing-icon="i-lucide-chevron-down" label="Udfyld for dem" :disabled="busy" />
                                    </UDropdownMenu>
                                </div>
                            </template>
                        </UAlert>

                        <UCard>
                            <template #header>
                                <div class="flex flex-wrap items-baseline justify-between gap-2">
                                    <h2 class="font-semibold text-highlighted">Deltagere pr. dag</h2>
                                    <p class="text-xs text-muted">Kun spejdere med registrerede dage tælles med.</p>
                                </div>
                            </template>
                            <ul class="grid grid-cols-[repeat(auto-fill,minmax(6.5rem,1fr))] gap-2">
                                <li v-for="day in campDays" :key="day.id" class="rounded-md border border-default px-3 py-2">
                                    <p class="text-xs font-medium capitalize text-muted">{{ formatShortDay(day.date) }}</p>
                                    <p class="text-xl font-semibold tabular-nums text-highlighted" :aria-label="`${dayCounts.get(day.id) ?? 0} spejdere ${formatLongDay(day.date)}`">{{ dayCounts.get(day.id) ?? 0 }}</p>
                                    <p class="text-xs text-dimmed">{{ halfLabel(day) }}</p>
                                </li>
                            </ul>
                        </UCard>
                    </template>

                    <UCard :ui="{ body: 'p-0 sm:p-0' }">
                        <template #header>
                            <div class="flex flex-col gap-3">
                                <div class="flex items-center justify-between gap-2">
                                    <h2 class="font-semibold text-highlighted">
                                        Spejdere
                                        <span class="font-normal text-muted">({{ showOnlyMissing ? `${visibleScouts.length} af ${Group.scouts.length}` : Group.scouts.length }})</span>
                                    </h2>
                                    <UButton size="sm" icon="i-lucide-plus" label="Opret" @click="openCreateScout" />
                                </div>

                                <div v-if="hasCampDays && Group.scouts.length" class="flex flex-wrap items-center gap-2">
                                    <UCheckbox
                                        :model-value="selectAllState"
                                        :label="selectedIds.length ? `${selectedIds.length} valgt` : 'Vælg alle'"
                                        :disabled="busy"
                                        @update:model-value="toggleSelectAll"
                                    />
                                    <USelect
                                        v-if="Group.patrols.length"
                                        :key="patrolPickerKey"
                                        :model-value="undefined"
                                        :items="patrolSelectItems"
                                        placeholder="Vælg patrulje…"
                                        size="sm"
                                        class="w-44"
                                        :disabled="busy"
                                        aria-label="Vælg alle spejdere i en patrulje"
                                        @update:model-value="selectPatrol"
                                    />
                                    <template v-if="selectedIds.length">
                                        <span class="text-sm text-muted">Sæt til</span>
                                        <UButton
                                            v-for="period in periods"
                                            :key="period"
                                            size="sm"
                                            color="neutral"
                                            variant="outline"
                                            :label="fixedPeriodLabels[period]"
                                            :disabled="busy"
                                            @click="applyToSelected(period)"
                                        />
                                        <UButton size="sm" color="neutral" variant="ghost" label="Ryd valg" :disabled="busy" @click="selectedIds = []" />
                                    </template>
                                </div>
                            </div>
                        </template>

                        <p v-if="Group.scouts.length === 0" class="px-4 py-3 text-sm italic text-muted sm:px-6">Ingen spejdere oprettet endnu.</p>
                        <ul v-else class="divide-y divide-default">
                            <li v-for="scout in visibleScouts" :key="scout.id" class="flex flex-col gap-2 px-4 py-3 sm:px-6">
                                <div class="flex flex-col gap-2 sm:flex-row sm:items-start sm:justify-between">
                                    <div class="flex min-w-0 items-start gap-3">
                                        <UCheckbox
                                            v-if="hasCampDays"
                                            class="mt-0.5"
                                            :model-value="selectedIds.includes(scout.id)"
                                            :disabled="busy"
                                            :aria-label="`Vælg ${scout.name}`"
                                            @update:model-value="(checked: boolean | 'indeterminate') => toggleSelected(scout.id, checked === true)"
                                        />
                                        <div class="min-w-0">
                                            <div class="flex flex-wrap items-center gap-2">
                                                <p class="font-medium text-highlighted">{{ scout.name }}</p>
                                                <UBadge v-if="hasCampDays && scout.campDayIds.length === 0" label="Dage mangler" color="warning" variant="subtle" size="sm" icon="i-lucide-calendar-x" />
                                            </div>
                                            <p class="text-xs text-muted">{{ formatBirthday(scout.birthday) }} · {{ genderLabel(scout.gender) }}</p>
                                            <div v-if="scoutPatrols(scout).length" class="mt-1 flex flex-wrap gap-1">
                                                <UBadge v-for="patrol in scoutPatrols(scout)" :key="patrol.id" :label="patrol.name" color="info" variant="subtle" size="sm" />
                                            </div>
                                        </div>
                                    </div>
                                    <div class="flex shrink-0 flex-wrap items-center gap-1 pl-7 sm:pl-0">
                                        <USelect
                                            v-if="hasCampDays"
                                            :model-value="shownPeriod(scout)"
                                            :items="periodItems"
                                            placeholder="Vælg dage"
                                            size="xs"
                                            class="w-36"
                                            :color="scout.campDayIds.length === 0 ? 'warning' : 'neutral'"
                                            :highlight="scout.campDayIds.length === 0"
                                            :disabled="busy"
                                            :aria-label="`Deltagelse for ${scout.name}`"
                                            @update:model-value="(period: CampPeriod) => changePeriod(scout, period)"
                                        />
                                        <UButton color="neutral" variant="outline" size="xs" icon="i-lucide-flag" label="Patruljer" @click="openPatrols(scout)" />
                                        <UButton color="error" variant="ghost" size="xs" icon="i-lucide-trash-2" :aria-label="`Slet ${scout.name}`" @click="confirmDeleteScout(scout)" />
                                    </div>
                                </div>
                                <div v-if="hasCampDays && shownPeriod(scout) === 'custom'" class="flex flex-wrap gap-1 pl-7" role="group" :aria-label="`Dage for ${scout.name}`">
                                    <UButton
                                        v-for="day in campDays"
                                        :key="day.id"
                                        size="xs"
                                        class="capitalize"
                                        :color="scout.campDayIds.includes(day.id) ? 'primary' : 'neutral'"
                                        :variant="scout.campDayIds.includes(day.id) ? 'solid' : 'outline'"
                                        :label="formatShortDay(day.date)"
                                        :aria-pressed="scout.campDayIds.includes(day.id)"
                                        :aria-label="formatLongDay(day.date)"
                                        :disabled="busy"
                                        @click="toggleDay(scout, day.id)"
                                    />
                                </div>
                            </li>
                        </ul>
                        <template #footer>
                            <p class="text-xs text-muted">En spejder kan tilknyttes flere patruljer. En spejder, der ikke deltager, slettes.</p>
                        </template>
                    </UCard>

                    <UCard :ui="{ body: 'p-0 sm:p-0' }">
                        <template #header>
                            <div class="flex items-center justify-between gap-2">
                                <h2 class="font-semibold text-highlighted">Patruljer <span class="font-normal text-muted">({{ Group.patrols.length }})</span></h2>
                                <UButton size="sm" icon="i-lucide-plus" label="Opret" @click="openCreatePatrol" />
                            </div>
                        </template>
                        <p v-if="Group.patrols.length === 0" class="px-4 py-3 text-sm italic text-muted sm:px-6">Ingen patruljer oprettet endnu.</p>
                        <ul v-else class="divide-y divide-default">
                            <li v-for="patrol in Group.patrols" :key="patrol.id" class="flex items-start justify-between gap-2 px-4 py-3 sm:px-6">
                                <div class="min-w-0">
                                    <p class="font-medium text-highlighted">{{ patrol.name }}</p>
                                    <p class="text-xs text-muted">{{ patrolScouts(patrol).length }} {{ patrolScouts(patrol).length === 1 ? 'spejder' : 'spejdere' }}</p>
                                    <p v-if="patrolScouts(patrol).length" class="mt-1 text-xs text-muted">{{ patrolScouts(patrol).map((scout) => scout.name).join(', ') }}</p>
                                </div>
                                <UButton color="error" variant="ghost" size="xs" icon="i-lucide-trash-2" :aria-label="`Slet ${patrol.name}`" @click="confirmDeletePatrol(patrol)" />
                            </li>
                        </ul>
                    </UCard>
                </template>
            </div>

            <UModal v-model:open="createScoutOpen" title="Opret spejder" :dismissible="!busy">
                <template #body>
                    <form id="final-signup-scout-form" class="flex flex-col gap-4" @submit.prevent="createScout(false)">
                        <UFormField label="Navn" required>
                            <UInput ref="newScoutNameInput" v-model="newScoutName" placeholder="Spejderens fulde navn" :disabled="busy" class="w-full" autofocus />
                        </UFormField>
                        <div class="grid gap-4 sm:grid-cols-2">
                            <UFormField label="Fødselsdato" required>
                                <UInput v-model="newScoutBirthday" type="date" :disabled="busy" class="w-full" />
                            </UFormField>
                            <UFormField label="Køn">
                                <USelect v-model="newScoutGender" :items="genderItems" :disabled="busy" class="w-full" />
                            </UFormField>
                        </div>
                        <UFormField v-if="hasCampDays" label="Deltager" hint="Valgfrit" description="Uden dage vises spejderen som manglende, indtil dagene er udfyldt.">
                            <USelect v-model="newScoutPeriod" :items="createPeriodItems" placeholder="Ikke valgt" :disabled="busy" class="w-full" />
                        </UFormField>
                        <div v-if="hasCampDays && newScoutPeriod === 'custom'" class="flex flex-wrap gap-1" role="group" aria-label="Dage for den nye spejder">
                            <UButton
                                v-for="day in campDays"
                                :key="day.id"
                                size="xs"
                                class="capitalize"
                                :color="newScoutCustomDays.includes(day.id) ? 'primary' : 'neutral'"
                                :variant="newScoutCustomDays.includes(day.id) ? 'solid' : 'outline'"
                                :label="formatShortDay(day.date)"
                                :aria-pressed="newScoutCustomDays.includes(day.id)"
                                :aria-label="formatLongDay(day.date)"
                                :disabled="busy"
                                @click="toggleNewScoutDay(day.id)"
                            />
                        </div>
                    </form>
                </template>
                <template #footer>
                    <div class="flex w-full flex-wrap justify-end gap-2">
                        <UButton color="neutral" variant="outline" label="Annuller" :disabled="busy" @click="createScoutOpen = false" />
                        <UButton color="neutral" variant="outline" label="Opret og tilføj en mere" :loading="busy" :disabled="!canCreateScout" @click="createScout(true)" />
                        <UButton type="submit" form="final-signup-scout-form" label="Opret spejder" :loading="busy" :disabled="!canCreateScout" />
                    </div>
                </template>
            </UModal>

            <UModal v-model:open="createPatrolOpen" title="Opret patrulje" :dismissible="!busy">
                <template #body>
                    <form id="final-signup-patrol-form" @submit.prevent="createPatrol">
                        <UFormField label="Patruljens navn" required>
                            <UInput v-model="newPatrolName" placeholder="Fx Ulvepatruljen" :disabled="busy" class="w-full" autofocus />
                        </UFormField>
                    </form>
                </template>
                <template #footer>
                    <div class="flex w-full justify-end gap-2">
                        <UButton color="neutral" variant="outline" label="Annuller" :disabled="busy" @click="createPatrolOpen = false" />
                        <UButton type="submit" form="final-signup-patrol-form" label="Opret patrulje" :loading="busy" :disabled="!newPatrolName.trim() || busy" />
                    </div>
                </template>
            </UModal>

            <UModal v-model:open="patrolsOpen" :title="`Patruljer for ${scoutForPatrols?.name ?? ''}`">
                <template #body>
                    <ul v-if="scoutForPatrols && scoutPatrols(scoutForPatrols).length" class="flex flex-col gap-2">
                        <li v-for="patrol in scoutPatrols(scoutForPatrols)" :key="patrol.id" class="flex items-center justify-between gap-2 rounded-md border border-default px-3 py-2">
                            <span class="text-sm font-medium">{{ patrol.name }}</span>
                            <UButton color="error" variant="ghost" size="sm" label="Fjern" :disabled="busy" @click="removeFromPatrol(patrol.id)" />
                        </li>
                    </ul>
                    <p v-else class="text-sm italic text-muted">Spejderen er ikke tilknyttet nogen patruljer endnu.</p>

                    <UFormField v-if="availablePatrols.length" label="Tilføj til patrulje" class="mt-4">
                        <UFieldGroup class="w-full">
                            <USelect v-model="patrolToAdd" :items="availablePatrols" placeholder="Vælg patrulje…" class="w-full" />
                            <UButton label="Tilføj" :disabled="patrolToAdd == null || busy" :loading="busy" @click="addToPatrol" />
                        </UFieldGroup>
                    </UFormField>
                    <p v-else-if="Group.patrols.length" class="mt-4 text-xs text-muted">Spejderen er allerede i alle patruljer.</p>
                    <p v-else class="mt-4 text-xs text-muted">Opret patruljer først for at tildele spejdere.</p>
                </template>
                <template #footer>
                    <div class="flex w-full justify-end">
                        <UButton color="neutral" variant="outline" label="Luk" @click="patrolsOpen = false" />
                    </div>
                </template>
            </UModal>

            <ConfirmModal
                v-model:open="deleteScoutOpen"
                title="Slet spejder"
                :description="`Slet ${scoutToDelete?.name}? Spejderens patruljemedlemskaber og dage slettes også.`"
                confirm-label="Slet spejder"
                color="error"
                :loading="busy"
                @confirm="deleteScout"
            />

            <ConfirmModal
                v-model:open="deletePatrolOpen"
                title="Slet patrulje"
                :description="`Slet ${patrolToDelete?.name}? Alle medlemskaber af patruljen slettes også.`"
                confirm-label="Slet patrulje"
                color="error"
                :loading="busy"
                @confirm="deletePatrol"
            />
        </template>
    </UDashboardPanel>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, ref, watch } from "vue";
import { useRoute } from "vue-router";
import { storeToRefs } from "pinia";
import { useGroupStore } from "@/Stores/GroupStore";
import GroupFinalSignupService from "@/Services/GroupFinalSignupService";
import CampDayService, { type CampDay } from "@/Services/CampDayService";
import { getErrorMessage } from "@/lib/api";
import { useNotify } from "@/lib/notify";
import { formatBirthday, genderItems, genderLabel } from "@/lib/scouts";
import {
    availablePeriods,
    daysForPeriod,
    fixedPeriodLabels,
    formatLongDay,
    formatShortDay,
    halfLabel,
    periodLabels,
    periodOf,
    sortCampDays,
    type CampPeriod,
    type FixedCampPeriod
} from "@/lib/campDays";
import ConfirmModal from "@/Components/ConfirmModal.vue";
import type { PatrolDto, ScoutDto } from "@/types";

const groupStore = useGroupStore();
const { Group } = storeToRefs(groupStore);
const signupService = new GroupFinalSignupService();
const campDayService = new CampDayService();
const notify = useNotify();
const route = useRoute();

const loading = ref(true);
const busy = ref(false);
const error = ref("");

const hasGroup = computed(() => !!Group.value.id && Group.value.id !== "0");

const loadFailed = ref(false);

const campDays = ref<CampDay[]>([]);
const campDaysLoadFailed = ref(false);
const hasCampDays = computed(() => !campDaysLoadFailed.value && campDays.value.length > 0);

async function loadCampDays() {
    campDaysLoadFailed.value = false;
    try {
        campDays.value = sortCampDays(await campDayService.list());
    } catch {
        campDaysLoadFailed.value = true;
    }
}

async function load() {
    loading.value = true;
    loadFailed.value = false;
    try {
        await Promise.all([groupStore.LOAD_GROUP(), loadCampDays()]);
    } catch {
        loadFailed.value = true;
    } finally {
        loading.value = false;
    }
}

onMounted(load);

function scoutPatrols(scout: ScoutDto): PatrolDto[] {
    const ids = new Set(scout.memberships.map((membership) => membership.patrolId));

    return Group.value.patrols.filter((patrol) => ids.has(patrol.id));
}

function patrolScouts(patrol: PatrolDto): ScoutDto[] {
    return Group.value.scouts.filter((scout) => scout.memberships.some((membership) => membership.patrolId === patrol.id));
}

async function run(action: () => Promise<unknown>, success?: string): Promise<boolean> {
    if (busy.value) return false;

    busy.value = true;
    error.value = "";
    try {
        await action();
        await groupStore.GET_GROUP();
        if (success) notify.success(success);

        return true;
    } catch (cause) {
        error.value = getErrorMessage(cause, "Der opstod en fejl. Prøv igen.");
        notify.error(error.value);

        return false;
    } finally {
        busy.value = false;
    }
}

const periods = computed(() => availablePeriods(campDays.value));
const periodItems = computed(() => [
    ...periods.value.map((period) => ({ label: periodLabels[period], value: period })),
    { label: periodLabels.custom, value: "custom" as const }
]);
const createPeriodItems = computed(() => [{ label: "Ikke valgt", value: "none" as const }, ...periodItems.value]);

const missingScouts = computed(() => Group.value.scouts.filter((scout) => scout.campDayIds.length === 0));
const missingTitle = computed(() => {
    const count = missingScouts.value.length;
    return `${count} ${count === 1 ? "spejder mangler" : "spejdere mangler"} dage`;
});
const missingNames = computed(() => {
    const names = missingScouts.value.map((scout) => scout.name);
    const shown = names.slice(0, 8).join(", ");

    return names.length > 8 ? `${shown} og ${names.length - 8} flere.` : `${shown}.`;
});

const onlyMissing = ref(route.query.filter === "missing-days");
const showOnlyMissing = computed(() => hasCampDays.value && onlyMissing.value && missingScouts.value.length > 0);
const visibleScouts = computed(() => (showOnlyMissing.value ? missingScouts.value : Group.value.scouts));

const dayCounts = computed(() => {
    const counts = new Map<number, number>();
    for (const scout of Group.value.scouts) {
        for (const dayId of scout.campDayIds) counts.set(dayId, (counts.get(dayId) ?? 0) + 1);
    }

    return counts;
});

const customScoutIds = ref<number[]>([]);

function shownPeriod(scout: ScoutDto): CampPeriod | undefined {
    if (customScoutIds.value.includes(scout.id)) return "custom";

    return periodOf(scout.campDayIds, campDays.value) ?? undefined;
}

async function saveDays(scoutIds: number[], campDayIds: number[], success?: string) {
    return await run(() => signupService.setScoutDays(scoutIds, campDayIds), success);
}

async function changePeriod(scout: ScoutDto, period: CampPeriod) {
    if (period === "custom") {
        if (!customScoutIds.value.includes(scout.id)) customScoutIds.value = [...customScoutIds.value, scout.id];
        return;
    }

    customScoutIds.value = customScoutIds.value.filter((id) => id !== scout.id);
    await saveDays([scout.id], daysForPeriod(campDays.value, period));
}

async function toggleDay(scout: ScoutDto, dayId: number) {
    const selected = scout.campDayIds.includes(dayId)
        ? scout.campDayIds.filter((id) => id !== dayId)
        : [...scout.campDayIds, dayId];

    if (selected.length === 0) {
        notify.error("En spejder skal have mindst én dag. Slet spejderen, hvis den ikke deltager.");
        return;
    }

    if (!customScoutIds.value.includes(scout.id)) customScoutIds.value = [...customScoutIds.value, scout.id];
    await saveDays([scout.id], selected);
}

const selectedIds = ref<number[]>([]);

watch(
    () => Group.value.scouts.map((scout) => scout.id),
    (ids) => {
        const existing = new Set(ids);
        selectedIds.value = selectedIds.value.filter((id) => existing.has(id));
        customScoutIds.value = customScoutIds.value.filter((id) => existing.has(id));
    }
);

const selectAllState = computed<boolean | "indeterminate">(() => {
    const visible = visibleScouts.value;
    if (visible.length === 0) return false;

    const selected = visible.filter((scout) => selectedIds.value.includes(scout.id)).length;
    if (selected === 0) return false;

    return selected === visible.length ? true : "indeterminate";
});

function toggleSelectAll(checked: boolean | "indeterminate") {
    selectedIds.value = checked === true ? visibleScouts.value.map((scout) => scout.id) : [];
}

function toggleSelected(scoutId: number, checked: boolean) {
    selectedIds.value = checked
        ? [...new Set([...selectedIds.value, scoutId])]
        : selectedIds.value.filter((id) => id !== scoutId);
}

const patrolSelectItems = computed(() => Group.value.patrols.map((patrol) => ({ label: patrol.name, value: patrol.id })));

const patrolPickerKey = ref(0);

function selectPatrol(patrolId: number) {
    patrolPickerKey.value++;
    const patrol = Group.value.patrols.find((candidate) => candidate.id === patrolId);
    if (!patrol) return;

    const scouts = patrolScouts(patrol);
    if (scouts.length === 0) {
        notify.error(`${patrol.name} har ingen spejdere.`);
        return;
    }

    selectedIds.value = [...new Set([...selectedIds.value, ...scouts.map((scout) => scout.id)])];
}

async function applyToSelected(period: FixedCampPeriod) {
    const scoutIds = [...selectedIds.value];
    if (scoutIds.length === 0) return;

    const label = fixedPeriodLabels[period].toLowerCase();
    const message = `${scoutIds.length} ${scoutIds.length === 1 ? "spejder er" : "spejdere er"} sat til ${label}.`;
    if (await saveDays(scoutIds, daysForPeriod(campDays.value, period), message)) {
        customScoutIds.value = customScoutIds.value.filter((id) => !scoutIds.includes(id));
        selectedIds.value = [];
    }
}

const quickFillItems = computed(() => periods.value.map((period) => ({
    label: `Giv alle uden dage ${fixedPeriodLabels[period].toLowerCase()}`,
    onSelect: () => fillMissing(period)
})));

async function fillMissing(period: FixedCampPeriod) {
    const scoutIds = missingScouts.value.map((scout) => scout.id);
    if (scoutIds.length === 0) return;

    let updated = 0;
    const saved = await run(async () => {
        updated = (await signupService.setScoutDays(scoutIds, daysForPeriod(campDays.value, period), true)).updated;
    });
    if (saved) notify.success(`${updated} ${updated === 1 ? "spejder har" : "spejdere har"} fået ${fixedPeriodLabels[period].toLowerCase()}.`);
}

const createScoutOpen = ref(false);
const newScoutNameInput = ref<{ inputRef?: HTMLInputElement } | null>(null);
const newScoutName = ref("");
const newScoutBirthday = ref("");
const newScoutGender = ref<"Male" | "Female">("Male");
const newScoutPeriod = ref<CampPeriod | "none" | undefined>();
const newScoutCustomDays = ref<number[]>([]);

const canCreateScout = computed(() => !!newScoutName.value.trim() && !!newScoutBirthday.value && !busy.value);

const newScoutDayIds = computed(() => {
    const period = newScoutPeriod.value;
    if (!hasCampDays.value || period == null || period === "none") return [];
    if (period === "custom") return newScoutCustomDays.value;

    return daysForPeriod(campDays.value, period);
});

function toggleNewScoutDay(dayId: number) {
    newScoutCustomDays.value = newScoutCustomDays.value.includes(dayId)
        ? newScoutCustomDays.value.filter((id) => id !== dayId)
        : [...newScoutCustomDays.value, dayId];
}

function openCreateScout() {
    newScoutName.value = "";
    newScoutBirthday.value = "";
    newScoutGender.value = "Male";
    newScoutPeriod.value = undefined;
    newScoutCustomDays.value = [];
    createScoutOpen.value = true;
}

async function createScout(addAnother: boolean) {
    if (!canCreateScout.value) return;

    const name = newScoutName.value.trim();
    const birthday = new Date(`${newScoutBirthday.value}T00:00:00`).toISOString();
    if (!await run(() => signupService.createScout(name, birthday, newScoutGender.value, newScoutDayIds.value), `${name} er oprettet.`)) return;

    if (!addAnother) {
        createScoutOpen.value = false;
        return;
    }

    newScoutName.value = "";
    newScoutBirthday.value = "";
    await nextTick();
    newScoutNameInput.value?.inputRef?.focus();
}

const deleteScoutOpen = ref(false);
const scoutToDelete = ref<ScoutDto | null>(null);

function confirmDeleteScout(scout: ScoutDto) {
    scoutToDelete.value = scout;
    deleteScoutOpen.value = true;
}

async function deleteScout() {
    const scout = scoutToDelete.value;
    if (!scout) return;

    if (await run(() => signupService.deleteScout(scout.id), "Spejderen er slettet.")) {
        deleteScoutOpen.value = false;
    }
}

const createPatrolOpen = ref(false);
const newPatrolName = ref("");

function openCreatePatrol() {
    newPatrolName.value = "";
    createPatrolOpen.value = true;
}

async function createPatrol() {
    if (!newPatrolName.value.trim()) return;

    if (await run(() => signupService.createPatrol(newPatrolName.value.trim()), "Patruljen er oprettet.")) {
        createPatrolOpen.value = false;
    }
}

const deletePatrolOpen = ref(false);
const patrolToDelete = ref<PatrolDto | null>(null);

function confirmDeletePatrol(patrol: PatrolDto) {
    patrolToDelete.value = patrol;
    deletePatrolOpen.value = true;
}

async function deletePatrol() {
    const patrol = patrolToDelete.value;
    if (!patrol) return;

    if (await run(() => signupService.deletePatrol(patrol.id), "Patruljen er slettet.")) {
        deletePatrolOpen.value = false;
    }
}

const patrolsOpen = ref(false);
const scoutForPatrolsId = ref<number | null>(null);
const scoutForPatrols = computed(() => Group.value.scouts.find((scout) => scout.id === scoutForPatrolsId.value) ?? null);
const patrolToAdd = ref<number | undefined>();

const availablePatrols = computed(() => {
    const scout = scoutForPatrols.value;
    if (!scout) return [];

    const memberOf = new Set(scout.memberships.map((membership) => membership.patrolId));

    return Group.value.patrols
        .filter((patrol) => !memberOf.has(patrol.id))
        .map((patrol) => ({ label: patrol.name, value: patrol.id }));
});

function openPatrols(scout: ScoutDto) {
    scoutForPatrolsId.value = scout.id;
    patrolToAdd.value = undefined;
    patrolsOpen.value = true;
}

async function addToPatrol() {
    const scout = scoutForPatrols.value;
    const patrolId = patrolToAdd.value;
    if (!scout || patrolId == null) return;

    if (await run(() => signupService.addScoutToPatrol(scout.id, patrolId), `${scout.name} er tilføjet til patruljen.`)) {
        patrolToAdd.value = undefined;
    }
}

async function removeFromPatrol(patrolId: number) {
    const scout = scoutForPatrols.value;
    if (!scout) return;

    await run(() => signupService.removeScoutFromPatrol(scout.id, patrolId), `${scout.name} er fjernet fra patruljen.`);
}
</script>
