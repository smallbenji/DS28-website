<template>
    <BModal v-model="open" has-modal-card :width="1024" custom-content-class="board-card-modal-container" :can-cancel="saving ? false : ['escape', 'outside']">
        <div v-if="open" class="modal-card board-card-modal">
            <header class="modal-card-head">
                <div class="board-card-context">
                    <span>{{ store.SelectedBoard?.title ?? 'Tavle' }}</span>
                    <span aria-hidden="true">/</span>
                    <span class="board-card-key is-family-monospace">{{ isNew ? 'Nyt kort' : `DS-${card?.id}` }}</span>
                </div>
                <div class="board-card-header-actions">
                    <template v-if="currentPlacement">
                        <BDropdown v-if="canManage" position="is-bottom-left" aria-role="list" append-to-body @active-change="loadStatusColumns(currentPlacement, $event)">
                            <template #trigger>
                                <button
                                    type="button"
                                    class="board-card-status-pill"
                                    :class="{ 'is-complete': currentPlacement.isDoneColumn }"
                                    :disabled="saving"
                                    :aria-label="`Status på ${currentPlacement.boardTitle}: ${currentPlacement.columnTitle}. Skift status`"
                                >
                                    <font-awesome-icon v-if="currentPlacement.isDoneColumn" icon="check" />
                                    <span v-else class="board-card-status-dot" aria-hidden="true"></span>
                                    <span>{{ currentPlacement.columnTitle }}</span>
                                    <i class="fas fa-chevron-down" aria-hidden="true"></i>
                                </button>
                            </template>
                            <BDropdownItem v-if="statusLoading[currentPlacement.boardId]" disabled>Henter statusser…</BDropdownItem>
                            <BDropdownItem v-else-if="!statusColumns[currentPlacement.boardId]?.length" @click="loadStatusColumns(currentPlacement, true)">
                                Kunne ikke hente statusser. Prøv igen
                            </BDropdownItem>
                            <BDropdownItem
                                v-for="column in statusColumns[currentPlacement.boardId] || []"
                                :key="column.id"
                                aria-role="listitem"
                                :disabled="saving || String(column.id) === String(currentPlacement.columnId)"
                                @click="changePlacementStatus(currentPlacement, column)"
                            >
                                <span class="board-card-status-option">
                                    <span>{{ column.title }}</span>
                                    <span v-if="column.isDoneColumn" class="board-card-status-done">Færdig</span>
                                    <font-awesome-icon v-if="String(column.id) === String(currentPlacement.columnId)" icon="check" />
                                </span>
                            </BDropdownItem>
                        </BDropdown>
                        <span v-else class="board-card-status-pill is-static" :class="{ 'is-complete': currentPlacement.isDoneColumn }">
                            <font-awesome-icon v-if="currentPlacement.isDoneColumn" icon="check" />
                            <span v-else class="board-card-status-dot" aria-hidden="true"></span>
                            <span>{{ currentPlacement.columnTitle }}</span>
                        </span>
                    </template>
                    <BDropdown v-if="canManage && !isNew" position="is-bottom-left" aria-role="list">
                        <template #trigger>
                            <BButton size="is-small" class="is-ghost" aria-label="Korthandlinger" :disabled="saving">•••</BButton>
                        </template>
                        <BDropdownItem v-if="currentPlacement && placements.length > 1" aria-role="listitem" @click="confirmRemovePlacement(currentPlacement)">Fjern fra denne tavle</BDropdownItem>
                        <BDropdownItem aria-role="listitem" class="has-text-danger" @click="remove">Slet kort</BDropdownItem>
                    </BDropdown>
                    <button type="button" class="delete" aria-label="Luk" :disabled="saving" @click="open = false" />
                </div>
            </header>

            <section class="modal-card-body">
                <div class="board-card-columns">
                    <div class="board-card-main">
                        <BoardTagInput v-if="canManage" ref="tagInput" v-model="selectedTags" class="board-card-tags" :tags="availableTags" :disabled="saving" />
                        <div v-else-if="card?.tags.length" class="tags board-card-tags">
                            <BoardTagBadge v-for="tag in card.tags" :key="tag.id" :tag="tag" />
                        </div>

                        <BField v-if="canManage" class="board-card-title-field">
                            <BInput v-model="form.title" aria-label="Kortets titel" placeholder="Hvad skal gøres?" @keydown.enter="save" />
                        </BField>
                        <h2 v-else class="board-card-read-only-title">{{ card?.title }}</h2>

                        <section class="board-card-description" aria-labelledby="board-card-description-heading">
                            <div class="board-card-section-header">
                                <h3 id="board-card-description-heading" class="board-card-heading">Beskrivelse</h3>
                                <BButton v-if="canManage" size="is-small" class="is-ghost" icon-left="pen" @click="focusDescription">Rediger</BButton>
                            </div>
                            <div v-if="canManage" ref="descriptionFrame" class="board-card-description-frame" @click="focusDescription">
                                <RichTextEditor v-model="form.description" />
                            </div>
                            <div v-else class="board-card-description-frame is-read-only">
                                <RichTextViewer v-if="card?.description" :html="card.description" />
                                <p v-else class="has-text-grey is-size-7">Ingen beskrivelse.</p>
                            </div>
                        </section>

                        <BoardCardDependencies v-if="!isNew && card" :board-id="boardId" :card="card" :can-manage="canManage" />

                        <BoardCardPullRequests
                            v-if="pullRequestCard"
                            :key="pullRequestCard.id"
                            :board-id="boardId"
                            :card="pullRequestCard"
                            :can-manage="canManage"
                        />

                        <section v-if="!isNew && card && card.links.length" class="board-card-links-section" aria-labelledby="board-card-links-heading">
                            <h3 id="board-card-links-heading" class="board-card-heading">Koblinger</h3>
                            <div class="board-card-links">
                                <div v-for="link in card.links" :key="`${link.entityType}-${link.entityId}`" class="board-card-link">
                                    <i class="fas fa-link" aria-hidden="true"></i>
                                    <span>{{ link.entityType }} #{{ link.entityId }}</span>
                                </div>
                            </div>
                        </section>
                    </div>

                    <aside class="board-card-side" aria-label="Detaljer">
                        <div class="board-card-properties">
                            <BoardCardProperty label="Ansvarlige" :action-label="`Ansvarlige: ${assigneeNames || 'ingen'}. Rediger`" :editable="canManage" :close-on-click="false" :disabled="saving">
                                <template #value>
                                    <template v-if="form.assignees.length">
                                        <span class="board-card-avatars" aria-hidden="true">
                                            <span v-for="user in form.assignees" :key="user.id" class="board-card-avatar">
                                                <img v-if="user.profilePicture?.url" :src="user.profilePicture.url" alt="" />
                                                <span v-else>{{ initials(user) }}</span>
                                            </span>
                                        </span>
                                        <span class="board-card-property-text">{{ assigneeFirstNames }}</span>
                                    </template>
                                    <span v-else class="has-text-grey">Ingen</span>
                                </template>
                                <template #editor>
                                    <div class="board-card-editor">
                                        <div v-for="user in form.assignees" :key="user.id" class="board-card-editor-row">
                                            <span>{{ user.firstName }} {{ user.lastName }}</span>
                                            <BButton size="is-small" class="is-ghost" :aria-label="`Fjern ${user.firstName} ${user.lastName} som ansvarlig`" title="Fjern ansvarlig" @click="removeAssignee(user.id)">×</BButton>
                                        </div>
                                        <BField label="Tilføj ansvarlig" class="mt-2">
                                            <BInput v-model="assigneeSearch" icon="magnifying-glass" placeholder="Navn eller email..." :loading="assigneeSearching" />
                                        </BField>
                                        <div v-if="assigneeResults.length > 0" class="board-card-assignee-results">
                                            <div v-for="user in assigneeResults" :key="user.id" class="board-card-assignee-result">
                                                <div class="board-card-assignee-info">
                                                    <div class="board-card-assignee-name">{{ user.firstName }} {{ user.lastName }}</div>
                                                    <div class="board-card-assignee-email">{{ user.email }}</div>
                                                </div>
                                                <BButton size="is-small" type="is-primary" :disabled="form.assignees.some(assignee => assignee.id === user.id)" @click="selectAssignee(user)">
                                                    {{ form.assignees.some(assignee => assignee.id === user.id) ? 'Tilføjet' : 'Tilføj' }}
                                                </BButton>
                                            </div>
                                        </div>
                                        <p v-else-if="assigneeSearch && !assigneeSearching" class="has-text-grey is-size-7">Ingen brugere fundet.</p>
                                    </div>
                                </template>
                            </BoardCardProperty>

                            <BoardCardProperty label="Stilling" :action-label="`Stilling: ${positionTitle ?? 'ingen'}. Rediger`" :editable="canManage" :disabled="saving">
                                <template #value>
                                    <span v-if="positionTitle" class="board-card-property-text">{{ positionTitle }}</span>
                                    <span v-else class="has-text-grey">Ingen stilling</span>
                                </template>
                                <template #editor>
                                    <div class="board-card-editor board-card-choices">
                                        <button type="button" class="board-card-choice" :aria-pressed="form.ownerPositionId === null" @click="form.ownerPositionId = null">
                                            <span>Ingen stilling</span>
                                            <font-awesome-icon v-if="form.ownerPositionId === null" icon="check" />
                                        </button>
                                        <button v-for="position in orgPositions" :key="position.id" type="button" class="board-card-choice" :aria-pressed="form.ownerPositionId === position.id" @click="form.ownerPositionId = position.id">
                                            <span>{{ position.title }}</span>
                                            <font-awesome-icon v-if="form.ownerPositionId === position.id" icon="check" />
                                        </button>
                                    </div>
                                </template>
                            </BoardCardProperty>

                            <BoardCardProperty label="Deadline" :action-label="`Deadline: ${dueDateLabel || 'ingen'}. Rediger`" :editable="canManage" :close-on-click="false" :disabled="saving">
                                <template #value>
                                    <span v-if="dueDateLabel" class="board-card-property-text" :title="dueDateHint">{{ dueDateLabel }}</span>
                                    <span v-else class="has-text-grey">Ingen</span>
                                </template>
                                <template #editor>
                                    <div class="board-card-editor">
                                        <BField label="Deadline">
                                            <BInput v-model="form.dueDate" type="date" />
                                        </BField>
                                        <BButton v-if="form.dueDate" size="is-small" @click="form.dueDate = ''">Fjern deadline</BButton>
                                    </div>
                                </template>
                            </BoardCardProperty>

                            <BoardCardProperty label="Prioritet" :action-label="`Prioritet: ${boardPriorityLabel(form.priority)}. Rediger`" :editable="canManage" :disabled="saving">
                                <template #value>
                                    <i v-if="Number(form.priority) > 0" class="fas fa-arrow-up board-card-priority-high" aria-hidden="true"></i>
                                    <i v-else-if="Number(form.priority) < 0" class="fas fa-arrow-down has-text-grey" aria-hidden="true"></i>
                                    <span class="board-card-property-text">{{ boardPriorityLabel(form.priority) }}</span>
                                </template>
                                <template #editor>
                                    <div class="board-card-editor board-card-choices">
                                        <button v-for="option in boardPriorityOptions" :key="option.value" type="button" class="board-card-choice" :aria-pressed="form.priority === option.value" @click="form.priority = option.value">
                                            <span>{{ option.label }}</span>
                                            <font-awesome-icon v-if="form.priority === option.value" icon="check" />
                                        </button>
                                    </div>
                                </template>
                            </BoardCardProperty>

                            <BoardCardProperty label="Farve" :action-label="`Farve: ${selectedColorLabel}. Rediger`" :editable="canManage" :disabled="saving">
                                <template #value>
                                    <span class="board-card-color-dot" :style="{ backgroundColor: colorFill(form.color) }" aria-hidden="true"></span>
                                    <span class="board-card-property-text">{{ selectedColorLabel }}</span>
                                </template>
                                <template #editor>
                                    <div class="board-card-editor">
                                        <div class="board-card-color-options" role="group" aria-label="Kortets farve">
                                            <button
                                                v-for="color in cardColorOptions"
                                                :key="color.key"
                                                type="button"
                                                class="board-card-color-option"
                                                :style="{ backgroundColor: colorFill(color.value) }"
                                                :aria-label="color.label"
                                                :title="color.label"
                                                :aria-pressed="form.color === color.value"
                                                @click="form.color = color.value"
                                            >
                                                <span aria-hidden="true">{{ form.color === color.value ? '✓' : color.value === null ? '×' : '' }}</span>
                                            </button>
                                        </div>
                                    </div>
                                </template>
                            </BoardCardProperty>
                        </div>

                        <section v-if="!isNew && (otherPlacements.length > 0 || (canManage && availableBoards.length > 0))" class="board-card-section" aria-labelledby="board-card-placements-heading">
                            <h3 id="board-card-placements-heading" class="board-card-heading">Også på andre tavler</h3>

                            <ul class="board-card-placements">
                                <li v-for="placement in otherPlacements" :key="placement.boardId" class="board-card-placement">
                                    <span class="board-card-placement-board">{{ placement.boardTitle }}</span>
                                    <BDropdown v-if="canManage" position="is-bottom-left" aria-role="list" append-to-body @active-change="loadStatusColumns(placement, $event)">
                                        <template #trigger>
                                            <BButton size="is-small" class="board-card-status" :class="{ 'is-complete': placement.isDoneColumn }" :disabled="saving" :loading="statusLoading[placement.boardId]" :aria-label="`Status på ${placement.boardTitle}: ${placement.columnTitle}. Skift status`">
                                                <font-awesome-icon v-if="placement.isDoneColumn" icon="check" />
                                                <span>{{ placement.columnTitle }}</span>
                                                <i class="fas fa-chevron-down" aria-hidden="true"></i>
                                            </BButton>
                                        </template>
                                        <BDropdownItem v-if="statusLoading[placement.boardId]" disabled>Henter statusser…</BDropdownItem>
                                        <BDropdownItem v-else-if="!statusColumns[placement.boardId]?.length" @click="loadStatusColumns(placement, true)">
                                            Kunne ikke hente statusser. Prøv igen
                                        </BDropdownItem>
                                        <BDropdownItem v-for="column in statusColumns[placement.boardId] || []" :key="column.id" aria-role="listitem" :disabled="saving || String(column.id) === String(placement.columnId)" @click="changePlacementStatus(placement, column)">
                                            <span class="board-card-status-option">
                                                <span>{{ column.title }}</span>
                                                <span v-if="column.isDoneColumn" class="board-card-status-done">Færdig</span>
                                                <font-awesome-icon v-if="String(column.id) === String(placement.columnId)" icon="check" />
                                            </span>
                                        </BDropdownItem>
                                    </BDropdown>
                                    <span v-else class="board-card-status is-static" :class="{ 'is-complete': placement.isDoneColumn }">{{ placement.columnTitle }}</span>
                                    <BButton v-if="canManage" size="is-small" class="is-ghost" :disabled="saving" @click="confirmRemovePlacement(placement)">Fjern</BButton>
                                </li>
                            </ul>

                            <template v-if="canManage && availableBoards.length > 0">
                                <BButton v-if="!placementPickerOpen" size="is-small" class="is-ghost board-card-add-placement" icon-left="plus" @click="placementPickerOpen = true">Tilføj til tavle</BButton>
                                <div v-else class="board-card-placement-picker">
                                    <BSelect v-model="placementBoardId" expanded size="is-small">
                                        <option :value="null">Vælg tavle…</option>
                                        <option v-for="board in availableBoards" :key="board.id" :value="String(board.id)">{{ board.title }}</option>
                                    </BSelect>
                                    <BSelect v-if="placementBoardId !== null && placementColumns.length > 0" v-model="placementColumnId" expanded size="is-small">
                                        <option v-for="column in placementColumns" :key="column.id" :value="String(column.id)">{{ column.title }}</option>
                                    </BSelect>
                                    <div class="buttons">
                                        <BButton size="is-small" @click="closePlacementPicker">Annuller</BButton>
                                        <BButton size="is-small" type="is-primary" :disabled="placementBoardId === null || placementColumnId === null" :loading="saving" @click="addPlacement">Tilføj til tavle</BButton>
                                    </div>
                                </div>
                                <p class="board-card-status-help">Status og tavler gemmes med det samme.</p>
                            </template>
                        </section>
                    </aside>
                </div>
            </section>

            <footer v-if="canManage" class="modal-card-foot">
                <span v-if="isDirty" class="board-card-dirty">
                    <span class="board-card-dirty-dot" aria-hidden="true"></span>
                    Ugemte ændringer
                </span>
                <div class="buttons">
                    <BButton :disabled="saving" @click="open = false">Annuller</BButton>
                    <BButton type="is-primary" :loading="saving" @click="save">{{ isNew ? 'Opret kort' : 'Gem ændringer' }}</BButton>
                </div>
            </footer>
        </div>
    </BModal>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, reactive, ref, watch } from 'vue';
