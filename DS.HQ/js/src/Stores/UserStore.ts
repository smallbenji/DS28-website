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

    return {
        Users,
        USERS,
        GET_USERS,
    }
});