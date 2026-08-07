import { useGroupsStore } from "@/Stores/GroupsStore";
import { useUserStore } from "@/Stores/UserStore";
import { createRouter, createWebHistory, type RouteRecordRaw } from "vue-router";
import { useLoading } from "buefy";
import { useMeStore } from "@/Stores/MeStore";
import { useGroupStore } from "@/Stores/GroupStore";

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
        path: "/profile",
        component: () => import("@/Views/Profile.vue")
    },
    {
        path: "/groups",
        component: () => import("@/Views/Groups.vue"),
        meta: { requiresGroupsData: true }
    },
    {
        path: "/invitation/:id",
        component: () => import("@/Views/Invitation.vue")
    },
    {
        path: "/reset-password/:id",
        component: () => import("@/Views/ResetPassword.vue")
    },
    {
        path: "/login",
        component: () => import("@/Views/Login.vue"),
        meta: { guestOnly: true }
    },
    {
        path: "/register",
        component: () => import("@/Views/Register.vue"),
        meta: { guestOnly: true }
    },
    {
        path: "/group",
        component: () => import("@/Views/Group.vue"),
        meta: { requiresGroupData: true }
    }
];

const router = createRouter({
    history: createWebHistory(),
    routes
});

router.beforeEach(async (to) => {
    const Loading = useLoading();
    // const loading = Loading.open({});
    let loadingInstance: any = null;
    let loadingTimeout: ReturnType<typeof setTimeout> | null = null;
    
    const meStore = useMeStore();
    const userStore = useUserStore();
    const groupsStore = useGroupsStore();
    const groupStore = useGroupStore();

    const promises = [];

    loadingTimeout = setTimeout(() => {
        loadingInstance = Loading.open({});
    }, 100);

    try {
        promises.push(meStore.GET_ME());

        if (to.meta.requiresUserData) {
            promises.push(userStore.GET_USERS());
            promises.push(userStore.GET_GROUPS());
            promises.push(groupsStore.GET_GROUPS());
        }

        if (to.meta.requiresHomeData) {
            promises.push(meStore.GET_HQ());
        }

        if (to.meta.requiresGroupsData && !to.meta.requiresUserData) {
            promises.push(groupsStore.GET_GROUPS());
        }

        if (to.meta.requiresGroupData) {
            promises.push(groupStore.GET_GROUP());
        }

        if (promises.length > 0) {
            await Promise.all(promises);
        }

        if (to.meta.guestOnly && meStore.ME.isAuthenticated) {
            return "/";
        }

        return true;
    } catch (error) {
        console.error("Navigation data prefetch failed:", error);
        return false;
    } finally {
        // loading.close();
        if (loadingTimeout) {
            clearTimeout(loadingTimeout);
        }
        if (loadingInstance) {
            loadingInstance.close();
        }
    }
});

export default router;