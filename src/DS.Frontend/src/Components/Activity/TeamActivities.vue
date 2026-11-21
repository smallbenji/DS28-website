<template>
    <UCard :ui="{ body: 'p-0 sm:p-0' }">
        <template #header>
            <div class="flex items-center justify-between gap-2">
                <h3 class="font-semibold text-highlighted">Aktiviteter <span class="font-normal text-muted">({{ team.activities.length }})</span></h3>
                <UButton v-if="canManageActivities" size="sm" icon="i-lucide-plus" label="Opret aktivitet" @click="openCreate" />
            </div>
        </template>

        <p v-if="team.activities.length === 0" class="px-4 py-3 text-sm text-muted sm:px-6">Der er endnu ingen aktiviteter i dette team.</p>
        <ul v-else class="divide-y divide-default">
            <li v-for="activity in team.activities" :key="activity.id">
                <RouterLink :to="`/activity/${activity.id}`" class="flex items-center gap-3 px-4 py-2.5 text-sm hover:bg-elevated/50 focus-visible:outline-2 focus-visible:outline-primary sm:px-6">
                    <span class="font-mono text-xs text-muted">{{ activity.id }}</span>
                    <span class="min-w-0 flex-1 truncate font-medium text-highlighted">{{ activity.name }}</span>
                    <span class="text-xs text-muted tabular-nums">{{ currency.format(activity.budget) }}</span>
                    <UBadge :color="activityReviewBadgeColor(activity.reviewStatus)" variant="subtle" size="sm" :label="activityReviewStatus(activity.reviewStatus).label" />
                </RouterLink>
            </li>
        </ul>

        <UModal v-model:open="createOpen" title="Opret aktivitet">
            <template #body>
                <form id="activity-create-form" @submit.prevent="createActivity">
                    <UFormField label="Navn" required>
                        <UInput v-model="activityName" placeholder="Aktivitetens navn" class="w-full" autofocus />
                    </UFormField>
                </form>
            </template>
            <template #footer>
                <div class="flex w-full justify-end gap-2">
                    <UButton color="neutral" variant="outline" label="Annuller" @click="createOpen = false" />
                    <UButton type="submit" form="activity-create-form" label="Opret aktivitet" :loading="creating" :disabled="!activityName.trim()" />
                </div>
            </template>
        </UModal>
    </UCard>
</template>

<script setup lang="ts">
import { computed, ref } from "vue";
import { useActivityStore } from "@/Stores/ActivityStore";
import { useMeStore } from "@/Stores/MeStore";
import { useNotify } from "@/lib/notify";
import { activityReviewStatus } from "@/lib/activityReview";
import { activityReviewBadgeColor } from "@/lib/activityReviewBadge";
import type { ActivityTeamDto } from "@/types";

const props = defineProps<{ team: ActivityTeamDto }>();

const activityStore = useActivityStore();
const meStore = useMeStore();
const notify = useNotify();

const currency = new Intl.NumberFormat("da-DK", { style: "currency", currency: "DKK", minimumFractionDigits: 0, maximumFractionDigits: 2 });

const createOpen = ref(false);
const creating = ref(false);
const activityName = ref("");

const canManageActivities = computed(() => {
    if (meStore.Me?.appRoles.includes("ActivityAdmin")) return true;

    return props.team.members.some((member) => member.userId === meStore.Me?.id && member.isAdmin);
});

function openCreate() {
    activityName.value = "";
    createOpen.value = true;
}

async function createActivity() {
    if (!activityName.value.trim() || creating.value) return;

    creating.value = true;
    try {
        const ok = await activityStore.ADD_ACTIVITY(props.team.id, activityName.value.trim());
        notify.result(ok, "Aktiviteten er oprettet.", "Aktiviteten kunne ikke oprettes.");
        if (ok) createOpen.value = false;
    } finally {
        creating.value = false;
    }
}
</script>
