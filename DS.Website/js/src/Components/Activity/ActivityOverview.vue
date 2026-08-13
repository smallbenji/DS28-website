<template>
    <div class="activity-overview ui-card">
        <div class="activity-overview-header">
            <h3 class="title is-5 mb-0">Alle aktiviteter</h3>
            <span class="tag is-light">{{ ALL_ACTIVITIES.length }} aktiviteter</span>
        </div>
        <div v-if="ALL_ACTIVITIES.length > 0" class="activity-overview-table-wrapper">
            <table class="ui-table">
                <thead>
                    <tr>
                        <th>ID</th>
                        <th>Navn</th>
                        <th>Hold</th>
                    </tr>
                </thead>
                <tbody>
                    <tr
                        v-for="activity in ALL_ACTIVITIES"
                        :key="activity.id"
                        class="activity-overview-row"
                        @click="openActivity(activity.id)"
                    >
                        <td class="has-text-grey">{{ activity.id }}</td>
                        <td class="has-text-weight-medium">{{ activity.name }}</td>
                        <td>{{ activity.teamName }}</td>
                    </tr>
                </tbody>
            </table>
        </div>
        <div v-else class="empty-state">
            <i class="fas fa-newspaper"></i>
            <span class="empty-state-title">Der er endnu ingen aktiviteter</span>
        </div>
    </div>
</template>
<script lang="ts" setup>
import { useActivityStore } from '@/Stores/ActivityStore';
import { storeToRefs } from 'pinia';
import { useRouter } from 'vue-router';

const router = useRouter();
const activityStore = useActivityStore();
const { ALL_ACTIVITIES } = storeToRefs(activityStore);

const openActivity = (id: number) => {
    router.push(`/activity/${id}`);
};
</script>
<style lang="scss">
.activity-overview {
    padding: 1rem;

    &-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 1rem;
        padding: 0.25rem 0.5rem 0.75rem;
    }

    &-table-wrapper {
        overflow-x: auto;
    }
}
</style>
