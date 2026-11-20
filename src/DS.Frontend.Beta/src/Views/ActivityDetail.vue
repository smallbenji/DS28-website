<template>
    <UDashboardPanel id="activity-detail">
        <template #header>
            <UDashboardNavbar :title="activity ? (form.name || 'Aktivitet') : 'Aktivitet'">
                <template #leading>
                    <UDashboardSidebarCollapse />
                    <UButton to="/activity" color="neutral" variant="ghost" icon="i-lucide-arrow-left" aria-label="Tilbage til aktiviteter" />
                </template>
            </UDashboardNavbar>
        </template>

        <template #body>
            <div v-if="loading" class="mx-auto flex w-full max-w-5xl flex-col gap-4">
                <USkeleton class="h-16 w-80" />
                <USkeleton class="h-24" />
                <USkeleton class="h-64" />
            </div>

            <UEmpty
                v-else-if="!activity"
                icon="i-lucide-newspaper"
                title="Aktiviteten blev ikke fundet"
                description="Den findes ikke, eller du har ikke adgang til den."
                :actions="[{ label: 'Til aktiviteter', to: '/activity' }]"
                class="h-full"
            />

            <div v-else class="mx-auto flex w-full max-w-5xl flex-col gap-6">
                <div class="flex flex-wrap items-start justify-between gap-4">
                    <div class="min-w-0 flex-1">
                        <p class="text-xs uppercase text-muted">Aktivitet</p>
                        <div v-if="!editingName" class="flex items-center gap-2">
                            <h1 class="truncate text-2xl font-semibold text-highlighted">{{ form.name || 'Aktivitet' }}</h1>
                            <UButton
                                v-if="activity.canEdit"
                                color="neutral"
                                variant="ghost"
                                size="sm"
                                icon="i-lucide-pencil"
                                aria-label="Rediger aktivitetens navn"
                                :disabled="saving || reviewBusy"
                                @click="startNameEdit"
                            />
                        </div>
                        <form v-else class="mt-1 flex flex-wrap items-end gap-2" @submit.prevent="applyName">
                            <UFormField label="Aktivitetens navn" required class="min-w-64 flex-1">
                                <UInput
                                    ref="nameInput"
                                    v-model="nameDraft"
                                    placeholder="Aktivitetens navn"
                                    :disabled="saving || reviewBusy"
                                    class="w-full"
                                    @keydown.esc.prevent="editingName = false"
                                />
                            </UFormField>
                            <UButton type="submit" label="Anvend" :disabled="!nameDraft.trim()" />
                            <UButton color="neutral" variant="ghost" label="Annuller" @click="editingName = false" />
                        </form>
                    </div>
                    <div class="flex flex-col items-end gap-2">
                        <div class="text-right" aria-label="Aktivitetens tildelte budget">
                            <p class="text-xs uppercase text-muted">Budget</p>
                            <p class="text-xl font-semibold text-highlighted tabular-nums">{{ formattedBudget }}</p>
                        </div>
                        <UBadge v-if="activityTabFilled && catalogTabFilled" color="success" variant="subtle" icon="i-lucide-check" label="Indhold tilføjet" />
                        <UBadge v-else color="error" variant="subtle" icon="i-lucide-circle-alert" :label="`Mangler indhold: ${missingTabs.join(', ')}`" />
                    </div>
                </div>

                <ActivityReviewPanel :activity="activity" :is-admin="isActivityAdmin" :has-unsaved-changes="hasChanges || editingName" @busy="reviewBusy = $event" />

                <UTabs v-model="activeTab" :items="tabs" :content="false" variant="link" class="w-full" />

                <div v-if="activeTab === 'overview'" class="grid gap-6 lg:grid-cols-2">
                    <UCard>
                        <template #header>
                            <div class="flex items-center justify-between gap-2">
                                <h2 class="font-semibold text-highlighted">Katalog</h2>
                                <UButton v-if="activity.canEdit" color="neutral" variant="ghost" size="sm" label="Rediger katalog" @click="activeTab = 'catalog'" />
                            </div>
                        </template>
                        <template v-if="catalogTabFilled">
                            <h3 class="font-semibold text-highlighted">{{ form.catalog.name || 'Katalogtekst' }}</h3>
                            <p v-if="form.catalog.summary" class="mt-1 text-sm font-medium">{{ form.catalog.summary }}</p>
                            <p v-if="form.catalog.description" class="mt-2 whitespace-pre-line text-sm text-muted">{{ form.catalog.description }}</p>
                            <div v-if="missingCatalogText" class="mt-3 flex flex-wrap items-center gap-2 rounded-md bg-elevated px-3 py-2 text-sm">
                                <span>{{ missingCatalogText }} mangler.</span>
                                <UButton v-if="activity.canEdit" color="neutral" variant="ghost" size="xs" label="Tilføj katalogtekst" @click="activeTab = 'catalog'" />
                            </div>
                        </template>
                        <UEmpty
                            v-else
                            icon="i-lucide-globe"
                            description="Giv aktiviteten en katalogtekst."
                            :actions="activity.canEdit ? [{ label: 'Tilføj katalogtekst', onClick: () => { activeTab = 'catalog'; } }] : []"
                        />
                    </UCard>

                    <UCard>
                        <template #header>
                            <div class="flex items-center justify-between gap-2">
                                <h2 class="font-semibold text-highlighted">
                                    Filer og mapper
                                    <span v-if="!filesLoading && !filesError && googleConnected" class="font-normal text-muted">({{ overviewFiles.length }})</span>
                                </h2>
                                <UButton v-if="googleConnected" color="neutral" variant="ghost" size="sm" :label="activity.canEdit ? 'Se alle og opret' : 'Se alle'" @click="showFiles" />
                            </div>
                        </template>
                        <div v-if="filesLoading" class="flex flex-col gap-2" role="status" aria-label="Henter filer fra Google Drev">
                            <USkeleton v-for="index in 4" :key="index" class="h-8" />
                        </div>
                        <GoogleNotConnected v-else-if="!googleConnected" :can-manage="canManageGoogle" />
                        <UAlert
                            v-else-if="filesError"
                            color="error"
                            variant="subtle"
                            title="Filerne kunne ikke hentes."
                            :actions="[{ label: 'Prøv igen', color: 'error', variant: 'outline', onClick: retryFiles }]"
                        />
                        <ul v-else-if="overviewFiles.length" class="flex flex-col gap-1">
                            <li v-for="file in overviewFiles.slice(0, 6)" :key="file.id">
                                <button v-if="file.kind === 'folder'" type="button" class="flex w-full items-center gap-2 rounded-md px-2 py-1.5 text-left text-sm hover:bg-elevated" @click="showFolder(file)">
                                    <UIcon :name="fileIcon(file.kind)" class="size-4 text-muted" />
                                    <span class="min-w-0 flex-1 truncate">{{ file.name }}</span>
                                    <UIcon name="i-lucide-chevron-right" class="size-4 text-muted" />
                                </button>
                                <a v-else :href="file.url" target="_blank" rel="noopener" class="flex items-center gap-2 rounded-md px-2 py-1.5 text-sm hover:bg-elevated" :title="`Åbn ${file.name} i Google (ny fane)`">
                                    <UIcon :name="fileIcon(file.kind)" class="size-4 text-muted" />
                                    <span class="min-w-0 flex-1 truncate">{{ file.name }}</span>
                                    <UIcon name="i-lucide-external-link" class="size-3.5 text-muted" />
                                    <span class="sr-only">(åbner i ny fane)</span>
                                </a>
                            </li>
                        </ul>
                        <UEmpty
                            v-else
                            icon="i-lucide-folder-open"
                            description="Saml aktivitetens dokumenter og regneark her."
                            :actions="activity.canEdit ? [{ label: 'Opret den første fil', onClick: showFiles }] : []"
                        />
                        <p v-if="!filesLoading && !filesError && overviewFiles.length > 6" class="mt-3 text-xs text-muted">Viser 6 af {{ overviewFiles.length }} filer og mapper i aktivitetens hovedmappe.</p>
                    </UCard>
                </div>

                <UCard v-else-if="activeTab === 'catalog'">
                    <template #header>
                        <h2 class="font-semibold text-highlighted">Katalog</h2>
                        <p class="text-sm text-muted">Teksten bruges i aktivitetskataloget.</p>
                    </template>
                    <div class="flex flex-col gap-4">
                        <UFormField label="Katalognavn">
                            <UInput v-model="form.catalog.name" placeholder="Katalognavn" :disabled="!activity.canEdit || saving || reviewBusy" class="w-full" />
                        </UFormField>
                        <UFormField label="Resumé">
                            <UInput v-model="form.catalog.summary" placeholder="Kort resumé" :disabled="!activity.canEdit || saving || reviewBusy" class="w-full" />
                        </UFormField>
                        <UFormField label="Beskrivelse">
                            <UTextarea v-model="form.catalog.description" placeholder="Beskrivelse" :rows="6" autoresize :disabled="!activity.canEdit || saving || reviewBusy" class="w-full" />
                        </UFormField>
                    </div>
                </UCard>

                <div v-else-if="activeTab === 'files'" class="flex flex-col gap-6">
                    <GoogleNotConnected v-if="!googleConnected" :can-manage="canManageGoogle" />
                    <template v-else>
                        <UCard v-if="activity.canEdit">
                            <template #header>
                                <h2 class="font-semibold text-highlighted">Opret fil eller mappe</h2>
                                <p class="text-sm text-muted">Filen eller mappen gemmes med det samme i Google Drev. Brug »Gem aktivitet« til ændringer på de øvrige faner.</p>
                            </template>
                            <form class="flex flex-wrap items-end gap-3" @submit.prevent="createFile">
                                <UFormField label="Navn" class="min-w-56 flex-1">
                                    <UInput v-model="fileTitle" :placeholder="fileTitlePlaceholder" class="w-full" />
                                </UFormField>
                                <UFormField label="Type" class="w-44">
                                    <USelect v-model="fileKind" :items="fileKindItems" class="w-full" />
                                </UFormField>
                                <UButton type="submit" icon="i-lucide-plus" label="Opret" :loading="creatingFile" :disabled="creatingFile || reviewBusy" />
                            </form>
                        </UCard>

                        <UCard :ui="{ body: 'p-0 sm:p-0' }">
                            <template #header>
                                <nav v-if="folderPath.length" class="mb-2 flex flex-wrap items-center gap-1 text-sm" aria-label="Mappeplacering">
                                    <UButton color="neutral" variant="link" size="sm" icon="i-lucide-folder-open" :label="activity.name || 'Aktiviteten'" class="px-0" @click="goToFolder(0)" />
                                    <template v-for="(folder, index) in folderPath" :key="folder.id">
                                        <UIcon name="i-lucide-chevron-right" class="size-3.5 text-muted" />
                                        <UButton color="neutral" variant="link" size="sm" :label="folder.name" class="px-0" :disabled="index === folderPath.length - 1" @click="goToFolder(index + 1)" />
                                    </template>
                                </nav>
                                <h2 class="font-semibold text-highlighted">Filer og mapper <span class="font-normal text-muted">({{ files.length }})</span></h2>
                            </template>

                            <div v-if="filesLoading" class="flex flex-col gap-2 p-4 sm:p-6">
                                <USkeleton v-for="index in 4" :key="index" class="h-8" />
                            </div>
                            <p v-else-if="files.length === 0" class="px-4 py-6 text-center text-sm text-muted sm:px-6">
                                {{ activity.canEdit ? 'Der er endnu ingen filer eller mapper her. Opret den første ovenfor.' : 'Der er endnu ingen filer eller mapper her.' }}
                            </p>
                            <div v-else class="overflow-x-auto">
                                <table class="w-full text-sm">
                                    <thead class="border-b border-default text-left text-xs text-muted">
                                        <tr>
                                            <th scope="col" class="px-4 py-2 font-medium sm:px-6">Navn</th>
                                            <th scope="col" class="px-2 py-2 font-medium">Oprettet af</th>
                                            <th scope="col" class="px-4 py-2 font-medium sm:px-6">Sidst ændret</th>
                                        </tr>
                                    </thead>
                                    <tbody class="divide-y divide-default">
                                        <tr v-for="file in files" :key="file.id" class="hover:bg-elevated/50">
                                            <td class="px-4 py-2 sm:px-6">
                                                <button v-if="file.kind === 'folder'" type="button" class="flex items-center gap-2 text-left font-medium hover:underline" :title="`Åbn mappen ${file.name}`" @click="openFolder(file)">
                                                    <UIcon name="i-lucide-folder" class="size-4 text-muted" />
                                                    {{ file.name }}
                                                    <UIcon name="i-lucide-chevron-right" class="size-3.5 text-muted" />
                                                </button>
                                                <a v-else :href="file.url" target="_blank" rel="noopener" class="flex items-center gap-2 font-medium hover:underline" :title="`Åbn ${file.name} i Google (ny fane)`">
                                                    <UIcon :name="fileIcon(file.kind)" class="size-4 text-muted" />
                                                    {{ file.name }}
                                                    <UIcon name="i-lucide-external-link" class="size-3.5 text-muted" />
                                                    <span class="sr-only">(åbner i ny fane)</span>
                                                </a>
                                            </td>
                                            <td class="px-2 py-2 text-muted">{{ file.createdBy || '-' }}</td>
                                            <td class="whitespace-nowrap px-4 py-2 text-muted sm:px-6">{{ file.modifiedAt ? dateFormat.format(new Date(file.modifiedAt)) : '-' }}</td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                        </UCard>
                    </template>
                </div>

                <UAlert
                    v-if="saveConflict"
                    color="warning"
                    variant="subtle"
                    icon="i-lucide-triangle-alert"
                    title="Aktiviteten er ændret af en anden"
                    description="Genindlæs og gennemgå den nyeste version, før du fortsætter."
                    :actions="[{ label: 'Genindlæs aktivitet', color: 'warning', variant: 'outline', disabled: saving || reviewBusy, onClick: () => { reloadOpen = true; } }]"
                />

                <div v-if="activity.canEdit && hasChanges" class="sticky bottom-0 flex flex-wrap items-center justify-end gap-3 rounded-lg border border-default bg-default p-3 shadow-md">
                    <span class="me-auto flex items-center gap-1.5 text-sm text-warning" role="status">
                        <span class="size-2 rounded-full bg-warning" aria-hidden="true" />
                        Du har ændringer, der ikke er gemt.
                    </span>
                    <UButton color="neutral" variant="ghost" label="Fortryd" :disabled="saving" @click="resetForm" />
                    <UButton color="success" icon="i-lucide-check" label="Gem aktivitet" :loading="saving" :disabled="saving || reviewBusy || editingName" @click="saveActivity" />
                </div>
            </div>

            <ConfirmModal
                v-model:open="reloadOpen"
                title="Genindlæs aktivitet"
                description="Dine lokale ændringer kasseres, og den nyeste version hentes. Vil du fortsætte?"
                confirm-label="Genindlæs"
                color="warning"
                :loading="saving"
                @confirm="reloadActivity"
            />
        </template>
    </UDashboardPanel>
