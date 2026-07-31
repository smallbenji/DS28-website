import { useGroupStore } from "@/Stores/GroupStore";
import { useUserStore } from "@/Stores/UserStore";
import { createRouter, createWebHistory, type RouteRecordRaw } from "vue-router";
import { useLoading } from "buefy";

const routes: RouteRecordRaw[] = [

];

const router = createRouter({
    history: createWebHistory(),
    routes
});

export default router;