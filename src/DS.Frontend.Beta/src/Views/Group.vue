<template>
    <UDashboardPanel id="group">
        <template #header>
            <UDashboardNavbar title="Gruppe">
                <template #leading>
                    <UDashboardSidebarCollapse />
                </template>
            </UDashboardNavbar>
        </template>

        <template #body>
            <div v-if="loading" class="flex flex-col gap-4">
                <USkeleton class="h-16 w-72" />
                <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
                    <USkeleton v-for="index in 4" :key="index" class="h-20" />
                </div>
            </div>

            <UEmpty
                v-else-if="!hasGroup"
                icon="i-lucide-users-round"
                title="Du er ikke tilknyttet en gruppe"
                description="Kontakt lejrens ledelse, hvis du skal tilknyttes en spejdergruppe."
                class="h-full"
            />

            <div v-else class="grid gap-6 lg:grid-cols-[minmax(0,1fr)_20rem]">
                <div class="flex min-w-0 flex-col gap-6">
                    <div>
                        <h1 class="text-2xl font-semibold text-highlighted">{{ Group.name }}</h1>
                        <p class="text-sm text-muted">Gruppe-ID {{ Group.id }} · {{ districtLabel(Group.district) }}</p>
                    </div>

                    <div class="grid grid-cols-3 gap-4">
                        <UCard v-for="stat in stats" :key="stat.label" :ui="{ body: 'p-3 sm:p-4' }">
                            <p class="text-xs uppercase text-muted">{{ stat.label }}</p>
                            <p class="text-2xl font-semibold text-highlighted tabular-nums">{{ stat.value }}</p>
                        </UCard>
                    </div>

                    <section aria-labelledby="group-signup-heading">
                        <h2 id="group-signup-heading" class="mb-3 text-sm font-semibold text-highlighted">Tilmelding</h2>
                        <div class="grid grid-cols-1 gap-4 sm:grid-cols-2">
                            <UPageCard
                                v-for="link in links"
                                :key="link.to"
                                :title="link.title"
                                :icon="link.icon"
                                :to="link.to"
                                :aria-label="link.title"
                                variant="outline"
                                spotlight
                            />
                            <UPageCard
                                v-for="item in comingSoon"
                                :key="item.title"
                                :title="item.title"
                                description="Ikke tilgængelig endnu"
                                :icon="item.icon"
                                variant="subtle"
                                class="opacity-60"
                                aria-disabled="true"
                            />
                        </div>
                    </section>
                </div>

                <UCard class="self-start" :ui="{ body: 'p-0 sm:p-0' }">
                    <template #header>
                        <div class="flex items-center justify-between gap-2">
                            <h2 class="font-semibold text-highlighted">Brugere</h2>
                            <UButton :to="membersUrl" color="neutral" variant="outline" size="sm" label="Administrer" />
                        </div>
                    </template>
                    <p v-if="Group.users.length === 0" class="px-4 py-3 text-sm text-muted sm:px-6">Ingen brugere i gruppen endnu.</p>
                    <ul v-else class="divide-y divide-default">
                        <li v-for="user in Group.users" :key="user.id" class="flex items-center gap-3 px-4 py-2 text-sm sm:px-6">
                            <UserAvatar :user="user" size="sm" />
                            {{ user.firstName }} {{ user.lastName }}
                        </li>
                    </ul>
                </UCard>
            </div>
        </template>
    </UDashboardPanel>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { storeToRefs } from "pinia";
import { useGroupStore } from "@/Stores/GroupStore";
import UserAvatar from "@/Components/User/UserAvatar.vue";
import { districtLabel } from "@/types";

const groupStore = useGroupStore();
const { Group } = storeToRefs(groupStore);

const loading = ref(true);

onMounted(async () => {
    try {
        await groupStore.GET_GROUP();
    } finally {
        loading.value = false;
    }
});

const hasGroup = computed(() => !!Group.value.id && Group.value.id !== "0");

const stats = computed(() => [
    { label: "Spejdere", value: Group.value.scouts.length },
    { label: "Patruljer", value: Group.value.patrols.length },
    { label: "Brugere", value: Group.value.users.length }
]);

function legacyUrl(path: string) {
    return new URL(path, window.location.origin).href;
}

const membersUrl = legacyUrl("/group/members");

const links = [
    { title: "Forhåndstilmelding", icon: "i-lucide-clipboard-list", to: "/group/pre-signup" },
    { title: "Endelig tilmelding", icon: "i-lucide-clipboard-check", to: legacyUrl("/group/final-signup") }
];

const comingSoon = [
    { title: "Aktivitetsønskerunde", icon: "i-lucide-list-checks" },
    { title: "Aktivitetstilmelding", icon: "i-lucide-calendar-check" }
];
</script>
