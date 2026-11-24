<template>
    <div class="flex min-h-0 flex-1 flex-col bg-default">
        <div class="flex min-h-(--ui-header-height) shrink-0 flex-wrap items-center gap-2 border-b border-default px-4 py-2 sm:px-6">
            <template v-if="variant === 'page'">
                <UDashboardSidebarCollapse />
                <UButton :to="boardPath" color="neutral" variant="ghost" size="sm" icon="i-lucide-arrow-left" aria-label="Tilbage til tavlen" />
            </template>
            <nav class="flex min-w-0 flex-1 items-center gap-1.5 text-sm text-muted" aria-label="Brødkrummer">
                <template v-if="variant === 'page'">
                    <RouterLink to="/board" class="hidden hover:text-highlighted hover:underline sm:inline">Tavler</RouterLink>
                    <UIcon name="i-lucide-chevron-right" class="hidden size-4 shrink-0 sm:inline" />
                    <RouterLink :to="boardPath" class="truncate hover:text-highlighted hover:underline">{{ store.SelectedBoard?.title ?? 'Tavle' }}</RouterLink>
                    <UIcon name="i-lucide-chevron-right" class="size-4 shrink-0" />
                </template>
                <template v-else>
                    <span class="truncate">{{ store.SelectedBoard?.title ?? 'Tavle' }}</span>
                    <span aria-hidden="true">/</span>
                </template>
                <span class="font-mono text-highlighted">{{ isNew ? 'Nyt kort' : `DS-${card?.id}` }}</span>
                <UTooltip v-if="!isNew && variant === 'modal'" text="Kopiér link til kortet">
                    <UButton color="neutral" variant="ghost" size="xs" icon="i-lucide-link-2" aria-label="Kopiér link til kortet" @click="copyLink" />
                </UTooltip>
            </nav>

            <UButton v-if="!isNew && variant === 'page'" color="neutral" variant="ghost" size="sm" icon="i-lucide-link-2" label="Kopiér link" class="hidden sm:inline-flex" @click="copyLink" />
            <UButton v-if="!isNew && variant === 'page'" color="neutral" variant="ghost" size="sm" icon="i-lucide-link-2" aria-label="Kopiér link til kortet" class="sm:hidden" @click="copyLink" />

                <template v-if="currentPlacement">
                    <UDropdownMenu v-if="canManage" :items="statusItems(currentPlacement)" :content="{ align: 'end' }" @update:open="(value: boolean) => loadStatusColumns(currentPlacement!, value)">
                        <UButton
                            :color="currentPlacement.isDoneColumn ? 'success' : 'neutral'"
                            variant="subtle"
                            size="sm"
                            :icon="currentPlacement.isDoneColumn ? 'i-lucide-check' : 'i-lucide-circle-dot'"
                            :label="currentPlacement.columnTitle"
                            trailing-icon="i-lucide-chevron-down"
                            :disabled="saving"
                            :aria-label="`Status på ${currentPlacement.boardTitle}: ${currentPlacement.columnTitle}. Skift status`"
                        />
                    </UDropdownMenu>
                    <UBadge v-else :color="currentPlacement.isDoneColumn ? 'success' : 'neutral'" variant="subtle" :label="currentPlacement.columnTitle" />
                </template>

                <UTooltip v-if="!isNew && variant === 'modal'" text="Åbn i fuld skærm">
                    <UButton color="neutral" variant="ghost" size="sm" icon="i-lucide-maximize-2" aria-label="Åbn i fuld skærm" :disabled="saving" @click="openFullPage" />
                </UTooltip>
                <UDropdownMenu v-if="canManage && !isNew" :items="cardMenu" :content="{ align: 'end' }">
                    <UButton color="neutral" variant="ghost" size="sm" icon="i-lucide-ellipsis" aria-label="Korthandlinger" :disabled="saving" />
                </UDropdownMenu>
                <UButton v-if="variant === 'modal'" color="neutral" variant="ghost" size="sm" icon="i-lucide-x" aria-label="Luk" :disabled="saving" @click="emit('close')" />
        </div>

        <div class="min-h-0 flex-1 overflow-y-auto">
            <div class="grid min-h-full" :class="variant === 'page' ? 'lg:grid-cols-[minmax(0,1fr)_22rem]' : 'lg:grid-cols-[minmax(0,1fr)_20rem]'" @keydown.capture="onShortcut">
                <div class="flex min-w-0 flex-col gap-6 p-4" :class="variant === 'page' ? 'w-full max-w-4xl sm:p-8' : 'sm:p-6'">
                    <UInput
                        v-if="canManage"
                        v-model="form.title"
                        size="xl"
                        variant="ghost"
                        placeholder="Hvad skal gøres?"
                        aria-label="Kortets titel"
                        class="w-full"
                        :ui="{ base: 'px-0 text-2xl font-semibold' }"
                        @keydown.enter.prevent="save"
                    />
                    <h2 v-else class="text-2xl font-semibold text-highlighted">{{ card?.title }}</h2>

                    <section aria-label="Beskrivelse">
                        <RichTextEditor v-if="canManage" v-model="form.description" placeholder="Tilføj en beskrivelse…" />
                        <RichTextViewer v-else-if="card?.description" :html="card.description" />
                        <p v-else class="text-sm text-muted">Ingen beskrivelse.</p>
                    </section>

                    <section v-if="relationTabs.length" aria-label="Relationer" :class="{ 'grid gap-8 xl:grid-cols-2': variant === 'page' }">
                        <UTabs v-if="variant === 'modal'" v-model="relationTab" :items="relationTabs" :content="false" variant="link" size="sm" class="mb-3 w-full" />
                        <div v-for="relation in visibleRelations" :key="relation.value" class="min-w-0">
                            <h3 v-if="variant === 'page'" class="mb-2 flex items-center gap-2 text-sm font-semibold text-highlighted" aria-hidden="true">
                                <UIcon :name="relation.icon" class="size-4 text-muted" />
                                {{ relation.label }}
                                <UBadge v-if="relation.badge" :label="String(relation.badge)" color="neutral" variant="subtle" size="sm" />
                            </h3>
                            <BoardCardDependencies v-if="relation.value === 'dependencies' && card" :board-id="boardId" :card="card" :can-manage="canManage" />
                            <BoardCardPullRequests v-else-if="relation.value === 'pull-requests' && pullRequestCard" :key="pullRequestCard.id" :board-id="boardId" :card="pullRequestCard" :can-manage="canManage" />
                            <BoardCardEpic v-else-if="relation.value === 'epic' && pullRequestCard" :key="`epic-${pullRequestCard.id}`" :board-id="boardId" :card="pullRequestCard" :can-manage="canManage" @open="openOtherCard" />
                            <ul v-else-if="relation.value === 'links' && card" class="flex flex-col gap-1 text-sm">
                                <li v-for="link in card.links" :key="`${link.entityType}-${link.entityId}`" class="flex items-center gap-2">
                                    <UIcon name="i-lucide-link" class="size-4 text-muted" />
                                    {{ link.entityType }} #{{ link.entityId }}
                                </li>
                            </ul>
                        </div>
                    </section>
                </div>

                <aside class="order-first flex flex-col gap-6 border-b border-default bg-elevated/40 p-4 sm:p-5 lg:order-none lg:border-s lg:border-b-0" aria-label="Egenskaber">
                    <section aria-labelledby="card-properties-heading">
                        <h3 id="card-properties-heading" class="mb-2 text-xs font-semibold uppercase tracking-wide text-muted">Egenskaber</h3>
                        <dl class="grid grid-cols-[5.5rem_minmax(0,1fr)] items-center gap-x-2 gap-y-0.5">
                            <dt class="text-xs text-muted">Ansvarlige</dt>
                            <dd class="min-w-0">
                                <UPopover :content="{ align: 'start', side: 'bottom' }" :ui="{ content: 'w-80 p-3' }">
                                    <button type="button" :class="propertyButton" aria-label="Ansvarlige. Vis og ret">
                                        <template v-if="assigneeList.length">
                                            <UAvatarGroup size="2xs" :max="4">
                                                <UAvatar v-for="user in assigneeList" :key="user.id" :src="user.profilePicture?.url" :text="initials(user)" :alt="`${user.firstName} ${user.lastName}`" />
                                            </UAvatarGroup>
                                            <span v-if="assigneeList.length === 1" class="min-w-0 truncate">{{ assigneeList[0]!.firstName }} {{ assigneeList[0]!.lastName }}</span>
                                            <span v-if="liveCard" class="text-xs text-muted">{{ completedAssigneeCount }}/{{ assigneeList.length }} klarmeldt</span>
                                        </template>
                                        <span v-else class="text-muted">{{ canManage ? 'Tilføj ansvarlig' : 'Ingen' }}</span>
                                    </button>
                                    <template #content>
                                        <BoardCardAssignees :key="liveCard?.id ?? 'new'" v-model:pending="form.assignees" :board-id="boardId" :card="liveCard" :can-manage="canManage" />
                                    </template>
                                </UPopover>
                            </dd>

                            <dt class="text-xs text-muted">Prioritet</dt>
                            <dd class="min-w-0">
                                <USelect
                                    v-if="canManage"
                                    v-model="form.priority"
                                    :items="priorityItems"
                                    :icon="priorityIcon.icon"
                                    variant="ghost"
                                    size="sm"
                                    class="w-full"
                                    :ui="{ leadingIcon: priorityIcon.class, trailingIcon: 'hidden' }"
                                    :disabled="saving"
                                    aria-label="Prioritet"
                                />
                                <p v-else :class="propertyText"><UIcon :name="priorityIcon.icon" class="size-4" :class="priorityIcon.class" />{{ boardPriorityLabel(form.priority) }}</p>
                            </dd>

                            <dt class="text-xs text-muted">Deadline</dt>
                            <dd class="min-w-0">
                                <div v-if="canManage" class="flex flex-wrap items-center gap-x-1">
                                    <UInput v-model="form.dueDate" type="date" variant="ghost" size="sm" class="w-36 shrink-0" :disabled="saving" aria-label="Deadline" />
                                    <span v-if="dueDateHint" class="px-2.5 text-xs text-muted">{{ dueDateHint }}</span>
                                </div>
                                <p v-else :class="propertyText">
                                    <span :class="{ 'text-muted': !dueDateLabel }">{{ dueDateLabel || 'Ingen' }}</span>
                                    <span v-if="dueDateHint" class="text-xs text-muted">{{ dueDateHint }}</span>
                                </p>
                            </dd>

                            <dt class="text-xs text-muted">Tags</dt>
                            <dd class="min-w-0">
                                <UPopover v-if="canManage" :content="{ align: 'start', side: 'bottom' }" :ui="{ content: 'w-80 p-3' }">
                                    <button type="button" :class="propertyButton" aria-label="Tags. Ret tags">
                                        <BoardTagBadge v-for="tag in selectedTags" :key="tag.id" :tag="tag" />
                                        <span v-if="!selectedTags.length" class="text-muted">Tilføj tags</span>
                                    </button>
                                    <template #content>
                                        <BoardTagInput ref="tagInput" v-model="selectedTags" :tags="availableTags" :disabled="saving" />
                                    </template>
                                </UPopover>
                                <div v-else :class="propertyText">
                                    <BoardTagBadge v-for="tag in card?.tags ?? []" :key="tag.id" :tag="tag" />
                                    <span v-if="!card?.tags.length" class="text-muted">Ingen</span>
                                </div>
                            </dd>

                            <dt class="text-xs text-muted">Farve</dt>
                            <dd class="min-w-0">
                                <UPopover v-if="canManage" :content="{ align: 'start', side: 'bottom' }" :ui="{ content: 'p-3' }">
                                    <button type="button" :class="propertyButton" :disabled="saving" aria-label="Farve. Skift farve">
                                        <span class="size-4 shrink-0 rounded border border-default" :style="{ backgroundColor: form.color ?? 'transparent' }" aria-hidden="true" />
                                        <span class="min-w-0 truncate" :class="{ 'text-muted': !form.color }">{{ selectedColorLabel }}</span>
                                    </button>
                                    <template #content>
                                        <div class="flex max-w-60 flex-wrap gap-1.5" role="group" aria-label="Kortets farve">
                                            <button
                                                v-for="color in cardColorOptions"
                                                :key="color.key"
                                                type="button"
                                                class="flex size-7 items-center justify-center rounded-md border text-xs focus-visible:outline-2 focus-visible:outline-primary"
                                                :class="form.color === color.value ? 'border-inverted ring-2 ring-primary' : 'border-default'"
                                                :style="{ backgroundColor: color.value ?? 'var(--ui-bg)', color: color.value ? readableTextColor(color.value) : undefined }"
                                                :aria-label="color.label"
                                                :title="color.label"
                                                :aria-pressed="form.color === color.value"
                                                @click="form.color = color.value"
                                            >
                                                <UIcon v-if="form.color === color.value" name="i-lucide-check" class="size-3.5" />
                                                <UIcon v-else-if="color.value === null" name="i-lucide-ban" class="size-3.5 text-muted" />
                                            </button>
                                        </div>
                                    </template>
                                </UPopover>
                                <p v-else :class="propertyText">
                                    <span class="size-4 shrink-0 rounded border border-default" :style="{ backgroundColor: form.color ?? 'transparent' }" aria-hidden="true" />
                                    <span :class="{ 'text-muted': !form.color }">{{ selectedColorLabel }}</span>
                                </p>
                            </dd>
                        </dl>
                    </section>

                    <section v-if="!isNew && (placements.length > 0 || (canManage && availableBoards.length > 0))" aria-labelledby="card-placements-heading">
                        <h3 id="card-placements-heading" class="mb-2 text-xs font-semibold uppercase tracking-wide text-muted">Tavler</h3>
                        <ul class="flex flex-col gap-0.5">
                            <li v-for="placement in sortedPlacements" :key="placement.boardId" class="flex min-h-8 items-center gap-2 rounded-md px-2 text-sm">
                                <UIcon name="i-lucide-square-kanban" class="size-4 shrink-0 text-muted" />
                                <span class="min-w-0 flex-1 truncate" :class="{ 'font-medium text-highlighted': isCurrentBoard(placement) }">{{ placement.boardTitle }}</span>
                                <UDropdownMenu v-if="canManage && !isCurrentBoard(placement)" :items="statusItems(placement)" :content="{ align: 'end' }" @update:open="(value: boolean) => loadStatusColumns(placement, value)">
                                    <UButton :color="placement.isDoneColumn ? 'success' : 'neutral'" variant="subtle" size="xs" :label="placement.columnTitle" trailing-icon="i-lucide-chevron-down" :disabled="saving" :aria-label="`Status på ${placement.boardTitle}: ${placement.columnTitle}. Skift status`" />
                                </UDropdownMenu>
                                <UBadge v-else :color="placement.isDoneColumn ? 'success' : 'neutral'" variant="subtle" size="sm" :label="placement.columnTitle" />
                                <UDropdownMenu v-if="canManage && placements.length > 1" :items="placementMenu(placement)" :content="{ align: 'end' }">
                                    <UButton color="neutral" variant="ghost" size="xs" icon="i-lucide-ellipsis" :aria-label="`Handlinger for ${placement.boardTitle}`" :disabled="saving" />
                                </UDropdownMenu>
                            </li>
                        </ul>

                        <template v-if="canManage && availableBoards.length > 0">
                            <UButton v-if="!placementPickerOpen" class="mt-1" color="neutral" variant="ghost" size="xs" icon="i-lucide-plus" label="Tilføj til tavle" @click="placementPickerOpen = true" />
                            <div v-else class="mt-2 flex flex-col gap-2">
                                <USelectMenu v-model="placementBoardId" :items="placementBoardItems" value-key="value" placeholder="Vælg tavle…" size="sm" class="w-full" />
                                <USelect v-if="placementColumns.length" v-model="placementColumnId" :items="placementColumnItems" size="sm" class="w-full" />
                                <div class="flex gap-2">
                                    <UButton color="neutral" variant="outline" size="xs" label="Annuller" @click="closePlacementPicker" />
                                    <UButton size="xs" label="Tilføj til tavle" :disabled="!placementBoardId || !placementColumnId" :loading="saving" @click="addPlacement" />
                                </div>
                            </div>
                        </template>
                        <p v-if="canManage" class="mt-2 px-2 text-xs text-muted">Status og tavler gemmes med det samme.</p>
                    </section>
                </aside>
            </div>
        </div>

        <div v-if="canManage" class="flex shrink-0 items-center border-t border-default p-4 sm:px-6">
            <div class="flex w-full flex-wrap items-center justify-between gap-2">
                <span v-if="isDirty" class="flex items-center gap-1.5 text-xs text-warning">
                    <span class="size-2 rounded-full bg-warning" aria-hidden="true" />
                    Ugemte ændringer
                </span>
                <span v-else />
                <div class="flex items-center gap-2">
                    <span class="hidden items-center gap-1 text-xs text-muted sm:flex">
                        <UKbd :value="isMac ? 'meta' : 'ctrl'" size="sm" />
                        <UKbd value="enter" size="sm" />
                        gemmer
                    </span>
                    <UButton v-if="variant === 'modal'" color="neutral" variant="outline" label="Annuller" :disabled="saving" @click="emit('close')" />
                    <UButton v-else color="neutral" variant="outline" label="Fortryd" :disabled="saving || !isDirty" @click="resetForm" />
                    <UButton :label="isNew ? 'Opret kort' : 'Gem ændringer'" :loading="saving" @click="save" />
                </div>
            </div>
        </div>
    </div>

    <ConfirmModal
        v-model:open="deleteOpen"
        title="Slet kort"
        :description="`Slet ${card?.title}?`"
        confirm-label="Slet"
        color="error"
        :loading="saving"
        @confirm="doRemove"
    />

    <ConfirmModal
        v-model:open="removePlacementOpen"
        title="Fjern fra tavle"
        :description="removePlacementDescription"
        confirm-label="Fjern"
        color="error"
        :loading="saving"
        @confirm="doRemovePlacement"
    />
