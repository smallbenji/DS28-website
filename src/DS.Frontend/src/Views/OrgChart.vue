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

            <UDashboardToolbar v-if="roots.length > 0">
                <div class="flex w-full items-center gap-2 py-1">
                    <UInput v-model="chart.query.value" icon="i-lucide-search" placeholder="Søg stilling eller person" class="min-w-0 flex-1 sm:w-72 sm:flex-none" variant="soft" aria-label="Søg stilling eller person" />
                    <span v-if="chart.searching.value" class="hidden text-xs text-muted sm:inline" aria-live="polite">{{ chart.matchCount.value }} fundet</span>
                    <div class="ms-auto flex shrink-0 items-center gap-2">
                        <UFieldGroup>
                            <UButton icon="i-lucide-chevrons-down-up" color="neutral" variant="outline" aria-label="Fold alle sammen" title="Fold alle sammen" @click="chart.collapseAll()" />
                            <UButton icon="i-lucide-chevrons-up-down" color="neutral" variant="outline" aria-label="Fold alle ud" title="Fold alle ud" @click="chart.expandAll()" />
                        </UFieldGroup>
                        <UFieldGroup v-if="layout === 'diagram' && !isMobile">
                            <UButton icon="i-lucide-zoom-out" color="neutral" variant="outline" aria-label="Zoom ud" :disabled="zoom <= minZoom" @click="setZoom(zoom - zoomStep)" />
                            <UButton color="neutral" variant="outline" :label="`${Math.round(zoom * 100)} %`" class="w-16 justify-center tabular-nums" aria-label="Nulstil zoom" @click="setZoom(1)" />
                            <UButton icon="i-lucide-zoom-in" color="neutral" variant="outline" aria-label="Zoom ind" :disabled="zoom >= maxZoom" @click="setZoom(zoom + zoomStep)" />
                            <UButton icon="i-lucide-scan" color="neutral" variant="outline" aria-label="Tilpas til vinduet" title="Tilpas til vinduet" @click="fitToWindow" />
                        </UFieldGroup>
                        <UFieldGroup>
                            <UButton icon="i-lucide-network" :label="isMobile ? undefined : 'Diagram'" color="neutral" :variant="layout === 'diagram' ? 'subtle' : 'outline'" :aria-pressed="layout === 'diagram'" aria-label="Vis som diagram" @click="layout = 'diagram'" />
                            <UButton icon="i-lucide-list-tree" :label="isMobile ? undefined : 'Liste'" color="neutral" :variant="layout === 'list' ? 'subtle' : 'outline'" :aria-pressed="layout === 'list'" aria-label="Vis som liste" @click="layout = 'list'" />
                        </UFieldGroup>
                    </div>
                </div>
            </UDashboardToolbar>
        </template>

        <template #body>
            <div v-if="loading" class="flex justify-center gap-4 pt-8">
                <USkeleton v-for="index in 3" :key="index" class="h-28 w-56" />
            </div>

            <UEmpty v-else-if="roots.length === 0" icon="i-lucide-network" title="Ingen stillinger endnu." class="h-full" />

            <div
                v-else-if="layout === 'diagram'"
                ref="diagramScroller"
                class="-m-4 h-[calc(100%+2rem)] overflow-auto p-6 sm:-m-6 sm:h-[calc(100%+3rem)] focus-visible:outline-2 focus-visible:outline-primary"
                tabindex="0"
                role="region"
                aria-label="Organisationsdiagram. Rul vandret og lodret for at se alle stillinger."
            >
                <ul ref="diagramTree" class="flex w-max min-w-full justify-center gap-6 pb-24" :style="{ zoom }">
                    <OrgNode v-for="root in roots" :key="root.position.id" :node="root" />
                </ul>
            </div>

            <ul v-else class="-mx-2 pb-24" aria-label="Organisation som liste">
                <OrgListRow v-for="root in roots" :key="root.position.id" :node="root" :depth="0" />
            </ul>

            <OrgPositionDrawer
                v-model:open="drawerOpen"
                v-model:tab="drawerTab"
                :position="selectedPosition"
                :positions="positions"
                :roots="roots"
                :assignable-groups="assignableGroups"
                :users="users"
                :current-user-id="currentUserId"
                :is-sys-admin="isSysAdmin"
                :side="isMobile ? 'bottom' : 'right'"
                :move="movePosition"
                @refresh="handleRefresh"
                @select="openDrawer"
                @create-child="createChildFromDrawer"
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
import { computed, nextTick, onMounted, ref } from "vue";
import { useMediaQuery } from "@vueuse/core";
import { useToast } from "@nuxt/ui/composables";
import OrgService from "@/Services/OrgService";
import UserService from "@/Services/UserService";
import { useMeStore } from "@/Stores/MeStore";
import { useNotify } from "@/lib/notify";
import { createOrgChartContext, provideOrgChart } from "@/lib/org/orgChartContext";
import { buildOrgTree } from "@/lib/org/orgTree";
import OrgNode from "@/Components/Org/OrgNode.vue";
import OrgListRow from "@/Components/Org/OrgListRow.vue";
import OrgPositionDrawer from "@/Components/Org/OrgPositionDrawer.vue";
import OrgColorSelect from "@/Components/Org/OrgColorSelect.vue";
import { groupTitle, type OrgPositionCreateDto, type OrgPositionDto, type UserDto } from "@/types";

