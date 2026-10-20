<template>
    <div class="activity-detail ui-card" v-if="activity">
        <div class="activity-detail-header">
            <div class="activity-detail-heading">
                <p class="activity-detail-eyebrow">Aktivitet</p>
                <div v-if="!editingName" class="activity-detail-heading-row">
                    <h1 class="title is-4 mb-0">{{ form.name || 'Aktivitet' }}</h1>
                    <BButton v-if="activity.canEdit" size="is-small" class="is-ghost" :disabled="saving || reviewBusy" aria-label="Rediger aktivitetens navn" @click="startNameEdit">
                        <i class="fas fa-pen" aria-hidden="true"></i>
                    </BButton>
                </div>
                <form v-else class="activity-detail-name-editor" @submit.prevent="applyName">
                    <BField label="Aktivitetens navn">
                        <BInput :disabled="saving || reviewBusy" ref="nameInput" v-model="nameDraft" placeholder="Aktivitetens navn" required @keydown.esc.prevent="editingName = false" />
                    </BField>
                    <div class="activity-detail-name-actions">
                        <BButton size="is-small" native-type="submit">Anvend</BButton>
                        <BButton size="is-small" class="is-ghost" @click="editingName = false">Annuller</BButton>
                    </div>
                </form>
            </div>
            <div class="activity-detail-header-meta">
                <div class="activity-detail-budget" aria-label="Aktivitetens tildelte budget">
                    <span>Budget</span>
                    <strong>{{ formattedBudget }}</strong>
                </div>
                <span
                    class="tag is-success is-light is-medium"
                    v-if="activityTabFilled && catalogTabFilled"
                >
                    <i class="fas fa-check"></i> Indhold tilføjet
                </span>
                <span class="tag is-danger is-light is-medium" v-else>
                    <i class="fas fa-circle-exclamation"></i> Mangler indhold: {{ missingTabs.join(', ') }}
                </span>
            </div>
        </div>
        <ActivityReviewPanel :activity="activity" :is-admin="isActivityAdmin" :has-unsaved-changes="hasChanges || editingName" @busy="reviewBusy = $event" />
        <div class="activity-detail-body">
            <nav class="activity-detail-nav">
                <button
                    v-for="tab in tabs"
                    :key="tab.key"
                    type="button"
                    class="activity-detail-nav-item"
                    :class="{ 'is-active': activeTab === tab.key }"
                    @click="activeTab = tab.key"
                    :aria-current="activeTab === tab.key ? 'page' : undefined"
                >
                    <i class="activity-detail-nav-icon" :class="tab.icon"></i>
                    <span class="activity-detail-nav-label">{{ tab.label }}</span>
                    <i class="far fa-circle-check activity-detail-nav-check" v-if="tab.filled"></i>
                </button>
            </nav>
            <div class="activity-detail-panel">
                <div v-if="activeTab === 'overview'" class="activity-detail-overview">
                    <section class="activity-detail-summary activity-detail-catalog-summary">
                        <header class="activity-detail-section-header">
                            <h2>Katalog</h2>
                            <BButton v-if="activity.canEdit" size="is-small" class="is-ghost" @click="activeTab = 'catalog'">Rediger katalog</BButton>
                        </header>
                        <template v-if="catalogTabFilled">
                            <h3 class="activity-detail-catalog-title">{{ form.catalog.name || 'Katalogtekst' }}</h3>
                            <p v-if="form.catalog.summary" class="activity-detail-catalog-intro">{{ form.catalog.summary }}</p>
                            <p v-if="form.catalog.description" class="activity-detail-catalog-description">{{ form.catalog.description }}</p>
                            <div v-if="missingCatalogText" class="activity-detail-catalog-incomplete">
                                <p>{{ missingCatalogText }} mangler.</p>
                                <BButton v-if="activity.canEdit" size="is-small" class="is-ghost" @click="activeTab = 'catalog'">Tilføj katalogtekst</BButton>
                            </div>
                        </template>
                        <div v-else class="activity-detail-empty">
                            <i class="fas fa-globe" aria-hidden="true"></i>
                            <p>Giv aktiviteten en katalogtekst.</p>
                            <BButton v-if="activity.canEdit" size="is-small" @click="activeTab = 'catalog'">Tilføj katalogtekst</BButton>
                        </div>
                    </section>
                    <section class="activity-detail-summary activity-detail-file-summary">
                        <header class="activity-detail-section-header">
                            <h2>Filer og mapper <span v-if="!filesLoading && !filesError && googleConnected" class="activity-detail-count">{{ overviewFiles.length }}</span></h2>
                            <BButton v-if="googleConnected" size="is-small" class="is-ghost" @click="showFiles">{{ activity.canEdit ? 'Se alle og opret' : 'Se alle' }}</BButton>
                        </header>
                        <p v-if="filesLoading" class="activity-detail-muted" role="status">Henter filer fra Google Drev…</p>
                        <BNotification v-else-if="!googleConnected" type="is-warning" :closable="false">
                            Google Drev er ikke forbundet.
                            <router-link v-if="canManageGoogle" to="/admin/google">Forbind Google Drev</router-link>
                        </BNotification>
                        <div v-else-if="filesError" class="activity-detail-empty">
                            <p>Filerne kunne ikke hentes.</p>
                            <BButton size="is-small" @click="retryFiles">Prøv igen</BButton>
                        </div>
                        <ul v-else-if="overviewFiles.length" class="activity-detail-quick-files">
                            <li v-for="file in overviewFiles.slice(0, 6)" :key="file.id">
                                <button v-if="file.kind === 'folder'" class="activity-detail-quick-file" type="button" @click="showFolder(file)">
                                    <i :class="fileIcon(file.kind)" aria-hidden="true"></i>
                                    <span>{{ file.name }}</span>
                                    <i class="fas fa-chevron-right" aria-hidden="true"></i>
                                </button>
                                <a v-else class="activity-detail-quick-file" :href="file.url" target="_blank" rel="noopener" :title="`Åbn ${file.name} i Google (ny fane)`">
                                    <i :class="fileIcon(file.kind)" aria-hidden="true"></i>
                                    <span>{{ file.name }}</span>
                                    <i class="fas fa-arrow-up-right-from-square" aria-hidden="true"></i>
                                    <span class="is-sr-only">(åbner i ny fane)</span>
                                </a>
                            </li>
                        </ul>
                        <div v-else class="activity-detail-empty">
                            <i class="far fa-folder-open" aria-hidden="true"></i>
                            <p>Saml aktivitetens dokumenter og regneark her.</p>
                            <BButton v-if="activity.canEdit" size="is-small" @click="showFiles">Opret den første fil</BButton>
                        </div>
                        <p v-if="!filesLoading && !filesError && overviewFiles.length > 6" class="activity-detail-muted mt-3">Viser 6 af {{ overviewFiles.length }} filer og mapper i aktivitetens hovedmappe.</p>
                    </section>
                </div>
                <div class="activity-detail-form" v-else-if="activeTab === 'catalog'">
                    <BField label="Katalognavn">
                        <BInput :disabled="!activity.canEdit || saving || reviewBusy" v-model="form.catalog.name" placeholder="Katalognavn" />
                    </BField>
                    <BField label="Resumé">
                        <BInput :disabled="!activity.canEdit || saving || reviewBusy" v-model="form.catalog.summary" placeholder="Kort resumé" />
                    </BField>
                    <BField label="Beskrivelse">
                        <BInput :disabled="!activity.canEdit || saving || reviewBusy" v-model="form.catalog.description" type="textarea" placeholder="Beskrivelse" />
                    </BField>
                </div>
                <div class="activity-detail-form activity-detail-files" v-else-if="activeTab === 'files'">
                    <h2 class="title is-5 mb-0">Filer</h2>
                    <BNotification v-if="!googleConnected" type="is-warning" :closable="false">
                        Google Drev er ikke forbundet.
                        <router-link v-if="canManageGoogle" to="/admin/google">Forbind Google Drev</router-link>
                    </BNotification>
                    <template v-else>
                        <section v-if="activity.canEdit" class="activity-detail-files-creation">
                            <h3 class="has-text-weight-semibold mb-2">Opret fil eller mappe</h3>
                            <p class="activity-detail-files-help mb-4">Filen eller mappen gemmes med det samme i Google Drev. Brug Gem aktivitet til ændringer på de øvrige faner.</p>
                            <form class="activity-detail-files-create" @submit.prevent="createFile">
                                <BField class="activity-detail-files-name" label="Navn">
                                    <BInput v-model="fileTitle" :placeholder="fileTitlePlaceholder" />
                                </BField>
                                <BField class="activity-detail-files-kind" label="Type">
                                    <BSelect v-model="fileKind" expanded>
                                        <option value="doc">Dokument</option>
                                        <option value="sheet">Regneark</option>
                                        <option value="slides">Præsentation</option>
                                        <option value="folder">Mappe</option>
                                    </BSelect>
                                </BField>
                                <BButton
                                    type="is-primary"
                                    icon-left="plus"
                                    :loading="creatingFile"
                                    :disabled="creatingFile || reviewBusy"
                                    native-type="submit"
                                >
                                    Opret
                                </BButton>
                            </form>
                        </section>
                        <section class="activity-detail-files-body">
                            <nav class="activity-detail-files-path" v-if="folderPath.length > 0" aria-label="Mappeplacering">
                                <button type="button" class="activity-detail-files-crumb" @click="goToFolder(0)">
                                    <i class="far fa-folder-open"></i>
                                    <span>{{ activity?.name || 'Aktiviteten' }}</span>
                                </button>
                                <template v-for="(folder, index) in folderPath" :key="folder.id">
                                    <i class="fas fa-chevron-right activity-detail-files-crumb-separator"></i>
                                    <button type="button" class="activity-detail-files-crumb" :disabled="index === folderPath.length - 1" @click="goToFolder(index + 1)">
                                        <span>{{ folder.name }}</span>
                                    </button>
                                </template>
                            </nav>
                            <h3 class="has-text-weight-semibold mb-3">Filer og mapper ({{ files.length }})</h3>
                            <BTable :data="files" :loading="filesLoading" hoverable>
                                <BTableColumn field="name" label="Navn" v-slot="props">
                                    <button v-if="props.row.kind === 'folder'" type="button" class="activity-detail-files-link activity-detail-files-folder" :title="`Åbn mappen ${props.row.name}`" @click="openFolder(props.row)">
                                        <i class="far fa-folder" aria-hidden="true"></i>
                                        <span>{{ props.row.name }}</span>
                                        <i class="fas fa-chevron-right" aria-hidden="true"></i>
                                    </button>
                                    <a v-else class="activity-detail-files-link" :href="props.row.url" target="_blank" rel="noopener" :title="`Åbn ${props.row.name} i Google (ny fane)`">
                                        <i :class="fileIcon(props.row.kind)" aria-hidden="true"></i>
                                        <span>{{ props.row.name }}</span>
                                        <i class="fas fa-arrow-up-right-from-square" aria-hidden="true"></i>
                                        <span class="is-sr-only">(åbner i ny fane)</span>
                                    </a>
                                </BTableColumn>
                                <BTableColumn field="createdBy" label="Oprettet af" v-slot="props">
                                    <span class="activity-detail-files-meta">{{ props.row.createdBy || '—' }}</span>
                                </BTableColumn>
                                <BTableColumn field="modifiedAt" label="Sidst ændret" v-slot="props">
                                    <span class="activity-detail-files-meta">{{ props.row.modifiedAt ? formatDate(props.row.modifiedAt) : '—' }}</span>
                                </BTableColumn>
                                <template #empty>
                                    <p class="activity-detail-files-empty">{{ activity.canEdit ? 'Der er endnu ingen filer eller mapper her. Opret den første ovenfor.' : 'Der er endnu ingen filer eller mapper her.' }}</p>
                                </template>
                            </BTable>
                        </section>
                    </template>
                </div>
            </div>
        </div>
        <BNotification v-if="saveConflict" type="is-warning" :closable="false" class="mt-4">
            Aktiviteten er ændret af en anden. Genindlæs og gennemgå den nyeste version, før du fortsætter.
            <BButton size="is-small" class="ml-2" :disabled="saving || reviewBusy" @click="confirmReloadActivity">Genindlæs aktivitet</BButton>
        </BNotification>
        <div v-if="activity.canEdit && hasChanges" class="activity-detail-actions">
            <span class="activity-detail-muted" role="status">Du har ændringer, der ikke er gemt.</span>
            <BButton type="is-success" icon-left="check" :loading="saving" :disabled="saving || reviewBusy || editingName" @click="saveActivity">
                Gem aktivitet
            </BButton>
        </div>
    </div>