</template>

<script setup lang="ts">
import { computed, reactive, ref, watch } from "vue";
import { useRouter } from "vue-router";
import type { DropdownMenuItem } from "@nuxt/ui";
import BoardService from "@/Services/BoardService";
import { useBoardStore } from "@/Stores/BoardStore";
import { htmlToPlainText } from "@/lib/util";
import { useNotify } from "@/lib/notify";
import { readableTextColor } from "@/lib/board/colorContrast";
import { boardPriorityFilters } from "@/lib/board/boardFilters";
import ConfirmModal from "@/Components/ConfirmModal.vue";
import RichTextEditor from "@/Components/RichText/RichTextEditor.vue";
import RichTextViewer from "@/Components/RichText/RichTextViewer.vue";
import BoardTagInput from "@/Components/Board/BoardTagInput.vue";
import BoardTagBadge from "@/Components/Board/BoardTagBadge.vue";
import BoardCardDependencies from "@/Components/Board/BoardCardDependencies.vue";
import BoardCardEpic from "@/Components/Board/BoardCardEpic.vue";
import BoardCardPullRequests from "@/Components/Board/BoardCardPullRequests.vue";
import BoardCardAssignees from "@/Components/Board/BoardCardAssignees.vue";
import {
    boardPriorityLabel,
    boardPriorityOptions,
    type BoardColumnDto,
    type BoardTagDto,
    type UserSummaryDto,
    type WorkItemDto,
    type WorkItemPlacementDto
} from "@/types";

