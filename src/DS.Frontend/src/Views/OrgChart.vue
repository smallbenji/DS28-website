<template>
    <UDashboardPanel id="org">
        <template #header>
            <UDashboardNavbar title="Organisation">
                <template #leading>
                    <UDashboardSidebarCollapse />
                </template>
                <template #right>
                    <UButton v-if="isSysAdmin && roots.length === 0 && !loading" icon="i-lucide-plus" label="Ny rodstilling" @click="rootOpen = true" />
                </template>
            </UDashboardNavbar>
        </template>

        <template #body>
            <div v-if="loading" class="flex justify-center gap-4 pt-8">
                <USkeleton v-for="index in 3" :key="index" class="h-28 w-56" />
            </div>

            <UEmpty v-else-if="roots.length === 0" icon="i-lucide-network" title="Ingen stillinger endnu." class="h-full" />

            <div
                v-else
                class="-m-4 overflow-auto p-6 sm:-m-6 focus-visible:outline-2 focus-visible:outline-primary"
                tabindex="0"
                role="region"
                aria-label="Organisationsdiagram. Rul vandret og lodret for at se alle stillinger."
            >
                <ul class="flex w-max min-w-full justify-center gap-6">
                    <OrgNode v-for="root in roots" :key="root.position.id" :node="root" @select="openDrawer" />
                </ul>
            </div>

            <OrgPositionDrawer
                v-model:open="drawerOpen"
                :position="selectedPosition"
                :positions="positions"
                :assignable-groups="assignableGroups"
                :users="users"
                :current-user-id="currentUserId"
                :is-sys-admin="isSysAdmin"
                @refresh="handleRefresh"
            />

            <UModal v-model:open="rootOpen" title="Ny rodstilling">
                <template #body>
                    <form id="org-root-form" class="flex flex-col gap-4" @submit.prevent="createRoot">
                        <UFormField label="Titel" required>
                            <UInput v-model="rootTitle" placeholder="Fx Lejrchef" class="w-full" autofocus />
                        </UFormField>
                        <UFormField label="Farve">
                            <OrgColorSelect v-model="rootColor" />
                        </UFormField>
                        <UFormField label="Grupper">
                            <USelectMenu v-model="rootGroups" :items="groupItems" value-key="value" multiple placeholder="Vælg grupper…" class="w-full" />
                        </UFormField>
                    </form>
                </template>
                <template #footer>
                    <div class="flex w-full justify-end gap-2">
                        <UButton color="neutral" variant="outline" label="Annuller" @click="rootOpen = false" />
                        <UButton type="submit" form="org-root-form" label="Opret" :loading="saving" :disabled="!rootTitle.trim()" />
                    </div>
                </template>
            </UModal>
        </template>
    </UDashboardPanel>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from "vue";
import OrgService from "@/Services/OrgService";
import UserService from "@/Services/UserService";
import { useMeStore } from "@/Stores/MeStore";
import { useNotify } from "@/lib/notify";
import OrgNode from "@/Components/Org/OrgNode.vue";
import OrgPositionDrawer from "@/Components/Org/OrgPositionDrawer.vue";
import OrgColorSelect from "@/Components/Org/OrgColorSelect.vue";
import { groupTitle, type OrgNodeModel, type OrgPositionDto, type UserDto } from "@/types";

const service = new OrgService();
const userService = new UserService();
const meStore = useMeStore();
const notify = useNotify();

const positions = ref<OrgPositionDto[]>([]);
const assignableGroups = ref<string[]>([]);
const users = ref<UserDto[]>([]);
const loading = ref(true);
const saving = ref(false);
const selectedId = ref<number | null>(null);
const drawerOpen = ref(false);
const rootOpen = ref(false);
const rootTitle = ref("");
const rootColor = ref<string | null>(null);
const rootGroups = ref<string[]>([]);

const currentUserId = computed(() => meStore.Me?.id ?? "");
const isSysAdmin = computed(() => meStore.Me?.roles.includes("SysAdmin") ?? false);
const selectedPosition = computed(() => positions.value.find((position) => position.id === selectedId.value) ?? null);
const groupItems = computed(() => assignableGroups.value.map((group) => ({ label: groupTitle(group), value: group })));

const roots = computed<OrgNodeModel[]>(() => {
    const paByOwner = new Map<number, OrgPositionDto>();
    positions.value.forEach((position) => {
        if (position.paForPositionId !== null) paByOwner.set(position.paForPositionId, position);
    });

    const nodes = new Map<number, OrgNodeModel>();
    positions.value
        .filter((position) => position.paForPositionId === null)
        .forEach((position) => nodes.set(position.id, { position, pa: paByOwner.get(position.id) ?? null, children: [] }));

    const result: OrgNodeModel[] = [];
    nodes.forEach((node) => {
        const parentId = node.position.parentPositionId;
        const parent = parentId === null ? undefined : nodes.get(parentId);
        if (parent) parent.children.push(node);
        else result.push(node);
    });

    return result;
});

async function loadOrg() {
    const [orgPositions, groups] = await Promise.all([service.getPositions(), service.getAssignableGroups()]);
    positions.value = orgPositions;
    assignableGroups.value = groups;
}

async function load() {
    loading.value = true;
    try {
        await Promise.all([loadOrg(), userService.getUsers().then((data) => { users.value = data; })]);
    } finally {
        loading.value = false;
    }
}

function openDrawer(positionId: number) {
    selectedId.value = positionId;
    drawerOpen.value = true;
}

async function handleRefresh() {
    await loadOrg();
    if (selectedPosition.value === null) drawerOpen.value = false;
}

async function createRoot() {
    if (!rootTitle.value.trim()) return;

    saving.value = true;
    try {
        const ok = await service.createPosition({ title: rootTitle.value.trim(), parentPositionId: null, groups: rootGroups.value, color: rootColor.value });
        notify.result(ok, "Rodstillingen er oprettet.", "Rodstillingen kunne ikke oprettes.");
        if (ok) {
            rootOpen.value = false;
            rootTitle.value = "";
            rootColor.value = null;
            rootGroups.value = [];
            await loadOrg();
        }
    } finally {
        saving.value = false;
    }
}

onMounted(load);
</script>
