import UserService from "@/Services/UserService";
import { defineStore } from "pinia";
import { computed, ref } from "vue";

export const useUserStore = defineStore("user", () => {
    const userService = new UserService();

    const Users = ref<DSUser[]>([]);
    const Groups = ref<KcGroup[]>([]);

    const USERS = computed(() => Users.value);
    const GROUPS = computed(() => Groups.value);

    async function GET_USERS() {
        var data = await userService.getUsers();
        Users.value = data;

        return data;
    }

    async function GET_GROUPS() {
        var data = await userService.getGroups();
        Groups.value = data;

        return data;
    }

    async function ADD_USER_TO_ROLE(user: DSUser, groupId: string) {
        var data = await userService.AssignRoleToUser(user, groupId);

        if (data)
            GET_USERS();

        return data;
    }

    async function REMOVE_USER_FROM_ROLE(user: DSUser, groupId: string) {
        var data = await userService.RemoveRoleFromUser(user, groupId);

        if (data)
            GET_USERS();

        return data;
    }

    async function CREATE_USER(user: DSUser) {
        var data = await userService.createUser(user);

        if (data)
            GET_USERS();

        return data;
    }

    async function UPDATE_USER(user: DSUser) {
        var data = await userService.updateUser(user);

        if (data)
            GET_USERS();

        return data;
    }

    return {
        Users, Groups,
        USERS, GROUPS,
        GET_USERS, GET_GROUPS, UPDATE_USER, CREATE_USER, REMOVE_USER_FROM_ROLE, ADD_USER_TO_ROLE
    }
});