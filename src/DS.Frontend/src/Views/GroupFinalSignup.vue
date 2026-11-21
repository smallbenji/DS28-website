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
                <div v-if="loading" class="grid gap-6 lg:grid-cols-2">
                    <USkeleton class="h-72" />
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

                    <div class="grid gap-6 lg:grid-cols-2">
                        <UCard :ui="{ body: 'p-0 sm:p-0' }">
                            <template #header>
                                <div class="flex items-center justify-between gap-2">
                                    <h2 class="font-semibold text-highlighted">Spejdere <span class="font-normal text-muted">({{ Group.scouts.length }})</span></h2>
                                    <UButton size="sm" icon="i-lucide-plus" label="Opret" @click="openCreateScout" />
                                </div>
                            </template>
                            <p v-if="Group.scouts.length === 0" class="px-4 py-3 text-sm italic text-muted sm:px-6">Ingen spejdere oprettet endnu.</p>
                            <ul v-else class="divide-y divide-default">
                                <li v-for="scout in Group.scouts" :key="scout.id" class="flex items-start justify-between gap-2 px-4 py-3 sm:px-6">
                                    <div class="min-w-0">
                                        <p class="font-medium text-highlighted">{{ scout.name }}</p>
                                        <p class="text-xs text-muted">{{ formatBirthday(scout.birthday) }} · {{ genderLabel(scout.gender) }}</p>
                                        <div v-if="scoutPatrols(scout).length" class="mt-1 flex flex-wrap gap-1">
                                            <UBadge v-for="patrol in scoutPatrols(scout)" :key="patrol.id" :label="patrol.name" color="info" variant="subtle" size="sm" />
                                        </div>
                                    </div>
                                    <div class="flex shrink-0 gap-1">
                                        <UButton color="neutral" variant="outline" size="xs" icon="i-lucide-flag" label="Patruljer" @click="openPatrols(scout)" />
                                        <UButton color="error" variant="ghost" size="xs" icon="i-lucide-trash-2" :aria-label="`Slet ${scout.name}`" @click="confirmDeleteScout(scout)" />
                                    </div>
                                </li>
                            </ul>
                            <template #footer>
                                <p class="text-xs text-muted">En spejder kan tilknyttes flere patruljer.</p>
                            </template>
                        </UCard>

                        <UCard class="self-start" :ui="{ body: 'p-0 sm:p-0' }">
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
                    </div>
                </template>
            </div>

            <UModal v-model:open="createScoutOpen" title="Opret spejder" :dismissible="!busy">
                <template #body>
                    <form id="final-signup-scout-form" class="flex flex-col gap-4" @submit.prevent="createScout">
                        <UFormField label="Navn" required>
                            <UInput v-model="newScoutName" placeholder="Spejderens fulde navn" :disabled="busy" class="w-full" autofocus />
                        </UFormField>
                        <div class="grid gap-4 sm:grid-cols-2">
                            <UFormField label="Fødselsdato" required>
                                <UInput v-model="newScoutBirthday" type="date" :disabled="busy" class="w-full" />
                            </UFormField>
                            <UFormField label="Køn">
                                <USelect v-model="newScoutGender" :items="genderItems" :disabled="busy" class="w-full" />
                            </UFormField>
                        </div>
                    </form>
                </template>
                <template #footer>
                    <div class="flex w-full justify-end gap-2">
                        <UButton color="neutral" variant="outline" label="Annuller" :disabled="busy" @click="createScoutOpen = false" />
                        <UButton type="submit" form="final-signup-scout-form" label="Opret spejder" :loading="busy" :disabled="!newScoutName.trim() || !newScoutBirthday || busy" />
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
                :description="`Slet ${scoutToDelete?.name}? Spejderens patruljemedlemskaber slettes også.`"
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
import { computed, onMounted, ref } from "vue";
import { storeToRefs } from "pinia";
import { useGroupStore } from "@/Stores/GroupStore";
import GroupFinalSignupService from "@/Services/GroupFinalSignupService";
import { getErrorMessage } from "@/lib/api";
import { useNotify } from "@/lib/notify";
import { formatBirthday, genderItems, genderLabel } from "@/lib/scouts";
import ConfirmModal from "@/Components/ConfirmModal.vue";
import type { PatrolDto, ScoutDto } from "@/types";

const groupStore = useGroupStore();
const { Group } = storeToRefs(groupStore);
const signupService = new GroupFinalSignupService();
const notify = useNotify();

const loading = ref(true);
const busy = ref(false);
const error = ref("");

const hasGroup = computed(() => !!Group.value.id && Group.value.id !== "0");

const loadFailed = ref(false);

async function load() {
    loading.value = true;
    loadFailed.value = false;
    try {
        await groupStore.LOAD_GROUP();
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

async function run(action: () => Promise<unknown>, success: string): Promise<boolean> {
    if (busy.value) return false;

    busy.value = true;
    error.value = "";
    try {
        await action();
        await groupStore.GET_GROUP();
        notify.success(success);

        return true;
    } catch (cause) {
        error.value = getErrorMessage(cause, "Der opstod en fejl. Prøv igen.");
        notify.error(error.value);

        return false;
    } finally {
        busy.value = false;
    }
}

const createScoutOpen = ref(false);
const newScoutName = ref("");
const newScoutBirthday = ref("");
const newScoutGender = ref<"Male" | "Female">("Male");

function openCreateScout() {
    newScoutName.value = "";
    newScoutBirthday.value = "";
    newScoutGender.value = "Male";
    createScoutOpen.value = true;
}

async function createScout() {
    if (!newScoutName.value.trim() || !newScoutBirthday.value) return;

    const birthday = new Date(`${newScoutBirthday.value}T00:00:00`).toISOString();
    if (await run(() => signupService.createScout(newScoutName.value.trim(), birthday, newScoutGender.value), "Spejderen er oprettet.")) {
        createScoutOpen.value = false;
    }
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
