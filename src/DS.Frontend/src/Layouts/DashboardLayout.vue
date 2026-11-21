<template>
    <UDashboardGroup unit="rem">
        <UDashboardSidebar id="hq" collapsible resizable :ui="{ footer: 'border-t border-default' }">
            <template #header="{ collapsed }">
                <div class="flex items-center gap-2 px-1">
                    <UIcon name="i-lucide-tent-tree" class="size-5 shrink-0 text-primary" />
                    <span v-if="!collapsed" class="truncate font-semibold text-highlighted">DS28 HQ</span>
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
            </template>

            <template #footer="{ collapsed }">
                <div class="flex w-full min-w-0 items-center gap-1" :class="{ 'flex-col': collapsed }">
                    <UDropdownMenu :items="userMenu" :content="{ align: 'start', side: 'top', sideOffset: 8 }" :ui="{ content: 'w-(--reka-dropdown-menu-trigger-width) min-w-56' }" class="min-w-0 flex-1">
                        <button
                            type="button"
                            class="-m-1 flex w-full min-w-0 items-center gap-2 rounded-md p-1 text-left hover:bg-elevated focus-visible:outline-2 focus-visible:outline-primary data-[state=open]:bg-elevated"
                            :aria-label="`Brugermenu for ${Me?.name ?? ''}`"
                        >
                            <UAvatar :src="Me?.profilePicture?.url" :alt="Me?.name" size="sm" />
                            <span v-if="!collapsed" class="min-w-0 flex-1">
                                <span class="block truncate text-sm font-medium text-highlighted">{{ Me?.name }}</span>
                                <span class="block truncate text-xs text-muted">{{ Me?.email }}</span>
                            </span>
                            <UIcon v-if="!collapsed" name="i-lucide-chevrons-up-down" class="size-4 shrink-0 text-muted" />
                        </button>
                    </UDropdownMenu>
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
import type { DropdownMenuItem, NavigationMenuItem } from "@nuxt/ui";
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
        window.location.assign("/login");
    } catch {
        notify.error("Du kunne ikke logges ud. Prøv igen.");
    }
}

const userMenu = computed<DropdownMenuItem[][]>(() => {
    const account: DropdownMenuItem[] = [
        { label: "Min profil", icon: "i-lucide-user", to: "/profile" }
    ];

    if (Me.value?.isImpersonating) {
        account.push({ label: `Stop visning som ${impersonationTitle.value}`, icon: "i-lucide-user-x", color: "warning", onSelect: () => { void stopImpersonation(); } });
    } else if (Me.value?.roles.includes("SysAdmin")) {
        account.push({ label: "Vis som …", icon: "i-lucide-user-cog", onSelect: () => { impersonationOpen.value = true; } });
    }

    return [
        [{ type: "label", label: Me.value?.name ?? "", avatar: { src: Me.value?.profilePicture?.url, alt: Me.value?.name } }],
        account,
        [{ label: "Log ud", icon: "i-lucide-log-out", color: "error", onSelect: () => { void logout(); } }]
    ];
});
</script>