</template>

<script setup lang="ts">
import { computed, nextTick, onMounted, reactive, ref, watch } from "vue";
import { useRoute } from "vue-router";
import { storeToRefs } from "pinia";
import { useActivityStore } from "@/Stores/ActivityStore";
import { useMeStore } from "@/Stores/MeStore";
import GoogleService from "@/Services/GoogleService";
import ActivityService from "@/Services/ActivityService";
import { getErrorMessage, getErrorStatus } from "@/lib/api";
import { useNotify } from "@/lib/notify";
import ConfirmModal from "@/Components/ConfirmModal.vue";
import ActivityReviewPanel from "@/Components/Activity/ActivityReviewPanel.vue";
import GoogleNotConnected from "@/Components/Activity/GoogleNotConnected.vue";
import type { ActivityDto, ActivityFileDto, ActivityFileKind, CatalogDataDto } from "@/types";

type ActivityTab = "overview" | "catalog" | "files";

interface ActivityFormDto {
    id: number;
    name: string;
    budget: number;
    reviewVersion: number;
    catalog: CatalogDataDto;
}

const route = useRoute();
const activityService = new ActivityService();
const googleService = new GoogleService();
const activityStore = useActivityStore();
const meStore = useMeStore();
const notify = useNotify();
const { SelectedActivity, Files } = storeToRefs(activityStore);

