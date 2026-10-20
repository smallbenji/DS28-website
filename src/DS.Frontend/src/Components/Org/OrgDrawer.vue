<template>
    <BSidebar v-model="open" right fullheight overlay type="is-light" :can-cancel="['escape', 'outside']">
        <div v-if="position" class="org-drawer">
            <header class="org-drawer-head">
                <p class="org-drawer-title">
                    {{ position.title }}
                </p>
                <button type="button" class="delete" aria-label="Luk" @click="open = false" />
            </header>

            <section class="org-drawer-section">
                <h3 class="org-drawer-heading">Stilling</h3>
                <template v-if="canEdit">
                    <BField label="Titel">
                        <BInput v-model="editTitle" />
                    </BField>
                    <BField label="Farve">
                        <div class="org-drawer-add">
                            <span class="org-color-swatch" :style="swatchStyle(editColor)" />
                            <BSelect v-model="editColor" expanded>
                                <option v-for="color in ORG_COLORS" :key="color.value ?? 'none'" :value="color.value">
                                    {{ color.label }}
                                </option>
                            </BSelect>
                        </div>
                    </BField>
                    <BField label="Grupper">
                        <BSelect v-model="editGroups" multiple expanded>
                            <option v-for="group in assignableGroups" :key="group" :value="group">
                                {{ groupTitle(group) }}
                            </option>
                        </BSelect>
                    </BField>
                    <BButton type="is-primary" size="is-small" :loading="loading" @click="saveEdit">
                        Gem
                    </BButton>
                </template>
                <b-taglist v-else-if="position.groups.length">
                    <b-tag v-for="group in position.groups" :key="group" size="is-small">
                        {{ groupTitle(group) }}
                    </b-tag>
                </b-taglist>
                <p v-else class="has-text-grey is-size-7">Ingen grupper</p>
            </section>

            <section class="org-drawer-section">
                <h3 class="org-drawer-heading">Indehavere</h3>
                <ul v-if="position.holders.length" class="org-drawer-holders">
                    <li v-for="holder in position.holders" :key="holder.userId">
                        <span class="org-holder-name">{{ holder.name }}</span>
                        <button
                            v-if="canManage && canRemoveHolder"
                            type="button"
                            class="delete is-small"
                            :aria-label="`Fjern ${holder.name}`"
                            @click="removeHolder(holder.userId)"
                        />
                    </li>
                </ul>
                <p v-else class="has-text-grey is-size-7">Ingen tilknyttet</p>

                <div v-if="canManage" class="org-drawer-add">
                    <BSelect v-model="selectedUserId" expanded>
                        <option :value="undefined">Vælg bruger…</option>
                        <option v-for="user in availableUsers" :key="user.id" :value="user.id">
                            {{ user.firstName }} {{ user.lastName }}
                        </option>
                    </BSelect>
                    <BButton size="is-small" :loading="loading" @click="addHolder">Tilknyt</BButton>
                </div>
            </section>

            <section v-if="canManage" class="org-drawer-section">
                <h3 class="org-drawer-heading">Inviter ny bruger</h3>
                <div class="org-drawer-add">
                    <BInput v-model="inviteEmail" type="email" placeholder="eksempel@mail.dk" />
                    <BButton size="is-small" :loading="loading" @click="sendInvite">Send</BButton>
                </div>
            </section>

            <section v-if="canManage && !isPa && !hasPa" class="org-drawer-section">
                <h3 class="org-drawer-heading">Personlig assistent</h3>
                <BButton size="is-small" :loading="loading" @click="addPa">Tilføj personlig assistent</BButton>
            </section>

            <section v-if="canManage && !isPa" class="org-drawer-section">
                <h3 class="org-drawer-heading">Flyt</h3>
                <div class="org-drawer-add">
                    <BSelect v-model="moveParentId" expanded>
                        <option v-if="isSysAdmin" :value="null">Ingen (rodstilling)</option>
                        <option v-for="candidate in moveCandidates" :key="candidate.id" :value="candidate.id">
                            {{ candidate.title }}
                        </option>
                    </BSelect>
                    <BButton size="is-small" :loading="loading" @click="saveMove">Flyt</BButton>
                </div>
            </section>

            <section v-if="canCreateChild && !isPa" class="org-drawer-section">
                <h3 class="org-drawer-heading">Ny understilling</h3>
                <BField label="Titel">
                    <BInput v-model="createChildTitle" placeholder="Fx Økonomiansvarlig" />
                </BField>
                <BField label="Farve">
                    <div class="org-drawer-add">
                        <span class="org-color-swatch" :style="swatchStyle(createChildColor)" />
                        <BSelect v-model="createChildColor" expanded>
                            <option v-for="color in ORG_COLORS" :key="color.value ?? 'none'" :value="color.value">
                                {{ color.label }}
                            </option>
                        </BSelect>
                    </div>
                </BField>
                <BField label="Grupper">
                    <BSelect v-model="createChildGroups" multiple expanded>
                        <option v-for="group in assignableGroups" :key="group" :value="group">
                            {{ groupTitle(group) }}
                        </option>
                    </BSelect>
                </BField>
                <BButton size="is-small" :loading="loading" @click="createChild">Opret</BButton>
            </section>

            <section v-if="canManage && (isPa || !position.isSystem)" class="org-drawer-section">
                <BButton size="is-small" type="is-danger" @click="confirmDelete">
                    {{ isPa ? 'Slet personlig assistent' : 'Slet stilling' }}
                </BButton>
            </section>
        </div>
    </BSidebar>
