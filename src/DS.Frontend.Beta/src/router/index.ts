import { createRouter, createWebHistory, type RouteRecordRaw } from "vue-router";
import { useMeStore } from "@/Stores/MeStore";

const routes: RouteRecordRaw[] = [
    {
        path: "/",
        component: () => import("@/Layouts/DashboardLayout.vue"),
        children: [
            {
                path: "",
                component: () => import("@/Views/Home.vue"),
                meta: { pageTitle: "HQ" }
            },
            {
                path: "activity",
                component: () => import("@/Views/ActivityDashboard.vue"),
                meta: { pageTitle: "Aktivitetsmodul" }
            },
            {
                path: "activity/teams/:id?",
                component: () => import("@/Views/TeamManagement.vue"),
                meta: { pageTitle: "Teamstyring", requiredRole: "ActivityAdmin" }
            },
            {
                path: "activity/:id",
                component: () => import("@/Views/ActivityDetail.vue"),
                meta: { pageTitle: "Aktivitetsmodul" }
            },
            {
                path: "admin",
                component: () => import("@/Views/Admin.vue"),
                meta: { pageTitle: "Admin", requiredRole: "AdminAccess" }
            },
            {
                path: "admin/user/:id?",
                component: () => import("@/Views/UserManagement.vue"),
                meta: { pageTitle: "Brugerstyring" }
            },
            {
                path: "admin/groups/:id?",
                component: () => import("@/Views/GroupManagement.vue"),
                meta: { pageTitle: "Gruppestyring" }
            },
            {
                path: "admin/camp-settings",
                component: () => import("@/Views/CampSettings.vue"),
                meta: { pageTitle: "Lejrindstillinger", requiredAnyRole: ["PreSignupManage", "AdminAccess"] }
            },
            {
                path: "admin/google",
                redirect: (to) => ({ path: "/admin/camp-settings", query: to.query })
            },
            {
                path: "admin/exports",
                component: () => import("@/Views/Exports.vue"),
                meta: { pageTitle: "Dataudtræk", requiredRole: "ExportsView" }
            },
            {
                path: "admin/openiddict",
                component: () => import("@/Views/OpenIddictClients.vue"),
                meta: { pageTitle: "OpenIddict-klienter", requiredRole: "OpenIddictManage" }
            },
            {
                path: "org",
                component: () => import("@/Views/OrgChart.vue"),
                meta: { pageTitle: "Organisation", requiredRole: "OrgView" }
            },
            {
                path: "group",
                component: () => import("@/Views/Group.vue"),
                meta: { pageTitle: "Gruppe" }
            },
            {
                path: "group/pre-signup",
                component: () => import("@/Views/GroupPreSignupEdit.vue"),
                meta: { pageTitle: "Forhåndstilmelding" }
            },
            {
                path: "group/final-signup",
                component: () => import("@/Views/GroupFinalSignup.vue"),
                meta: { pageTitle: "Endelig tilmelding" }
            },
            {
                path: "group/members",
                component: () => import("@/Views/GroupMembers.vue"),
                meta: { pageTitle: "Gruppens brugere" }
            },
            {
                path: "profile",
                component: () => import("@/Views/Profile.vue"),
                meta: { pageTitle: "Min profil" }
            },
            {
                path: "board",
                component: () => import("@/Views/Boards.vue"),
                meta: { pageTitle: "Tavler", requiredRole: "BoardView" }
            },
            {
                path: "board/:id",
                component: () => import("@/Views/BoardDetail.vue"),
                meta: { pageTitle: "Tavle", requiredRole: "BoardView" }
            }
        ]
    },
    {
        path: "/",
        component: () => import("@/Layouts/GuestLayout.vue"),
        children: [
            { path: "login", component: () => import("@/Views/Login.vue"), meta: { pageTitle: "Log ind", guestOnly: true } },
            { path: "register", component: () => import("@/Views/Register.vue"), meta: { pageTitle: "Opret bruger", guestOnly: true } },
            { path: "forgot-password", component: () => import("@/Views/ForgotPassword.vue"), meta: { pageTitle: "Glemt adgangskode", guestOnly: true } },
            { path: "reset-password/:id", component: () => import("@/Views/ResetPassword.vue"), meta: { pageTitle: "Nulstil adgangskode", public: true } },
            { path: "invitation/:id", component: () => import("@/Views/Invitation.vue"), meta: { pageTitle: "Invitation", public: true } },
            { path: "group-invitation/:id", component: () => import("@/Views/GroupInvitation.vue"), meta: { pageTitle: "Gruppeinvitation", public: true } },
            { path: "group-pre-signup", component: () => import("@/Views/GroupPreSignup.vue"), meta: { pageTitle: "Forhåndstilmelding", guestOnly: true } },
            { path: "forms/:id?", component: () => import("@/Views/FormPreview.vue"), meta: { pageTitle: "Formularvisning", wide: true } }
        ]
    },
    {
        path: "/twofactor-setup",
        component: () => import("@/Views/TwoFactorSetup.vue"),
        meta: { pageTitle: "Totrinsbekræftelse" }
    },
    {
        path: "/access-denied",
        component: () => import("@/Views/AccessDenied.vue"),
        meta: { pageTitle: "Adgang nægtet" }
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

router.afterEach((to, _from, failure) => {
    if (failure) return;

    document.title = typeof to.meta.pageTitle === "string" ? `${to.meta.pageTitle} · DS28 HQ (beta)` : "DS28 HQ (beta)";
});

export default router;