const activity = SelectedActivity;
const files = Files;

const loading = ref(true);
const activeTab = ref<ActivityTab>("overview");
const editingName = ref(false);
const nameDraft = ref("");
const nameInput = ref<{ inputRef?: HTMLInputElement } | null>(null);
const overviewFiles = ref<ActivityFileDto[]>([]);
const filesLoading = ref(true);
const filesError = ref(false);
const saving = ref(false);
const reviewBusy = ref(false);
const saveConflict = ref(false);
const reloadOpen = ref(false);
const creatingFile = ref(false);
const fileTitle = ref("");
const fileKind = ref<ActivityFileKind>("doc");
const googleConnected = ref(true);
const folderPath = ref<{ id: string; name: string }[]>([]);
const hasFiles = ref(false);

const fileKindItems = [
    { label: "Dokument", value: "doc" },
    { label: "Regneark", value: "sheet" },
    { label: "Præsentation", value: "slides" },
    { label: "Mappe", value: "folder" }
];

const dateFormat = new Intl.DateTimeFormat("da-DK", { dateStyle: "medium", timeZone: "Europe/Copenhagen" });

const isActivityAdmin = computed(() => meStore.Me?.appRoles.includes("ActivityAdmin") ?? false);
const canManageGoogle = computed(() => meStore.Me?.appRoles.includes("AdminAccess") ?? false);