type OrgLayout = "diagram" | "list";
type OrgDrawerTab = "position" | "people" | "structure";

const minZoom = 0.4;
const maxZoom = 1.5;
const zoomStep = 0.1;

const service = new OrgService();
const userService = new UserService();
const meStore = useMeStore();
const notify = useNotify();
const toast = useToast();
const isMobile = useMediaQuery("(max-width: 639px)");

const positions = ref<OrgPositionDto[]>([]);
const assignableGroups = ref<string[]>([]);
const users = ref<UserDto[]>([]);
const loading = ref(true);
const saving = ref(false);
const selectedId = ref<number | null>(null);
const drawerOpen = ref(false);
const drawerTab = ref<OrgDrawerTab>("position");
const layout = ref<OrgLayout>("diagram");
const zoom = ref(1);
const diagramScroller = ref<HTMLElement | null>(null);
const diagramTree = ref<HTMLElement | null>(null);
const rootOpen = ref(false);
const rootTitle = ref("");
const rootColor = ref<string | null>(null);
const rootGroups = ref<string[]>([]);

const currentUserId = computed(() => meStore.Me?.id ?? "");
const isSysAdmin = computed(() => meStore.Me?.roles.includes("SysAdmin") ?? false);
const selectedPosition = computed(() => positions.value.find((position) => position.id === selectedId.value) ?? null);
const groupItems = computed(() => assignableGroups.value.map((group) => ({ label: groupTitle(group), value: group })));
const roots = computed(() => buildOrgTree(positions.value));

const chart = createOrgChartContext({
    positions,
    roots,
    currentUserId,
    isSysAdmin,
    dragEnabled: computed(() => !isMobile.value),
    groupItems,
    onSelect: (positionId) => openDrawer(positionId),
    onCreate: createChild,
    onMove: async (positionId, parentPositionId) => { await movePosition(positionId, parentPositionId); }
});
provideOrgChart(chart);

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

function openDrawer(positionId: number, tab?: OrgDrawerTab) {
    const position = positions.value.find((candidate) => candidate.id === positionId);
    if (!position) return;

    chart.cancelCreate();
    if (selectedId.value !== positionId || tab) drawerTab.value = tab ?? (position.paForPositionId === null ? "position" : "people");
    selectedId.value = positionId;
    drawerOpen.value = true;
}

async function handleRefresh() {
    await loadOrg();
    if (selectedPosition.value === null) drawerOpen.value = false;
}

async function revealPosition(positionId: number) {
    chart.highlight(positionId);
    await nextTick();
    document.querySelector(`[data-position-id="${positionId}"]`)?.scrollIntoView({ behavior: "smooth", block: "nearest", inline: "center" });
}

function createChildFromDrawer(positionId: number) {
    drawerOpen.value = false;
    chart.startCreate(positionId);
}

async function createChild(data: OrgPositionCreateDto): Promise<boolean> {
    const previousIds = new Set(positions.value.map((position) => position.id));
    const ok = await service.createPosition(data);
    if (!ok) {
        notify.error("Stillingen kunne ikke oprettes.");
        return false;
    }

    await loadOrg();
    const created = positions.value.find((position) => !previousIds.has(position.id) && position.parentPositionId === data.parentPositionId);
    toast.add({
        title: "Stillingen er oprettet.",
        color: "success",
        icon: "i-lucide-circle-check",
        actions: created ? [{ label: "Tilføj personer", color: "neutral", variant: "outline", onClick: () => openDrawer(created.id, "people") }] : undefined
    });
    if (created) await revealPosition(created.id);

    return true;
}

async function movePosition(positionId: number, parentPositionId: number | null): Promise<boolean> {
    const position = positions.value.find((candidate) => candidate.id === positionId);
    if (!position) return false;

    const previousParentId = position.parentPositionId;
    const ok = await service.movePosition(positionId, parentPositionId);
    if (!ok) {
        notify.error("Stillingen kunne ikke flyttes.");
        return false;
    }

    await loadOrg();
    const parentTitle = positions.value.find((candidate) => candidate.id === parentPositionId)?.title;
    toast.add({
        title: parentTitle ? `${position.title} er flyttet under ${parentTitle}.` : `${position.title} er nu en rodstilling.`,
        color: "success",
        icon: "i-lucide-circle-check",
        actions: [{ label: "Fortryd", color: "neutral", variant: "outline", onClick: () => { void undoMove(positionId, previousParentId); } }]
    });
    await revealPosition(positionId);

    return true;
}

async function undoMove(positionId: number, parentPositionId: number | null) {
    const ok = await service.movePosition(positionId, parentPositionId);
    notify.result(ok, "Flytningen er fortrudt.", "Flytningen kunne ikke fortrydes.");
    if (!ok) return;

    await loadOrg();
    await revealPosition(positionId);
}

function setZoom(value: number) {
    zoom.value = Math.min(maxZoom, Math.max(minZoom, Math.round(value * 100) / 100));
}

function fitToWindow() {
    const scroller = diagramScroller.value;
    const tree = diagramTree.value;
    if (!scroller || !tree) return;

    const naturalWidth = tree.getBoundingClientRect().width / zoom.value;
    const availableWidth = scroller.clientWidth - 48;
    setZoom(Math.min(1, availableWidth / naturalWidth));
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

onMounted(() => {
    if (isMobile.value) layout.value = "list";
    void load();
});
</script>
