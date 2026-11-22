import { computed, inject, reactive, ref, watch, type InjectionKey, type Ref } from "vue";
import type { BoardDetailDto, WorkItemDto } from "@/types";

export type BoardQuickFilter = "mine" | "overdue" | "waiting";

export const boardQuickFilters: { key: BoardQuickFilter; label: string; icon: string }[] = [
    { key: "mine", label: "Mine kort", icon: "i-lucide-user" },
    { key: "overdue", label: "Overskredet", icon: "i-lucide-calendar-x" },
    { key: "waiting", label: "Venter", icon: "i-lucide-link" }
];

export const boardUnassignedFilterId = "none";

export const boardPriorityFilters: { value: number; label: string; icon: string }[] = [
    { value: 1, label: "Høj", icon: "i-lucide-arrow-up" },
    { value: 0, label: "Normal", icon: "i-lucide-minus" },
    { value: -1, label: "Lav", icon: "i-lucide-arrow-down" }
];

export const boardCompactKey: InjectionKey<Ref<boolean>> = Symbol("boardCompact");

export function useBoardCompact() {
    return inject(boardCompactKey, ref(false));
}

function currentDate() {
    return new Intl.DateTimeFormat("sv-SE", { timeZone: "Europe/Copenhagen" }).format(new Date());
}

export function useBoardFilters(board: Ref<BoardDetailDto | null>, currentUserId: Ref<string | undefined>) {
    const search = ref("");
    const tagIds = ref<number[]>([]);
    const assigneeIds = ref<string[]>([]);
    const priorities = ref<number[]>([]);
    const quick = reactive<Record<BoardQuickFilter, boolean>>({ mine: false, overdue: false, waiting: false });

    const doneColumnIds = computed(() => new Set((board.value?.columns ?? []).filter((column) => column.isDoneColumn).map((column) => Number(column.id))));
    const activeCount = computed(() => tagIds.value.length + assigneeIds.value.length + priorities.value.length + Object.values(quick).filter(Boolean).length);
    const isFiltering = computed(() => activeCount.value > 0 || search.value.trim() !== "");

    function matchesSearch(card: WorkItemDto, query: string) {
        if (!query) return true;
        if (card.title.toLowerCase().includes(query)) return true;

        const number = query.replace(/^ds-?/, "");

        return number !== "" && String(card.id) === number;
    }

    function matchesAssignees(card: WorkItemDto) {
        if (!card.assignees.length) return assigneeIds.value.includes(boardUnassignedFilterId);

        return card.assignees.some((user) => assigneeIds.value.includes(user.id));
    }

    function matches(card: WorkItemDto, today: string) {
        if (!matchesSearch(card, search.value.trim().toLowerCase())) return false;
        if (tagIds.value.length && !card.tags?.some((tag) => tagIds.value.includes(Number(tag.id)))) return false;
        if (assigneeIds.value.length && !matchesAssignees(card)) return false;
        if (priorities.value.length && !priorities.value.includes(Number(card.priority))) return false;
        if (quick.mine && !card.assignees.some((user) => user.id === currentUserId.value)) return false;

        const isDone = doneColumnIds.value.has(Number(card.columnId));
        if (quick.overdue && (isDone || !card.dueDate || card.dueDate >= today)) return false;
        if (quick.waiting && (isDone || !(card.waitingOn ?? []).some((dependency) => !dependency.isDone))) return false;

        return true;
    }

    const filteredCards = computed(() => {
        const cards = board.value?.cards ?? [];
        if (!isFiltering.value) return cards;

        const today = currentDate();

        return cards.filter((card) => matches(card, today));
    });

    function toggleTag(tagId: number) {
        tagIds.value = tagIds.value.includes(tagId) ? tagIds.value.filter((id) => id !== tagId) : [...tagIds.value, tagId];
    }

    function toggleAssignee(userId: string) {
        assigneeIds.value = assigneeIds.value.includes(userId) ? assigneeIds.value.filter((id) => id !== userId) : [...assigneeIds.value, userId];
    }

    function togglePriority(priority: number) {
        priorities.value = priorities.value.includes(priority) ? priorities.value.filter((value) => value !== priority) : [...priorities.value, priority];
    }

    function clearFilters() {
        tagIds.value = [];
        assigneeIds.value = [];
        priorities.value = [];
        quick.mine = false;
        quick.overdue = false;
        quick.waiting = false;
    }

    function clearAll() {
        search.value = "";
        clearFilters();
    }

    watch(() => board.value?.tags, (tags) => {
        const existing = new Set((tags ?? []).map((tag) => Number(tag.id)));
        if (tagIds.value.some((id) => !existing.has(id))) tagIds.value = tagIds.value.filter((id) => existing.has(id));
    });

    watch(() => board.value?.cards, (cards) => {
        if (!cards) return;

        const existing = new Set(cards.flatMap((card) => card.assignees.map((user) => user.id)));
        existing.add(boardUnassignedFilterId);
        if (assigneeIds.value.some((id) => !existing.has(id))) assigneeIds.value = assigneeIds.value.filter((id) => existing.has(id));
    });

    return reactive({
        search,
        tagIds,
        assigneeIds,
        priorities,
        quick,
        currentUserId,
        activeCount,
        isFiltering,
        filteredCards,
        toggleTag,
        toggleAssignee,
        togglePriority,
        clearFilters,
        clearAll
    });
}

export type BoardFilters = ReturnType<typeof useBoardFilters>;