const form = reactive<ActivityFormDto>({
    id: 0,
    name: "",
    budget: 0,
    reviewVersion: 0,
    catalog: { id: 0, name: "", summary: "", description: "" }
});

const activityId = computed(() => {
    const id = route.params.id;

    return Number(Array.isArray(id) ? id[0] : id);
});

async function loadActivity() {
    if (!Number.isFinite(activityId.value)) {
        loading.value = false;
        return;
    }

    loading.value = true;
    try {
        if (Number(SelectedActivity.value?.id) !== activityId.value) SelectedActivity.value = null;
        await activityStore.GET_ACTIVITY(activityId.value);
    } finally {
        loading.value = false;
    }
}

onMounted(loadActivity);
watch(activityId, (id, previous) => {
    if (id !== previous) void loadActivity();
});

async function startNameEdit() {
    if (!activity.value?.canEdit || reviewBusy.value) return;

    nameDraft.value = form.name;
    editingName.value = true;
    await nextTick();
    nameInput.value?.inputRef?.focus();
}

function applyName() {
    if (!nameDraft.value.trim()) return;

    form.name = nameDraft.value.trim();
    editingName.value = false;
}

const missingCatalogText = computed(() => {
    const missing = [];
    if (!form.catalog.summary.trim()) missing.push("Resumé");
    if (!form.catalog.description.trim()) missing.push("beskrivelse");

    return missing.join(" og ").replace(/^beskrivelse/, "Beskrivelse");
});

