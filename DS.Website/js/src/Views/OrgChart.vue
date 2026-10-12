<template>
    <div class="org-chart">
        <div class="org-chart-head">
            <h1 class="title is-4 mb-2">Organisationsdiagram</h1>
            <p class="subtitle is-6">Stillinger og hvem der beklæder dem.</p>
            <BButton v-if="isSysAdmin" type="is-primary" size="is-small" @click="rootOpen = true">
                Ny rodstilling
            </BButton>
        </div>

        <p v-if="loading" role="status">Indlæser organisation…</p>

        <p v-else-if="roots.length === 0" class="has-text-grey">Ingen stillinger endnu.</p>

        <div v-else class="org-tree-viewport" tabindex="0" role="region" aria-label="Organisationsdiagram, rul vandret og lodret for at se alle stillinger">
            <ul class="org-tree">
                <OrgNode
                    v-for="root in roots"
                    :key="root.position.id"
                    :node="root"
                    @select="openDrawer"
                />
            </ul>
        </div>

        <OrgDrawer
            v-model="drawerOpen"
            :position="selectedPosition"
            :positions="positions"
            :assignable-groups="assignableGroups"
            :users="users"
            :current-user-id="currentUserId"
            :is-sys-admin="isSysAdmin"
            @refresh="handleRefresh"
        />

        <BModal v-model="rootOpen" has-modal-card>
            <div class="modal-card">
                <header class="modal-card-head">
                    <p class="modal-card-title">Ny rodstilling</p>
                    <button type="button" class="delete" @click="rootOpen = false" />
                </header>
                <section class="modal-card-body">
                    <BField label="Titel">
                        <BInput v-model="rootTitle" placeholder="Fx Lejrchef" />
                    </BField>
                    <BField label="Farve">
                        <div class="org-drawer-add">
                            <span class="org-color-swatch" :style="swatchStyle(rootColor)" />
                            <BSelect v-model="rootColor" expanded>
                                <option v-for="color in ORG_COLORS" :key="color.value ?? 'none'" :value="color.value">
                                    {{ color.label }}
                                </option>
                            </BSelect>
                        </div>
                    </BField>
                    <BField label="Grupper">
                        <BSelect v-model="rootGroups" multiple expanded>
                            <option v-for="group in assignableGroups" :key="group" :value="group">
                                {{ groupTitle(group) }}
                            </option>
                        </BSelect>
                    </BField>
                </section>
                <footer class="modal-card-foot">
                    <div class="buttons">
                        <BButton type="is-primary" :loading="saving" @click="createRoot">Opret</BButton>
                        <BButton @click="rootOpen = false">Annuller</BButton>
                    </div>
                </footer>
            </div>
        </BModal>
    </div>
</template>

<script lang="ts" setup>
import { computed, onMounted, ref } from 'vue';
import { BButton, BField, BInput, BModal, BSelect } from 'buefy';
import type { OrgNodeModel, OrgPositionDto, UserDto } from '@/types';
import { groupTitle } from '@/types';
import OrgService from '@/Services/OrgService';
import UserService from '@/Services/UserService';
import OrgNode from '@/Components/Org/OrgNode.vue';
import OrgDrawer from '@/Components/Org/OrgDrawer.vue';
import { ORG_COLORS } from '@/Components/Org/orgPalette';
import { useMeStore } from '@/Stores/MeStore';

const service = new OrgService();
const userService = new UserService();
const meStore = useMeStore();

const positions = ref<OrgPositionDto[]>([]);
const assignableGroups = ref<string[]>([]);
const users = ref<UserDto[]>([]);
const loading = ref(true);
const saving = ref(false);
const rootOpen = ref(false);
const rootTitle = ref('');
const rootGroups = ref<string[]>([]);
const rootColor = ref<string | null>(null);
const selectedId = ref<number | null>(null);
const drawerOpen = ref(false);

const currentUserId = computed(() => meStore.ME.id);
const isSysAdmin = computed(() => (meStore.ME.roles ?? []).includes('SysAdmin'));
const selectedPosition = computed(
    () => positions.value.find((position) => position.id === selectedId.value) ?? null
);

function swatchStyle(color: string | null): Record<string, string> {
    return color ? { backgroundColor: color } : {};
}

const roots = computed<OrgNodeModel[]>(() => {
    const paByOwner = new Map<number, OrgPositionDto>();
    positions.value.forEach((position) => {
        if (position.paForPositionId !== null) paByOwner.set(position.paForPositionId, position);
    });

    const map = new Map<number, OrgNodeModel>();
    positions.value
        .filter((position) => position.paForPositionId === null)
        .forEach((position) => map.set(position.id, {
            position,
            pa: paByOwner.get(position.id) ?? null,
            children: []
        }));

    const result: OrgNodeModel[] = [];
    map.forEach((node) => {
        const parentId = node.position.parentPositionId;
        const parent = parentId === null ? undefined : map.get(parentId);
        if (parent) parent.children.push(node);
        else result.push(node);
    });

    return result;
});

async function load() {
    loading.value = true;
    try {
        await Promise.all([loadOrg(), loadUsers()]);
    } finally {
        loading.value = false;
    }
}

async function loadOrg() {
    const [orgPositions, groups] = await Promise.all([
        service.getPositions(),
        service.getAssignableGroups()
    ]);
    positions.value = orgPositions;
    assignableGroups.value = groups;
}

async function loadUsers() {
    users.value = await userService.getUsers();
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
        const success = await service.createPosition({
            title: rootTitle.value.trim(),
            parentPositionId: null,
            groups: rootGroups.value,
            color: rootColor.value
        });
        if (success) {
            rootOpen.value = false;
            rootTitle.value = '';
            rootGroups.value = [];
            rootColor.value = null;
            await loadOrg();
        }
    } finally {
        saving.value = false;
    }
}

onMounted(load);
</script>

<style lang="scss" scoped>
.org-chart {
    display: flex;
    flex-direction: column;
    flex: 1;
    min-width: 0;
    min-height: 0;
    padding: 0 1rem 1rem;
}

.org-chart-head {
    flex-shrink: 0;
    margin-bottom: 1.5rem;
}

.org-chart :deep(.b-sidebar) {
    --bulma-sidebar-width: 20rem;
}

.org-tree-viewport {
    width: 100%;
    flex: 1;
    min-height: 0;
    overflow: auto;
    padding: 0.5rem 0 1rem;
}

.org-tree {
    width: max-content;
    min-width: 100%;
    display: flex;
    gap: 2rem;
    justify-content: center;
    align-items: flex-start;
    list-style: none;
    margin: 0;
    padding: 0;
}

.org-tree :deep(> .org-node) {
    align-items: center;
}

.org-drawer-add {
    display: flex;
    align-items: center;
    gap: 0.5rem;
}

.org-color-swatch {
    width: 1rem;
    height: 1rem;
    border-radius: 0.25rem;
    border: 1px solid hsl(0, 0%, 74%);
    flex-shrink: 0;
}
</style>