const props = defineProps<{ boardId: number; columnId: number; card: WorkItemDto | null; canManage: boolean; variant: "modal" | "page" }>();
const saving = defineModel<boolean>("busy", { default: false });
const emit = defineEmits<{ close: []; removed: []; openCard: [card: WorkItemDto] }>();

const store = useBoardStore();
const boardService = new BoardService();
const notify = useNotify();
const router = useRouter();

const selectedTags = ref<BoardTagDto[]>([]);
const tagInput = ref<InstanceType<typeof BoardTagInput>>();
const placementPickerOpen = ref(false);
const placementBoardId = ref<string | undefined>();
const placementColumnId = ref<string | undefined>();
const placementColumns = ref<BoardColumnDto[]>([]);
const statusColumns = ref<Record<number, BoardColumnDto[]>>({});
const statusLoading = ref<Record<number, boolean>>({});
const savedSnapshot = ref("");
const deleteOpen = ref(false);
const removePlacementOpen = ref(false);
const placementToRemove = ref<WorkItemPlacementDto | null>(null);
const relationTab = ref("dependencies");
const isMac = typeof navigator !== "undefined" && /Mac|iPhone|iPad/.test(navigator.userAgent);
const propertyButton = "flex min-h-8 w-full flex-wrap items-center gap-1.5 rounded-md px-2.5 py-1 text-start text-sm text-highlighted hover:bg-default hover:ring hover:ring-default focus-visible:outline-2 focus-visible:outline-primary disabled:cursor-not-allowed";
const propertyText = "flex min-h-8 flex-wrap items-center gap-1.5 px-2.5 py-1 text-sm text-highlighted";

