import UserService from "@/Services/UserService";
import PasswordResetService from "@/Services/PasswordResetService";
import { defineStore } from "pinia";
import { computed, ref } from "vue";
import type { ResetPasswordLinkDto, RoleDto, UserDto } from "@/types";

export const useUserStore = defineStore("user", () => {
    const userService = new UserService();
    const passwordResetService = new PasswordResetService();

    const Users = ref<UserDto[]>([]);
    const Groups = ref<RoleDto[]>([]);
    const AssignableGroups = ref<string[]>([]);

    const USERS = computed(() => Users.value);
    const GROUPS = computed(() => Groups.value);
    const ASSIGNABLE_GROUPS = computed(() => AssignableGroups.value);

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

    async function GET_ASSIGNABLE_GROUPS() {
        var data = await userService.getAssignableGroups();
        AssignableGroups.value = data;

        return data;
    }

    async function ADD_USER_TO_ROLE(user: UserDto, roleName: string) {
        var data = await userService.AssignRoleToUser(user, roleName);

        if (data)
            GET_USERS();

        return data;
    }

    async function REMOVE_USER_FROM_ROLE(user: UserDto, roleName: string) {
        var data = await userService.RemoveRoleFromUser(user, roleName);

        if (data)
            GET_USERS();

        return data;
    }

    async function CREATE_USER(user: UserDto) {
        var data = await userService.createUser(user);

        if (data)
            GET_USERS();

        return data;
    }

    async function UPDATE_USER(user: UserDto) {
        var data = await userService.updateUser(user);

        if (data)
            GET_USERS();

        return data;
    }

    async function DELETE_USER(user: UserDto) {
        var data = await userService.deleteUser(user);

        if (data)
            GET_USERS();

        return data;
    }

    async function LOCK_USER(user: UserDto) {
        var data = await userService.lockUser(user);

        if (data)
            GET_USERS();

        return data;
    }

    async function UNLOCK_USER(user: UserDto) {
        var data = await userService.unlockUser(user);

        if (data)
            GET_USERS();

        return data;
    }

    async function INVITE_USER(email: string, roles: string[]) {
        return await userService.inviteUser(email, roles);
    }

    async function CREATE_RESET_PASSWORD_LINK(user: UserDto): Promise<ResetPasswordLinkDto | null> {
        return await passwordResetService.createResetPasswordLink(user.id);
    }

    return {
        Users, Groups, AssignableGroups,
        USERS, GROUPS, ASSIGNABLE_GROUPS,
        GET_USERS, GET_GROUPS, GET_ASSIGNABLE_GROUPS, UPDATE_USER, DELETE_USER, LOCK_USER, UNLOCK_USER, CREATE_USER, REMOVE_USER_FROM_ROLE, ADD_USER_TO_ROLE, INVITE_USER, CREATE_RESET_PASSWORD_LINK
    }
});