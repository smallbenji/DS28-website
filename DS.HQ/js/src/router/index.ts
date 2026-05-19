import { useGroupStore } from "@/Stores/GroupStore";
import { useUserStore } from "@/Stores/UserStore";
import { createRouter, createWebHistory, type RouteRecordRaw } from "vue-router";
import { useLoading } from "buefy";

const routes: RouteRecordRaw[] = [
    {
        path: "/user",
        component: () => import("@/Views/User.vue"),
        beforeEnter: async () => {
            const Loading = useLoading();
            const loading = Loading.open({});
            const userStore = useUserStore();
            const groupStore = useGroupStore();

            try {
                await userStore.GET_USERS();
                await userStore.GET_GROUPS();
                await groupStore.GET_GROUPS();

                return true;
            } catch {
                return false;
            } finally {
                loading.close();
            }
        }
    },
    {
        path: "/group",
        component: () => import("@/Views/Group.vue"),
        beforeEnter: async () => {
            const Loading = useLoading();
            const loading = Loading.open({});
            const groupStore = useGroupStore();

            try {
                await groupStore.GET_GROUPS();

                return true;
            } catch {
                return false;
            } finally {
                loading.close();
            }
        }
    }
];

const router = createRouter({
    history: createWebHistory(),
    routes
});

export default router;