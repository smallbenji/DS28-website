import { useMeStore } from "@/Stores/MeStore";
import { createRouter, createWebHistory, type RouteRecordRaw } from "vue-router";
// import { useLoading } from "buefy";

const routes: RouteRecordRaw[] = [
    {
        path: "/",
        component: () => import('@/Views/Home.vue'),
        beforeEnter: async () => {
            const meStore = useMeStore();
            await meStore.GET_ME();
        }
    },
    {
        path: "/activity",
        component: () => import('@/Views/Activity.vue'),
    },
    {
        path: "/teams",
        component: () => import('@/Views/Teams.vue'),
    }
];

const router = createRouter({
    history: createWebHistory(),
    routes
});

export default router;