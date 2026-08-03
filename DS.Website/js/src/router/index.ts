import { useGroupStore } from "@/Stores/GroupStore";
import { useUserStore } from "@/Stores/UserStore";
import { createRouter, createWebHistory, type RouteRecordRaw } from "vue-router";
import { useLoading } from "buefy";
import { useMeStore } from "@/Stores/MeStore";

const routes: RouteRecordRaw[] = [
    {
        path: "/",
        component: () => import("@/Views/Home.vue"),
        beforeEnter: async () => {
            const Loading = useLoading();
            const loading = Loading.open({});
            const meStore = useMeStore();

            try {
                await meStore.GET_ME();

                return true;
            } catch {
                return false;
            } finally {
                loading.close();
            }
        }
    },
    {
        path: "/user",
        component: () => import("@/Views/User.vue"),
        beforeEnter: async () => {
            const Loading = useLoading();
            const loading = Loading.open({});
            const userStore = useUserStore();
            const groupStore = useGroupStore();
            const meStore = useMeStore();

            try {
                await userStore.GET_USERS();
                await userStore.GET_GROUPS();
                await groupStore.GET_GROUPS();
                await meStore.GET_ME();

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
            const meStore = useMeStore();

            try {
                await groupStore.GET_GROUPS();
                await meStore.GET_ME();

                return true;
            } catch {
                return false;
            } finally {
                loading.close();
            }
        }
    },
    {
        path: "/invitation/:id",
        component: () => import("@/Views/Invitation.vue")
    }
];

const router = createRouter({
    history: createWebHistory(),
    routes
});

export default router;