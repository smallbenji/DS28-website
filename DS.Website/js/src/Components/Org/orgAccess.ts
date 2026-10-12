import type { OrgPositionDto } from '@/types';

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

    const manager = positions.find((candidate) => candidate.id === managedPositionId);
    return manager?.holders.some((holder) => holder.userId === currentUserId) ?? false;
}

export function canCreateChild(
    position: OrgPositionDto,
    currentUserId: string,
    isSysAdmin: boolean
): boolean {
    if (isSysAdmin) return true;

    return position.holders.some((holder) => holder.userId === currentUserId);
}
