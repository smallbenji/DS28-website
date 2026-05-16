import Home from "@/Views/Home.vue";
import { createRouter, createWebHistory, type RouteRecordRaw } from "vue-router";

const routes: RouteRecordRaw[] = [
    {
        path: "/",
        component: Home
    }
];

const router = createRouter({
    history: createWebHistory(),
    routes
});

export default router;