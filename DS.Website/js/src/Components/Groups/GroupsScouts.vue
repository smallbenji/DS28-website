<template>
    <nav class="panel">
        <p class="panel-heading">
            Spejdere
        </p>
        <div class="panel-body">
            <div v-if="selectedGroup.scouts && selectedGroup.scouts.length > 0" v-for="scout in selectedGroup.scouts" :key="scout.id" class="panel-block">
                <div class="columns is-vcentered is-mobile" style="width: 100%; margin: 0;">
                    <div class="column is-6" style="padding: 0.25rem 0.75rem;">
                        <strong>{{ scout.name }}</strong>
                        <div class="is-size-7 has-text-grey">
                            {{ formatDate(scout.birthday) }}, {{ translateGender(scout.gender) }}
                        </div>
                    </div>
                    <div class="column is-4" style="padding: 0.25rem 0.75rem;">
                        <BButton type="is-info is-small" @click="openPatrolsModal(scout)">
                            Patruljer ({{ scout.memberships?.length ?? 0 }})
                        </BButton>
                    </div>
                    <div class="column is-2" style="padding: 0.25rem 0.75rem;">
                        <BButton type="is-danger is-small" @click="openDeleteModal(scout)">
                            Slet
                        </BButton>
                    </div>
                </div>
            </div>
            <div v-else class="panel-block">
                Ingen spejdere
            </div>
            <div class="panel-block">
                <BButton type="is-success" @click="openCreateModal">Opret spejder</BButton>
            </div>
        </div>
    </nav>

    <BModal v-model="isCreateModalOpen" has-modal-card>
        <div class="modal-card">
            <header class="modal-card-head">
                <p class="modal-card-title">Opret spejder</p>
            </header>
            <section class="modal-card-body" style="border-bottom-left-radius: 6px; border-bottom-right-radius: 6px;">
                <BField label="Navn">
                    <BInput v-model="newScoutName" placeholder="Spejderens navn" @keyup.enter="createScout" />
                </BField>
                <BField label="Fødselsdato">
                    <BInput v-model="newScoutBirthday" type="date" @keyup.enter="createScout" />
                </BField>
                <BField label="Køn">
                    <BSelect v-model="newScoutGender" expanded>
                        <option value="Male">Mand</option>
                        <option value="Female">Kvinde</option>
                    </BSelect>
                </BField>
                <BButton type="is-success" @click="createScout">
                    Opret spejder
                </BButton>
            </section>
        </div>
    </BModal>

    <BModal v-model="isPatrolsModalOpen" has-modal-card>
        <div class="modal-card">
            <header class="modal-card-head">
                <p class="modal-card-title">Patruljer for {{ scoutToEdit?.name }}</p>
            </header>
            <section class="modal-card-body">
                <div v-if="scoutToEdit?.memberships && scoutToEdit.memberships.length > 0">
                    <div v-for="membership in scoutToEdit.memberships" :key="membership.id" class="field is-grouped is-align-items-center" style="margin-bottom: 0.5rem;">
                        <div class="control">
                            <span
                                class="tag is-medium"
                                :class="membership.isPatrolLeader ? 'is-warning' : 'is-info'"
                                style="cursor: pointer; user-select: none;"
                                @click="toggleLeader(membership.patrolId)"
                                title="Klik for at markere som leder / ikke-leder"
                            >
                                <span v-if="membership.isPatrolLeader" class="icon is-small mr-1" style="margin-right: 0.25rem;">
                                    <i class="fas fa-crown"></i>
                                </span>
                                {{ getPatrolName(membership.patrolId) }}
                            </span>
                        </div>
                        <div class="control">
                            <BButton type="is-danger is-small" @click="removePatrol(membership.patrolId)">
                                Fjern
                            </BButton>
                        </div>
                    </div>
                </div>
                <div v-else class="is-italic has-text-grey">
                    Ingen patruljer
                </div>
                <div v-if="getAvailablePatrols().length > 0" style="margin-top: 1rem;">
                    <BField label="Tilføj til patrulje">
                        <div class="field has-addons">
                            <div class="control is-expanded">
                                <BSelect v-model="selectedPatrolToAdd" expanded placeholder="Vælg patrulje...">
                                    <option v-for="patrol in getAvailablePatrols()" :key="patrol.id" :value="patrol.id">
                                        {{ patrol.name }}
                                    </option>
                                </BSelect>
                            </div>
                            <div class="control">
                                <BButton type="is-success" :disabled="!selectedPatrolToAdd" @click="addPatrol">
                                    Tilføj
                                </BButton>
                            </div>
                        </div>
                    </BField>
                </div>
            </section>
            <footer class="modal-card-foot">
                <BButton type="is-primary" @click="isPatrolsModalOpen = false">
                    Luk
                </BButton>
            </footer>
        </div>
    </BModal>

    <BModal v-model="isDeleteModalOpen" has-modal-card>
        <div class="modal-card">
            <div class="modal-card-head">
                Slet spejder
            </div>
            <div class="modal-card-body">
                Er du sikker på, at du vil slette spejderen "{{ scoutToDelete?.name }}"? Alle tilknyttede medlemsskaber slettes også.
            </div>
            <div class="modal-card-foot">
                <BButton type="is-danger" @click="deleteScout">
                    Slet spejder
                </BButton>
                <BButton type="is-primary" @click="isDeleteModalOpen = false">
                    Annuller
                </BButton>
            </div>
        </div>
    </BModal>
</template>
<script lang="ts" setup>
import { ref } from 'vue';
import { BButton, BModal, BField, BInput, BSelect, useToast } from 'buefy';
import { useGroupsStore } from '@/Stores/GroupsStore';
import GroupsService from '@/Services/GroupsService';

