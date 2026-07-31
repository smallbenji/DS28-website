import TeamService from "@/Services/TeamService";
import { defineStore } from "pinia";
import { computed, ref } from "vue";

export const useTeamStore = defineStore("team", () => {
    const teamService = new TeamService();
    const Teams = ref<DSTeam[]>([]);
    const TEAMS = computed(() => Teams.value);

    async function GET_TEAMS() {
        var data = await teamService.getTeams();
        Teams.value = data;
        return data;
    }

    async function CREATE_TEAM(name: string) {
        const success = await teamService.createTeam(name);
        if (success) {
            await GET_TEAMS();
        }
        return success;
    }

    async function DELETE_TEAM(teamId: number) {
        const success = await teamService.deleteTeam(teamId);
        if (success) {
            await GET_TEAMS();
        }
        return success;
    }

    async function ADD_MEMBER(teamId: number, userID: string) {
        const success = await teamService.addMember(teamId, userID);
        if (success) {
            await GET_TEAMS();
        }
        return success;
    }

    async function SET_MEMBER_ADMIN(teamId: number, userID: string, isAdmin: boolean) {
        const success = await teamService.updateMember(teamId, userID, isAdmin);
        if (success) {
            await GET_TEAMS();
        }
        return success;
    }

    async function REMOVE_MEMBER(teamId: number, userID: string) {
        const success = await teamService.removeMember(teamId, userID);
        if (success) {
            await GET_TEAMS();
        }
        return success;
    }

    return {
        Teams,
        TEAMS,
        GET_TEAMS,
        CREATE_TEAM,
        DELETE_TEAM,
        ADD_MEMBER,
        SET_MEMBER_ADMIN,
        REMOVE_MEMBER
    };
});