</template>
<script lang="ts" setup>
import { useActivityStore } from '@/Stores/ActivityStore';
import { useMeStore } from '@/Stores/MeStore';
import GoogleService from '@/Services/GoogleService';
import ActivityService from '@/Services/ActivityService';
import ActivityReviewPanel from '@/Components/Activity/ActivityReviewPanel.vue';
import { getErrorMessage, getErrorStatus } from '@/lib/api';
import { formatDate } from '@/lib/util';
import { BButton, BField, BInput, BNotification, BSelect, BTable, BTableColumn, useDialog, useToast } from 'buefy';
import { storeToRefs } from 'pinia';
import { reactive, ref, computed, watch, nextTick } from 'vue';
import type { ActivityFileDto, ActivityFileKind, CatalogDataDto } from '@/types';

interface ActivityFormDto {
    id: number;
    name: string;
    budget: number;
    reviewVersion: number;
    catalog: CatalogDataDto;
}

const Toast = useToast();
const Dialog = useDialog();
const activityService = new ActivityService();
const activityStore = useActivityStore();
const meStore = useMeStore();
const googleService = new GoogleService();
const { SelectedActivity, FILES } = storeToRefs(activityStore);

const activeTab = ref('overview');
const editingName = ref(false);
const nameDraft = ref('');
const nameInput = ref<InstanceType<typeof BInput> | null>(null);
const overviewFiles = ref<ActivityFileDto[]>([]);
const filesLoading = ref(true);
const filesError = ref(false);
const saving = ref(false);
const reviewBusy = ref(false);
const saveConflict = ref(false);
const creatingFile = ref(false);
const fileTitle = ref('');
const fileKind = ref<ActivityFileKind>('doc');
const googleConnected = ref(true);
const folderPath = ref<{ id: string; name: string }[]>([]);
const hasFiles = ref(false);