const form = reactive({
    title: "",
    description: "",
    color: null as string | null,
    dueDate: "",
    priority: "0",
    assignees: [] as UserSummaryDto[]
});

const isNew = computed(() => props.card === null);
const boardPath = computed(() => `/board/${props.boardId}`);
const cardPath = computed(() => `${boardPath.value}/card/${props.card?.id}`);
const availableTags = computed(() => String(store.SelectedBoard?.id) === String(props.boardId) ? store.SelectedBoard?.tags ?? [] : []);

const liveCard = computed(() => {
    if (!props.card) return null;

    const board = store.SelectedBoard;
    const live = board && Number(board.id) === props.boardId ? board.cards.find((card) => card.id === props.card?.id) : undefined;
    return live ?? props.card;
});

const pullRequestCard = computed(() => {
    const board = store.SelectedBoard;
    if (!board?.isDeveloperBoard || Number(board.id) !== props.boardId) return undefined;

    return board.cards.find((card) => card.id === props.card?.id);
});

const priorityItems = boardPriorityOptions.map((option) => ({ label: option.label, value: option.value }));

const priorityIcon = computed(() => {
    const priority = Number(form.priority);
    const icon = boardPriorityFilters.find((option) => option.value === priority)?.icon ?? "i-lucide-minus";
    if (priority > 0) return { icon, class: "text-error" };

    return { icon, class: "text-muted" };
});

