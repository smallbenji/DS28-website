<template>
    <div class="activity-overview">
        <div class="activity-overview-counts" aria-label="Filtrer efter godkendelsesstatus">
            <button v-for="option in countFilters" :key="option.value" type="button" class="activity-overview-count" :class="{ 'is-active': statusFilter === option.value }" :aria-pressed="statusFilter === option.value" @click="statusFilter = option.value">
                <span>{{ option.label }}</span>
                <strong>{{ countFor(option.value) }}</strong>
            </button>
        </div>
        <div class="ui-card activity-overview-list">
            <div class="activity-overview-header">
                <h3 class="title is-5 mb-0">Aktiviteter</h3>
                <span class="has-text-grey is-size-7">{{ filteredActivities.length }} af {{ ALL_ACTIVITIES.length }}</span>
            </div>
            <div class="activity-overview-filters">
                <BField label="Søg" class="activity-overview-search">
                    <BInput v-model="search" placeholder="Aktivitet eller team…" icon="magnifying-glass" />
                </BField>
                <BField label="Team">
                    <BSelect v-model="teamFilter" expanded>
                        <option value="">Alle teams</option>
                        <option v-for="team in teams" :key="team.id" :value="String(team.id)">{{ team.name }}</option>
                    </BSelect>
                </BField>
                <BField label="Status">
                    <BSelect v-model="statusFilter" expanded>
                        <option value="all">Alle statusser</option>
                        <option v-for="status in activityReviewStatuses" :key="status.value" :value="status.value">{{ status.label }}</option>
                    </BSelect>
                </BField>
            </div>
            <BTable :data="filteredActivities" hoverable default-sort="submittedAt" default-sort-direction="desc">
                <BTableColumn field="name" label="Aktivitet" sortable v-slot="props">
                    <router-link :to="`/activity/${props.row.id}`" class="has-text-weight-medium">{{ props.row.name }}</router-link>
                </BTableColumn>
                <BTableColumn field="teamName" label="Team" sortable v-slot="props">{{ props.row.teamName }}</BTableColumn>
                <BTableColumn field="reviewStatus" label="Status" sortable v-slot="props">
                    <span class="tag" :class="activityReviewStatus(props.row.reviewStatus).type">{{ activityReviewStatus(props.row.reviewStatus).label }}</span>
                </BTableColumn>
                <BTableColumn field="budget" label="Budget" sortable numeric v-slot="props">{{ currency.format(props.row.budget) }}</BTableColumn>
                <BTableColumn field="submittedAt" label="Indsendt" sortable v-slot="props">{{ props.row.submittedAt ? dateFormat.format(new Date(props.row.submittedAt)) : '—' }}</BTableColumn>
                <template #empty>
                    <div class="activity-overview-empty">
                        <p>{{ ALL_ACTIVITIES.length ? 'Ingen aktiviteter matcher filtrene.' : 'Der er endnu ingen aktiviteter.' }}</p>
                        <BButton v-if="ALL_ACTIVITIES.length" size="is-small" @click="resetFilters">Nulstil filtre</BButton>
                    </div>
                </template>
            </BTable>
        </div>
    </div>
</template>

<script lang="ts" setup>
import { useActivityStore } from '@/Stores/ActivityStore';
import { storeToRefs } from 'pinia';
import { computed, ref } from 'vue';
import { BButton, BField, BInput, BSelect, BTable, BTableColumn } from 'buefy';
import { activityReviewStatus, activityReviewStatuses } from '@/lib/activityReview';
import type { ActivityReviewStatus } from '@/types';

const activityStore = useActivityStore();
const { ALL_ACTIVITIES, Teams } = storeToRefs(activityStore);
const search = ref('');
const teamFilter = ref('');
const statusFilter = ref<ActivityReviewStatus | 'all'>('all');
const currency = new Intl.NumberFormat('da-DK', { style: 'currency', currency: 'DKK', minimumFractionDigits: 0, maximumFractionDigits: 2 });
const dateFormat = new Intl.DateTimeFormat('da-DK', { dateStyle: 'medium', timeZone: 'Europe/Copenhagen' });
const countFilters: { value: ActivityReviewStatus | 'all'; label: string }[] = [
    { value: 'Submitted', label: 'Afventer godkendelse' },
    { value: 'ChangesRequested', label: 'Sendt retur' },
    { value: 'Approved', label: 'Godkendte' },
    { value: 'all', label: 'Alle aktiviteter' }
];
const teams = computed(() => [...Teams.value].sort((a, b) => a.name.localeCompare(b.name, 'da')));
const filteredActivities = computed(() => {
    const query = search.value.trim().toLocaleLowerCase('da-DK');
    return Teams.value
        .filter((team) => !teamFilter.value || String(team.id) === teamFilter.value)
        .flatMap((team) => team.activities.map((activity) => ({ ...activity, teamName: team.name })))
        .filter((activity) => (statusFilter.value === 'all' || activity.reviewStatus === statusFilter.value)
            && (!query || `${activity.name} ${activity.teamName}`.toLocaleLowerCase('da-DK').includes(query)));
});

function countFor(status: ActivityReviewStatus | 'all') {
    return status === 'all' ? ALL_ACTIVITIES.value.length : ALL_ACTIVITIES.value.filter((activity) => activity.reviewStatus === status).length;
}

function resetFilters() {
    search.value = '';
    teamFilter.value = '';
    statusFilter.value = 'all';
}
</script>

<style scoped>
.activity-overview-counts {
    display: grid;
    grid-template-columns: repeat(4, minmax(0, 1fr));
    gap: 0.75rem;
    margin-bottom: 1.25rem;
}

.activity-overview-count {
    display: flex;
    flex-direction: column;
    align-items: flex-start;
    gap: 0.5rem;
    padding: 1rem 1.25rem;
    background: #fff;
    border: 1px solid #e5eaf0;
    border-radius: 8px;
    font: inherit;
    color: #596575;
    text-align: left;
    cursor: pointer;
}

.activity-overview-count strong {
    font-size: 1.75rem;
    color: #363f4d;
}

.activity-overview-count:hover,
.activity-overview-count.is-active {
    border-color: #a9b8d2;
    background: #f6f8fc;
}

.activity-overview-list {
    padding: 1.25rem;
}

.activity-overview-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
    margin-bottom: 1rem;
}

.activity-overview-filters {
    display: flex;
    flex-wrap: wrap;
    align-items: flex-start;
    gap: 1rem;
    margin-bottom: 0.75rem;
}

.activity-overview-search {
    flex: 1 1 16rem;
}

.activity-overview-filters > .field {
    margin-bottom: 0;
}

.activity-overview-empty {
    display: flex;
    flex-direction: column;
    align-items: center;
    gap: 0.75rem;
    padding: 2rem;
    color: #737b86;
}

@media (max-width: 768px) {
    .activity-overview-counts {
        grid-template-columns: repeat(2, minmax(0, 1fr));
    }
}
</style>
