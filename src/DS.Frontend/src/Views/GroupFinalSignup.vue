<template>
    <section class="section">
        <div class="columns is-centered">
            <div class="column is-10-tablet is-8-desktop">
                <template v-if="loading">
                    <div class="card">
                        <div class="card-content">
                            <p role="status">Henter tilmeldingsdata…</p>
                        </div>
                    </div>
                </template>
                <template v-else-if="error && !loaded">
                    <div class="card">
                        <div class="card-content">
                            <BNotification type="is-danger" :closable="false" role="alert">{{ error }}</BNotification>
                            <BButton @click="load">Prøv igen</BButton>
                        </div>
                    </div>
                </template>
                <template v-else>
                    <div class="mb-4">
                        <h1 class="title is-4 mb-1">Endelig tilmelding</h1>
                        <p class="subtitle is-6 has-text-grey">{{ groupName }} · Gruppenummer {{ groupId }}</p>
                    </div>

                    <BNotification v-if="error" type="is-danger" :closable="false" role="alert" class="mb-4">{{ error }}</BNotification>

                    <div class="columns is-desktop">
                        <div class="column">
                            <div class="card">
                                <header class="card-header">
                                    <p class="card-header-title">
                                        <span class="icon mr-2"><i class="fas fa-users"></i></span>
                                        Spejdere ({{ scouts.length }})
                                    </p>
                                    <div class="card-header-icon">
                                        <BButton size="is-small" type="is-success" @click="openCreateScoutModal">
                                            <span class="icon"><i class="fas fa-plus"></i></span>
                                            <span>Opret</span>
                                        </BButton>
                                    </div>
                                </header>
                                <div class="card-content p-0">
                                    <div v-if="scouts.length === 0" class="px-5 py-4 has-text-grey is-italic">
                                        Ingen spejdere oprettet endnu.
                                    </div>
                                    <div
                                        v-for="scout in scouts"
                                        :key="scout.id"
                                        class="scout-row px-5 py-3"
                                    >
                                        <div class="scout-info">
                                            <strong>{{ scout.name }}</strong>
                                            <div class="is-size-7 has-text-grey">
                                                {{ formatBirthday(scout.birthday) }} · {{ translateGender(scout.gender) }}
                                            </div>
                                            <div v-if="scoutPatrols(scout).length > 0" class="tags mt-1">
                                                <span
                                                    v-for="patrol in scoutPatrols(scout)"
                                                    :key="patrol.id"
                                                    class="tag is-info is-light is-small"
                                                >{{ patrol.name }}</span>
                                            </div>
                                        </div>
                                        <div class="scout-actions">
                                            <BButton size="is-small" type="is-info is-light" @click="openPatrolModal(scout)">
                                                Patruljer
                                            </BButton>
                                            <BButton size="is-small" type="is-danger is-light" @click="openDeleteScoutModal(scout)">
                                                Slet
                                            </BButton>
                                        </div>
                                    </div>
                                </div>
                                <footer class="card-footer px-5 py-3">
                                    <span class="has-text-grey is-size-7">En spejder kan tilknyttes flere patruljer.</span>
                                </footer>
                            </div>
                        </div>

                        <div class="column">
                            <div class="card">
                                <header class="card-header">
                                    <p class="card-header-title">
                                        <span class="icon mr-2"><i class="fas fa-flag"></i></span>
                                        Patruljer ({{ patrols.length }})
                                    </p>
                                    <div class="card-header-icon">
                                        <BButton size="is-small" type="is-success" @click="openCreatePatrolModal">
                                            <span class="icon"><i class="fas fa-plus"></i></span>
                                            <span>Opret</span>
                                        </BButton>
                                    </div>
                                </header>
                                <div class="card-content p-0">
                                    <div v-if="patrols.length === 0" class="px-5 py-4 has-text-grey is-italic">
                                        Ingen patruljer oprettet endnu.
                                    </div>
                                    <div
                                        v-for="patrol in patrols"
                                        :key="patrol.id"
                                        class="patrol-row px-5 py-3"
                                    >
                                        <div class="patrol-info">
                                            <strong>{{ patrol.name }}</strong>
                                            <div class="is-size-7 has-text-grey">
                                                {{ patrolMemberCount(patrol) }} spejder{{ patrolMemberCount(patrol) === 1 ? '' : 'e' }}
                                            </div>
                                            <div v-if="patrolScouts(patrol).length > 0" class="mt-1">
                                                <span
                                                    v-for="scout in patrolScouts(patrol)"
                                                    :key="scout.id"
                                                    class="is-size-7 has-text-grey"
                                                    style="margin-right: 0.5rem;"
                                                >{{ scout.name }}</span>
                                            </div>
                                        </div>
                                        <BButton size="is-small" type="is-danger is-light" @click="openDeletePatrolModal(patrol)">
                                            Slet
                                        </BButton>
                                    </div>
                                </div>
                            </div>
                        </div>
                    </div>

                    <div class="mt-4">
                        <BButton @click="router.push('/group')">Tilbage til gruppen</BButton>
                    </div>
                </template>
            </div>
        </div>

        <BModal v-model="isCreateScoutModalOpen" has-modal-card>
            <div class="modal-card">
                <header class="modal-card-head">
                    <p class="modal-card-title">Opret spejder</p>
                </header>
                <section class="modal-card-body">
                    <BField label="Navn">
                        <BInput v-model="newScoutName" placeholder="Spejderens fulde navn" :disabled="busy" />
                    </BField>
                    <BField label="Fødselsdato">
                        <BDatepicker v-model="newScoutBirthday" locale="da-DK" placeholder="dd-mm-åååå" :disabled="busy" icon="calendar" editable :append-to-body="true" />
                    </BField>
                    <BField label="Køn">
                        <BSelect v-model="newScoutGender" expanded :disabled="busy">
                            <option value="Male">Mand</option>
                            <option value="Female">Kvinde</option>
                        </BSelect>
                    </BField>
                </section>
                <footer class="modal-card-foot buttons">
                    <BButton type="is-success" :loading="busy" :disabled="!canCreateScout || busy" @click="createScout">Opret spejder</BButton>
                    <BButton :disabled="busy" @click="isCreateScoutModalOpen = false">Annuller</BButton>
                </footer>
            </div>
        </BModal>

        <BModal v-model="isDeleteScoutModalOpen" has-modal-card>
            <div class="modal-card">
                <header class="modal-card-head">
                    <p class="modal-card-title">Slet spejder</p>
                </header>
                <section class="modal-card-body">
                    Er du sikker på, at du vil slette <strong>{{ scoutToDelete?.name }}</strong>? Alle tilknyttede patruljemedlemsskaber slettes også.
                </section>
                <footer class="modal-card-foot">
                    <BButton type="is-danger" :loading="busy" :disabled="busy" @click="deleteScout">Slet spejder</BButton>
                    <BButton :disabled="busy" @click="isDeleteScoutModalOpen = false">Annuller</BButton>
                </footer>
            </div>
        </BModal>

        <BModal v-model="isCreatePatrolModalOpen" has-modal-card>
            <div class="modal-card">
                <header class="modal-card-head">
                    <p class="modal-card-title">Opret patrulje</p>
                </header>
                <section class="modal-card-body">
                    <BField label="Patruljens navn">
                        <BInput v-model="newPatrolName" placeholder="f.eks. Ulvepatruljen" :disabled="busy" />
                    </BField>
                </section>
                <footer class="modal-card-foot buttons">
                    <BButton type="is-success" :loading="busy" :disabled="!newPatrolName.trim() || busy" @click="createPatrol">Opret patrulje</BButton>
                    <BButton :disabled="busy" @click="isCreatePatrolModalOpen = false">Annuller</BButton>
                </footer>
            </div>
        </BModal>

        <BModal v-model="isDeletePatrolModalOpen" has-modal-card>
            <div class="modal-card">
                <header class="modal-card-head">
                    <p class="modal-card-title">Slet patrulje</p>
                </header>
                <section class="modal-card-body">
                    Er du sikker på, at du vil slette patruljen <strong>{{ patrolToDelete?.name }}</strong>? Alle tilknyttede medlemsskaber slettes også.
                </section>
                <footer class="modal-card-foot">
                    <BButton type="is-danger" :loading="busy" :disabled="busy" @click="deletePatrol">Slet patrulje</BButton>
                    <BButton :disabled="busy" @click="isDeletePatrolModalOpen = false">Annuller</BButton>
                </footer>
            </div>
        </BModal>

        <BModal v-model="isPatrolModalOpen" has-modal-card>
            <div class="modal-card">
                <header class="modal-card-head">
                    <p class="modal-card-title">Patruljer for {{ scoutToEdit?.name }}</p>
                </header>
                <section class="modal-card-body">
                    <div v-if="scoutToEdit && scoutPatrols(scoutToEdit).length > 0">
                        <div
                            v-for="patrol in scoutPatrols(scoutToEdit)"
                            :key="patrol.id"
                            class="field is-grouped is-align-items-center"
                            style="margin-bottom: 0.5rem;"
                        >
                            <div class="control is-expanded">
                                <span class="tag is-info is-medium">{{ patrol.name }}</span>
                            </div>
                            <div class="control">
                                <BButton size="is-small" type="is-danger is-light" :loading="busy" :disabled="busy" @click="removeFromPatrol(patrol.id)">
                                    Fjern
                                </BButton>
                            </div>
                        </div>
                    </div>
                    <p v-else class="has-text-grey is-italic">Spejderen er ikke tilknyttet nogen patruljer endnu.</p>

                    <div v-if="availablePatrolsForEdit.length > 0" class="mt-4">
                        <BField label="Tilføj til patrulje">
                            <div class="field has-addons">
                                <div class="control is-expanded">
                                    <BSelect v-model="selectedPatrolToAdd" expanded placeholder="Vælg patrulje…">
                                        <option v-for="p in availablePatrolsForEdit" :key="p.id" :value="p.id">{{ p.name }}</option>
                                    </BSelect>
                                </div>
                                <div class="control">
                                    <BButton type="is-success" :disabled="!selectedPatrolToAdd || busy" :loading="busy" @click="addToPatrol">Tilføj</BButton>
                                </div>
                            </div>
                        </BField>
                    </div>
                    <p v-else-if="patrols.length > 0" class="has-text-grey is-size-7 mt-4">Spejderen er allerede i alle patruljer.</p>
                    <p v-else class="has-text-grey is-size-7 mt-4">Opret patruljer først for at tildele spejdere.</p>
                </section>
                <footer class="modal-card-foot">
                    <BButton type="is-primary" @click="isPatrolModalOpen = false">Luk</BButton>
                </footer>
            </div>
        </BModal>
    </section>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue';
