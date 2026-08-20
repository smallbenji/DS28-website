import { useGroupsStore } from "@/Stores/GroupsStore";
import { useUserStore } from "@/Stores/UserStore";
import { createRouter, createWebHistory, type RouteRecordRaw } from "vue-router";
import { useLoading } from "buefy";
import { useMeStore } from "@/Stores/MeStore";
import { useGroupStore } from "@/Stores/GroupStore";
import { useActivityStore } from "@/Stores/ActivityStore";

const routes: RouteRecordRaw[] = [
    {
        path: "/",
        component: () => import("@/Views/Home.vue"),
        meta: { requiresHomeData: true, pageTitle: "HQ", pageIcon: "house", hideBack: true }
    },
    {
        path: "/user/:id?",
        component: () => import("@/Views/User.vue"),
        meta: { requiresUserData: true, pageTitle: "Brugerstyring", pageIcon: "user-pen" }
    },
    {
        path: "/profile",
        component: () => import("@/Views/Profile.vue"),
        meta: { pageTitle: "Min profil", pageIcon: "user" }
    },
    {
        path: "/groups/:id?",
        component: () => import("@/Views/Groups.vue"),
        meta: { requiresGroupsData: true, pageTitle: "Gruppestyring", pageIcon: "users-gear" }
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
        path: "/twofactor-setup",
        component: () => import("@/Views/TwoFactorSetup.vue")
    },
    {
        path: "/group",
        component: () => import("@/Views/Group.vue"),
        meta: { requiresGroupData: true, pageTitle: "Gruppe", pageIcon: "users" }
    },
    {
        path: "/activity",
        component: () => import("@/Views/Activity.vue"),
        meta: { requiresActivityTeamData: true, pageTitle: "Aktivitetsmodul", pageIcon: "fa-newspaper" }
    },
    {
        path: "/activity/teams/:id?",
        component: () => import("@/Views/TeamManagement.vue"),
        meta: { requiresActivityTeamData: true, requiresActivityAdmin: true, pageTitle: "Teamstyring", pageIcon: "users-gear" }
    },
    {
        path: "/activity/:id",
        component: () => import("@/Views/ActivityDetail.vue"),
        meta: { requiresActivityData: true, pageTitle: "Aktivitetsmodul", pageIcon: "fa-newspaper" }
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
    const activityStore = useActivityStore();

    const promises = [];

    loadingTimeout = setTimeout(() => {
        loadingInstance = Loading.open({});
    }, 100);

    try {
        promises.push(meStore.GET_ME());

        if (to.meta.requiresUserData) {
            promises.push(userStore.GET_USERS());
            promises.push(userStore.GET_GROUPS());
            promises.push(userStore.GET_ASSIGNABLE_GROUPS());
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

        if (to.meta.requiresActivityTeamData) {
            promises.push(activityStore.GET_TEAMS());
        }

        if (to.meta.requiresActivityData) {
            const id = Array.isArray(to.params.id) ? to.params.id[0] : to.params.id;
            if (id) {
                promises.push(activityStore.GET_ACTIVITY(Number(id)));
            }
        }

        if (to.meta.requiresActivityAdmin && !meStore.ME.appRoles.includes("ActivityAdmin")) {
            return "/activity";
        }

        if (promises.length > 0) {
            await Promise.all(promises);
        }

        if (to.meta.guestOnly && meStore.ME.isAuthenticated) {
            return "/";
        }

        const mustSetupTwoFactor = meStore.ME.mustEnableTwoFactor;
        if (mustSetupTwoFactor && to.path !== "/twofactor-setup") {
            return "/twofactor-setup";
        }
        if (!mustSetupTwoFactor && to.path === "/twofactor-setup") {
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