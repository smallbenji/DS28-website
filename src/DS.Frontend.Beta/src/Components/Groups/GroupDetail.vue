<template>
    <div class="flex flex-col gap-6">
        <div>
            <h2 class="text-xl font-semibold text-highlighted">{{ group.name }}</h2>
            <p class="text-sm text-muted">Gruppe-ID {{ group.id }} · {{ districtLabel(group.district) }}</p>
        </div>

        <div class="grid min-w-0 gap-6 xl:grid-cols-2">
            <div class="flex min-w-0 flex-col gap-6">
                <UCard>
                    <template #header>
                        <h3 class="font-semibold text-highlighted">Stamdata</h3>
                    </template>
                    <form id="group-form" class="flex flex-col gap-4" @submit.prevent="save">
                        <UFormField label="ID">
                            <UInput :model-value="group.id" disabled class="w-full" />
                        </UFormField>
                        <UFormField label="Navn">
                            <UInput v-model="draftName" class="w-full" />
                        </UFormField>
                        <UFormField label="Distrikt">
                            <USelect v-model="draftDistrict" :items="districtItems" class="w-full" />
                        </UFormField>
                    </form>
                    <template #footer>
                        <div class="flex justify-end gap-2">
                            <UButton v-if="isDirty" color="neutral" variant="ghost" label="Fortryd" @click="resetDraft" />
                            <UButton type="submit" form="group-form" label="Gem" :loading="saving" :disabled="!isDirty || !draftName.trim()" />
                        </div>
                    </template>
                </UCard>

                <UCard :ui="{ body: 'p-0 sm:p-0' }">
                    <template #header>
                        <div class="flex items-center justify-between gap-2">
                            <h3 class="font-semibold text-highlighted">Spejdere <span class="font-normal text-muted">({{ group.scouts.length }})</span></h3>
                            <UButton size="sm" icon="i-lucide-plus" label="Opret spejder" @click="scoutCreateOpen = true" />
                        </div>
                    </template>
                    <p v-if="group.scouts.length === 0" class="px-4 py-3 text-sm text-muted sm:px-6">Ingen spejdere</p>
                    <ul v-else class="divide-y divide-default">
                        <li v-for="scout in group.scouts" :key="scout.id" class="flex items-center justify-between gap-2 px-4 py-2 sm:px-6">
                            <div class="min-w-0">
                                <p class="truncate text-sm font-medium text-highlighted">{{ scout.name }}</p>
                                <p class="text-xs text-muted">{{ formatBirthday(scout.birthday) }}, {{ genderLabel(scout.gender) }}</p>
                            </div>
                            <div class="flex shrink-0 gap-1">
                                <UButton color="neutral" variant="outline" size="xs" icon="i-lucide-flag" :label="`Patruljer (${scout.memberships.length})`" @click="openPatrols(scout)" />
                                <UButton color="error" variant="ghost" size="xs" icon="i-lucide-trash-2" :aria-label="`Slet ${scout.name}`" @click="confirmDeleteScout(scout)" />
                            </div>
                        </li>
                    </ul>
                </UCard>
            </div>

            <div class="flex min-w-0 flex-col gap-6">
                <GroupRegistrationHistory v-if="hasRole('AdminAccess')" :key="group.id" :group-id="group.id" />

                <UCard :ui="{ body: 'p-0 sm:p-0' }">
                    <template #header>
                        <h3 class="font-semibold text-highlighted">Brugere <span class="font-normal text-muted">({{ users.length }})</span></h3>
                    </template>
                    <p v-if="users.length === 0" class="px-4 py-3 text-sm text-muted sm:px-6">Ingen brugere</p>
                    <ul v-else class="divide-y divide-default">
                        <li v-for="user in users" :key="user.id">
                            <RouterLink :to="`/admin/user/${user.id}`" class="flex items-center gap-3 px-4 py-2 text-sm hover:bg-elevated/50 sm:px-6">
                                <UserAvatar :user="user" size="sm" />
                                {{ user.firstName }} {{ user.lastName }}
                            </RouterLink>
                        </li>
                    </ul>
                </UCard>

                <UCard :ui="{ body: 'p-0 sm:p-0' }">
                    <template #header>
                        <div class="flex items-center justify-between gap-2">
                            <h3 class="font-semibold text-highlighted">Patruljer <span class="font-normal text-muted">({{ group.patrols.length }})</span></h3>
                            <UButton size="sm" icon="i-lucide-plus" label="Opret patrulje" @click="openCreatePatrol" />
                        </div>
                    </template>
                    <p v-if="group.patrols.length === 0" class="px-4 py-3 text-sm text-muted sm:px-6">Ingen patruljer</p>
                    <ul v-else class="divide-y divide-default">
                        <li v-for="patrol in group.patrols" :key="patrol.id" class="flex items-start justify-between gap-2 px-4 py-2 sm:px-6">
                            <div class="min-w-0">
                                <p class="text-sm font-medium text-highlighted">{{ patrol.name }}</p>
                                <p v-if="scoutsInPatrol(patrol.id).length === 0" class="text-xs italic text-muted">Ingen spejdere</p>
                                <ul v-else class="mt-1 flex flex-wrap gap-1">
                                    <li v-for="scout in scoutsInPatrol(patrol.id)" :key="scout.id">
                                        <UBadge
                                            :label="scout.name"
                                            :icon="isLeader(scout, patrol.id) ? 'i-lucide-crown' : undefined"
                                            :color="isLeader(scout, patrol.id) ? 'warning' : 'neutral'"
                                            :title="isLeader(scout, patrol.id) ? 'Patruljeleder' : undefined"
                                            variant="subtle"
                                            size="sm"
                                        />
                                    </li>
                                </ul>
                            </div>
                            <UButton color="error" variant="ghost" size="xs" icon="i-lucide-trash-2" :aria-label="`Slet ${patrol.name}`" @click="confirmDeletePatrol(patrol)" />
                        </li>
                    </ul>
                </UCard>
            </div>
        </div>

        <ScoutCreateModal v-model:open="scoutCreateOpen" :group-id="group.id" />
        <ScoutPatrolsModal v-model:open="scoutPatrolsOpen" :group="group" :scout="scoutForPatrols" />

        <UModal v-model:open="patrolCreateOpen" title="Opret patrulje">
            <template #body>
                <form id="create-patrol-form" @submit.prevent="createPatrol">
                    <UFormField label="Navn" required>
                        <UInput v-model="newPatrolName" placeholder="Patruljens navn" class="w-full" autofocus />
                    </UFormField>
                </form>
            </template>
            <template #footer>
                <div class="flex w-full justify-end gap-2">
                    <UButton color="neutral" variant="outline" label="Annuller" @click="patrolCreateOpen = false" />
                    <UButton type="submit" form="create-patrol-form" label="Opret patrulje" :loading="creatingPatrol" :disabled="!newPatrolName.trim()" />
                </div>
            </template>
        </UModal>

        <ConfirmModal
            v-model:open="deletePatrolOpen"
            title="Slet patrulje"
            :description="`Slet ${patrolToDelete?.name}? Alle medlemskaber af patruljen slettes også.`"
            confirm-label="Slet patrulje"
            color="error"
            :loading="deleting"
            @confirm="deletePatrol"
        />

        <ConfirmModal
            v-model:open="deleteScoutOpen"
            title="Slet spejder"
            :description="`Slet ${scoutToDelete?.name}? Spejderens patruljemedlemskaber slettes også.`"
            confirm-label="Slet spejder"
            color="error"
            :loading="deleting"
            @confirm="deleteScout"
        />
    </div>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import { storeToRefs } from "pinia";