import { useRouter } from 'vue-router';
import { BButton, BDatepicker, BField, BInput, BModal, BNotification, BSelect, useToast } from 'buefy';
import GroupService from '@/Services/GroupService';
import GroupFinalSignupService from '@/Services/GroupFinalSignupService';
import type { PatrolDto, ScoutDto } from '@/types';

const router = useRouter();
const groupService = new GroupService();
const signupService = new GroupFinalSignupService();
const Toast = useToast();

const loading = ref(true);
const loaded = ref(false);
const error = ref('');
const busy = ref(false);

const groupName = ref('');
const groupId = ref('');
const scouts = ref<ScoutDto[]>([]);
const patrols = ref<PatrolDto[]>([]);

const isCreateScoutModalOpen = ref(false);
const newScoutName = ref('');
const newScoutBirthday = ref<Date | null>(null);
const newScoutGender = ref<'Male' | 'Female'>('Male');
const canCreateScout = computed(() => newScoutName.value.trim().length > 0 && newScoutBirthday.value != null);

const isDeleteScoutModalOpen = ref(false);
const scoutToDelete = ref<ScoutDto | null>(null);

const isCreatePatrolModalOpen = ref(false);
const newPatrolName = ref('');

const isDeletePatrolModalOpen = ref(false);
const patrolToDelete = ref<PatrolDto | null>(null);

