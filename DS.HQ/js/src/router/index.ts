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
            // Get data
        }
    }
];

const router = createRouter({

    history: createWebHistory(),
    routes
});

export default router;