const isActivityAdmin = computed(() => meStore.ME.appRoles.includes('ActivityAdmin'));

const canManageGoogle = computed(() => meStore.ME.appRoles.includes('AdminAccess'));

const form = reactive<ActivityFormDto>({
    id: 0,
    name: '',
    budget: 0,
    reviewVersion: 0,
    catalog: { id: 0, name: '', summary: '', description: '' }
});

const activity = SelectedActivity;
const files = FILES;

const startNameEdit = async () => {
    if (!activity.value?.canEdit || reviewBusy.value) return;
    nameDraft.value = form.name;
    editingName.value = true;
    await nextTick();
    nameInput.value?.focus();
};

const applyName = () => {
    if (!nameDraft.value.trim()) return;
    form.name = nameDraft.value.trim();
    editingName.value = false;
};

const missingCatalogText = computed(() => {
    const missing = [];
    if (!form.catalog.summary.trim()) missing.push('Resumé');
    if (!form.catalog.description.trim()) missing.push('beskrivelse');
    return missing.join(' og ').replace(/^beskrivelse/, 'Beskrivelse');
});

const formattedBudget = computed(() => new Intl.NumberFormat('da-DK', {
    style: 'currency', currency: 'DKK', minimumFractionDigits: 0, maximumFractionDigits: 2
}).format(form.budget));

