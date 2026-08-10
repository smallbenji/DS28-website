<template>
    <nav class="panel team-activities">
        <div class="panel-heading">
            <span>Aktiviteter</span>
            <div class="flex"></div>
            <BButton
                v-if="canManageActivities"
                type="is-primary"
                size="is-small"
                icon-left="plus"
                @click="open = true"
            >
                Opret aktivitet
            </BButton>
        </div>
        <div class="panel-body">
            <template v-if="SelectedTeamId">
                <div v-if="selectedTeam && selectedTeam.activities.length > 0" class="team-activities-table-wrapper">
                    <table class="ui-table">
                        <thead>
                            <tr>
                                <th>ID</th>
                                <th>Navn</th>
                                <th>Budget</th>
                                <th class="has-text-right">Status</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr
                                v-for="activity in selectedTeam.activities"
                                :key="activity.id"
                                class="team-activities-row"
                                @click="openActivity(activity.id)"
                            >
                                <td class="has-text-grey">{{ activity.id }}</td>
                                <td class="has-text-weight-medium">{{ activity.name }}</td>
                                <td>{{ activity.budget }}</td>
                                <td class="has-text-right">
                                    <span class="tag is-success is-light" v-if="isFilled(activity)">
                                        <i class="fas fa-check"></i> Udfyldt
                                    </span>
                                    <span class="tag is-danger is-light" v-else>
                                        <i class="fas fa-circle-exclamation"></i> Mangler
                                    </span>
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
                <div v-else class="empty-state">
                    <i class="fas fa-newspaper"></i>
                    <span class="empty-state-title">Der er endnu ingen aktiviteter i dette team</span>
                </div>
            </template>
        </div>
    </nav>
    <BModal v-model="open" has-modal-card>
        <div class="modal-card">
            <header class="modal-card-head">
                <p class="modal-card-title">Opret aktivitet</p>
            </header>
            <section class="modal-card-body">
                <BField label="Navn">
                    <BInput v-model="activityName" placeholder="Aktivitetens navn" @keyup.enter="createActivity" />
                </BField>
            </section>
            <footer class="modal-card-foot">
                <div class="buttons">
                    <BButton type="is-primary" :loading="creating" @click="createActivity">
                        Opret aktivitet
                    </BButton>
                    <BButton @click="open = false">Annuller</BButton>
                </div>
            </footer>
        </div>
    </BModal>
</template>
<script lang="ts" setup>
import { useActivityStore } from '@/Stores/ActivityStore';
import { useMeStore } from '@/Stores/MeStore';
import { BButton, BField, BInput, BModal, useToast } from 'buefy';
import { storeToRefs } from 'pinia';
import { computed, ref } from 'vue';
import { useRouter } from 'vue-router';
import type { ActivityDto, ActivityTeamDto } from '@/types';

const Toast = useToast();
const router = useRouter();
const open = ref(false);
const creating = ref(false);
const activityName = ref('');
const activityStore = useActivityStore();
const meStore = useMeStore();
const { Teams, SelectedTeamId } = storeToRefs(activityStore);

const selectedTeam = computed<ActivityTeamDto | null>(
    () => Teams.value.find((team) => team.id === SelectedTeamId.value) ?? null
);

const canManageActivities = computed(() => {
    if (meStore.ME.appRoles.includes('ActivityAdmin')) return true;
    return selectedTeam.value?.members.some(
        (member) => member.userId === meStore.ME.id && member.isAdmin
    ) ?? false;
});

const isFilled = (activity: ActivityDto): boolean => {
    const nameFilled = activity.name.trim().length > 0;
    const catalog = activity.catalog;
    const catalogFilled = !!(catalog?.name?.trim() || catalog?.summary?.trim() || catalog?.description?.trim());
    return nameFilled && catalogFilled;
};

const openActivity = (id: number) => {
    router.push(`/activity/${id}`);
};

const createActivity = async () => {
    if (!SelectedTeamId.value) return;

    if (!activityName.value) {
        Toast.open({
            message: 'Udfyld venligst aktivitetens navn',
            type: 'is-warning'
        });
        return;
    }

    creating.value = true;
    const success = await activityStore.ADD_ACTIVITY(SelectedTeamId.value, activityName.value);
    creating.value = false;

    if (success) {
        Toast.open({
            message: 'Aktiviteten er oprettet',
            type: 'is-success'
        });
        open.value = false;
        activityName.value = '';
    } else {
        Toast.open({
            message: 'Der skete en fejl ved oprettelse af aktiviteten',
            type: 'is-danger'
        });
    }
};
</script>
<style lang="scss">
.team-activities {
    margin-bottom: 1rem;

    .panel-heading {
        display: flex;
        align-items: center;
        gap: 0.5rem;
    }

    .panel-body {
        padding: 1rem 0;
    }

    &-table-wrapper {
        overflow-x: auto;
    }

    .tag i {
        margin-right: 0.3rem;
    }
}
</style>
