<template>
    <UDashboardPanel id="groups-list" :default-size="22" :min-size="18" :max-size="32" resizable>
        <template #header>
            <UDashboardNavbar title="Gruppestyring">
                <template #leading>
                    <UDashboardSidebarCollapse />
                    <UButton to="/admin" color="neutral" variant="ghost" icon="i-lucide-arrow-left" aria-label="Tilbage til admin" />
                </template>
                <template #trailing>
                    <UBadge :label="String(groups.length)" color="neutral" variant="subtle" />
                </template>
            </UDashboardNavbar>
            <UDashboardToolbar>
                <UInput v-model="search" icon="i-lucide-search" placeholder="Søg på navn eller ID" class="w-full" variant="soft" />
            </UDashboardToolbar>
        </template>

        <template #body>
            <div v-if="loading" class="flex flex-col gap-2">
                <USkeleton v-for="index in 8" :key="index" class="h-14" />
            </div>

            <UAlert v-else-if="loadFailed" color="error" variant="subtle" icon="i-lucide-circle-alert" title="Grupperne kunne ikke hentes" description="Prøv at genindlæse siden." />

            <p v-else-if="filteredGroups.length === 0" class="py-6 text-center text-sm text-muted">Ingen grupper matcher søgningen.</p>

            <ul v-else class="-mx-2 flex flex-col gap-1">
                <li v-for="group in filteredGroups" :key="group.id">
                    <button
                        type="button"
                        class="flex w-full items-center gap-3 rounded-md px-2 py-2 text-left transition-colors hover:bg-elevated/60 focus-visible:outline-2 focus-visible:outline-primary"
                        :class="{ 'bg-elevated': selectedGroup?.id === group.id }"
                        :aria-current="selectedGroup?.id === group.id ? 'true' : undefined"
                        @click="select(group)"
                    >
                        <UAvatar :text="group.id" size="md" />
                        <span class="min-w-0 flex-1">
                            <span class="block truncate text-sm font-medium text-highlighted">{{ group.name }}</span>
                            <span class="block truncate text-xs text-muted">{{ districtLabel(group.district) }} · {{ group.scouts.length }} spejdere · {{ group.patrols.length }} patruljer</span>
                        </span>
                    </button>
                </li>
            </ul>
        </template>

        <template #footer>
            <div class="border-t border-default p-4">
                <UButton icon="i-lucide-plus" label="Ny gruppe" block @click="createOpen = true" />
            </div>
        </template>
    </UDashboardPanel>

    <UDashboardPanel v-if="!isMobile" id="group-detail">
        <template #header>
            <UDashboardNavbar :title="selectedGroup?.name ?? 'Gruppe'">
                <template v-if="selectedGroup" #right>
                    <UButton color="neutral" variant="ghost" icon="i-lucide-x" aria-label="Luk gruppe" @click="clearSelection" />
                </template>
            </UDashboardNavbar>
        </template>
        <template #body>
            <GroupDetail v-if="selectedGroup" :group="selectedGroup" />
            <UEmpty v-else icon="i-lucide-users-round" title="Vælg en gruppe" description="Vælg en gruppe på listen for at se og redigere den." class="h-full" />
        </template>
    </UDashboardPanel>

    <USlideover v-else v-model:open="detailOpen" :title="selectedGroup?.name ?? 'Gruppe'">
        <template #body>
            <GroupDetail v-if="selectedGroup" :group="selectedGroup" />
        </template>
    </USlideover>

    <GroupCreateModal v-model:open="createOpen" />
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import { useRoute, useRouter } from "vue-router";
import { storeToRefs } from "pinia";
import { useGroupsStore } from "@/Stores/GroupsStore";
import { useIsMobile } from "@/lib/useIsMobile";
import GroupDetail from "@/Components/Groups/GroupDetail.vue";
import GroupCreateModal from "@/Components/Groups/GroupCreateModal.vue";
import { districtLabel, type GroupDto } from "@/types";

const route = useRoute();
const router = useRouter();
const groupsStore = useGroupsStore();
const { Groups } = storeToRefs(groupsStore);
const isMobile = useIsMobile();

const loading = ref(true);
const loadFailed = ref(false);
const search = ref("");
const createOpen = ref(false);

onMounted(async () => {
    try {
        await groupsStore.GET_GROUPS();
    } catch {
        loadFailed.value = true;
    } finally {
        loading.value = false;
    }
});

const groups = computed(() => Groups.value.groups);

const selectedGroup = computed<GroupDto | null>(() => {
    const id = route.params.id;
    if (!id) return null;

    const idValue = Array.isArray(id) ? id[0] : id;

    return groups.value.find((group) => String(group.id) === idValue) ?? null;
});

const detailOpen = computed({
    get: () => selectedGroup.value != null,
    set: (open: boolean) => {
        if (!open) clearSelection();
    }
});

const filteredGroups = computed(() => {
    const query = search.value.trim().toLocaleLowerCase("da-DK");
    if (!query) return groups.value;

    return groups.value.filter((group) => group.name.toLocaleLowerCase("da-DK").includes(query)
        || String(group.id).toLocaleLowerCase("da-DK").includes(query));
});

function select(group: GroupDto) {
    if (selectedGroup.value?.id === group.id) {
        clearSelection();
        return;
    }

    router.replace(`/admin/groups/${group.id}`);
}

function clearSelection() {
    router.replace("/admin/groups");
}

</script>
