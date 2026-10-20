import GroupService from "@/Services/GroupService";
import { District, type GroupDto } from "@/types";
import { defineStore } from "pinia";
import { computed, ref } from "vue";

export const useGroupStore = defineStore("group", () => {
    const groupStore = new GroupService();
    const Group = ref<GroupDto>({ name: "", id: "", district: District.DANEHOF, patrols: [], scouts: [], users: [] });
    const GROUP = computed(() => Group.value);

    async function GET_GROUP() {
        var data = await groupStore.getGroup();
        Group.value = data;
        return data;
    }

    return {
        Group,
        GROUP,
        GET_GROUP
    }
});