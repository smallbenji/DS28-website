<template>
    <nav class="panel">
        <p class="panel-heading">
            Gruppe spejdere
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
                    <div class="column is-6" style="padding: 0.25rem 0.75rem;">
                        <div class="tags" v-if="scout.memberships && scout.memberships.length > 0" style="margin-bottom: 0.5rem;">
                            <span 
                                v-for="membership in scout.memberships" 
                                :key="membership.id" 
                                class="tag"
                                :class="membership.isPatrolLeader ? 'is-warning' : 'is-info'"
                                style="cursor: pointer; user-select: none;"
                                @click="toggleLeader(scout.id, membership.patrolId)"
                                title="Klik for at markere som leder / ikke-leder"
                            >
                                <span v-if="membership.isPatrolLeader" class="icon is-small mr-1" style="margin-right: 0.25rem;">
                                    <i class="fas fa-crown"></i>
                                </span>
                                {{ getPatrolName(membership.patrolId) }}
                                <button class="delete is-small" @click.stop="removePatrol(scout, membership.patrolId)"></button>
                            </span>
                        </div>
                        <BSelect 
                            v-if="getAvailablePatrols(scout).length > 0"
                            :model-value="null" 
                            @update:model-value="(val: number | null) => addPatrol(scout, val)" 
                            expanded
                            size="is-small"
                        >
                            <option :value="null" disabled selected>Tilmeld patrulje...</option>
                            <option v-for="patrol in getAvailablePatrols(scout)" :key="patrol.id" :value="patrol.id">
                                {{ patrol.name }}
                            </option>
                        </BSelect>
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
</template>
<script lang="ts" setup>
import { ref } from 'vue';
import { BButton, BModal, BField, BInput, BSelect, useToast } from 'buefy';
import { useGroupStore } from '@/Stores/GroupStore';
import GroupService from '@/Services/GroupService';

const props = defineProps<{
    selectedGroup: DSGroup
}>();

const emit = defineEmits<{
    (e: 'scout-created', scout: DSScout): void;
    (e: 'patrol-assigned', scoutId: number, patrolId: number, action: 'add' | 'remove'): void;
    (e: 'patrol-leader-toggled', scoutId: number, patrolId: number): void;
}>();

const Toast = useToast();
const groupStore = useGroupStore();
const groupService = new GroupService();

const isCreateModalOpen = ref(false);
const newScoutName = ref('');
const newScoutBirthday = ref('');
const newScoutGender = ref<'Male' | 'Female'>('Male');

const openCreateModal = () => {
    newScoutName.value = '';
    newScoutBirthday.value = '';
    newScoutGender.value = 'Male';
    isCreateModalOpen.value = true;
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

const getAvailablePatrols = (scout: DSScout) => {
    const currentPatrolIds = scout.memberships ? scout.memberships.map(m => m.patrolId) : [];
    return props.selectedGroup.patrols.filter(p => !currentPatrolIds.includes(p.id));
};

const addPatrol = async (scout: DSScout, patrolId: number | null) => {
    if (!patrolId) return;

    const success = await groupService.addPatrol(scout.id, patrolId);
    if (success) {
        Toast.open({
            message: `${scout.name} er tilføjet til patruljen`,
            type: 'is-success'
        });
        emit('patrol-assigned', scout.id, patrolId, 'add');
        await groupStore.GET_GROUPS();
    } else {
        Toast.open({
            message: 'Der skete en fejl ved tilføjelse til patruljen',
            type: 'is-danger'
        });
    }
};

const removePatrol = async (scout: DSScout, patrolId: number) => {
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

const toggleLeader = async (scoutId: number, patrolId: number) => {
    const success = await groupService.toggleLeader(scoutId, patrolId);
    if (success) {
        Toast.open({
            message: 'Leder-status opdateret',
            type: 'is-success'
        });
        emit('patrol-leader-toggled', scoutId, patrolId);
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
