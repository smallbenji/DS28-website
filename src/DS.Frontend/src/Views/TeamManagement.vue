<template>
    <TeamBrowser
        title="Teamstyring"
        :teams="Teams"
        :selected-id="selectedId"
        :loading="loading"
        empty-title="Der er endnu ingen teams"
        empty-description="Opret det første team for at komme i gang."
        back-to="/activity"
        @select="(team) => router.replace(`/activity/teams/${team.id}`)"
        @clear="router.replace('/activity/teams')"
    />
</template>

<script setup lang="ts">
import { computed, onMounted, ref, watch } from "vue";
import { useRoute, useRouter } from "vue-router";
import { storeToRefs } from "pinia";
import { useActivityStore } from "@/Stores/ActivityStore";
import TeamBrowser from "@/Components/Activity/TeamBrowser.vue";

const route = useRoute();
const router = useRouter();
const activityStore = useActivityStore();
const { Teams, SelectedTeamId } = storeToRefs(activityStore);

const loading = ref(true);

onMounted(async () => {
    try {
        await activityStore.GET_TEAMS();
    } finally {
        loading.value = false;
    }
});

const selectedId = computed(() => {
    const id = route.params.id;
    if (!id) return null;

    const value = Number(Array.isArray(id) ? id[0] : id);

    return Number.isFinite(value) ? value : null;
});

watch(selectedId, (id) => {
    SelectedTeamId.value = id;
}, { immediate: true });
</script>
