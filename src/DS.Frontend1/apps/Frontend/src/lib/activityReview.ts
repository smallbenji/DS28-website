import type { ActivityReviewStatus } from '@ds28/shared-core/types';

export const activityReviewStatuses: { value: ActivityReviewStatus; label: string; type: string }[] = [
    { value: 'Draft', label: 'Kladde', type: 'is-light' },
    { value: 'Submitted', label: 'Afventer godkendelse', type: 'is-info is-light' },
    { value: 'ChangesRequested', label: 'Ændringer ønskes', type: 'is-warning is-light' },
    { value: 'Approved', label: 'Godkendt', type: 'is-success is-light' }
];

export function activityReviewStatus(status: ActivityReviewStatus) {
    return activityReviewStatuses.find((option) => option.value === status) ?? activityReviewStatuses[0]!;
}
