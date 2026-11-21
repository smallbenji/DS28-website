import { computed, inject, reactive, ref, watch, type InjectionKey, type Ref } from "vue";
import type { BoardDetailDto, WorkItemDto } from "@/types";

export type BoardQuickFilter = "mine" | "overdue" | "waiting";

export const boardQuickFilters: { key: BoardQuickFilter; label: string; icon: string }[] = [
    { key: "mine", label: "Mine kort", icon: "i-lucide-user" },
    { key: "overdue", label: "Overskredet", icon: "i-lucide-calendar-x" },
    { key: "waiting", label: "Venter", icon: "i-lucide-link" }
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
    const quick = reactive<Record<BoardQuickFilter, boolean>>({ mine: false, overdue: false, waiting: false });

    const doneColumnIds = computed(() => new Set((board.value?.columns ?? []).filter((column) => column.isDoneColumn).map((column) => Number(column.id))));
    const activeCount = computed(() => tagIds.value.length + Object.values(quick).filter(Boolean).length);
    const isFiltering = computed(() => activeCount.value > 0 || search.value.trim() !== "");

    function matchesSearch(card: WorkItemDto, query: string) {
        if (!query) return true;
        if (card.title.toLowerCase().includes(query)) return true;

        const number = query.replace(/^ds-?/, "");

        return number !== "" && String(card.id) === number;
    }

    function matches(card: WorkItemDto, today: string) {
        if (!matchesSearch(card, search.value.trim().toLowerCase())) return false;
        if (tagIds.value.length && !card.tags?.some((tag) => tagIds.value.includes(Number(tag.id)))) return false;
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

    function clearFilters() {
        tagIds.value = [];
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

    return reactive({ search, tagIds, quick, activeCount, isFiltering, filteredCards, toggleTag, clearFilters, clearAll });
}

export type BoardFilters = ReturnType<typeof useBoardFilters>;