const assigneeList = computed(() => liveCard.value?.assignees ?? form.assignees.map((user) => ({ ...user, completedAt: null })));
const completedAssigneeCount = computed(() => assigneeList.value.filter((user) => user.completedAt).length);

const visibleRelations = computed(() => props.variant === "page"
    ? relationTabs.value
    : relationTabs.value.filter((relation) => relation.value === relationTab.value));

const relationTabs = computed(() => {
    const card = liveCard.value;
    if (!card) return [];

    const tabs = [{ label: "Afhængigheder", value: "dependencies", icon: "i-lucide-git-merge", badge: count((card.waitingOn?.length ?? 0) + (card.blocking?.length ?? 0)) }];
    if (pullRequestCard.value) {
        tabs.push({ label: "Pull requests", value: "pull-requests", icon: "i-lucide-git-pull-request", badge: count(pullRequestCard.value.pullRequests?.length ?? 0) });
        tabs.push({ label: "Epic", value: "epic", icon: "i-lucide-layers", badge: count(pullRequestCard.value.epicProgress?.total ?? 0) });
    }
    if (card.links.length) tabs.push({ label: "Koblinger", value: "links", icon: "i-lucide-link", badge: count(card.links.length) });

    return tabs;
});

const cardColorOptions = computed(() => {
    const options: { key: string; value: string | null; label: string }[] = [
        { key: "none", value: null, label: "Ingen farve" },
        ...(store.SelectedBoard?.colors ?? []).map((color) => ({ key: String(color.id), value: color.color, label: color.name }))
    ];
    if (form.color && !options.some((color) => color.value === form.color)) {
        options.push({ key: "current", value: form.color, label: "Nuværende farve (uden for tavlens palette)" });
    }

    return options;
});
const selectedColorLabel = computed(() => cardColorOptions.value.find((color) => color.value === form.color)?.label ?? "Ingen farve");

