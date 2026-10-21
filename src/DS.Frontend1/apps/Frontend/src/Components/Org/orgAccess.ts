import type { OrgPositionDto } from '@ds28/shared-core/types';

function collectPositionWithAncestors(positionId: number, positions: OrgPositionDto[]): number[] {
    const result = [positionId];
    const byId = new Map(positions.map((position) => [position.id, position]));

    const visited = new Set<number>();
    let current = positionId;
    while (true) {
        const position = byId.get(current);
        const parentId = position?.parentPositionId ?? null;
        if (parentId === null || visited.has(parentId)) break;

        result.push(parentId);
        visited.add(parentId);
        current = parentId;
    }

    return result;
}

function isManagedBy(positionIds: number[], positions: OrgPositionDto[], currentUserId: string): boolean {
    return positionIds.some((id) => {
        const position = positions.find((candidate) => candidate.id === id);
        return position?.holders.some((holder) => holder.userId === currentUserId) ?? false;
    });
}

export function canManagePosition(
    position: OrgPositionDto,
    positions: OrgPositionDto[],
    currentUserId: string,
    isSysAdmin: boolean
): boolean {
    if (isSysAdmin) return true;
    if (position.isSystem) return false;

    const managedPositionId = position.paForPositionId ?? position.parentPositionId;
    if (managedPositionId === null) return false;

    return isManagedBy(collectPositionWithAncestors(managedPositionId, positions), positions, currentUserId);
}

export function canCreateChild(
    position: OrgPositionDto,
    positions: OrgPositionDto[],
    currentUserId: string,
    isSysAdmin: boolean
): boolean {
    return canManagePosition(position, positions, currentUserId, isSysAdmin);
}
