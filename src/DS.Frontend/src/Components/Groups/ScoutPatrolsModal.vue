<template>
    <UModal v-model:open="open" :title="`Patruljer for ${scout?.name ?? ''}`" description="Klik på kronen for at gøre spejderen til patruljeleder eller fjerne det.">
        <template #body>
            <p v-if="!scout || scout.memberships.length === 0" class="text-sm text-muted">Spejderen er ikke i nogen patrulje.</p>
            <ul v-else class="flex flex-col gap-2">
                <li v-for="membership in scout.memberships" :key="membership.patrolId" class="flex items-center justify-between gap-2 rounded-md border border-default px-3 py-2">
                    <span class="flex items-center gap-2 text-sm font-medium">
                        {{ patrolName(membership.patrolId) }}
                        <UBadge v-if="membership.isPatrolLeader" label="Patruljeleder" color="warning" variant="subtle" size="sm" />
                    </span>
                    <span class="flex gap-1">
                        <UButton
                            :color="membership.isPatrolLeader ? 'warning' : 'neutral'"
                            variant="ghost"
                            size="sm"
                            icon="i-lucide-crown"
                            :aria-label="membership.isPatrolLeader ? 'Fjern som patruljeleder' : 'Gør til patruljeleder'"
                            :aria-pressed="membership.isPatrolLeader"
                            :loading="busyPatrolId === membership.patrolId"
                            @click="toggleLeader(membership.patrolId)"
                        />
                        <UButton color="error" variant="ghost" size="sm" label="Fjern" :disabled="busyPatrolId != null" @click="remove(membership.patrolId)" />
                    </span>
                </li>
            </ul>

            <UFormField v-if="availablePatrols.length > 0" label="Tilføj til patrulje" class="mt-4">
                <UFieldGroup class="w-full">
                    <USelect v-model="patrolToAdd" :items="availablePatrols" placeholder="Vælg patrulje…" class="w-full" />
                    <UButton label="Tilføj" :disabled="patrolToAdd == null" :loading="adding" @click="add" />
                </UFieldGroup>
            </UFormField>
        </template>
        <template #footer>
            <div class="flex w-full justify-end">
                <UButton color="neutral" variant="outline" label="Luk" @click="open = false" />
            </div>
        </template>
    </UModal>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue";
import GroupsService from "@/Services/GroupsService";
import { useGroupsStore } from "@/Stores/GroupsStore";
import { useNotify } from "@/lib/notify";
import type { GroupDto, ScoutDto } from "@/types";

const props = defineProps<{ group: GroupDto; scout: ScoutDto | null }>();
const open = defineModel<boolean>("open", { required: true });

const groupsService = new GroupsService();
const groupsStore = useGroupsStore();
const notify = useNotify();

const patrolToAdd = ref<number | undefined>();
const adding = ref(false);
const busyPatrolId = ref<number | null>(null);

watch(open, (isOpen) => {
    if (isOpen) patrolToAdd.value = undefined;
});

const availablePatrols = computed(() => {
    const memberOf = new Set(props.scout?.memberships.map((membership) => membership.patrolId) ?? []);

    return props.group.patrols
        .filter((patrol) => !memberOf.has(patrol.id))
        .map((patrol) => ({ label: patrol.name, value: patrol.id }));
});

function patrolName(patrolId: number): string {
    return props.group.patrols.find((patrol) => patrol.id === patrolId)?.name ?? "Ukendt patrulje";
}

async function add() {
    if (!props.scout || patrolToAdd.value == null) return;

    adding.value = true;
    try {
        const ok = await groupsService.addPatrol(props.scout.id, patrolToAdd.value);
        notify.result(ok, `${props.scout.name} er tilføjet til patruljen.`, "Spejderen kunne ikke tilføjes til patruljen.");
        if (ok) {
            patrolToAdd.value = undefined;
            await groupsStore.GET_GROUPS();
        }
    } finally {
        adding.value = false;
    }
}

async function remove(patrolId: number) {
    if (!props.scout) return;

    busyPatrolId.value = patrolId;
    try {
        const ok = await groupsService.removePatrol(props.scout.id, patrolId);
        notify.result(ok, `${props.scout.name} er fjernet fra patruljen.`, "Spejderen kunne ikke fjernes fra patruljen.");
        if (ok) await groupsStore.GET_GROUPS();
    } finally {
        busyPatrolId.value = null;
    }
}

async function toggleLeader(patrolId: number) {
    if (!props.scout) return;

    busyPatrolId.value = patrolId;
    try {
        const ok = await groupsService.toggleLeader(props.scout.id, patrolId);
        notify.result(ok, "Patruljelederen er opdateret.", "Patruljelederen kunne ikke opdateres.");
        if (ok) await groupsStore.GET_GROUPS();
    } finally {
        busyPatrolId.value = null;
    }
}
</script>
