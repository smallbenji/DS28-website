import GroupsService from "@/Services/GroupsService";
import { defineStore } from "pinia";
import { computed, ref } from "vue";
import type { GroupDto, GroupsDto } from "@/types";

export const useGroupsStore = defineStore("groups", () => {
    const groupService = new GroupsService();
    const Groups = ref<GroupsDto>({ groups: [], users: {}});
    const GROUPS = computed(() => Groups.value);

    async function GET_GROUPS() {
        var data = await groupService.getGroups();
        Groups.value = data;
        return data;
    }

    async function CREATE_GROUP(group: GroupDto) {
        const success = await groupService.createGroup(group);
        if (success) {
            await GET_GROUPS();
        }
        return success;
    }

    async function UPDATE_GROUP(group: GroupDto) {
        const success = await groupService.updateGroup(group);
        if (success) {
            await GET_GROUPS();
        }
        return success;
    }

    return {
        Groups,
        GROUPS,
        GET_GROUPS,
        CREATE_GROUP,
        UPDATE_GROUP
    }
});
