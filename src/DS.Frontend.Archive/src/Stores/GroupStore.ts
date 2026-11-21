import GroupService from "@/Services/GroupService";
import { District, type GroupDto } from "@/types";
import { defineStore } from "pinia";
import { ref } from "vue";

export const useGroupStore = defineStore("group", () => {
    const groupStore = new GroupService();
    const emptyGroup = (): GroupDto => ({ name: "", id: "", district: District.DANEHOF, patrols: [], scouts: [], users: [] });
    const Group = ref<GroupDto>(emptyGroup());

    async function GET_GROUP() {
        var data = await groupStore.getGroup();
        Group.value = data;
        return data;
    }

    async function LOAD_GROUP() {
        const data = await groupStore.findGroup();
        Group.value = data ?? emptyGroup();
        return data;
    }

    return {
        Group,
        GET_GROUP,
        LOAD_GROUP
    }
});