const formattedBudget = computed(() => new Intl.NumberFormat("da-DK", {
    style: "currency", currency: "DKK", minimumFractionDigits: 0, maximumFractionDigits: 2
}).format(form.budget));

const hasChanges = computed(() => {
    if (!activity.value) return false;

    const catalog = activity.value.catalog ?? { id: 0, name: "", summary: "", description: "" };

    return form.name !== (activity.value.name ?? "")
        || form.catalog.name !== (catalog.name ?? "")
        || form.catalog.summary !== (catalog.summary ?? "")
        || form.catalog.description !== (catalog.description ?? "");
});

const activityTabFilled = computed(() => form.name.trim().length > 0);
const catalogTabFilled = computed(() => form.catalog.name.trim().length > 0
    || form.catalog.summary.trim().length > 0
    || form.catalog.description.trim().length > 0);
const missingTabs = computed(() => [
    ...(!activityTabFilled.value ? ["Navn"] : []),
    ...(!catalogTabFilled.value ? ["Katalog"] : [])
]);

const tabs = computed(() => [
    { label: "Overblik", value: "overview", icon: "i-lucide-layout-grid" },
    { label: "Katalog", value: "catalog", icon: "i-lucide-globe", trailingIcon: catalogTabFilled.value ? "i-lucide-circle-check" : undefined },
    { label: "Filer", value: "files", icon: "i-lucide-folder-open", trailingIcon: hasFiles.value ? "i-lucide-circle-check" : undefined }
]);

const currentFolderId = computed(() => folderPath.value.length > 0 ? folderPath.value[folderPath.value.length - 1]!.id : null);
const fileTitlePlaceholder = computed(() => fileKind.value === "folder" ? "Mappens navn" : form.name || "Navn");

function fileIcon(kind: ActivityFileKind) {
    if (kind === "sheet") return "i-lucide-sheet";
    if (kind === "slides") return "i-lucide-presentation";
    if (kind === "folder") return "i-lucide-folder";
    if (kind === "doc") return "i-lucide-file-text";

    return "i-lucide-file";
}

function syncForm(current: ActivityDto) {
    form.id = current.id;
    form.name = current.name ?? "";
    form.budget = current.budget;
    form.reviewVersion = current.reviewVersion;
    form.catalog = {
        ...(current.catalog ?? { id: 0 }),
        name: current.catalog?.name ?? "",
        summary: current.catalog?.summary ?? "",
        description: current.catalog?.description ?? ""
    };
}

