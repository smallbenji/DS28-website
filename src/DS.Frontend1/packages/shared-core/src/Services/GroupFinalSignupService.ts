import type { PatrolDto, ScoutDto } from '../types';
import { apiDelete, apiPost } from '../lib/api';

export default class GroupFinalSignupService {
    async createScout(name: string, birthday: string, gender: 'Male' | 'Female'): Promise<ScoutDto> {
        return await apiPost<ScoutDto>('/api/v1/group/scouts', { name, birthday, gender });
    }

    async deleteScout(scoutId: number): Promise<void> {
        await apiDelete(`/api/v1/group/scouts/${scoutId}`);
    }

    async createPatrol(name: string): Promise<PatrolDto> {
        return await apiPost<PatrolDto>('/api/v1/group/patrols', { name });
    }

    async deletePatrol(patrolId: number): Promise<void> {
        await apiDelete(`/api/v1/group/patrols/${patrolId}`);
    }

    async addScoutToPatrol(scoutId: number, patrolId: number): Promise<void> {
        await apiPost('/api/v1/group/scouts/add-patrol', { scoutId, patrolId });
    }

    async removeScoutFromPatrol(scoutId: number, patrolId: number): Promise<void> {
        await apiPost('/api/v1/group/scouts/remove-patrol', { scoutId, patrolId });
    }
}
