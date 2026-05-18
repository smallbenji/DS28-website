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

            try {
                const userStore = useUserStore();
                await userStore.GET_USERS();
                return true;
            } catch {
                return false;
            } finally {
                loading.close();
            }
        }
    },
    {
        path: "/userv2",
        component: () => import("@/Views/UserV2.vue"),
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

            try {
                const groupStore = useGroupStore();
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