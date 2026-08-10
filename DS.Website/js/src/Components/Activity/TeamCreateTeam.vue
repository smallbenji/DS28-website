<template>
    <BButton type="is-primary" icon-left="plus" @click="open = true">
        Opret team
    </BButton>
    <BModal v-model="open" has-modal-card>
        <div class="modal-card">
            <header class="modal-card-head">
                <p class="modal-card-title">Opret team</p>
            </header>
            <section class="modal-card-body">
                <BField label="Navn">
                    <BInput v-model="teamName" placeholder="Teamets navn" />
                </BField>
            </section>
            <footer class="modal-card-foot">
                <div class="buttons">
                    <BButton type="is-primary" @click="createTeam">
                        Opret team
                    </BButton>
                    <BButton @click="open = false">Annuller</BButton>
                </div>
            </footer>
        </div>
    </BModal>
</template>
<script lang="ts" setup>
import { useActivityStore } from '@/Stores/ActivityStore';
import { BButton, BField, BInput, BModal, useToast } from 'buefy';
import { ref } from 'vue';

const Toast = useToast();
const open = ref(false);
const teamName = ref('');
const activityStore = useActivityStore();

const createTeam = async () => {
    if (!teamName.value) {
        Toast.open({
            message: 'Udfyld venligst teamets navn',
            type: 'is-warning'
        });
        return;
    }

    const success = await activityStore.ADD_TEAM(teamName.value);
    if (success) {
        Toast.open({
            message: 'Teamet er oprettet',
            type: 'is-success'
        });
        open.value = false;
        teamName.value = '';
    } else {
        Toast.open({
            message: 'Der skete en fejl ved oprettelse af teamet',
            type: 'is-danger'
        });
    }
};
</script>
