import ActivityService from "@/Services/ActivityService";
import { defineStore } from "pinia";
import { computed, ref } from "vue";
import type { ActivityTeamDto } from "@/types";

export const useActivityStore = defineStore("activity", () => {
    const activityService = new ActivityService();
    const Teams = ref<ActivityTeamDto[]>([]);
    const TEAMS = computed(() => Teams.value);

    async function GET_TEAMS() {
        const data = await activityService.getTeams();
        Teams.value = data;
        return data;
    }

    async function ADD_ACTIVITY(teamId: number, name: string) {
        const success = await activityService.addActivity(teamId, name);
        if (success) {
            await GET_TEAMS();
        }
        return success;
    }

    async function ADD_TEAM(name: string) {
        const success = await activityService.addTeam(name);
        if (success) {
            await GET_TEAMS();
        }
        return success;
    }

    return {
        Teams,
        TEAMS,
        GET_TEAMS,
        ADD_ACTIVITY,
        ADD_TEAM
    }
});