const hasChanges = computed(() => {
    if (!activity.value) return false;
    const catalog = activity.value.catalog ?? { id: 0, name: '', summary: '', description: '' };
    return form.name !== (activity.value.name ?? "")
        || form.catalog.name !== (catalog.name ?? "")
        || form.catalog.summary !== (catalog.summary ?? "")
        || form.catalog.description !== (catalog.description ?? "");
});

const showFiles = async () => {
    activeTab.value = 'files';
    if (folderPath.value.length) await goToFolder(0);
};

const showFolder = async (file: ActivityFileDto) => {
    if (!activity.value || filesLoading.value) return;
    activeTab.value = 'files';
    folderPath.value = [{ id: file.id, name: file.name }];
    if (!await loadFolder(activity.value.id)) {
        folderPath.value = [];
        await loadFolder(activity.value.id);
    }
};

const retryFiles = async () => {
    folderPath.value = [];
    await loadFiles(form.id);
};

const currentFolderId = computed(() => folderPath.value.length > 0 ? folderPath.value[folderPath.value.length - 1].id : null);

const fileTitlePlaceholder = computed(() => fileKind.value === 'folder' ? 'Mappens navn' : form.name || 'Navn');

const loadFolder = async (activityId: number) => {
    filesLoading.value = true;
    filesError.value = false;
    try {
        await activityStore.GET_FILES(activityId, currentFolderId.value);
    } catch {
        filesError.value = true;
        Toast.open({
            message: 'Filerne kunne ikke hentes fra Google Drev',
            type: 'is-danger'
        });
        return false;
    } finally {
        filesLoading.value = false;
    }

    if (folderPath.value.length === 0) {
        overviewFiles.value = [...files.value];
        hasFiles.value = files.value.length > 0;
    }

    return true;
};

