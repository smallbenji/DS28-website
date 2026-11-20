<template>
    <UDashboardPanel id="activity-teams-list" :default-size="22" :min-size="18" :max-size="32" resizable>
        <template #header>
            <UDashboardNavbar :title="title">
                <template #leading>
                    <UDashboardSidebarCollapse />
                    <UButton v-if="backTo" :to="backTo" color="neutral" variant="ghost" icon="i-lucide-arrow-left" aria-label="Tilbage" />
                </template>
                <template #trailing>
                    <UBadge :label="String(teams.length)" color="neutral" variant="subtle" />
                </template>
            </UDashboardNavbar>
            <UDashboardToolbar>
                <UInput ref="searchInput" v-model="search" icon="i-lucide-search" placeholder="Søg efter team…" class="w-full" variant="soft">
                    <template #trailing>
                        <UKbd value="meta" class="hidden sm:inline-flex" />
                        <UKbd value="K" class="hidden sm:inline-flex" />
                    </template>
                </UInput>
            </UDashboardToolbar>
        </template>

        <template #body>
            <div v-if="loading" class="flex flex-col gap-2">
                <USkeleton v-for="index in 6" :key="index" class="h-14" />
            </div>

            <p v-else-if="filteredTeams.length === 0" class="py-6 text-center text-sm text-muted">
                {{ teams.length === 0 ? emptyTitle : 'Ingen teams matcher søgningen.' }}
            </p>

            <ul v-else class="-mx-2 flex flex-col gap-1">
                <li v-for="team in filteredTeams" :key="team.id">
                    <button
                        type="button"
                        class="flex w-full flex-col gap-0.5 rounded-md px-2 py-2 text-left transition-colors hover:bg-elevated/60 focus-visible:outline-2 focus-visible:outline-primary"
                        :class="{ 'bg-elevated': selectedTeam?.id === team.id }"
                        :aria-current="selectedTeam?.id === team.id ? 'true' : undefined"
                        @click="toggle(team)"
                    >
                        <span class="truncate text-sm font-medium text-highlighted">{{ team.name }}</span>
                        <span class="flex gap-3 text-xs text-muted">
                            <span class="flex items-center gap-1"><UIcon name="i-lucide-users" class="size-3.5" />{{ team.members.length }} medlemmer</span>
                            <span class="flex items-center gap-1"><UIcon name="i-lucide-newspaper" class="size-3.5" />{{ team.activities.length }} aktiviteter</span>
                        </span>
                    </button>
                </li>
            </ul>
        </template>

        <template #footer>
            <div class="border-t border-default p-4">
                <UButton icon="i-lucide-plus" label="Opret team" block @click="createOpen = true" />
            </div>
        </template>
    </UDashboardPanel>

    <UDashboardPanel v-if="!isMobile" id="activity-team-detail">
        <template #header>
            <UDashboardNavbar :title="selectedTeam?.name ?? 'Team'">
                <template v-if="selectedTeam" #right>
                    <UButton color="neutral" variant="ghost" icon="i-lucide-x" aria-label="Luk team" @click="emit('clear')" />
                </template>
            </UDashboardNavbar>
        </template>
        <template #body>
            <TeamWorkspace v-if="selectedTeam" :team="selectedTeam" />
            <UEmpty
                v-else-if="!loading && teams.length === 0"
                icon="i-lucide-users"
                :title="emptyTitle"
                :description="emptyDescription"
                :actions="[{ label: 'Opret team', icon: 'i-lucide-plus', onClick: () => { createOpen = true; } }]"
                class="h-full"
            />
            <UEmpty v-else icon="i-lucide-users" title="Vælg et team" description="Vælg et team på listen for at se medlemmer og aktiviteter." class="h-full" />
        </template>
    </UDashboardPanel>

    <USlideover v-else v-model:open="detailOpen" :title="selectedTeam?.name ?? 'Team'">
        <template #body>
            <TeamWorkspace v-if="selectedTeam" :team="selectedTeam" />
        </template>
    </USlideover>

    <TeamCreateModal v-model:open="createOpen" />
</template>

<script setup lang="ts">
import { computed, onMounted, onUnmounted, ref } from "vue";
import { useIsMobile } from "@/lib/useIsMobile";
import TeamWorkspace from "@/Components/Activity/TeamWorkspace.vue";
import TeamCreateModal from "@/Components/Activity/TeamCreateModal.vue";
import type { ActivityTeamDto } from "@/types";

const props = withDefaults(defineProps<{
    title: string;
    teams: ActivityTeamDto[];
    selectedId: number | null;
    loading?: boolean;
    emptyTitle: string;
    emptyDescription: string;
    backTo?: string;
}>(), {
    loading: false,
    backTo: undefined
});

const emit = defineEmits<{ (e: "select", team: ActivityTeamDto): void; (e: "clear"): void }>();

const isMobile = useIsMobile();
const search = ref("");
const createOpen = ref(false);

const selectedTeam = computed(() => props.teams.find((team) => team.id === props.selectedId) ?? null);

const detailOpen = computed({
    get: () => selectedTeam.value != null,
    set: (open: boolean) => {
        if (!open) emit("clear");
    }
});

const filteredTeams = computed(() => {
    const query = search.value.trim().toLocaleLowerCase("da-DK");
    if (!query) return props.teams;

    return props.teams.filter((team) => team.name.toLocaleLowerCase("da-DK").includes(query));
});

function toggle(team: ActivityTeamDto) {
    if (selectedTeam.value?.id === team.id) emit("clear");
    else emit("select", team);
}

const searchInput = ref<{ inputRef?: HTMLInputElement } | null>(null);

function handleGlobalKeyDown(event: KeyboardEvent) {
    if ((event.ctrlKey || event.metaKey) && event.key.toLowerCase() === "k") {
        event.preventDefault();
        searchInput.value?.inputRef?.focus();
    }
}

onMounted(() => window.addEventListener("keydown", handleGlobalKeyDown));
onUnmounted(() => window.removeEventListener("keydown", handleGlobalKeyDown));
</script>
