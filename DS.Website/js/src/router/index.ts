import { useGroupStore } from "@/Stores/GroupStore";
import { useUserStore } from "@/Stores/UserStore";
import { createRouter, createWebHistory, type RouteRecordRaw } from "vue-router";
import { useLoading } from "buefy";
import { useMeStore } from "@/Stores/MeStore";

const routes: RouteRecordRaw[] = [
    {
        path: "/",
        component: () => import("@/Views/Home.vue"),
        meta: { requiresHomeData: true }
    },
    {
        path: "/user",
        component: () => import("@/Views/User.vue"),
        meta: { requiresUserData: true }
    },
    {
        path: "/groups",
        component: () => import("@/Views/Group.vue"),
        meta: { requiresGroupData: true }
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

router.beforeEach(async (to) => {
    const Loading = useLoading();
    const loading = Loading.open({});
    
    const meStore = useMeStore();
    const userStore = useUserStore();
    const groupStore = useGroupStore();

    try {
        await meStore.GET_ME();

        if (to.meta.requiresUserData) {
            await userStore.GET_USERS();
            await userStore.GET_GROUPS();
            await groupStore.GET_GROUPS();
        }

        if (to.meta.requiresHomeData) {
            await meStore.GET_HQ();
        }

        if (to.meta.requiresGroupData) {
            await groupStore.GET_GROUPS();
        }

        return true;
    } catch (error) {
        console.error("Navigation data prefetch failed:", error);
        return false;
    } finally {
        loading.close();
    }
});

export default router;