const loadFiles = async (activityId: number) => {
    filesLoading.value = true;
    filesError.value = false;
    const status = await googleService.getStatus().catch(() => null);

    if (status) {
        googleConnected.value = status.connected;
    }

    if (!status) {
        filesLoading.value = false;
        filesError.value = true;
        return;
    }

    if (!googleConnected.value) {
        filesLoading.value = false;
        return;
    }

    await loadFolder(activityId);
};

const openFolder = async (file: ActivityFileDto) => {
    if (!activity.value || file.kind !== 'folder') return;

    const previous = folderPath.value;
    folderPath.value = [...previous, { id: file.id, name: file.name }];

    if (!await loadFolder(activity.value.id)) {
        folderPath.value = previous;
    }
};

const goToFolder = async (depth: number) => {
    if (!activity.value || depth === folderPath.value.length) return;

    const previous = folderPath.value;
    folderPath.value = previous.slice(0, depth);

    if (!await loadFolder(activity.value.id)) {
        folderPath.value = previous;
    }
};

watch(SelectedActivity, (activity) => {
    if (!activity) return;

    editingName.value = false;
    saveConflict.value = false;
    form.id = activity.id;
    form.name = activity.name ?? "";
    form.budget = activity.budget;
    form.reviewVersion = activity.reviewVersion;
    form.catalog = {
        ...(activity.catalog ?? { id: 0 }),
        name: activity.catalog?.name ?? '',
        summary: activity.catalog?.summary ?? '',
        description: activity.catalog?.description ?? ''
    };
    overviewFiles.value = [];
    fileTitle.value = '';
    folderPath.value = [];
    hasFiles.value = false;
    loadFiles(activity.id);
}, { immediate: true });

const fileIcon = (kind: ActivityFileKind) => {
    if (kind === 'sheet') return 'fas fa-table';
    if (kind === 'slides') return 'fas fa-display';
    if (kind === 'folder') return 'far fa-folder';
    if (kind === 'doc') return 'far fa-file-lines';

    return 'far fa-file';
};