import { BButton, BDropdown, BDropdownItem, BField, BInput, BModal, BSelect, useDialog, useToast } from 'buefy';
import BoardService from '@/Services/BoardService';
import OrgService from '@/Services/OrgService';
import { boardPriorityLabel, boardPriorityOptions, type BoardColumnDto, type OrgPositionDto, type BoardMemberDto, type WorkItemDto, type WorkItemPlacementDto } from '@/types';
import { useBoardStore } from '@/Stores/BoardStore';
import BoardTagInput from './BoardTagInput.vue';
import BoardTagBadge from './BoardTagBadge.vue';
import BoardCardPullRequests from './BoardCardPullRequests.vue';
import BoardCardDependencies from './BoardCardDependencies.vue';
import BoardCardProperty from './BoardCardProperty.vue';
import type { BoardTagDto } from '@/types';
import RichTextEditor from '@/Components/RichText/RichTextEditor.vue';
import RichTextViewer from '@/Components/RichText/RichTextViewer.vue';
import { htmlToPlainText } from '@/lib/util';
import type { UserSummaryDto } from '@/types';

const props = defineProps<{
    modelValue: boolean;
    boardId: number;
    columnId: number;
    card: WorkItemDto | null;
    canManage: boolean;
}>();

const emit = defineEmits<{
    'update:modelValue': [value: boolean];
    saved: [];
}>();

