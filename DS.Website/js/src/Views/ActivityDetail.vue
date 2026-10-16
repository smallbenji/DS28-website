<template>
    <div class="activity-detail ui-card" v-if="activity">
        <div class="activity-detail-header">
            <h1 class="title is-4 mb-0">{{ form.name || 'Aktivitet' }}</h1>
            <span
                class="tag is-success is-light is-medium"
                v-if="activityTabFilled && catalogTabFilled"
            >
                <i class="fas fa-check"></i> Udfyldt
            </span>
            <span class="tag is-danger is-light is-medium" v-else>
                <i class="fas fa-circle-exclamation"></i> Mangler: {{ missingTabs.join(', ') }}
            </span>
        </div>
        <div class="activity-detail-body">
            <nav class="activity-detail-nav">
                <button
                    v-for="(tab, index) in tabs"
                    :key="tab.key"
                    type="button"
                    class="activity-detail-nav-item"
                    :class="{ 'is-active': activeTab === index }"
                    @click="activeTab = index"
                >
                    <i class="activity-detail-nav-icon" :class="tab.icon"></i>
                    <span class="activity-detail-nav-label">{{ tab.label }}</span>
                    <i class="far fa-circle-check activity-detail-nav-check" v-if="tab.filled"></i>
                </button>
            </nav>
            <div class="activity-detail-panel">
                <div class="activity-detail-form" v-if="activeTab === 0">
                    <BField label="Navn">
                        <BInput v-model="form.name" placeholder="Aktivitetens navn" />
                    </BField>
                    <BField label="Budget">
                        <BInput v-model.number="form.budget" type="number" placeholder="0" disabled />
                    </BField>
                </div>
                <div class="activity-detail-form" v-else-if="activeTab === 1">
                    <BField label="Katalognavn">
                        <BInput v-model="form.catalog.name" placeholder="Katalognavn" />
                    </BField>
                    <BField label="Resumé">
                        <BInput v-model="form.catalog.summary" placeholder="Kort resumé" />
                    </BField>
                    <BField label="Beskrivelse">
                        <BInput v-model="form.catalog.description" type="textarea" placeholder="Beskrivelse" />
                    </BField>
                </div>
                <div class="activity-detail-form activity-detail-files" v-else-if="activeTab === 2">
                    <h2 class="title is-5 mb-0">Filer</h2>
                    <BNotification v-if="!googleConnected" type="is-warning" :closable="false">
                        Google Drev er ikke forbundet.
                        <router-link v-if="canManageGoogle" to="/admin/google">Forbind Google Drev</router-link>
                    </BNotification>
                    <template v-else>
                        <section class="activity-detail-files-creation">
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
                                    :disabled="creatingFile"
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
                            <BTable :data="files" hoverable>
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
                                    <p class="activity-detail-files-empty">Der er endnu ingen filer eller mapper her. Opret den første ovenfor.</p>
                                </template>
                            </BTable>
                        </section>
                    </template>
                </div>
            </div>
        </div>
        <div class="activity-detail-actions">
            <BButton type="is-success" icon-left="check" :loading="saving" :disabled="saving" @click="saveActivity">
                Gem aktivitet
            </BButton>
        </div>
    </div>
</template>
<script lang="ts" setup>
import { useActivityStore } from '@/Stores/ActivityStore';
import { useMeStore } from '@/Stores/MeStore';
import GoogleService from '@/Services/GoogleService';
import { formatDate } from '@/lib/util';
import { BButton, BField, BInput, BNotification, BSelect, BTable, BTableColumn, useToast } from 'buefy';
import { storeToRefs } from 'pinia';
import { reactive, ref, computed, watch } from 'vue';
import type { ActivityFileDto, ActivityFileKind, CatalogDataDto } from '@/types';

interface ActivityFormDto {
    id: number;
    name: string;
    budget: number;
    catalog: CatalogDataDto;
}

const Toast = useToast();
const activityStore = useActivityStore();
const meStore = useMeStore();
const googleService = new GoogleService();
const { SelectedActivity, FILES } = storeToRefs(activityStore);