import GroupsService from "@/Services/GroupsService";
import { useGroupsStore } from "@/Stores/GroupsStore";
import { useHasRole } from "@/lib/permissions";
import { useNotify } from "@/lib/notify";
import { districtItems, formatBirthday, genderLabel } from "@/lib/scouts";
import ConfirmModal from "@/Components/ConfirmModal.vue";
import UserAvatar from "@/Components/User/UserAvatar.vue";
import GroupRegistrationHistory from "@/Components/Groups/GroupRegistrationHistory.vue";
import ScoutCreateModal from "@/Components/Groups/ScoutCreateModal.vue";
import ScoutPatrolsModal from "@/Components/Groups/ScoutPatrolsModal.vue";
import { districtLabel, type District, type GroupDto, type PatrolDto, type ScoutDto } from "@/types";

const props = defineProps<{ group: GroupDto }>();

const groupsService = new GroupsService();
const groupsStore = useGroupsStore();
const { Groups } = storeToRefs(groupsStore);
const hasRole = useHasRole();
const notify = useNotify();

const users = computed(() => Groups.value.users[String(props.group.id)] ?? []);

const draftName = ref(props.group.name);
const draftDistrict = ref<District>(props.group.district);
const saving = ref(false);

function resetDraft() {
    draftName.value = props.group.name;
    draftDistrict.value = props.group.district;
}