const store = useBoardStore();
const pullRequestCard = computed(() => {
    const board = store.SelectedBoard;
    if (!board?.isDeveloperBoard || Number(board.id) !== props.boardId) return undefined;

    return board.cards.find(card => card.id === props.card?.id);
});
const boardService = new BoardService();
const orgService = new OrgService();
const Toast = useToast();
const Dialog = useDialog();

const saving = ref(false);
const selectedTags = ref<BoardTagDto[]>([]);
const tagInput = ref<InstanceType<typeof BoardTagInput>>();
const descriptionFrame = ref<HTMLElement>();
const assigneeSearch = ref('');
const assigneeSearching = ref(false);
const assigneeResults = ref<BoardMemberDto[]>([]);
const orgPositions = ref<OrgPositionDto[]>([]);
const placementPickerOpen = ref(false);
const placementBoardId = ref<string | null>(null);
const placementColumnId = ref<string | null>(null);
const placementColumns = ref<BoardColumnDto[]>([]);
const statusColumns = ref<Record<number, BoardColumnDto[]>>({});
const statusLoading = ref<Record<number, boolean>>({});
const savedSnapshot = ref('');
let searchTimeout: ReturnType<typeof setTimeout> | null = null;

const form = reactive({
    title: '',
    description: '',
    color: null as string | null,
    dueDate: '',
    priority: '0',
    assignees: [] as UserSummaryDto[],
    ownerPositionId: null as number | null
});

