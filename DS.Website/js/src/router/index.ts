import { useGroupsStore } from "@/Stores/GroupsStore";
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
        component: () => import("@/Views/Groups.vue"),
        meta: { requiresGroupData: true }
    },
    {
        path: "/invitation/:id",
        component: () => import("@/Views/Invitation.vue")
    },
    {
        path: "/group",
        component: () => import("@/Views/Group.vue")
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
    const groupStore = useGroupsStore();

    const promises = [];

    try {
        promises.push(meStore.GET_ME());

        if (to.meta.requiresUserData) {
            promises.push(userStore.GET_USERS());
            promises.push(userStore.GET_GROUPS());
            promises.push(groupStore.GET_GROUPS());
        }

        if (to.meta.requiresHomeData) {
            promises.push(meStore.GET_HQ());
        }

        if (to.meta.requiresGroupData && !to.meta.requiresUserData) {
            promises.push(groupStore.GET_GROUPS());
        }

        if (promises.length > 0) {
            await Promise.all(promises);
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