watch(() => props.group.id, resetDraft);

const isDirty = computed(() => draftName.value !== props.group.name || draftDistrict.value !== props.group.district);

async function save() {
    if (!isDirty.value || !draftName.value.trim()) return;

    saving.value = true;
    try {
        const ok = await groupsStore.UPDATE_GROUP({ ...props.group, name: draftName.value.trim(), district: draftDistrict.value });
        notify.result(ok, "Gruppen er gemt.", "Gruppen kunne ikke gemmes.");
        if (ok) resetDraft();
    } finally {
        saving.value = false;
    }
}

function scoutsInPatrol(patrolId: number): ScoutDto[] {
    return props.group.scouts.filter((scout) => scout.memberships.some((membership) => membership.patrolId === patrolId));
}

function isLeader(scout: ScoutDto, patrolId: number): boolean {
    return scout.memberships.some((membership) => membership.patrolId === patrolId && membership.isPatrolLeader);
}

const scoutCreateOpen = ref(false);
const scoutPatrolsOpen = ref(false);
const scoutForPatrolsId = ref<number | null>(null);
const scoutForPatrols = computed(() => props.group.scouts.find((scout) => scout.id === scoutForPatrolsId.value) ?? null);

function openPatrols(scout: ScoutDto) {
    scoutForPatrolsId.value = scout.id;
    scoutPatrolsOpen.value = true;
}

const patrolCreateOpen = ref(false);
const newPatrolName = ref("");
const creatingPatrol = ref(false);

function openCreatePatrol() {
    newPatrolName.value = "";
    patrolCreateOpen.value = true;
}

async function createPatrol() {
    if (!newPatrolName.value.trim()) return;

    creatingPatrol.value = true;
    try {
        const patrol = await groupsService.createPatrol(props.group.id, newPatrolName.value.trim());
        notify.result(patrol != null, "Patruljen er oprettet.", "Patruljen kunne ikke oprettes.");
        if (patrol) {
            patrolCreateOpen.value = false;
            await groupsStore.GET_GROUPS();
        }
    } finally {
        creatingPatrol.value = false;
    }
}

const deleting = ref(false);
const deletePatrolOpen = ref(false);
const patrolToDelete = ref<PatrolDto | null>(null);
const deleteScoutOpen = ref(false);
const scoutToDelete = ref<ScoutDto | null>(null);

function confirmDeletePatrol(patrol: PatrolDto) {
    patrolToDelete.value = patrol;
    deletePatrolOpen.value = true;
}

function confirmDeleteScout(scout: ScoutDto) {
    scoutToDelete.value = scout;
    deleteScoutOpen.value = true;
}

async function deletePatrol() {
    if (!patrolToDelete.value) return;

    deleting.value = true;
    try {
        const ok = await groupsService.deletePatrol(patrolToDelete.value.id);
        notify.result(ok, "Patruljen er slettet.", "Patruljen kunne ikke slettes.");
        if (ok) {
            deletePatrolOpen.value = false;
            await groupsStore.GET_GROUPS();
        }
    } finally {
        deleting.value = false;
    }
}

async function deleteScout() {
    if (!scoutToDelete.value) return;

    deleting.value = true;
    try {
        const ok = await groupsService.deleteScout(scoutToDelete.value.id);
        notify.result(ok, "Spejderen er slettet.", "Spejderen kunne ikke slettes.");
        if (ok) {
            deleteScoutOpen.value = false;
            await groupsStore.GET_GROUPS();
        }
    } finally {
        deleting.value = false;
    }
}
</script>