const open = computed({
    get: () => props.modelValue,
    set: (value: boolean) => emit('update:modelValue', value)
});

const availableTags = computed(() => String(store.SelectedBoard?.id) === String(props.boardId) ? store.SelectedBoard?.tags ?? [] : []);

const isNew = computed(() => props.card === null);

const cardColorOptions = computed(() => {
    const options: { key: string; value: string | null; label: string }[] = [
        { key: 'none', value: null, label: 'Ingen farve' },
        ...(store.SelectedBoard?.colors ?? []).map((color) => ({ key: String(color.id), value: color.color, label: color.name }))
    ];
    if (form.color && !options.some((color) => color.value === form.color)) {
        options.push({ key: 'current', value: form.color, label: 'Nuværende farve (uden for tavlens palette)' });
    }
    return options;
});
const selectedColorLabel = computed(() => cardColorOptions.value.find((color) => color.value === form.color)?.label ?? 'Ingen farve');

const assigneeNames = computed(() => form.assignees.map((user) => `${user.firstName} ${user.lastName}`).join(', '));
const assigneeFirstNames = computed(() => form.assignees.map((user) => user.firstName).join(', '));

const positionTitle = computed(() => {
    if (form.ownerPositionId === null) return null;

    return orgPositions.value.find((position) => position.id === form.ownerPositionId)?.title
        ?? (form.ownerPositionId === props.card?.ownerPositionId ? props.card?.ownerPositionTitle : null)
        ?? null;
});