const activeTab = ref(0);
const saving = ref(false);
const creatingFile = ref(false);
const fileTitle = ref('');
const fileKind = ref<ActivityFileKind>('doc');
const googleConnected = ref(true);
const folderPath = ref<{ id: string; name: string }[]>([]);
const hasFiles = ref(false);

const canManageGoogle = computed(() => meStore.ME.appRoles.includes('AdminAccess'));

const form = reactive<ActivityFormDto>({
    id: 0,
    name: '',
    budget: 0,
    catalog: { id: 0, name: '', summary: '', description: '' }
});

const activity = SelectedActivity;
const files = FILES;

const currentFolderId = computed(() => folderPath.value.length > 0 ? folderPath.value[folderPath.value.length - 1].id : null);

const fileTitlePlaceholder = computed(() => fileKind.value === 'folder' ? 'Mappens navn' : form.name || 'Navn');

const loadFolder = async (activityId: number) => {
    try {
        await activityStore.GET_FILES(activityId, currentFolderId.value);
    } catch {
        Toast.open({
            message: 'Filerne kunne ikke hentes fra Google Drev',
            type: 'is-danger'
        });
        return false;
    }

    if (folderPath.value.length === 0) {
        hasFiles.value = files.value.length > 0;
    }

    return true;
};

const loadFiles = async (activityId: number) => {
    const status = await googleService.getStatus().catch(() => null);

    if (status) {
        googleConnected.value = status.connected;
    }

    if (!googleConnected.value) return;

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

    form.id = activity.id;
    form.name = activity.name;
    form.budget = activity.budget;
    form.catalog = activity.catalog ?? { id: 0, name: '', summary: '', description: '' };
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
    ...(!activityTabFilled.value ? ['Aktivitet'] : []),
    ...(!catalogTabFilled.value ? ['Katalog'] : [])
]);

const tabs = computed(() => [
    { key: 'activity', label: 'Aktivitet', icon: 'fas fa-calendar-days', filled: activityTabFilled.value },
    { key: 'catalog', label: 'Katalog', icon: 'fas fa-globe', filled: catalogTabFilled.value },
    { key: 'files', label: 'Filer', icon: 'fas fa-folder-open', filled: hasFiles.value }
]);

const createFile = async () => {
    if (!activity.value || creatingFile.value) return;

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

const saveActivity = async () => {
    saving.value = true;
    const success = await activityStore.UPDATE_ACTIVITY({ ...form });
    saving.value = false;

    if (success) {
        Toast.open({
            message: 'Aktiviteten er opdateret',
            type: 'is-success'
        });
    } else {
        Toast.open({
            message: 'Der skete en fejl ved opdatering af aktiviteten',
            type: 'is-danger'
        });
    }
};
</script>
<style lang="scss">
.activity-detail {
    width: calc(100vw - 4rem);
    // min-height: calc(100vh - 7rem);
    padding: 1.5rem 2rem;
    margin: 1rem 2rem;

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
        align-items: stretch;
        gap: 2rem;
        min-height: calc(100vh - 18rem);
    }

    &-nav {
        flex: 0 0 19rem;
        display: flex;
        flex-direction: column;
        gap: 0.3rem;
        border-right: 1px solid rgba(0, 0, 0, 0.08);
        padding-right: 1.25rem;

        &-item {
            display: flex;
            align-items: center;
            gap: 1.1rem;
            width: 100%;
            padding: 1rem 1.15rem;
            border: 0;
            border-radius: 10px;
            background: transparent;
            color: #4a4a4a;
            font-family: inherit;
            font-size: 1.15rem;
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
            font-size: 1.35rem;
            color: #7a7a7a;
            text-align: center;
            transition: color 0.15s ease;
        }

        &-label {
            flex: 1;
        }

        &-check {
            color: #34c759;
            font-size: 1.4rem;
        }
    }

    &-panel {
        flex: 1;
        min-width: 0;
    }

    &-form {
        padding: 0.25rem 0.25rem 0.5rem;
        max-width: 36rem;

        box-shadow: 5px 5px 5px 0 rgba(0, 0, 0, 0.1);
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
        justify-content: flex-end;
    }
}
</style>
