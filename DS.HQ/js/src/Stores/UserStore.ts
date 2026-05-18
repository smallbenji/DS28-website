import UserService from "@/Services/UserService";
import { defineStore } from "pinia";
import { computed, ref } from "vue";

export const useUserStore = defineStore("user", () => {
    const userService = new UserService();

    const Users = ref<DSUser[]>([]);

    const USERS = computed(() => Users.value);

    async function GET_USERS() {
        var data = await userService.getUsers();
        Users.value = data;

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
        Users,
        USERS,
        GET_USERS, UPDATE_USER, CREATE_USER
    }
});