const props = defineProps<{
    selectedGroup: DSGroup
}>();

const emit = defineEmits<{
    (e: 'scout-created', scout: DSScout): void;
    (e: 'scout-deleted', scoutId: number): void;
    (e: 'patrol-assigned', scoutId: number, patrolId: number, action: 'add' | 'remove'): void;
    (e: 'patrol-leader-toggled', scoutId: number, patrolId: number): void;
}>();

const Toast = useToast();
const groupStore = useGroupsStore();
const groupService = new GroupsService();

const isCreateModalOpen = ref(false);
const newScoutName = ref('');
const newScoutBirthday = ref('');
const newScoutGender = ref<'Male' | 'Female'>('Male');

const isPatrolsModalOpen = ref(false);
const scoutToEdit = ref<DSScout | null>(null);
const selectedPatrolToAdd = ref<number | null>(null);

const isDeleteModalOpen = ref(false);
const scoutToDelete = ref<DSScout | null>(null);

const openCreateModal = () => {
    newScoutName.value = '';
    newScoutBirthday.value = '';
    newScoutGender.value = 'Male';
    isCreateModalOpen.value = true;
};

const openPatrolsModal = (scout: DSScout) => {
    scoutToEdit.value = scout;
    selectedPatrolToAdd.value = null;
    isPatrolsModalOpen.value = true;
};

const openDeleteModal = (scout: DSScout) => {
    scoutToDelete.value = scout;
    isDeleteModalOpen.value = true;
};

const deleteScout = async () => {
    if (!scoutToDelete.value) return;

    const success = await groupService.deleteScout(scoutToDelete.value.id);
    if (success) {
        Toast.open({
            message: 'Spejderen er slettet',
            type: 'is-success'
        });
        emit('scout-deleted', scoutToDelete.value.id);
        isDeleteModalOpen.value = false;
        scoutToDelete.value = null;
        await groupStore.GET_GROUPS();
    } else {
        Toast.open({
            message: 'Der skete en fejl ved sletning af spejderen',
            type: 'is-danger'
        });
    }
};

const formatDate = (dateStr: string) => {
    if (!dateStr) return '';
    const date = new Date(dateStr);
    return date.toLocaleDateString('da-DK', { day: 'numeric', month: 'short', year: 'numeric' });
};

const translateGender = (gender: 'Male' | 'Female' | number) => {
    return (gender === 'Male' || gender === 0) ? 'Mand' : 'Kvinde';
};

const getPatrolName = (patrolId: number): string => {
    const patrol = props.selectedGroup.patrols.find(p => p.id === patrolId);
    return patrol ? patrol.name : 'Ukendt patrulje';
};

const getAvailablePatrols = () => {
    if (!scoutToEdit.value) return [];
    const currentPatrolIds = scoutToEdit.value.memberships ? scoutToEdit.value.memberships.map(m => m.patrolId) : [];
    return props.selectedGroup.patrols.filter(p => !currentPatrolIds.includes(p.id));
};

const addPatrol = async () => {
    if (!scoutToEdit.value || !selectedPatrolToAdd.value) return;

    const scout = scoutToEdit.value;
    const patrolId = selectedPatrolToAdd.value;

    const success = await groupService.addPatrol(scout.id, patrolId);
    if (success) {
        Toast.open({
            message: `${scout.name} er tilføjet til patruljen`,
            type: 'is-success'
        });
        emit('patrol-assigned', scout.id, patrolId, 'add');
        selectedPatrolToAdd.value = null;
        await groupStore.GET_GROUPS();
    } else {
        Toast.open({
            message: 'Der skete en fejl ved tilføjelse til patruljen',
            type: 'is-danger'
        });
    }
};

const removePatrol = async (patrolId: number) => {
    if (!scoutToEdit.value) return;

    const scout = scoutToEdit.value;

    const success = await groupService.removePatrol(scout.id, patrolId);
    if (success) {
        Toast.open({
            message: `${scout.name} er fjernet fra patruljen`,
            type: 'is-success'
        });
        emit('patrol-assigned', scout.id, patrolId, 'remove');
        await groupStore.GET_GROUPS();
    } else {
        Toast.open({
            message: 'Der skete en fejl ved fjernelse fra patruljen',
            type: 'is-danger'
        });
    }
};

const toggleLeader = async (patrolId: number) => {
    if (!scoutToEdit.value) return;

    const success = await groupService.toggleLeader(scoutToEdit.value.id, patrolId);
    if (success) {
        Toast.open({
            message: 'Leder-status opdateret',
            type: 'is-success'
        });
        emit('patrol-leader-toggled', scoutToEdit.value.id, patrolId);
        await groupStore.GET_GROUPS();
    } else {
        Toast.open({
            message: 'Der skete en fejl ved opdatering af leder-status',
            type: 'is-danger'
        });
    }
};

const createScout = async () => {
    if (!newScoutName.value.trim()) {
        Toast.open({
            message: 'Udfyld venligst spejderens navn',
            type: 'is-warning'
        });
        return;
    }
    if (!newScoutBirthday.value) {
        Toast.open({
            message: 'Udfyld venligst fødselsdatoen',
            type: 'is-warning'
        });
        return;
    }

    const scout = await groupService.createScout(
        props.selectedGroup.id,
        newScoutName.value.trim(),
        newScoutBirthday.value,
        newScoutGender.value
    );

    if (scout) {
        Toast.open({
            message: 'Spejderen er oprettet',
            type: 'is-success'
        });
        emit('scout-created', scout);
        isCreateModalOpen.value = false;
        await groupStore.GET_GROUPS();
    } else {
        Toast.open({
            message: 'Der skete en fejl ved oprettelse af spejderen',
            type: 'is-danger'
        });
    }
};
</script>
<style lang="scss">
</style>
