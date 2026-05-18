import { useGroupStore } from "@/Stores/GroupStore";
import { useUserStore } from "@/Stores/UserStore";
import { createRouter, createWebHistory, type RouteRecordRaw } from "vue-router";

const routes: RouteRecordRaw[] = [
    {
        path: "/user",
        component: () => import("@/Views/User.vue"),
        beforeEnter: async () => {
            const userStore = useUserStore();
            await userStore.GET_USERS();
        }
    },
    {
        path: "/userv2",
        component: () => import("@/Views/UserV2.vue"),
        beforeEnter: async () => {
            const userStore = useUserStore();
            await userStore.GET_USERS();
            await userStore.GET_GROUPS();

            const groupStore = useGroupStore();
            await groupStore.GET_GROUPS();
        }
    },
    {
        path: "/group",
        component: () => import("@/Views/Group.vue"),
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