const dueDate = computed(() => form.dueDate ? new Date(`${form.dueDate}T12:00:00`) : null);
const dueDateLabel = computed(() => dueDate.value ? new Intl.DateTimeFormat("da-DK", { day: "numeric", month: "short", year: "numeric" }).format(dueDate.value) : "");
const dueDateHint = computed(() => {
    if (!dueDate.value) return "";

    const today = new Date(`${new Intl.DateTimeFormat("sv-SE", { timeZone: "Europe/Copenhagen" }).format(new Date())}T12:00:00`);
    const days = Math.round((dueDate.value.getTime() - today.getTime()) / 86400000);
    if (days === 0) return "I dag";
    if (days > 0) return `Om ${days} ${days === 1 ? "dag" : "dage"}`;

    return `${-days} ${days === -1 ? "dag" : "dage"} siden`;
});

const placements = computed<WorkItemPlacementDto[]>(() => {
    const cardId = props.card?.id;
    if (cardId == null) return [];

    const live = store.SelectedBoard?.cards.find((card) => String(card.id) === String(cardId));

    return live?.placements ?? props.card?.placements ?? [];
});
const currentPlacement = computed(() => placements.value.find((placement) => isCurrentBoard(placement)) ?? null);
const sortedPlacements = computed(() => [...placements.value].sort((a, b) => Number(isCurrentBoard(b)) - Number(isCurrentBoard(a))));
const availableBoards = computed(() => store.Boards.filter((board) => !board.isArchived
    && !placements.value.some((placement) => String(placement.boardId) === String(board.id))));
const placementBoardItems = computed(() => availableBoards.value.map((board) => ({ label: board.title, value: String(board.id) })));
const placementColumnItems = computed(() => placementColumns.value.map((column) => ({ label: column.title, value: String(column.id) })));

