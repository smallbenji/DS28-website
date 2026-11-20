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
            },
            {
                path: "admin",
                component: () => import("@/Views/Admin.vue"),
                meta: { requiredRole: "AdminAccess" }
            },
            {
                path: "admin/user/:id?",
                component: () => import("@/Views/UserManagement.vue")
            },
            {
                path: "admin/groups/:id?",
                component: () => import("@/Views/GroupManagement.vue")
            },
            {
                path: "admin/camp-settings",
                component: () => import("@/Views/CampSettings.vue"),
                meta: { requiredAnyRole: ["PreSignupManage", "AdminAccess"] }
            },
            {
                path: "admin/google",
                redirect: (to) => ({ path: "/admin/camp-settings", query: to.query })
            },
            {
                path: "admin/exports",
                component: () => import("@/Views/Exports.vue"),
                meta: { requiredRole: "ExportsView" }
            },
            {
                path: "admin/openiddict",
                component: () => import("@/Views/OpenIddictClients.vue"),
                meta: { requiredRole: "OpenIddictManage" }
            },
            {
                path: "org",
                component: () => import("@/Views/OrgChart.vue"),
                meta: { requiredRole: "OrgView" }
            },
            {
                path: "group",
                component: () => import("@/Views/Group.vue")
            },
            {
                path: "board",
                component: () => import("@/Views/Boards.vue"),
                meta: { requiredRole: "BoardView" }
            },
            {
                path: "board/:id",
                component: () => import("@/Views/BoardDetail.vue"),
                meta: { requiredRole: "BoardView" }
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

    const requiredAnyRole = to.meta.requiredAnyRole;
    if (Array.isArray(requiredAnyRole) && !requiredAnyRole.some((role) => meStore.Me!.appRoles.includes(role))) {
        return "/access-denied";
    }

    return true;
});

export default router;