const isPatrolModalOpen = ref(false);
const scoutToEdit = ref<ScoutDto | null>(null);
const selectedPatrolToAdd = ref<number | null>(null);

const availablePatrolsForEdit = computed(() => {
    if (!scoutToEdit.value) return [];
    const memberIds = new Set(scoutToEdit.value.memberships.map(m => m.patrolId));
    return patrols.value.filter(p => !memberIds.has(p.id));
});

function scoutPatrols(scout: ScoutDto): PatrolDto[] {
    const ids = new Set(scout.memberships.map(m => m.patrolId));
    return patrols.value.filter(p => ids.has(p.id));
}

function patrolScouts(patrol: PatrolDto): ScoutDto[] {
    return scouts.value.filter(s => s.memberships.some(m => m.patrolId === patrol.id));
}

function patrolMemberCount(patrol: PatrolDto): number {
    return scouts.value.filter(s => s.memberships.some(m => m.patrolId === patrol.id)).length;
}

function formatBirthday(dateStr: string): string {
    if (!dateStr) return '';
    return new Date(dateStr).toLocaleDateString('da-DK', { day: 'numeric', month: 'short', year: 'numeric' });
}

function translateGender(gender: 'Male' | 'Female' | number): string {
    return (gender === 'Male' || gender === 0) ? 'Mand' : 'Kvinde';
}

function showError(cause: unknown) {
    import('axios').then(({ default: axios }) => {
        error.value = axios.isAxiosError(cause) && typeof cause.response?.data === 'string'
            ? cause.response.data
            : 'Der opstod en fejl. Prøv igen.';
    });
}

async function load() {
    loading.value = true;
    error.value = '';
    try {
        const group = await groupService.getGroup();
        groupName.value = group.name;
        groupId.value = group.id;
        scouts.value = group.scouts ?? [];
        patrols.value = group.patrols ?? [];
        loaded.value = true;
    } catch (cause) {
        showError(cause);
    } finally {
        loading.value = false;
    }
}

function openCreateScoutModal() {
    newScoutName.value = '';
    newScoutBirthday.value = null;
    newScoutGender.value = 'Male';
    isCreateScoutModalOpen.value = true;
}

