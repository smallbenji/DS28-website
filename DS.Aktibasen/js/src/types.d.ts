type TeamRole = 'None' | 'Member' | 'Admin';

interface DSUser {
    id: string;
    userName: string;
    email: string;
    name: string;
}

interface DSTeamMember {
    userID: string;
    name: string;
    isAdmin: boolean;
}

interface DSTeamActivity {
    id: number;
    name: string;
}

interface DSTeam {
    id: number;
    name: string;
    role: TeamRole;
    members: DSTeamMember[];
    activities: DSTeamActivity[];
}

interface MeTeamInfo {
    teamId: number;
    teamName: string;
    role: TeamRole;
}

interface MeDTO {
    id: string;
    name: string;
    email: string;
    isActivityAdmin: boolean;
    teams: MeTeamInfo[];
}

interface ActivityDTO {
    id: number;
    name: string;
}

interface createActivityDTO {
    name: string;
}