const cardMenu = computed<DropdownMenuItem[][]>(() => [
    ...(currentPlacement.value && placements.value.length > 1
        ? [[{ label: "Fjern fra denne tavle", icon: "i-lucide-log-out", onSelect: () => confirmRemovePlacement(currentPlacement.value!) }]]
        : []),
    [{ label: "Slet kort", icon: "i-lucide-trash-2", color: "error" as const, onSelect: () => { deleteOpen.value = true; } }]
]);

const removePlacementDescription = computed(() => {
    const placement = placementToRemove.value;
    if (!placement || !props.card) return "";

    return isCurrentBoard(placement)
        ? `Fjern ${props.card.title} fra denne tavle? Kortet bevares på de øvrige tavler.`
        : `Fjern ${props.card.title} fra tavlen ${placement.boardTitle}?`;
});

const isDirty = computed(() => savedSnapshot.value !== "" && snapshot() !== savedSnapshot.value);

watch(() => [props.card?.id, props.boardId] as const, () => {
    resetForm();
    closePlacementPicker();
    statusColumns.value = {};
    relationTab.value = "dependencies";

    void loadBoards();
}, { immediate: true });

function resetForm() {
    selectedTags.value = (props.card?.tags ?? []).filter((tag) => availableTags.value.some((option) => Number(option.id) === Number(tag.id)));
    form.title = props.card?.title ?? "";
    form.description = props.card?.description ?? "";
    form.color = props.card?.color ?? null;
    form.dueDate = props.card?.dueDate ?? "";
    form.priority = String(props.card?.priority ?? 0);
    form.assignees = [...(props.card?.assignees ?? [])];
    savedSnapshot.value = snapshot();
}

watch(placementBoardId, () => {
    void loadPlacementColumns();
});


function snapshot() {
    return JSON.stringify({
        title: form.title,
        description: htmlToPlainText(form.description) ? form.description : "",
        color: form.color,
        dueDate: form.dueDate,
        priority: form.priority,
        assignees: form.assignees.map((user) => user.id),
        tags: selectedTags.value.map((tag) => `${tag.id}:${tag.name}`)
    });
}

function openOtherCard(card: WorkItemDto) {
    if (saving.value) return;
    if (isDirty.value) {
        notify.error("Gem eller annuller dine ændringer, før du åbner et andet kort.");
        return;
    }

    emit("openCard", card);
}

async function copyLink() {
    const url = new URL(cardPath.value, window.location.origin).toString();
    try {
        await navigator.clipboard.writeText(url);
        notify.success("Linket til kortet er kopieret.");
    } catch {
        notify.error("Linket kunne ikke kopieres.");
    }
}

function openFullPage() {
    if (isDirty.value) {
        notify.error("Gem eller annuller dine ændringer, før du åbner kortet i fuld skærm.");
        return;
    }

    void router.push(cardPath.value);
}

function count(value: number) {
    return value > 0 ? value : undefined;
}

function initials(user: UserSummaryDto) {
    return `${user.firstName?.[0] ?? ""}${user.lastName?.[0] ?? ""}`.toLocaleUpperCase("da-DK");
}

function onShortcut(event: KeyboardEvent) {
    if (event.key !== "Enter" || !(event.metaKey || event.ctrlKey) || !props.canManage) return;

    event.preventDefault();
    event.stopPropagation();
    void save();
}

function placementMenu(placement: WorkItemPlacementDto): DropdownMenuItem[] {
    const label = isCurrentBoard(placement) ? "Fjern fra denne tavle" : `Fjern fra ${placement.boardTitle}`;

    return [{ label, icon: "i-lucide-log-out", color: "error" as const, onSelect: () => confirmRemovePlacement(placement) }];
}

function isCurrentBoard(placement: WorkItemPlacementDto) {
    return String(placement.boardId) === String(props.boardId);
}

async function loadBoards() {
    if (!props.canManage || store.Boards.length > 0) return;

    await store.GET_BOARDS();
}

async function loadPlacementColumns() {
    placementColumnId.value = undefined;
    placementColumns.value = [];
    if (!placementBoardId.value) return;

    const columns = await boardService.getColumns(Number(placementBoardId.value));
    placementColumns.value = columns;
    if (columns[0]) placementColumnId.value = String(columns[0].id);
}