const activityTabFilled = computed(() => form.name.trim().length > 0);

const catalogTabFilled = computed(
    () => form.catalog.name.trim().length > 0
        || form.catalog.summary.trim().length > 0
        || form.catalog.description.trim().length > 0
);

const missingTabs = computed(() => [
    ...(!activityTabFilled.value ? ['Navn'] : []),
    ...(!catalogTabFilled.value ? ['Katalog'] : [])
]);

const tabs = computed(() => [
    { key: 'overview', label: 'Overblik', icon: 'fas fa-border-all', filled: false },
    { key: 'catalog', label: 'Katalog', icon: 'fas fa-globe', filled: catalogTabFilled.value },
    { key: 'files', label: 'Filer', icon: 'fas fa-folder-open', filled: hasFiles.value }
]);

const createFile = async () => {
    if (!activity.value?.canEdit || creatingFile.value || reviewBusy.value) return;

    creatingFile.value = true;
    try {
        await activityStore.ADD_FILE(activity.value.id, {
            title: fileTitle.value.trim() || null,
            kind: fileKind.value,
            folderId: currentFolderId.value
        });

        const createdFolder = fileKind.value === 'folder';

        fileTitle.value = '';

        if (folderPath.value.length === 0) {
            overviewFiles.value = [...files.value];
            hasFiles.value = files.value.length > 0;
        }

        Toast.open({
            message: createdFolder ? 'Mappen er oprettet' : 'Filen er oprettet',
            type: 'is-success'
        });
    } catch (error: any) {
        const status = error?.response?.status;
        const message = typeof error?.response?.data === 'string' && error.response.data
            ? error.response.data
            : status === 403
                ? 'Du har ikke rettigheder til at oprette filer og mapper til aktiviteten'
                : 'Der skete en fejl ved oprettelse af filen eller mappen';

        if (status === 503) {
            googleConnected.value = false;
        }

        Toast.open({
            message,
            type: 'is-danger'
        });
    } finally {
        creatingFile.value = false;
    }
};

const confirmReloadActivity = () => {
    Dialog.confirm({
        title: 'Genindlæs aktivitet',
        message: 'Dine lokale ændringer kasseres, og den nyeste version hentes. Vil du fortsætte?',
        confirmText: 'Genindlæs',
        cancelText: 'Behold ændringer',
        onConfirm: async () => {
            saving.value = true;
            try {
                const fresh = await activityService.getActivity(form.id);
                if (fresh) activityStore.SelectedActivity = fresh;
                else Toast.open({ message: 'Aktiviteten kunne ikke genindlæses. Prøv igen.', type: 'is-danger' });
            } finally {
                saving.value = false;
            }
        }
    });
};

