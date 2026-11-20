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
                path: "group/pre-signup",
                component: () => import("@/Views/GroupPreSignupEdit.vue")
            },
            {
                path: "group/final-signup",
                component: () => import("@/Views/GroupFinalSignup.vue")
            },
            {
                path: "group/members",
                component: () => import("@/Views/GroupMembers.vue")
            },
            {
                path: "profile",
                component: () => import("@/Views/Profile.vue")
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
        path: "/",
        component: () => import("@/Layouts/GuestLayout.vue"),
        children: [
            { path: "login", component: () => import("@/Views/Login.vue"), meta: { guestOnly: true } },
            { path: "register", component: () => import("@/Views/Register.vue"), meta: { guestOnly: true } },
            { path: "forgot-password", component: () => import("@/Views/ForgotPassword.vue"), meta: { guestOnly: true } },
            { path: "reset-password/:id", component: () => import("@/Views/ResetPassword.vue"), meta: { public: true } },
            { path: "invitation/:id", component: () => import("@/Views/Invitation.vue"), meta: { public: true } },
            { path: "group-invitation/:id", component: () => import("@/Views/GroupInvitation.vue"), meta: { public: true } },
            { path: "group-pre-signup", component: () => import("@/Views/GroupPreSignup.vue"), meta: { guestOnly: true } },
            { path: "forms/:id?", component: () => import("@/Views/FormPreview.vue"), meta: { wide: true } }
        ]
    },
    {
        path: "/twofactor-setup",
        component: () => import("@/Views/TwoFactorSetup.vue")
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

    if (to.meta.public) return true;

    if (to.meta.guestOnly) return meStore.Me?.isAuthenticated ? "/" : true;

    if (!meStore.Me?.isAuthenticated) {
        return { path: "/login", query: { returnUrl: `/beta${to.fullPath}` } };
    }

    const mustSetupTwoFactor = meStore.Me.mustEnableTwoFactor;
    if (mustSetupTwoFactor && to.path !== "/twofactor-setup") return "/twofactor-setup";
    if (!mustSetupTwoFactor && to.path === "/twofactor-setup") return "/";

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
