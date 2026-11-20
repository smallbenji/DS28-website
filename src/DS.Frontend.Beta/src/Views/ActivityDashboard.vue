<template>
    <TeamBrowser
        v-if="!isActivityAdmin"
        title="Mine teams"
        :teams="Teams"
        :selected-id="SelectedTeamId"
        :loading="loading"
        empty-title="Du har ingen teams endnu"
        empty-description="Opret dit første team for at komme i gang med aktiviteter."
        @select="(team) => { SelectedTeamId = team.id; }"
        @clear="SelectedTeamId = null"
    />
    <UDashboardPanel v-else id="activity">
        <template #header>
            <UDashboardNavbar title="Aktivitetsoversigt">
                <template #leading>
                    <UDashboardSidebarCollapse />
                </template>
                <template #right>
                    <UButton to="/activity/teams" icon="i-lucide-users" color="neutral" variant="outline" label="Teamstyring" />
                </template>
            </UDashboardNavbar>
        </template>

        <template #body>
            <div v-if="loading" class="grid grid-cols-2 gap-4 xl:grid-cols-4">
                <USkeleton v-for="index in 4" :key="index" class="h-24" />
            </div>

            <UAlert v-else-if="loadFailed" color="error" variant="subtle" icon="i-lucide-circle-alert" title="Aktiviteterne kunne ikke hentes" description="Prøv at genindlæse siden." />

            <template v-else>
                <div class="grid grid-cols-2 gap-4 xl:grid-cols-4">
                    <button
                        v-for="option in countFilters"
                        :key="option.value"
                        type="button"
                        class="rounded-lg text-left focus-visible:outline-2 focus-visible:outline-primary"
                        :aria-pressed="statusFilter === option.value"
                        @click="statusFilter = option.value"
                    >
                        <UCard :class="statusFilter === option.value ? 'ring-2 ring-primary' : 'hover:bg-elevated/50'">
                            <div class="flex items-center gap-3">
                                <div class="rounded-md bg-primary/10 p-2">
                                    <UIcon :name="option.icon" class="size-5 text-primary" />
                                </div>
                                <div>
                                    <p class="text-xs uppercase text-muted">{{ option.label }}</p>
                                    <p class="text-2xl font-semibold text-highlighted">{{ countFor(option.value) }}</p>
                                </div>
                            </div>
                        </UCard>
                    </button>
                </div>

                <UCard :ui="{ body: 'p-0 sm:p-0' }">
                    <template #header>
                        <div class="flex flex-wrap items-end gap-3">
                            <UFormField label="Søg" class="min-w-56 flex-1">
                                <UInput v-model="search" icon="i-lucide-search" placeholder="Aktivitet eller team…" class="w-full" />
                            </UFormField>
                            <UFormField label="Team" class="w-48">
                                <USelect v-model="teamFilter" :items="teamItems" class="w-full" />
                            </UFormField>
                            <UFormField label="Status" class="w-48">
                                <USelect v-model="statusFilter" :items="statusItems" class="w-full" />
                            </UFormField>
                            <span class="ms-auto pb-2 text-sm text-muted">{{ filteredActivities.length }} af {{ ALL_ACTIVITIES.length }}</span>
                        </div>
                    </template>

                    <UTable v-model:sorting="sorting" :data="filteredActivities" :columns="columns">
                        <template #empty>
                            <div class="flex flex-col items-center gap-2 py-6">
                                <p class="text-muted">{{ ALL_ACTIVITIES.length ? 'Ingen aktiviteter matcher filtrene.' : 'Der er endnu ingen aktiviteter.' }}</p>
                                <UButton v-if="ALL_ACTIVITIES.length" size="sm" color="neutral" variant="outline" label="Nulstil filtre" @click="resetFilters" />
                            </div>
                        </template>
                    </UTable>
                </UCard>
            </template>
        </template>
    </UDashboardPanel>
</template>

<script setup lang="ts">
import { computed, h, onMounted, ref, resolveComponent } from "vue";
import { storeToRefs } from "pinia";
import type { SortingState } from "@tanstack/vue-table";
import type { TableColumn } from "@nuxt/ui";
import { useActivityStore } from "@/Stores/ActivityStore";
import { useMeStore } from "@/Stores/MeStore";
import { activityReviewStatus, activityReviewStatuses } from "@/lib/activityReview";
import { activityReviewBadgeColor } from "@/lib/activityReviewBadge";
import TeamBrowser from "@/Components/Activity/TeamBrowser.vue";
import type { ActivityReviewStatus, ActivityWithTeamDto } from "@/types";

type StatusFilter = ActivityReviewStatus | "all";

const UBadge = resolveComponent("UBadge");
const UButton = resolveComponent("UButton");
const ULink = resolveComponent("ULink");

