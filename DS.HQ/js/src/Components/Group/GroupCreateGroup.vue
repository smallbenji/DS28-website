<template>
    <BButton type="is-primary" @click="openCreate" icon-left="plus">
        Tilføj ny gruppe
    </BButton>
    <BModal v-model="open" has-modal-card>
        <div class="modal-card">
            <header class="modal-card-head">
                <p class="modal-card-title">Opret ny gruppe</p>
            </header>
            <section class="modal-card-body" v-if="newGroup">
                <BField label="ID">
                    <BInput v-model.number="newGroup.id" type="number" placeholder="F.eks. 1" />
                </BField>
                <BField label="Navn">
                    <BInput v-model="newGroup.name" placeholder="Gruppens navn" />
                </BField>
                <BButton type="is-primary" @click="createGroup">
                    Opret gruppe
                </BButton>
            </section>
        </div>
    </BModal>
</template>
<script setup lang="ts">
import { useGroupStore } from '@/Stores/GroupStore';
import { BModal, BField, BInput, BButton, useToast } from 'buefy';
import { ref, toRaw } from 'vue';

const Toast = useToast();
const groupStore = useGroupStore();

const newGroup = ref<DSGroup | null>(null);
const open = ref<boolean>(false);

const openCreate = () => {
    open.value = true;
    newGroup.value = { id: 0, name: '', patrols: [] };
};

const createGroup = async () => {
    if (!newGroup.value) return;

    if (!newGroup.value.id || !newGroup.value.name) {
        Toast.open({
            message: 'Udfyld venligst både ID og navn',
            type: 'is-warning'
        });
        return;
    }

    const success = await groupStore.CREATE_GROUP(toRaw(newGroup.value));
    if (success) {
        Toast.open({
            message: 'Gruppen er oprettet',
            type: 'is-success'
        });
        open.value = false;
        newGroup.value = null;
    } else {
        Toast.open({
            message: 'Der skete en fejl ved oprettelse af gruppen',
            type: 'is-danger'
        });
    }
};
</script>