const dueDate = computed(() => form.dueDate ? new Date(`${form.dueDate}T12:00:00`) : null);
const dueDateLabel = computed(() => dueDate.value ? new Intl.DateTimeFormat('da-DK', { day: 'numeric', month: 'short', year: 'numeric' }).format(dueDate.value) : '');
const dueDateHint = computed(() => {
    if (!dueDate.value) return '';

    const today = new Date(`${new Intl.DateTimeFormat('sv-SE', { timeZone: 'Europe/Copenhagen' }).format(new Date())}T12:00:00`);
    const days = Math.round((dueDate.value.getTime() - today.getTime()) / 86400000);
    if (days === 0) return 'I dag';
    if (days > 0) return `Om ${days} ${days === 1 ? 'dag' : 'dage'}`;

    return `${-days} ${days === -1 ? 'dag' : 'dage'} siden`;
});

const placements = computed<WorkItemPlacementDto[]>(() => {
    const cardId = props.card?.id;
    if (cardId == null) return [];

    const live = store.SelectedBoard?.cards.find((card) => String(card.id) === String(cardId));
    return live?.placements ?? props.card?.placements ?? [];
});
const currentPlacement = computed(() => placements.value.find((placement) => isCurrentBoard(placement)) ?? null);
const otherPlacements = computed(() => placements.value.filter((placement) => !isCurrentBoard(placement)));

const availableBoards = computed(() =>
    store.Boards.filter((board) => !board.isArchived && !placements.value.some((placement) => String(placement.boardId) === String(board.id)))
);

const isDirty = computed(() => savedSnapshot.value !== '' && snapshot() !== savedSnapshot.value);

watch(
    () => [props.modelValue, props.card, props.boardId] as const,
    () => {
        if (!props.modelValue) return;

        selectedTags.value = (props.card?.tags ?? []).filter(tag => availableTags.value.some(option => Number(option.id) === Number(tag.id)));
        form.title = props.card?.title ?? '';
        form.description = props.card?.description ?? '';
        form.color = props.card?.color ?? null;
        form.dueDate = props.card?.dueDate ?? '';
        form.priority = String(props.card?.priority ?? 0);
        form.assignees = [...(props.card?.assignees ?? [])];
        form.ownerPositionId = props.card?.ownerPositionId ?? null;
        savedSnapshot.value = snapshot();

        assigneeSearch.value = '';
        assigneeResults.value = [];
        assigneeSearching.value = false;

        closePlacementPicker();
        statusColumns.value = {};

        void loadOrgPositions();
        void loadBoards();
    },
    { immediate: true }
);

watch(placementBoardId, () => {
    void loadPlacementColumns();
});

watch(assigneeSearch, (value) => {
    if (searchTimeout) clearTimeout(searchTimeout);

    if (!value.trim()) {
        assigneeResults.value = [];
        assigneeSearching.value = false;
        return;
    }

    assigneeSearching.value = true;
    searchTimeout = setTimeout(async () => {
        assigneeResults.value = await boardService.searchAssignees(value.trim(), props.boardId);
        assigneeSearching.value = false;
    }, 300);
});

onBeforeUnmount(() => {
    if (searchTimeout) clearTimeout(searchTimeout);
});

function snapshot() {
    return JSON.stringify({
        title: form.title,
        description: htmlToPlainText(form.description) ? form.description : '',
        color: form.color,
        dueDate: form.dueDate,
        priority: form.priority,
        assignees: form.assignees.map((user) => user.id),
        ownerPositionId: form.ownerPositionId,
        tags: selectedTags.value.map((tag) => `${tag.id}:${tag.name}`)
    });
}

function initials(user: UserSummaryDto) {
    return `${user.firstName?.[0] ?? ''}${user.lastName?.[0] ?? ''}`.toLocaleUpperCase('da-DK');
}

function colorFill(color: string | null) {
    return color ? `color-mix(in srgb, ${color} 40%, white)` : '#fff';
}

function focusDescription() {
    descriptionFrame.value?.querySelector<HTMLElement>('.tiptap')?.focus();
}

async function loadOrgPositions() {
    if (!props.canManage || orgPositions.value.length > 0) return;

    orgPositions.value = await orgService.getPositions();
}

async function loadBoards() {
    if (!props.canManage || store.Boards.length > 0) return;

    await store.GET_BOARDS();
}

async function loadPlacementColumns() {
    placementColumnId.value = null;
    placementColumns.value = [];

    if (placementBoardId.value === null) return;

    const columns = await boardService.getColumns(Number(placementBoardId.value));
    placementColumns.value = columns;

    const first = columns[0];
    if (first) placementColumnId.value = String(first.id);
}

function closePlacementPicker() {
    placementPickerOpen.value = false;
    placementBoardId.value = null;
    placementColumnId.value = null;
    placementColumns.value = [];
}

function isCurrentBoard(placement: WorkItemPlacementDto) {
    return String(placement.boardId) === String(props.boardId);
}

async function loadStatusColumns(placement: WorkItemPlacementDto, active: boolean) {
    if (!active || statusLoading.value[placement.boardId]) return;

    statusLoading.value[placement.boardId] = true;
    try {
        statusColumns.value[placement.boardId] = await boardService.getColumns(Number(placement.boardId));
    } finally {
        statusLoading.value[placement.boardId] = false;
    }
}

async function changePlacementStatus(placement: WorkItemPlacementDto, column: BoardColumnDto) {
    if (!props.card || saving.value || String(placement.columnId) === String(column.id)) return;

    saving.value = true;
    try {
        const success = await boardService.moveCard(Number(placement.boardId), Number(props.card.id), {
            columnId: Number(column.id),
            position: 2147483647
        });
        if (!success) {
            Toast.open({ message: 'Status kunne ikke ændres. Prøv igen.', type: 'is-danger' });
            return;
        }

        await store.GET_BOARD(props.boardId);
        Toast.open({ message: `Status på ${placement.boardTitle} er ændret til ${column.title}.`, type: 'is-success' });
    } finally {
        saving.value = false;
    }
}

