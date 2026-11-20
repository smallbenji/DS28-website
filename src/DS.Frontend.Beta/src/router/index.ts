import { createRouter, createWebHistory, type RouteRecordRaw } from "vue-router";
import { useMeStore } from "@/Stores/MeStore";

const routes: RouteRecordRaw[] = [
    {
        path: "/",
        component: () => import("@/Layouts/DashboardLayout.vue"),
        children: [
            {
                path: "",
                component: () => import("@/Views/Home.vue")
            },
            {
                path: "activity",
                component: () => import("@/Views/ActivityDashboard.vue")
            }
        ]
    },
    {
        path: "/access-denied",
        component: () => import("@/Views/AccessDenied.vue")
    },
    {
        path: "/:pathMatch(.*)*",
        redirect: "/"
    }
];

const router = createRouter({
    history: createWebHistory("/beta/"),
    routes
});

router.beforeEach(async (to) => {
    const meStore = useMeStore();

    try {
        await meStore.GET_ME();
    } catch (error) {
        console.error("Navigation data prefetch failed:", error);
        return false;
    }

    if (!meStore.Me?.isAuthenticated) {
        window.location.assign(`/login?returnUrl=${encodeURIComponent(`/beta${to.fullPath}`)}`);
        return false;
    }

    if (meStore.Me.mustEnableTwoFactor) {
        window.location.assign("/twofactor-setup");
        return false;
    }

    if (typeof to.meta.requiredRole === "string" && !meStore.Me.appRoles.includes(to.meta.requiredRole)) {
        return "/access-denied";
    }

    return true;
});

export default router;
