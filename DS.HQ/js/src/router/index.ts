import { useGroupStore } from "@/Stores/GroupStore";
import { useUserStore } from "@/Stores/UserStore";
import Group from "@/Views/Group.vue";
import User from "@/Views/User.vue";
import { createRouter, createWebHistory, type RouteRecordRaw } from "vue-router";

const routes: RouteRecordRaw[] = [
    {
        path: "/user",
        component: User,
        beforeEnter: async () => {
            const userStore = useUserStore();
            await userStore.GET_USERS();
        }
    },
    {
        path: "/group",
        component: Group,
        beforeEnter: async () => {
            const groupStore = useGroupStore();
            await groupStore.GET_GROUPS();
        }
    }
];

const router = createRouter({

    history: createWebHistory(),
    routes
});

export default router;