async function createScout() {
    if (!canCreateScout.value || !newScoutBirthday.value || busy.value) return;
    busy.value = true;
    error.value = '';
    try {
        const scout = await signupService.createScout(newScoutName.value.trim(), newScoutBirthday.value.toISOString(), newScoutGender.value);
        scout.memberships = scout.memberships ?? [];
        scouts.value.push(scout);
        isCreateScoutModalOpen.value = false;
        Toast.open({ message: 'Spejderen er oprettet', type: 'is-success' });
    } catch (cause) {
        showError(cause);
    } finally {
        busy.value = false;
    }
}

function openDeleteScoutModal(scout: ScoutDto) {
    scoutToDelete.value = scout;
    isDeleteScoutModalOpen.value = true;
}

async function deleteScout() {
    if (!scoutToDelete.value || busy.value) return;
    busy.value = true;
    error.value = '';
    try {
        await signupService.deleteScout(scoutToDelete.value.id);
        scouts.value = scouts.value.filter(s => s.id !== scoutToDelete.value!.id);
        isDeleteScoutModalOpen.value = false;
        scoutToDelete.value = null;
        Toast.open({ message: 'Spejderen er slettet', type: 'is-success' });
    } catch (cause) {
        showError(cause);
    } finally {
        busy.value = false;
    }
}

function openCreatePatrolModal() {
    newPatrolName.value = '';
    isCreatePatrolModalOpen.value = true;
}

async function createPatrol() {
    if (!newPatrolName.value.trim() || busy.value) return;
    busy.value = true;
    error.value = '';
    try {
        const patrol = await signupService.createPatrol(newPatrolName.value.trim());
        patrol.memberships = patrol.memberships ?? [];
        patrols.value.push(patrol);
        isCreatePatrolModalOpen.value = false;
        Toast.open({ message: 'Patruljen er oprettet', type: 'is-success' });
    } catch (cause) {
        showError(cause);
    } finally {
        busy.value = false;
    }
}

function openDeletePatrolModal(patrol: PatrolDto) {
    patrolToDelete.value = patrol;
    isDeletePatrolModalOpen.value = true;
}

async function deletePatrol() {
    if (!patrolToDelete.value || busy.value) return;
    busy.value = true;
    error.value = '';
    try {
        await signupService.deletePatrol(patrolToDelete.value.id);
        const deletedId = patrolToDelete.value.id;
        patrols.value = patrols.value.filter(p => p.id !== deletedId);
        for (const scout of scouts.value) {
            scout.memberships = scout.memberships.filter(m => m.patrolId !== deletedId);
        }
        isDeletePatrolModalOpen.value = false;
        patrolToDelete.value = null;
        Toast.open({ message: 'Patruljen er slettet', type: 'is-success' });
    } catch (cause) {
        showError(cause);
    } finally {
        busy.value = false;
    }
}

function openPatrolModal(scout: ScoutDto) {
    scoutToEdit.value = scout;
    selectedPatrolToAdd.value = null;
    isPatrolModalOpen.value = true;
}

async function addToPatrol() {
    if (!scoutToEdit.value || !selectedPatrolToAdd.value || busy.value) return;
    const scout = scoutToEdit.value;
    const patrolId = selectedPatrolToAdd.value;
    busy.value = true;
    error.value = '';
    try {
        await signupService.addScoutToPatrol(scout.id, patrolId);
        scout.memberships.push({ id: 0, scoutId: scout.id, patrolId, joinedDate: new Date().toISOString(), isPatrolLeader: false });
        selectedPatrolToAdd.value = null;
        Toast.open({ message: `${scout.name} er tilføjet til patruljen`, type: 'is-success' });
    } catch (cause) {
        showError(cause);
    } finally {
        busy.value = false;
    }
}

async function removeFromPatrol(patrolId: number) {
    if (!scoutToEdit.value || busy.value) return;
    const scout = scoutToEdit.value;
    busy.value = true;
    error.value = '';
    try {
        await signupService.removeScoutFromPatrol(scout.id, patrolId);
        scout.memberships = scout.memberships.filter(m => m.patrolId !== patrolId);
        Toast.open({ message: `${scout.name} er fjernet fra patruljen`, type: 'is-success' });
    } catch (cause) {
        showError(cause);
    } finally {
        busy.value = false;
    }
}

onMounted(load);
</script>

<style lang="scss" scoped>
.scout-row,
.patrol-row {
    display: flex;
    align-items: flex-start;
    justify-content: space-between;
    gap: 1rem;
    border-bottom: 1px solid rgba(0, 0, 0, 0.06);

    &:last-child {
        border-bottom: none;
    }
}

.scout-info,
.patrol-info {
    flex: 1;
    min-width: 0;
}

.scout-actions {
    display: flex;
    gap: 0.5rem;
    flex-shrink: 0;
    align-items: flex-start;
    padding-top: 0.1rem;
}
</style>