const { Me } = storeToRefs(useMeStore());
const activityStore = useActivityStore();
const { ALL_ACTIVITIES, Teams, SelectedTeamId } = storeToRefs(activityStore);

const isActivityAdmin = computed(() => Me.value?.appRoles.includes("ActivityAdmin") ?? false);

const loading = ref(true);
const loadFailed = ref(false);

onMounted(async () => {
    try {
        await activityStore.GET_TEAMS();
    } catch {
        loadFailed.value = true;
    } finally {
        loading.value = false;
    }
});

const allTeams = "all";
const search = ref("");
const teamFilter = ref<string>(allTeams);
const statusFilter = ref<StatusFilter>("all");
const sorting = ref<SortingState>([{ id: "submittedAt", desc: true }]);

const currency = new Intl.NumberFormat("da-DK", { style: "currency", currency: "DKK", minimumFractionDigits: 0, maximumFractionDigits: 2 });
const dateFormat = new Intl.DateTimeFormat("da-DK", { dateStyle: "medium", timeZone: "Europe/Copenhagen" });

const countFilters: { value: StatusFilter; label: string; icon: string }[] = [
    { value: "Submitted", label: "Afventer godkendelse", icon: "i-lucide-inbox" },
    { value: "ChangesRequested", label: "Sendt retur", icon: "i-lucide-undo-2" },
    { value: "Approved", label: "Godkendte", icon: "i-lucide-circle-check" },
    { value: "all", label: "Alle aktiviteter", icon: "i-lucide-list" }
];

const teamItems = computed(() => [
    { label: "Alle teams", value: allTeams },
    ...[...Teams.value]
        .sort((a, b) => a.name.localeCompare(b.name, "da"))
        .map((team) => ({ label: team.name, value: String(team.id) }))
]);

const statusItems = [
    { label: "Alle statusser", value: "all" },
    ...activityReviewStatuses.map((status) => ({ label: status.label, value: status.value }))
];

const filteredActivities = computed<ActivityWithTeamDto[]>(() => {
    const query = search.value.trim().toLocaleLowerCase("da-DK");

    return Teams.value
        .filter((team) => teamFilter.value === allTeams || String(team.id) === teamFilter.value)
        .flatMap((team) => team.activities.map((activity) => ({ ...activity, teamName: team.name })))
        .filter((activity) => (statusFilter.value === "all" || activity.reviewStatus === statusFilter.value)
            && (!query || `${activity.name} ${activity.teamName}`.toLocaleLowerCase("da-DK").includes(query)));
});

function countFor(status: StatusFilter): number {
    if (status === "all") return ALL_ACTIVITIES.value.length;

    return ALL_ACTIVITIES.value.filter((activity) => activity.reviewStatus === status).length;
}

function resetFilters() {
    search.value = "";
    teamFilter.value = allTeams;
    statusFilter.value = "all";
}

function sortableHeader(label: string) {
    return ({ column }: { column: { getIsSorted: () => false | "asc" | "desc"; toggleSorting: (desc?: boolean) => void } }) => {
        const sorted = column.getIsSorted();
        const icon = sorted === "asc" ? "i-lucide-arrow-up-narrow-wide" : sorted === "desc" ? "i-lucide-arrow-down-wide-narrow" : "i-lucide-arrow-up-down";

        return h(UButton, {
            color: "neutral",
            variant: "ghost",
            label,
            icon,
            trailing: true,
            class: "-mx-2.5",
            onClick: () => column.toggleSorting(sorted === "asc")
        });
    };
}

const columns: TableColumn<ActivityWithTeamDto>[] = [
    {
        accessorKey: "name",
        header: sortableHeader("Aktivitet"),
        cell: ({ row }) => h(ULink, { to: `/activity/${row.original.id}`, class: "font-medium" }, () => row.original.name)
    },
    {
        accessorKey: "teamName",
        header: sortableHeader("Team")
    },
    {
        accessorKey: "reviewStatus",
        header: sortableHeader("Status"),
        cell: ({ row }) => h(UBadge, {
            color: activityReviewBadgeColor(row.original.reviewStatus),
            variant: "subtle",
            label: activityReviewStatus(row.original.reviewStatus).label
        })
    },
    {
        accessorKey: "budget",
        header: sortableHeader("Budget"),
        meta: { class: { th: "text-right", td: "text-right tabular-nums" } },
        cell: ({ row }) => currency.format(row.original.budget)
    },
    {
        accessorKey: "submittedAt",
        header: sortableHeader("Indsendt"),
        sortUndefined: "last",
        cell: ({ row }) => row.original.submittedAt ? dateFormat.format(new Date(row.original.submittedAt)) : "-"
    }
];
</script>
