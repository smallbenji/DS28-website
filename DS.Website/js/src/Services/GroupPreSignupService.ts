import axios from 'axios';

export interface SignupGroup {
    id: string;
    name: string;
    district: string;
}

export interface ParticipantCounts {
    beaver: number;
    wolf: number;
    junior: number;
    trop: number;
    senior: number;
    rover: number;
    leader: number;
}

export interface GroupPreSignupRequest extends ParticipantCounts {
    groupId: string;
    firstName: string;
    lastName: string;
    email: string;
    password: string;
}

export default class GroupPreSignupService {
    async getOwn(): Promise<{ groupName: string; groupId: string; counts: ParticipantCounts }> {
        const { data } = await axios.get<{
            groupName: string;
            groupId: string;
            counts: Record<keyof ParticipantCounts, string | number>;
        }>('/api/v1/group/pre-signup');
        // The backend serializes numeric properties as strings.
        const counts = Object.fromEntries(
            Object.entries(data.counts).map(([key, value]) => [key, Number(value)])
        ) as unknown as ParticipantCounts;
        return { ...data, counts };
    }

    async updateOwn(counts: ParticipantCounts): Promise<void> {
        await axios.put('/api/v1/group/pre-signup', counts);
    }

    async lookup(groupId: string): Promise<SignupGroup> {
        return (await axios.get<SignupGroup>(`/api/v1/group-pre-signup/${encodeURIComponent(groupId)}`)).data;
    }

    async create(data: GroupPreSignupRequest): Promise<void> {
        await axios.post('/api/v1/group-pre-signup', data);
    }
}
