<template>
    <nav class="panel">
        <p class="panel-heading">
            Gruppe patruljer
        </p>
        <div class="panel-body">
            <div v-if="selectedGroup.patrols && selectedGroup.patrols.length > 0" v-for="patrol in selectedGroup.patrols" :key="patrol.id" class="panel-block">
                {{ patrol.name }}
            </div>
            <div v-else class="panel-block">
                Ingen patruljer
            </div>
            <div class="panel-block">
                <BButton type="is-success" @click="openCreateModal">Opret patrulje</BButton>
            </div>
        </div>
    </nav>

    <BModal v-model="isCreateModalOpen" has-modal-card>
        <div class="modal-card">
            <header class="modal-card-head">
                <p class="modal-card-title">Opret patrulje</p>
            </header>
            <section class="modal-card-body" style="border-bottom-left-radius: 6px; border-bottom-right-radius: 6px;">
                <BField label="Navn">
                    <BInput v-model="newPatrolName" placeholder="Patruljens navn" @keyup.enter="createPatrol" />
                </BField>
                <BButton type="is-success" @click="createPatrol">
                    Opret patrulje
                </BButton>
            </section>
        </div>
    </BModal>
</template>
<script lang="ts" setup>
import { ref } from 'vue';
import { BButton, BModal, BField, BInput, useToast } from 'buefy';
import { useGroupStore } from '@/Stores/GroupStore';
import GroupService from '@/Services/GroupService';

const props = defineProps<{
    selectedGroup: DSGroup
}>();

const emit = defineEmits<{
    (e: 'patrol-created', patrol: DSPatrol): void
}>();

const Toast = useToast();
const groupStore = useGroupStore();
const groupService = new GroupService();

const isCreateModalOpen = ref(false);
const newPatrolName = ref('');

const openCreateModal = () => {
    newPatrolName.value = '';
    isCreateModalOpen.value = true;
};

const createPatrol = async () => {
    if (!newPatrolName.value.trim()) {
        Toast.open({
            message: 'Udfyld venligst patruljens navn',
            type: 'is-warning'
        });
        return;
    }

    const patrol = await groupService.createPatrol(props.selectedGroup.id, newPatrolName.value.trim());
    if (patrol) {
        Toast.open({
            message: 'Patruljen er oprettet',
            type: 'is-success'
        });
        emit('patrol-created', patrol);
        isCreateModalOpen.value = false;
        newPatrolName.value = '';
        await groupStore.GET_GROUPS();
    } else {
        Toast.open({
            message: 'Der skete en fejl ved oprettelse af patruljen',
            type: 'is-danger'
        });
    }
};
</script>
<style lang="scss">
</style>