function resetForm() {
    if (activity.value) syncForm(activity.value);
    editingName.value = false;
}

async function loadFolder(id: number) {
    filesLoading.value = true;
    filesError.value = false;
    try {
        await activityStore.GET_FILES(id, currentFolderId.value);
    } catch {
        filesError.value = true;
        notify.error("Filerne kunne ikke hentes fra Google Drev.");
        return false;
    } finally {
        filesLoading.value = false;
    }

    if (folderPath.value.length === 0) {
        overviewFiles.value = [...files.value];
        hasFiles.value = files.value.length > 0;
    }

    return true;
}

async function loadFiles(id: number) {
    filesLoading.value = true;
    filesError.value = false;
    const status = await googleService.getStatus().catch(() => null);

    if (!status) {
        filesLoading.value = false;
        filesError.value = true;
        return;
    }

    googleConnected.value = status.connected;
    if (!googleConnected.value) {
        filesLoading.value = false;
        return;
    }

    await loadFolder(id);
}

async function showFiles() {
    activeTab.value = "files";
    if (folderPath.value.length) await goToFolder(0);
}

async function showFolder(file: ActivityFileDto) {
    if (!activity.value || filesLoading.value) return;

    activeTab.value = "files";
    folderPath.value = [{ id: file.id, name: file.name }];
    if (!await loadFolder(activity.value.id)) {
        folderPath.value = [];
        await loadFolder(activity.value.id);
    }
}

async function retryFiles() {
    folderPath.value = [];
    await loadFiles(form.id);
}

async function openFolder(file: ActivityFileDto) {
    if (!activity.value || file.kind !== "folder") return;

    const previous = folderPath.value;
    folderPath.value = [...previous, { id: file.id, name: file.name }];
    if (!await loadFolder(activity.value.id)) folderPath.value = previous;
}

async function goToFolder(depth: number) {
    if (!activity.value || depth === folderPath.value.length) return;

    const previous = folderPath.value;
    folderPath.value = previous.slice(0, depth);
    if (!await loadFolder(activity.value.id)) folderPath.value = previous;
}

watch(SelectedActivity, (current, previous) => {
    if (!current) return;

    editingName.value = false;
    saveConflict.value = false;
    syncForm(current);

    if (previous?.id === current.id) return;

    overviewFiles.value = [];
    fileTitle.value = "";
    folderPath.value = [];
    hasFiles.value = false;
    void loadFiles(current.id);
}, { immediate: true });

async function createFile() {
    if (!activity.value?.canEdit || creatingFile.value || reviewBusy.value) return;

    creatingFile.value = true;
    try {
        await activityStore.ADD_FILE(activity.value.id, {
            title: fileTitle.value.trim() || null,
            kind: fileKind.value,
            folderId: currentFolderId.value
        });

        const createdFolder = fileKind.value === "folder";
        fileTitle.value = "";

        if (folderPath.value.length === 0) {
            overviewFiles.value = [...files.value];
            hasFiles.value = files.value.length > 0;
        }

        notify.success(createdFolder ? "Mappen er oprettet." : "Filen er oprettet.");
    } catch (error) {
        const status = getErrorStatus(error);
        if (status === 503) googleConnected.value = false;

        notify.error(getErrorMessage(error, status === 403
            ? "Du har ikke rettigheder til at oprette filer og mapper til aktiviteten."
            : "Filen eller mappen kunne ikke oprettes."));
    } finally {
        creatingFile.value = false;
    }
}

async function reloadActivity() {
    saving.value = true;
    try {
        const fresh = await activityService.getActivity(form.id);
        if (fresh) {
            activityStore.SelectedActivity = fresh;
            reloadOpen.value = false;
        } else {
            notify.error("Aktiviteten kunne ikke genindlæses. Prøv igen.");
        }
    } finally {
        saving.value = false;
    }
}

async function saveActivity() {
    if (!activity.value?.canEdit || saving.value || reviewBusy.value || editingName.value) return;

    saving.value = true;
    try {
        const ok = await activityStore.UPDATE_ACTIVITY({ ...form });
        notify.result(ok, "Aktiviteten er opdateret.", "Aktiviteten kunne ikke opdateres.");
    } catch (error) {
        saveConflict.value = getErrorStatus(error) === 409;
        notify.error(getErrorMessage(error, "Aktiviteten kunne ikke opdateres."));
    } finally {
        saving.value = false;
    }
}
</script>
