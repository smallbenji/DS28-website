import axios from 'axios';
import type { PatrolDto, ScoutDto } from '@/types';

export default class GroupFinalSignupService {
    async createScout(name: string, birthday: string, gender: 'Male' | 'Female'): Promise<ScoutDto> {
        const { data } = await axios.post<ScoutDto>('/api/v1/group/scouts', { name, birthday, gender });
        return data;
    }

    async deleteScout(scoutId: number): Promise<void> {
        await axios.delete(`/api/v1/group/scouts/${scoutId}`);
    }

    async createPatrol(name: string): Promise<PatrolDto> {
        const { data } = await axios.post<PatrolDto>('/api/v1/group/patrols', { name });
        return data;
    }

    async deletePatrol(patrolId: number): Promise<void> {
        await axios.delete(`/api/v1/group/patrols/${patrolId}`);
    }

    async addScoutToPatrol(scoutId: number, patrolId: number): Promise<void> {
        await axios.post('/api/v1/group/scouts/add-patrol', { scoutId, patrolId });
    }

    async removeScoutFromPatrol(scoutId: number, patrolId: number): Promise<void> {
        await axios.post('/api/v1/group/scouts/remove-patrol', { scoutId, patrolId });
    }
}
