import type { OrgPositionDto } from '@/types';

export function canManagePosition(
    position: OrgPositionDto,
    positions: OrgPositionDto[],
    currentUserId: string,
    isSysAdmin: boolean
): boolean {
    if (isSysAdmin) return true;
    if (position.isSystem) return false;
    if (position.parentPositionId === null) return false;

    const parent = positions.find((candidate) => candidate.id === position.parentPositionId);
    return parent?.holders.some((holder) => holder.userId === currentUserId) ?? false;
}

export function canCreateChild(
    position: OrgPositionDto,
    currentUserId: string,
    isSysAdmin: boolean
): boolean {
    if (isSysAdmin) return true;

    return position.holders.some((holder) => holder.userId === currentUserId);
}
