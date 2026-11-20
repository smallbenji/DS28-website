<template>
    <UDashboardGroup unit="rem">
        <UDashboardSidebar id="hq" collapsible resizable :ui="{ footer: 'border-t border-default' }">
            <template #header="{ collapsed }">
                <div class="flex items-center gap-2 px-1">
                    <UIcon name="i-lucide-tent-tree" class="size-5 shrink-0 text-primary" />
                    <span v-if="!collapsed" class="truncate font-semibold text-highlighted">DS28 HQ</span>
                    <UBadge v-if="!collapsed" label="Beta" color="primary" variant="subtle" size="sm" />
                </div>
            </template>

            <template #default="{ collapsed }">
                <UDashboardSearchButton :collapsed="collapsed" label="Søg" class="bg-transparent ring-default" />
                <UTooltip v-if="Me?.isImpersonating && collapsed" :text="`Viser som ${impersonationTitle}. Stop visning`">
                    <UButton color="warning" variant="subtle" icon="i-lucide-user-x" :loading="stopping" :aria-label="`Viser som ${impersonationTitle}. Stop visning`" @click="stopImpersonation" />
                </UTooltip>
                <UAlert
                    v-else-if="Me?.isImpersonating"
                    color="warning"
                    variant="subtle"
                    icon="i-lucide-user-cog"
                    :title="`Viser som ${impersonationTitle}`"
                    :actions="[{ label: 'Stop visning', color: 'warning', variant: 'outline', size: 'xs', loading: stopping, onClick: stopImpersonation }]"
                    :ui="{ root: 'p-2', title: 'text-xs' }"
                />
                <UNavigationMenu :collapsed="collapsed" :items="moduleItems" orientation="vertical" tooltip />
                <UNavigationMenu :collapsed="collapsed" :items="footerItems" orientation="vertical" tooltip class="mt-auto" />
            </template>

            <template #footer="{ collapsed }">
                <div class="flex w-full min-w-0 items-center gap-1" :class="{ 'flex-col': collapsed }">
                    <RouterLink to="/profile" class="-m-1 flex min-w-0 flex-1 items-center gap-2 rounded-md p-1 hover:bg-elevated focus-visible:outline-2 focus-visible:outline-primary" aria-label="Min profil">
                        <UAvatar :src="Me?.profilePicture?.url" :alt="Me?.name" size="sm" />
                        <div v-if="!collapsed" class="min-w-0">
                            <p class="truncate text-sm font-medium text-highlighted">{{ Me?.name }}</p>
                            <p class="truncate text-xs text-muted">{{ Me?.email }}</p>
                        </div>
                    </RouterLink>
                    <ColorModeMenu />
                </div>
            </template>
        </UDashboardSidebar>

        <RouterView />

        <GlobalSearch />
        <ImpersonationModal v-model:open="impersonationOpen" />
    </UDashboardGroup>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import { useRouter } from "vue-router";
import type { NavigationMenuItem } from "@nuxt/ui";
import { storeToRefs } from "pinia";
import { useMeStore } from "@/Stores/MeStore";
import ColorModeMenu from "@/Components/ColorModeMenu.vue";
import GlobalSearch from "@/Components/GlobalSearch.vue";
import ImpersonationModal from "@/Components/ImpersonationModal.vue";
import AuthService from "@/Services/AuthService";
import { useNotify } from "@/lib/notify";

const router = useRouter();
const meStore = useMeStore();
const { Me } = storeToRefs(meStore);

const moduleItems = computed<NavigationMenuItem[]>(() => {
    const items: NavigationMenuItem[] = [
        {
            label: "HQ",
            icon: "i-lucide-house",
            to: "/",
            exact: true
        },
        {
            label: "Aktiviteter",
            icon: "i-lucide-calendar-days",
            to: "/activity"
        }
    ];

    if (Me.value?.appRoles.includes("BoardView")) {
        items.push({
            label: "Tavler",
            icon: "i-lucide-columns-3",
            to: "/board"
        });
    }

    if (Me.value?.appRoles.includes("OrgView")) {
        items.push({
            label: "Organisation",
            icon: "i-lucide-network",
            to: "/org"
        });
    }

    if (Me.value?.appRoles.includes("AdminAccess")) {
        items.push({
            label: "Admin",
            icon: "i-lucide-settings",
            to: "/admin"
        });
    }

    return items;
});

const authService = new AuthService();
const notify = useNotify();

const impersonationOpen = ref(false);
const stopping = ref(false);
const impersonationTitle = computed(() => Me.value?.title || Me.value?.roles[0] || "");

async function stopImpersonation() {
    if (stopping.value) return;

    stopping.value = true;
    try {
        const ok = await meStore.STOP_IMPERSONATION();
        if (!ok) {
            notify.error("Visningen kunne ikke stoppes.");
            return;
        }

        await router.push("/");
    } finally {
        stopping.value = false;
    }
}

async function logout() {
    try {
        await authService.logout();
        window.location.assign("/beta/login");
    } catch {
        notify.error("Du kunne ikke logges ud. Prøv igen.");
    }
}

const footerItems = computed<NavigationMenuItem[]>(() => {
    const items: NavigationMenuItem[] = [];

    if (!Me.value?.isImpersonating && Me.value?.roles.includes("SysAdmin")) {
        items.push({
            label: "Vis som …",
            icon: "i-lucide-user-cog",
            onSelect: () => { impersonationOpen.value = true; }
        });
    }

    items.push(
        {
            label: "Til nuværende HQ",
            icon: "i-lucide-arrow-left-right",
            to: "/",
            external: true
        },
        {
            label: "Log ud",
            icon: "i-lucide-log-out",
            onSelect: () => { void logout(); }
        }
    );

    return items;
});
</script>
