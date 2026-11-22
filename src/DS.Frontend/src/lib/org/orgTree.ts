import type { OrgNodeModel, OrgPositionDto } from '@/types';

export function buildOrgTree(positions: OrgPositionDto[]): OrgNodeModel[] {
    const paByOwner = new Map<number, OrgPositionDto>();
    positions.forEach((position) => {
        if (position.paForPositionId !== null) paByOwner.set(position.paForPositionId, position);
    });

    const nodes = new Map<number, OrgNodeModel>();
    positions
        .filter((position) => position.paForPositionId === null)
        .forEach((position) => nodes.set(position.id, { position, pa: paByOwner.get(position.id) ?? null, children: [] }));

    const roots: OrgNodeModel[] = [];
    nodes.forEach((node) => {
        const parentId = node.position.parentPositionId;
        const parent = parentId === null ? undefined : nodes.get(parentId);
        if (parent) parent.children.push(node);
        else roots.push(node);
    });

    return roots;
}

export function collectDescendantIds(positionId: number, positions: OrgPositionDto[]): Set<number> {
    const result = new Set<number>();
    const stack = [positionId];
    while (stack.length > 0) {
        const current = stack.pop() as number;
        positions
            .filter((candidate) => candidate.parentPositionId === current && candidate.paForPositionId === null)
            .forEach((candidate) => {
                if (!result.has(candidate.id)) {
                    result.add(candidate.id);
                    stack.push(candidate.id);
                }
            });
    }

    return result;
}

export function collectAncestors(positionId: number, positions: OrgPositionDto[]): OrgPositionDto[] {
    const byId = new Map(positions.map((position) => [position.id, position]));
    const result: OrgPositionDto[] = [];
    const visited = new Set<number>([positionId]);

    let parentId = byId.get(positionId)?.parentPositionId ?? null;
    while (parentId !== null && !visited.has(parentId)) {
        const parent = byId.get(parentId);
        if (!parent) break;

        result.unshift(parent);
        visited.add(parentId);
        parentId = parent.parentPositionId;
    }

    return result;
}

export function flattenOrgTree(roots: OrgNodeModel[]): { node: OrgNodeModel; depth: number }[] {
    const result: { node: OrgNodeModel; depth: number }[] = [];
    const visit = (node: OrgNodeModel, depth: number) => {
        result.push({ node, depth });
        node.children.forEach((child) => visit(child, depth + 1));
    };
    roots.forEach((root) => visit(root, 0));

    return result;
}

function holderMatches(position: OrgPositionDto | null, query: string): boolean {
    return position?.holders.some((holder) => `${holder.firstName} ${holder.lastName}`.toLowerCase().includes(query)) ?? false;
}

export function matchesOrgSearch(node: OrgNodeModel, query: string): boolean {
    const normalized = query.trim().toLowerCase();
    if (!normalized) return false;

    return node.position.title.toLowerCase().includes(normalized)
        || holderMatches(node.position, normalized)
        || holderMatches(node.pa, normalized);
}
