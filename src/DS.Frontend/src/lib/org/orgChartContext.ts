import { computed, inject, provide, ref, type ComputedRef, type InjectionKey, type Ref } from 'vue';
import type { OrgNodeModel, OrgPositionCreateDto, OrgPositionDto } from '@/types';
import { canCreateChild, canManagePosition } from '@/lib/org/orgAccess';
import { collectAncestors, collectDescendantIds, flattenOrgTree, matchesOrgSearch } from '@/lib/org/orgTree';

export interface OrgChartOptions {
    positions: Ref<OrgPositionDto[]>;
    roots: ComputedRef<OrgNodeModel[]>;
    currentUserId: ComputedRef<string>;
    isSysAdmin: ComputedRef<boolean>;
    dragEnabled: ComputedRef<boolean>;
    groupItems: ComputedRef<{ label: string; value: string }[]>;
    onSelect: (positionId: number) => void;
    onCreate: (data: OrgPositionCreateDto) => Promise<boolean>;
    onMove: (positionId: number, parentPositionId: number) => Promise<void>;
}

export function createOrgChartContext(options: OrgChartOptions) {
    const query = ref('');
    const collapsed = ref<Set<number>>(new Set());
    const creatingUnder = ref<number | null>(null);
    const creating = ref(false);
    const highlightedId = ref<number | null>(null);
    const draggedId = ref<number | null>(null);
    const dropTargetId = ref<number | null>(null);

    const flatNodes = computed(() => flattenOrgTree(options.roots.value));
    const matchedIds = computed(() => new Set(flatNodes.value
        .filter(({ node }) => matchesOrgSearch(node, query.value))
        .map(({ node }) => node.position.id)));
    const searching = computed(() => query.value.trim().length > 0);
    const expandedBySearch = computed(() => {
        const result = new Set<number>();
        matchedIds.value.forEach((id) => collectAncestors(id, options.positions.value).forEach((ancestor) => result.add(ancestor.id)));

        return result;
    });

    function isCollapsed(positionId: number): boolean {
        return collapsed.value.has(positionId) && !expandedBySearch.value.has(positionId);
    }

    function hiddenCount(positionId: number): number {
        return collectDescendantIds(positionId, options.positions.value).size;
    }

    function isMatch(positionId: number): boolean {
        return matchedIds.value.has(positionId);
    }

    function isDimmed(positionId: number): boolean {
        return searching.value && !matchedIds.value.has(positionId);
    }

    function toggle(positionId: number) {
        const next = new Set(collapsed.value);
        if (next.has(positionId)) next.delete(positionId);
        else next.add(positionId);
        collapsed.value = next;
    }

    function collapseAll() {
        collapsed.value = new Set(flatNodes.value
            .filter(({ node, depth }) => depth > 0 && node.children.length > 0)
            .map(({ node }) => node.position.id));
    }

    function expandAll() {
        collapsed.value = new Set();
    }

    function expand(positionId: number) {
        if (!collapsed.value.has(positionId)) return;

        const next = new Set(collapsed.value);
        next.delete(positionId);
        collapsed.value = next;
    }

    function canCreate(position: OrgPositionDto): boolean {
        return canCreateChild(position, options.positions.value, options.currentUserId.value, options.isSysAdmin.value);
    }

    function canDrag(position: OrgPositionDto): boolean {
        return options.dragEnabled.value
            && position.paForPositionId === null
            && canManagePosition(position, options.positions.value, options.currentUserId.value, options.isSysAdmin.value);
    }

    function canDrop(target: OrgPositionDto): boolean {
        const dragged = draggedId.value === null ? undefined : options.positions.value.find((position) => position.id === draggedId.value);
        if (!dragged || target.id === dragged.id || dragged.parentPositionId === target.id) return false;
        if (collectDescendantIds(dragged.id, options.positions.value).has(target.id)) return false;

        return canCreate(target);
    }

    function startCreate(positionId: number) {
        expand(positionId);
        creatingUnder.value = positionId;
    }

    function cancelCreate() {
        creatingUnder.value = null;
    }

    async function create(data: Omit<OrgPositionCreateDto, 'parentPositionId'>) {
        const parentPositionId = creatingUnder.value;
        if (parentPositionId === null) return;

        creating.value = true;
        try {
            if (await options.onCreate({ ...data, parentPositionId })) creatingUnder.value = null;
        } finally {
            creating.value = false;
        }
    }

    function handleDragStart(event: DragEvent, position: OrgPositionDto) {
        if (!canDrag(position)) return;

        event.dataTransfer?.setData('text/plain', String(position.id));
        if (event.dataTransfer) event.dataTransfer.effectAllowed = 'move';
        draggedId.value = position.id;
        creatingUnder.value = null;
    }

    function handleDragOver(event: DragEvent, position: OrgPositionDto) {
        if (!canDrop(position)) return;

        event.preventDefault();
        if (event.dataTransfer) event.dataTransfer.dropEffect = 'move';
        dropTargetId.value = position.id;
    }

    function handleDragLeave(event: DragEvent, position: OrgPositionDto) {
        const related = event.relatedTarget as Node | null;
        if (related && (event.currentTarget as HTMLElement).contains(related)) return;
        if (dropTargetId.value === position.id) dropTargetId.value = null;
    }

    function endDrag() {
        draggedId.value = null;
        dropTargetId.value = null;
    }

    async function drop(target: OrgPositionDto) {
        const positionId = draggedId.value;
        const valid = canDrop(target);
        endDrag();
        if (positionId === null || !valid) return;

        expand(target.id);
        await options.onMove(positionId, target.id);
    }

    function highlight(positionId: number) {
        highlightedId.value = positionId;
        setTimeout(() => {
            if (highlightedId.value === positionId) highlightedId.value = null;
        }, 1600);
    }

    return {
        query,
        searching,
        matchCount: computed(() => matchedIds.value.size),
        creatingUnder,
        creating,
        highlightedId,
        draggedId,
        dropTargetId,
        groupItems: options.groupItems,
        isCollapsed,
        hiddenCount,
        isMatch,
        isDimmed,
        toggle,
        collapseAll,
        expandAll,
        canCreate,
        canDrag,
        canDrop,
        startCreate,
        cancelCreate,
        create,
        handleDragStart,
        handleDragOver,
        handleDragLeave,
        endDrag,
        drop,
        highlight,
        select: options.onSelect
    };
}

export type OrgChartContext = ReturnType<typeof createOrgChartContext>;

const orgChartKey: InjectionKey<OrgChartContext> = Symbol('orgChart');

export function provideOrgChart(context: OrgChartContext) {
    provide(orgChartKey, context);
}

export function useOrgChart(): OrgChartContext {
    const context = inject(orgChartKey);
    if (!context) throw new Error('useOrgChart must be used inside the organisation chart.');

    return context;
}
