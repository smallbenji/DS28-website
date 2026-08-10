import ActivityService from "@/Services/ActivityService";
import { defineStore } from "pinia";
import { computed, ref } from "vue";
import type { ActivityDto, ActivityTeamDto, ActivityWithTeamDto, UserDto } from "@/types";

export const useActivityStore = defineStore("activity", () => {
    const activityService = new ActivityService();
    const Teams = ref<ActivityTeamDto[]>([]);
    const SearchResults = ref<UserDto[]>([]);
    const SelectedTeamId = ref<number | null>(null);
    const SelectedActivity = ref<ActivityDto | null>(null);
    const TEAMS = computed(() => Teams.value);
    const SEARCH_RESULTS = computed(() => SearchResults.value);
    const SELECTED_TEAM_ID = computed(() => SelectedTeamId.value);
    const SELECTED_ACTIVITY = computed(() => SelectedActivity.value);
    const ALL_ACTIVITIES = computed<ActivityWithTeamDto[]>(() =>
        Teams.value.flatMap((team) =>
            team.activities.map((activity) => ({ ...activity, teamName: team.name }))
        )
    );

    async function GET_TEAMS() {
        const data = await activityService.getTeams();
        Teams.value = data;
        if (SelectedTeamId.value === null && data.length > 0) {
            SelectedTeamId.value = data[0].id;
        }
        return data;
    }

    async function GET_ACTIVITY(id: number) {
        const data = await activityService.getActivity(id);
        SelectedActivity.value = data;
        return data;
    }

    async function UPDATE_ACTIVITY(data: ActivityDto) {
        const success = await activityService.updateActivity(data.id, data);
        if (success) {
            await GET_ACTIVITY(data.id);
            await GET_TEAMS();
        }
        return success;
    }

    async function SEARCH_USERS(search: string, teamId: number) {
        const data = await activityService.searchUsers(search, teamId);
        SearchResults.value = data;
        return data;
    }

    async function ADD_MEMBER(teamId: number, userId: string, isAdmin = false) {
        const success = await activityService.addMember(teamId, { userId, isAdmin });
        if (success) {
            await GET_TEAMS();
        }
        return success;
    }

    async function REMOVE_MEMBER(teamId: number, userId: string) {
        const success = await activityService.removeMember(teamId, userId);
        if (success) {
            await GET_TEAMS();
        }
        return success;
    }

    async function INVITE_USER(teamId: number, email: string, isAdmin = false) {
        const success = await activityService.inviteUser(teamId, { email, isAdmin });
        if (success) {
            await GET_TEAMS();
        }
        return success;
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
        SearchResults,
        SEARCH_RESULTS,
        SelectedTeamId,
        SELECTED_TEAM_ID,
        SelectedActivity,
        SELECTED_ACTIVITY,
        ALL_ACTIVITIES,
        GET_TEAMS,
        GET_ACTIVITY,
        UPDATE_ACTIVITY,
        SEARCH_USERS,
        ADD_MEMBER,
        REMOVE_MEMBER,
        INVITE_USER,
        ADD_ACTIVITY,
        ADD_TEAM
    }
});