async function addPlacement() {
    if (!props.card || placementBoardId.value === null || placementColumnId.value === null) return;

    saving.value = true;
    try {
        const success = await store.ADD_PLACEMENT(
            props.boardId,
            Number(placementBoardId.value),
            Number(props.card.id),
            Number(placementColumnId.value)
        );

        if (success) {
            Toast.open({ message: 'Kortet er tilføjet til tavlen.', type: 'is-success' });
            closePlacementPicker();
        } else {
            Toast.open({ message: 'Kortet kunne ikke tilføjes til tavlen.', type: 'is-danger' });
        }
    } finally {
        saving.value = false;
    }
}

function confirmRemovePlacement(placement: WorkItemPlacementDto) {
    if (!props.card) return;

    Dialog.confirm({
        title: 'Fjern fra tavle',
        message: isCurrentBoard(placement)
            ? `Er du sikker på, at du vil fjerne "${props.card.title}" fra denne tavle? Kortet bevares på de øvrige tavler.`
            : `Er du sikker på, at du vil fjerne "${props.card.title}" fra tavlen "${placement.boardTitle}"?`,
        confirmText: 'Fjern',
        cancelText: 'Annuller',
        type: 'is-danger',
        onConfirm: () => {
            void doRemovePlacement(placement);
        }
    });
}

async function doRemovePlacement(placement: WorkItemPlacementDto) {
    if (!props.card) return;

    saving.value = true;
    try {
        const success = await store.REMOVE_PLACEMENT(props.boardId, Number(placement.boardId), Number(props.card.id));
        if (!success) {
            Toast.open({ message: 'Kortet kunne ikke fjernes fra tavlen.', type: 'is-danger' });
            return;
        }

        if (isCurrentBoard(placement)) {
            Toast.open({ message: 'Kortet er fjernet fra tavlen.', type: 'is-success' });
            emit('saved');
            open.value = false;
        } else {
            Toast.open({ message: `Kortet er fjernet fra "${placement.boardTitle}".`, type: 'is-success' });
        }
    } finally {
        saving.value = false;
    }
}

function selectAssignee(user: BoardMemberDto) {
    if (form.assignees.some(assignee => assignee.id === user.id)) return;

    form.assignees.push({ id: user.id, firstName: user.firstName, lastName: user.lastName, profilePicture: null });
    assigneeSearch.value = '';
    assigneeResults.value = [];
}

function removeAssignee(userId: string) {
    form.assignees = form.assignees.filter(user => user.id !== userId);
}

async function save() {
    if (saving.value) return;
    if (tagInput.value && !tagInput.value.commitPending()) return;

    if (!form.title.trim()) {
        Toast.open({ message: 'Titlen må ikke være tom.', type: 'is-danger' });
        return;
    }

    saving.value = true;
    try {
        const base = {
            tagIds: selectedTags.value.filter(tag => tag.id > 0).map(tag => Number(tag.id)),
            newTagNames: selectedTags.value.filter(tag => tag.id < 0).map(tag => tag.name),
            title: form.title.trim(),
            description: htmlToPlainText(form.description) ? form.description : null,
            color: form.color,
            assigneeUserIds: form.assignees.map(user => user.id),
            ownerPositionId: form.ownerPositionId,
            dueDate: form.dueDate ? form.dueDate : null,
            priority: Number(form.priority)
        };

        const success = isNew.value
            ? (await store.ADD_CARD(props.boardId, {
                ...base,
                columnId: props.columnId,
                links: []
            })) !== null
            : await store.UPDATE_CARD(props.boardId, Number(props.card!.id), base);

        if (success) {
            Toast.open({ message: 'Kortet er gemt.', type: 'is-success' });
            emit('saved');
            open.value = false;
        } else {
            Toast.open({ message: 'Kortet kunne ikke gemmes.', type: 'is-danger' });
        }
    } finally {
        saving.value = false;
    }
}

function remove() {
    if (!props.card) return;

    Dialog.confirm({
        title: 'Slet kort',
        message: `Er du sikker på, at du vil slette "${props.card.title}"?`,
        confirmText: 'Slet',
        cancelText: 'Annuller',
        type: 'is-danger',
        onConfirm: () => {
            void doRemove();
        }
    });
}

async function doRemove() {
    if (!props.card) return;

    saving.value = true;
    try {
        const success = await store.REMOVE_CARD(props.boardId, Number(props.card.id));
        if (success) {
            Toast.open({ message: 'Kortet er slettet.', type: 'is-success' });
            emit('saved');
            open.value = false;
        } else {
            Toast.open({ message: 'Kortet kunne ikke slettes.', type: 'is-danger' });
        }
    } finally {
        saving.value = false;
    }
}
</script>

<style scoped>
.board-card-modal {
    width: 100%;
    max-width: 64rem;
    max-height: calc(100vh - 3rem);
}

.board-card-modal .modal-card-head {
    padding: 0.85rem 1.25rem 0.85rem 1.75rem;
    background: #fff;
    border-bottom: 1px solid #edf0f2;
    box-shadow: none;
    gap: 1rem;
}

.board-card-context {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    min-width: 0;
    color: #6b7280;
    font-size: 0.8rem;
    overflow-wrap: anywhere;
}