function closePlacementPicker() {
    placementPickerOpen.value = false;
    placementBoardId.value = undefined;
    placementColumnId.value = undefined;
    placementColumns.value = [];
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

function statusItems(placement: WorkItemPlacementDto): DropdownMenuItem[] {
    if (statusLoading.value[placement.boardId]) return [{ label: "Henter statusser…", disabled: true }];

    const columns = statusColumns.value[placement.boardId];
    if (!columns?.length) return [{ label: "Kunne ikke hente statusser. Prøv igen", onSelect: (event: Event) => { event.preventDefault(); void loadStatusColumns(placement, true); } }];

    return columns.map((column) => {
        const isCurrent = String(column.id) === String(placement.columnId);

        return {
            label: column.title,
            icon: isCurrent ? "i-lucide-check" : undefined,
            suffix: column.isDoneColumn ? "Færdig" : undefined,
            disabled: saving.value || isCurrent,
            onSelect: () => { void changePlacementStatus(placement, column); }
        };
    });
}

async function changePlacementStatus(placement: WorkItemPlacementDto, column: BoardColumnDto) {
    if (!props.card || saving.value || String(placement.columnId) === String(column.id)) return;

    saving.value = true;
    try {
        const ok = await boardService.moveCard(Number(placement.boardId), Number(props.card.id), { columnId: Number(column.id), position: 2147483647 });
        if (!ok) {
            notify.error("Status kunne ikke ændres. Prøv igen.");
            return;
        }

        await store.GET_BOARD(props.boardId);
        notify.success(`Status på ${placement.boardTitle} er ændret til ${column.title}.`);
    } finally {
        saving.value = false;
    }
}

async function addPlacement() {
    if (!props.card || !placementBoardId.value || !placementColumnId.value) return;

    saving.value = true;
    try {
        const ok = await store.ADD_PLACEMENT(props.boardId, Number(placementBoardId.value), Number(props.card.id), Number(placementColumnId.value));
        notify.result(ok, "Kortet er tilføjet til tavlen.", "Kortet kunne ikke tilføjes til tavlen.");
        if (ok) closePlacementPicker();
    } finally {
        saving.value = false;
    }
}

function confirmRemovePlacement(placement: WorkItemPlacementDto) {
    placementToRemove.value = placement;
    removePlacementOpen.value = true;
}

async function doRemovePlacement() {
    const placement = placementToRemove.value;
    if (!props.card || !placement) return;

    saving.value = true;
    try {
        const ok = await store.REMOVE_PLACEMENT(props.boardId, Number(placement.boardId), Number(props.card.id));
        if (!ok) {
            notify.error("Kortet kunne ikke fjernes fra tavlen.");
            return;
        }

        removePlacementOpen.value = false;
        if (isCurrentBoard(placement)) {
            notify.success("Kortet er fjernet fra tavlen.");
            emit("removed");
        } else {
            notify.success(`Kortet er fjernet fra ${placement.boardTitle}.`);
        }
    } finally {
        saving.value = false;
    }
}

async function save() {
    if (saving.value) return;
    if (tagInput.value && !tagInput.value.commitPending()) return;

    if (!form.title.trim()) {
        notify.error("Titlen må ikke være tom.");
        return;
    }

    saving.value = true;
    try {
        const base = {
            tagIds: selectedTags.value.filter((tag) => tag.id > 0).map((tag) => Number(tag.id)),
            newTagNames: selectedTags.value.filter((tag) => tag.id < 0).map((tag) => tag.name),
            title: form.title.trim(),
            description: htmlToPlainText(form.description) ? form.description : null,
            color: form.color,
            dueDate: form.dueDate ? form.dueDate : null,
            priority: Number(form.priority)
        };

        const ok = isNew.value
            ? (await store.ADD_CARD(props.boardId, { ...base, assigneeUserIds: form.assignees.map((user) => user.id), columnId: props.columnId, links: [] })) !== null
            : await store.UPDATE_CARD(props.boardId, Number(props.card!.id), base);

        notify.result(ok, "Kortet er gemt.", "Kortet kunne ikke gemmes.");
        if (!ok) return;

        if (props.variant === "modal" || isNew.value) {
            emit("close");
            return;
        }

        savedSnapshot.value = snapshot();
    } finally {
        saving.value = false;
    }
}

async function doRemove() {
    if (!props.card) return;

    saving.value = true;
    try {
        const ok = await store.REMOVE_CARD(props.boardId, Number(props.card.id));
        notify.result(ok, "Kortet er slettet.", "Kortet kunne ikke slettes.");
        if (ok) {
            deleteOpen.value = false;
            emit("removed");
        }
    } finally {
        saving.value = false;
    }
}
</script>