const saveActivity = async () => {
    if (!activity.value?.canEdit || saving.value || reviewBusy.value || editingName.value) return;

    saving.value = true;
    try {
        const success = await activityStore.UPDATE_ACTIVITY({ ...form });
        Toast.open({
            message: success ? 'Aktiviteten er opdateret' : 'Der skete en fejl ved opdatering af aktiviteten',
            type: success ? 'is-success' : 'is-danger'
        });
    } catch (error) {
        saveConflict.value = getErrorStatus(error) === 409;
        Toast.open({
            message: getErrorMessage(error, 'Der skete en fejl ved opdatering af aktiviteten'),
            type: 'is-danger',
            duration: 7000
        });
    } finally {
        saving.value = false;
    }
};
</script>
<style lang="scss">
.activity-detail {
    width: calc(100vw - 4rem);
    padding: 1.5rem 2rem;
    margin: 1rem 2rem;

    &-heading {
        min-width: 0;
        flex: 1;
    }

    &-heading-row {
        display: flex;
        align-items: center;
        gap: 0.5rem;
    }

    &-eyebrow,
    &-muted {
        color: #737b86;
        font-size: 0.85rem;
    }

    &-eyebrow {
        margin-bottom: 0.35rem;
    }

    &-overview {
        display: grid;
        grid-template-columns: minmax(0, 1.3fr) minmax(16rem, 1fr);
        gap: 1.25rem;
        align-items: start;
    }

    &-summary {
        min-width: 0;
        padding: 1.5rem;
        border: 1px solid #e9edf1;
        border-radius: 10px;
    }

    &-section-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        flex-wrap: wrap;
        gap: 0.5rem;
        margin-bottom: 1.25rem;

        h2 {
            font-weight: 600;
            font-size: 1rem;
        }
    }

    &-catalog-title {
        font-size: 1.35rem;
        font-weight: 600;
        overflow-wrap: anywhere;
    }

    &-catalog-intro {
        margin-top: 0.75rem;
        color: #596575;
        white-space: pre-wrap;
        overflow-wrap: anywhere;
    }

    &-catalog-description {
        margin-top: 1rem;
        line-height: 1.65;
        white-space: pre-wrap;
        overflow-wrap: anywhere;
        display: -webkit-box;
        -webkit-line-clamp: 5;
        -webkit-box-orient: vertical;
        overflow: hidden;
    }

    &-header-meta {
        display: flex;
        align-items: center;
        flex-wrap: wrap;
        gap: 1.5rem;
    }

    &-budget {
        display: flex;
        align-items: baseline;
        gap: 0.65rem;
        color: #737b86;
        font-size: 0.85rem;

        strong {
            color: #485365;
            font-size: 1rem;
            font-weight: 600;
        }
    }

    &-name-editor {
        width: min(32rem, 100%);

        .field {
            margin-bottom: 0.5rem;
        }
    }

    &-name-actions {
        display: flex;
        gap: 0.5rem;
    }

    &-catalog-incomplete {
        margin-top: 1rem;
        color: #737b86;
        font-size: 0.85rem;

        .button {
            padding-left: 0;
            margin-top: 0.25rem;
        }
    }

    &-count {
        margin-left: 0.4rem;
        color: #737b86;
        font-size: 0.85rem;
        font-weight: 400;
    }

    &-quick-files {
        display: grid;
        grid-template-columns: minmax(0, 1fr);
        gap: 0;
    }

    &-quick-file {
        display: flex;
        align-items: center;
        gap: 0.75rem;
        width: 100%;
        padding: 0.65rem 0.5rem;
        border: 0;
        border-radius: 4px;
        background: transparent;
        color: #485365;
        font: inherit;
        text-align: left;
        cursor: pointer;

        span {
            flex: 1;
            overflow-wrap: anywhere;
            min-width: 0;
        }

        i:last-of-type {
            font-size: 0.75rem;
            color: #8b929c;
        }

        &:hover {
            background: #f0f2f5;
        }
    }

    &-empty {
        display: flex;
        flex-direction: column;
        align-items: flex-start;
        gap: 0.75rem;
        color: #737b86;
        padding: 0.5rem 0;
    }

    &-header {
        display: flex;
        align-items: center;
        justify-content: space-between;
        gap: 1rem;
        margin-bottom: 1rem;

        .tag i {
            margin-right: 0.35rem;
        }
    }

    &-body {
        display: flex;
        flex-direction: column;
        gap: 1.5rem;
    }

    &-nav {
        display: flex;
        flex-wrap: wrap;
        gap: 0.35rem;
        border-bottom: 1px solid #edf0f2;
        padding-bottom: 0.75rem;

        &-item {
            display: flex;
            align-items: center;
            gap: 0.5rem;
            padding: 0.55rem 0.85rem;
            border: 0;
            border-radius: 10px;
            background: transparent;
            color: #4a4a4a;
            font-family: inherit;
            font-size: 0.9rem;
            text-align: left;
            cursor: pointer;
            transition: background-color 0.15s ease, color 0.15s ease;

            &:hover {
                background-color: #f2f2f2;
                color: #363636;

                .activity-detail-nav-icon {
                    color: #4a4a4a;
                }
            }

            &.is-active {
                background-color: #e8e8e8;
                color: #1f1f1f;
                font-weight: 600;

                .activity-detail-nav-icon {
                    color: #1f1f1f;
                }
            }
        }

        &-icon {
            flex: 0 0 1.5rem;
            font-size: 0.95rem;
            color: #7a7a7a;
            text-align: center;
            transition: color 0.15s ease;
        }

        &-label {
            flex: 1;
        }

        &-check {
            color: #34c759;
            font-size: 0.95rem;
        }
    }

    &-panel {
        flex: 1;
        min-width: 0;
    }

    &-form {
        padding: 0.25rem 0.25rem 0.5rem;
        max-width: 36rem;

        box-shadow: none;
        border: 1px solid rgba(0, 0, 0, 0.1);
        padding: 1rem;
        border-radius: 10px;

        .label {
            font-size: 1.05rem;
        }

        .input,
        .textarea {
            font-size: 1.05rem;
        }
    }

    &-files {
        max-width: none;
        display: flex;
        flex-direction: column;
        gap: 1rem;

        &-creation {
            padding-bottom: 1.5rem;
            border-bottom: 1px solid rgba(0, 0, 0, 0.08);
        }

        &-help,
        &-meta {
            color: #666;
            font-size: 0.95rem;
        }

        &-create {
            display: flex;
            flex-wrap: wrap;
            align-items: flex-end;
            gap: 1rem;
            max-width: 56rem;

            .field {
                margin-bottom: 0;
            }
        }

        &-name {
            flex: 1 1 18rem;
            min-width: 0;
        }

        &-kind {
            flex: 0 1 12rem;
        }

        &-body {
            min-width: 0;

            .table {
                width: 100%;
            }

            td {
                vertical-align: middle;
            }
        }

        &-link {
            display: inline-flex;
            align-items: center;
            gap: 0.75rem;
            padding: 0.35rem 0;
            border: 0;
            background: none;
            color: #485fc7;
            font: inherit;
            text-align: left;
            cursor: pointer;
            overflow-wrap: anywhere;

            .fa-arrow-up-right-from-square,
            .fa-chevron-right {
                font-size: 0.8rem;
            }
        }

        &-path {
            display: flex;
            flex-wrap: wrap;
            align-items: center;
            gap: 0.35rem;
            margin-bottom: 0.75rem;
            color: #666;
            font-size: 0.95rem;
        }

        &-crumb {
            display: inline-flex;
            align-items: center;
            gap: 0.4rem;
            padding: 0.1rem 0.35rem;
            border: 0;
            border-radius: 6px;
            background: none;
            color: #485fc7;
            font: inherit;
            cursor: pointer;

            &:hover:not(:disabled) {
                background-color: #f2f2f2;
            }

            &:disabled {
                color: #363636;
                cursor: default;
            }
        }

        &-crumb-separator {
            color: #999;
            font-size: 0.7rem;
        }

        &-empty {
            color: #666;
            padding: 1.5rem 0;
        }
    }

    @media (max-width: 768px) {
        width: auto;
        margin: 1rem;
        padding: 1rem;

        &-overview,
        &-quick-files {
            grid-template-columns: 1fr;
        }

        &-summary {
            padding: 1rem;
        }

        &-header {
            flex-wrap: wrap;

            .tag {
                height: auto;
                white-space: normal;
            }
        }

        &-body {
            flex-direction: column;
            gap: 1rem;
            min-height: 0;
        }

        &-nav {
            flex: 0 0 auto;
            border-right: 0;
            border-bottom: 1px solid rgba(0, 0, 0, 0.08);
            padding: 0 0 1rem;

            &-item {
                padding: 0.75rem;
                font-size: 1rem;
            }
        }

        &-files-kind {
            flex: 1 1 12rem;
        }
    }

    &-actions {
        border-top: 1px solid rgba(0, 0, 0, 0.1);
        padding-top: 1rem;
        margin-top: 0.5rem;
        display: flex;
        align-items: center;
        flex-wrap: wrap;
        gap: 1rem;
        justify-content: space-between;
    }
}
</style>