.board-card-key {
    padding: 0.1rem 0.4rem;
    border-radius: 4px;
    background: #f1f3f5;
    color: #374151;
    font-size: 0.75rem;
}

.board-card-header-actions {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    margin-left: auto;
    flex-shrink: 0;
}

.board-card-status-pill {
    display: inline-flex;
    align-items: center;
    gap: 0.45rem;
    min-height: 2rem;
    padding: 0 0.8rem;
    border: 0;
    border-radius: 999px;
    background: #edf0f4;
    color: #3f4a57;
    font: inherit;
    font-size: 0.8rem;
    font-weight: 600;
    cursor: pointer;
}

.board-card-status-pill.is-static {
    cursor: default;
}

.board-card-status-pill.is-complete {
    background: #e8f5ed;
    color: #287b4e;
}

.board-card-status-pill .fa-chevron-down {
    font-size: 0.6rem;
}

.board-card-status-dot {
    width: 0.5rem;
    height: 0.5rem;
    border-radius: 50%;
    background: #6b7280;
}

.board-card-modal .modal-card-body {
    padding: 0;
}

.board-card-columns {
    display: grid;
    grid-template-columns: minmax(0, 1fr) 19rem;
    align-items: stretch;
}

.board-card-main,
.board-card-side {
    min-width: 0;
}

.board-card-main {
    padding: 1.5rem 1.75rem;
    min-height: 24rem;
}

.board-card-side {
    padding: 1.5rem 1.25rem;
    background: #fafbfc;
    border-left: 1px solid #f1f3f5;
}

.board-card-tags {
    margin-bottom: 0.75rem;
}

.board-card-title-field :deep(.input) {
    height: auto;
    padding: 0.25rem 0;
    border-color: transparent;
    box-shadow: none;
    background: transparent;
    font-size: 1.6rem;
    font-weight: 700;
}

.board-card-title-field :deep(.input:hover) {
    border-bottom-color: #dce2e7;
}

.board-card-title-field :deep(.input:focus) {
    border-bottom-color: var(--bulma-primary);
}

.board-card-read-only-title {
    font-size: 1.6rem;
    font-weight: 700;
    line-height: 1.25;
    overflow-wrap: anywhere;
    margin-bottom: 0.75rem;
}

.board-card-heading {
    font-size: 0.75rem;
    font-weight: 600;
    color: #66717e;
}

.board-card-section-header {
    display: flex;
    align-items: center;
    justify-content: space-between;
    margin-bottom: 0.5rem;
}

.board-card-description {
    margin-top: 1.25rem;
}

.board-card-description-frame {
    border: 1px solid #edf0f2;
    border-radius: 10px;
    background: #fcfcfd;
    cursor: text;
}

.board-card-description-frame.is-read-only {
    padding: 0.75rem 1rem;
    cursor: default;
}

.board-card-description-frame :deep(.rich-text-editor) {
    border-color: transparent;
    background: transparent;
}

.board-card-description-frame :deep(.rich-text-toolbar) {
    display: none;
    background: transparent;
    border-bottom: 1px solid #edf0f2;
    padding: 0.25rem 0.5rem;
}

.board-card-description-frame :deep(.tiptap) {
    min-height: 10rem;
    padding: 0.75rem 1rem;
}

.board-card-description-frame:focus-within {
    border-color: #cfd5dd;
    background: #fff;
}

.board-card-description-frame:focus-within :deep(.rich-text-toolbar) {
    display: flex;
}

.board-card-links-section {
    margin-top: 1.5rem;
}

.board-card-links {
    display: flex;
    flex-wrap: wrap;
    gap: 0.5rem;
    margin-top: 0.5rem;
}

.board-card-link {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.5rem 0.75rem;
    border: 1px solid #e5e9ed;
    border-radius: 10px;
    font-size: 0.85rem;
}

.board-card-link .fa-link {
    color: #6b7280;
}

.board-card-properties {
    display: flex;
    flex-direction: column;
    gap: 0.1rem;
}

.board-card-property-text {
    min-width: 0;
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
}

.board-card-avatars {
    display: inline-flex;
    flex: none;
}

.board-card-avatar {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    width: 1.4rem;
    height: 1.4rem;
    border-radius: 50%;
    overflow: hidden;
    background: #dbe4f0;
    color: #374151;
    font-size: 0.6rem;
    font-weight: 700;
    box-shadow: 0 0 0 2px #fafbfc;
}

.board-card-avatar + .board-card-avatar {
    margin-left: -0.35rem;
}

.board-card-avatar img {
    width: 100%;
    height: 100%;
    object-fit: cover;
}

.board-card-priority-high {
    color: #c2410c;
}

.board-card-color-dot {
    flex: none;
    width: 0.9rem;
    height: 0.9rem;
    border-radius: 50%;
    border: 1px solid rgba(0, 0, 0, 0.12);
}

.board-card-editor {
    width: 16rem;
    max-width: calc(100vw - 3rem);
    white-space: normal;
}

.board-card-editor-row {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.5rem;
    font-size: 0.9rem;
}

.board-card-choices {
    display: flex;
    flex-direction: column;
}

.board-card-choice {
    display: flex;
    align-items: center;
    justify-content: space-between;
    gap: 0.5rem;
    min-height: 2.25rem;
    padding: 0 0.5rem;
    border: 0;
    border-radius: 4px;
    background: transparent;
    font: inherit;
    font-size: 0.9rem;
    text-align: left;
    cursor: pointer;
}

.board-card-choice:hover,
.board-card-choice:focus-visible {
    background: #f7f8fa;
}

