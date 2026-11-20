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
                <UNavigationMenu :collapsed="collapsed" :items="moduleItems" orientation="vertical" tooltip />
                <UNavigationMenu :collapsed="collapsed" :items="footerItems" orientation="vertical" tooltip class="mt-auto" />
            </template>

            <template #footer="{ collapsed }">
                <RouterLink to="/profile" class="-m-1 flex min-w-0 flex-1 items-center gap-2 rounded-md p-1 hover:bg-elevated focus-visible:outline-2 focus-visible:outline-primary" aria-label="Min profil">
                    <UAvatar :src="Me?.profilePicture?.url" :alt="Me?.name" size="sm" />
                    <div v-if="!collapsed" class="min-w-0">
                        <p class="truncate text-sm font-medium text-highlighted">{{ Me?.name }}</p>
                        <p class="truncate text-xs text-muted">{{ Me?.email }}</p>
                    </div>
                </RouterLink>
            </template>
        </UDashboardSidebar>

        <RouterView />
    </UDashboardGroup>
</template>

<script setup lang="ts">
import { computed } from "vue";
import type { NavigationMenuItem } from "@nuxt/ui";
import { storeToRefs } from "pinia";
import { useMeStore } from "@/Stores/MeStore";

const { Me } = storeToRefs(useMeStore());

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

const footerItems: NavigationMenuItem[] = [
    {
        label: "Til nuværende HQ",
        icon: "i-lucide-arrow-left-right",
        to: "/",
        external: true
    }
];
</script>