</template>

<script lang="ts" setup>
import { computed, ref, watch } from 'vue';
import { BButton, BField, BInput, BSelect, BSidebar, BTaglist, BTag, useDialog, useToast } from 'buefy';
import type { OrgPositionDto, UserDto } from '@/types';
import { groupTitle } from '@/types';
import OrgService from '@/Services/OrgService';
import { canCreateChild as canCreateChildPosition, canManagePosition } from './orgAccess';
import { ORG_COLORS } from './orgPalette';

const props = defineProps<{
    modelValue: boolean;
    position: OrgPositionDto | null;
    positions: OrgPositionDto[];
    assignableGroups: string[];
    users: UserDto[];
    currentUserId: string;
    isSysAdmin: boolean;
}>();

const emit = defineEmits<{
    (event: 'update:modelValue', value: boolean): void;
    (event: 'refresh'): void;
}>();

const service = new OrgService();
const Toast = useToast();
const Dialog = useDialog();

const loading = ref(false);
const editTitle = ref('');
const editGroups = ref<string[]>([]);
const editColor = ref<string | null>(null);
const moveParentId = ref<number | null>(null);
const inviteEmail = ref('');
const selectedUserId = ref<string | undefined>(undefined);
const createChildTitle = ref('');
const createChildGroups = ref<string[]>([]);
const createChildColor = ref<string | null>(null);

const open = computed({
    get: () => props.modelValue,
    set: (value: boolean) => emit('update:modelValue', value)
});

const position = computed(() => props.position);

const canManage = computed(
    () => position.value !== null && canManagePosition(position.value, props.positions, props.currentUserId, props.isSysAdmin)
);

const canEdit = computed(() => canManage.value && position.value !== null && !position.value.isSystem && position.value.paForPositionId === null);
const isPa = computed(() => position.value?.paForPositionId != null);
const hasPa = computed(
    () => position.value !== null && props.positions.some((candidate) => candidate.paForPositionId === position.value!.id)
);
const canCreateChild = computed(
    () => position.value !== null
        && canCreateChildPosition(position.value, props.positions, props.currentUserId, props.isSysAdmin)
);
const canRemoveHolder = computed(() => !position.value?.isSystem || (position.value?.holders.length ?? 0) > 1);

const availableUsers = computed(() => {
    const held = new Set(position.value?.holders.map((holder) => holder.userId) ?? []);
    return props.users.filter((user) => !held.has(user.id));
});

const descendantIds = computed(() => {
    const result = new Set<number>();
    const positionId = position.value?.id;
    if (positionId === undefined) return result;

    const stack = [positionId];
    while (stack.length > 0) {
        const current = stack.pop() as number;
        props.positions
            .filter((candidate) => candidate.parentPositionId === current)
            .forEach((candidate) => {
                if (!result.has(candidate.id)) {
                    result.add(candidate.id);
                    stack.push(candidate.id);
                }
            });
    }

    return result;
});

const moveCandidates = computed(() => {
    const positionId = position.value?.id;
    return props.positions
        .filter((candidate) => candidate.id !== positionId
            && candidate.paForPositionId === null
            && !descendantIds.value.has(candidate.id))
        .sort((a, b) => a.title.localeCompare(b.title));
});

watch(
    () => props.position?.id,
    () => {
        const current = position.value;
        editTitle.value = current?.title ?? '';
        editGroups.value = current ? [...current.groups] : [];
        editColor.value = current?.color ?? null;
        moveParentId.value = current?.parentPositionId ?? null;
        inviteEmail.value = '';
        selectedUserId.value = undefined;
        createChildTitle.value = '';
        createChildGroups.value = [];
        createChildColor.value = null;
    },
    { immediate: true }
);

function report(success: boolean, message: string) {
    Toast.open({ message, type: success ? 'is-success' : 'is-danger' });

    return success;
}

function swatchStyle(color: string | null): Record<string, string> {
    return color ? { backgroundColor: color } : {};
}