.board-card-color-options {
    display: flex;
    flex-wrap: wrap;
    gap: 0.4rem;
}

.board-card-color-option {
    width: 1.75rem;
    height: 1.75rem;
    padding: 0;
    border: 1px solid #cfd5dd;
    border-radius: 50%;
    color: #363636;
    cursor: pointer;
}

.board-card-color-option[aria-pressed="true"] {
    outline: 2px solid #68717d;
    outline-offset: 2px;
}

.board-card-assignee-results {
    border-top: 1px solid rgba(0, 0, 0, 0.08);
    margin-top: 0.5rem;
    padding-top: 0.25rem;
}

.board-card-assignee-result {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    justify-content: space-between;
    gap: 0.5rem 0.75rem;
    padding: 0.5rem 0;
    border-bottom: 1px solid rgba(0, 0, 0, 0.05);
}

.board-card-assignee-result:last-child {
    border-bottom: none;
}

.board-card-assignee-info {
    min-width: 0;
}

.board-card-assignee-name {
    font-weight: 600;
    font-size: 0.9rem;
}

.board-card-assignee-email {
    font-size: 0.8rem;
    color: #6b7280;
    word-break: break-word;
}

.board-card-section {
    margin-top: 1.25rem;
    padding-top: 1.25rem;
    border-top: 1px solid #e9edf0;
}

.board-card-placements {
    list-style: none;
    margin: 0.5rem 0 0.5rem;
    padding: 0;
}

.board-card-placement {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 0.35rem 0.5rem;
    padding: 0.5rem 0.65rem;
    margin-bottom: 0.4rem;
    background: #fff;
    border: 1px solid #e9edf0;
    border-radius: 10px;
}

.board-card-placement-board {
    flex: 1 1 auto;
    min-width: 0;
    font-size: 0.85rem;
    font-weight: 600;
    overflow-wrap: anywhere;
}

.board-card-side :deep(.board-card-status),
.board-card-status.is-static {
    height: auto;
    min-height: 1.8rem;
    padding: 0.25rem 0.6rem;
    border: 0;
    border-radius: 999px;
    background: #edf0f4;
    color: #596575;
    box-shadow: none;
    font-size: 0.75rem;
    font-weight: 600;
    white-space: normal;
    text-align: left;
}

.board-card-side :deep(.board-card-status.is-complete),
.board-card-status.is-static.is-complete {
    background: #e8f5ed;
    color: #287b4e;
}

.board-card-status svg,
.board-card-status-pill svg {
    margin-right: 0.1rem;
}

.board-card-status .fa-chevron-down {
    margin-left: 0.4rem;
    font-size: 0.6rem;
}

.board-card-status-option {
    display: flex;
    align-items: center;
    gap: 0.75rem;
    white-space: normal;
}

.board-card-status-done {
    color: #287b4e;
    font-size: 0.7rem;
    margin-left: auto;
}

.board-card-side :deep(.button.is-ghost) {
    color: #66717e;
    padding-left: 0.5rem;
    padding-right: 0.5rem;
    text-decoration: none;
}

.board-card-side :deep(.button.is-ghost:hover) {
    background: #edf0f2;
    color: #363636;
}

.board-card-add-placement {
    margin-left: -0.5rem;
}

.board-card-placement-picker {
    display: flex;
    flex-direction: column;
    gap: 0.5rem;
}

.board-card-placement-picker .buttons {
    justify-content: flex-end;
    margin: 0;
}

.board-card-status-help {
    margin: 0.5rem 0 0;
    font-size: 0.72rem;
    color: #6b7280;
}

.board-card-modal .modal-card-foot {
    justify-content: flex-end;
    gap: 1rem;
    padding: 0.85rem 1.75rem;
    background: #fff;
    border-top: 1px solid #edf0f2;
}

.board-card-dirty {
    display: inline-flex;
    align-items: center;
    gap: 0.5rem;
    margin-right: auto;
    font-size: 0.8rem;
    color: #6b7280;
}

.board-card-dirty-dot {
    width: 0.5rem;
    height: 0.5rem;
    border-radius: 50%;
    background: #f5a524;
}

.board-card-modal .modal-card-foot .buttons {
    margin: 0;
}

.board-card-modal .modal-card-foot .button {
    margin-bottom: 0;
}

@media screen and (max-width: 768px) {
    .board-card-modal {
        max-height: calc(100dvh - 1rem);
        max-width: calc(100vw - 1rem);
        margin: 0 auto;
    }

    .board-card-modal .modal-card-head {
        flex-wrap: wrap;
        padding: 0.75rem 1rem;
    }

    .board-card-columns {
        grid-template-columns: 1fr;
    }

    .board-card-main {
        min-height: 0;
        padding: 1.25rem 1rem;
    }

    .board-card-side {
        padding: 1.25rem 1rem;
        border-left: none;
        border-top: 1px solid #edf0f2;
    }

    .board-card-modal .modal-card-foot {
        padding: 0.75rem 1rem;
    }

    .board-card-modal .modal-card-foot .buttons {
        flex: 1 1 auto;
        flex-wrap: nowrap;
    }

    .board-card-modal .modal-card-foot .buttons .button {
        flex: 1 1 0;
        min-height: 2.75rem;
    }

    .board-card-modal .modal-card-foot .buttons .button.is-primary {
        flex-grow: 2;
    }
}
</style>

<style>
.board-card-modal-container {
    width: calc(100vw - 2.5rem);
}
</style>