async function saveEdit() {
    if (!editTitle.value.trim()) return report(false, 'Titel er påkrævet.');

    loading.value = true;
    try {
        const success = await service.updatePosition(position.value!.id, {
            title: editTitle.value.trim(),
            groups: editGroups.value,
            color: editColor.value
        });
        if (report(success, success ? 'Stilling opdateret.' : 'Kunne ikke opdatere stillingen.')) {
            emit('refresh');
        }
    } finally {
        loading.value = false;
    }
}

async function addPa() {
    loading.value = true;
    try {
        const success = await service.createPa(position.value!.id);
        if (report(success, success ? 'Personlig assistent tilføjet.' : 'Kunne ikke tilføje den personlige assistent.')) {
            emit('refresh');
        }
    } finally {
        loading.value = false;
    }
}

async function saveMove() {
    loading.value = true;
    try {
        const success = await service.movePosition(position.value!.id, moveParentId.value);
        if (report(success, success ? 'Stilling flyttet.' : 'Kunne ikke flytte stillingen.')) {
            emit('refresh');
        }
    } finally {
        loading.value = false;
    }
}

async function addHolder() {
    if (!selectedUserId.value) return report(false, 'Vælg en bruger.');

    loading.value = true;
    try {
        const success = await service.addHolder(position.value!.id, selectedUserId.value);
        if (report(success, success ? 'Bruger tilknyttet.' : 'Kunne ikke tilknytte brugeren.')) {
            selectedUserId.value = undefined;
            emit('refresh');
        }
    } finally {
        loading.value = false;
    }
}

async function removeHolder(userId: string) {
    loading.value = true;
    try {
        const success = await service.removeHolder(position.value!.id, userId);
        if (report(success, success ? 'Bruger fjernet.' : 'Kunne ikke fjerne brugeren.')) {
            emit('refresh');
        }
    } finally {
        loading.value = false;
    }
}

async function sendInvite() {
    if (!inviteEmail.value.trim()) return report(false, 'Email er påkrævet.');

    loading.value = true;
    try {
        const success = await service.invite(position.value!.id, inviteEmail.value.trim());
        if (report(success, success ? 'Invitation sendt.' : 'Kunne ikke sende invitationen.')) {
            inviteEmail.value = '';
        }
    } finally {
        loading.value = false;
    }
}

async function createChild() {
    if (!createChildTitle.value.trim()) return report(false, 'Titel er påkrævet.');

    loading.value = true;
    try {
        const success = await service.createPosition({
            title: createChildTitle.value.trim(),
            parentPositionId: position.value!.id,
            groups: createChildGroups.value,
            color: createChildColor.value
        });
        if (report(success, success ? 'Stilling oprettet.' : 'Kunne ikke oprette stillingen.')) {
            createChildTitle.value = '';
            createChildGroups.value = [];
            createChildColor.value = null;
            emit('refresh');
        }
    } finally {
        loading.value = false;
    }
}

function confirmDelete() {
    Dialog.confirm({
        title: isPa.value ? 'Slet personlig assistent' : 'Slet stilling',
        message: `Slet "${position.value!.title}"?`,
        confirmText: 'Slet',
        type: 'is-danger',
        onConfirm: () => removePosition()
    });
}

async function removePosition() {
    loading.value = true;
    try {
        const success = await service.deletePosition(position.value!.id);
        if (report(success, success ? 'Stilling slettet.' : 'Kunne ikke slette stillingen.')) {
            open.value = false;
            emit('refresh');
        }
    } finally {
        loading.value = false;
    }
}
</script>

<style lang="scss" scoped>
:deep(.sidebar-content) {
    width: min(40rem, 100vw);
}

.org-drawer {
    display: flex;
    flex-direction: column;
    gap: 1.25rem;
    padding: 1.25rem;
    height: 100%;
    overflow-y: auto;
}

.org-drawer-head {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 1rem;
}

.org-drawer-title {
    display: flex;
    align-items: center;
    gap: 0.4rem;
    font-size: 1.1rem;
    font-weight: 600;
}

.org-drawer-section {
    border-top: 1px solid hsl(0, 0%, 86%);
    padding-top: 1rem;
}

.org-drawer-heading {
    font-size: 0.85rem;
    font-weight: 600;
    text-transform: uppercase;
    color: hsl(0, 0%, 40%);
    margin-bottom: 0.5rem;
}

.org-drawer-holders {
    display: flex;
    flex-direction: column;
    gap: 0.35rem;
}

.org-drawer-holders li {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.5rem;
}

.org-drawer-add {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-top: 0.5rem;
}

.org-color-swatch {
    width: 1rem;
    height: 1rem;
    border-radius: 0.25rem;
    border: 1px solid hsl(0, 0%, 74%);
    flex-shrink: 0;
}
</style>
