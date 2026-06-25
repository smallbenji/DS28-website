interface KcUser {
    id: string;
    firstName: string;
    lastName: string;
    userName: string;
    email: string;
}

interface KcGroup {
    id: string;
    name: string;
    path: string;
}

interface DSUser {
    user: KcUser;
    groupNumber: string;
    roles: KcGroup[];
    group: DSGroup | null;
}

interface DSGroup {
    id: number;
    name: string;
    patrols: DSPatrol[];
    scouts?: DSScout[];
}

interface DSPatrol {
    id: number;
    name: string;
}

interface DSScout {
    id: number;
    name: string;
    birthday: string;
    gender: 'Male' | 'Female' | number;
    groupId: number;
    memberships: DSPatrolMembership[];
}

interface DSPatrolMembership {
    id: number;
    scoutId: number;
    patrolId: number;
    joinedDate: string;
    isPatrolLeader: boolean;
}

interface GroupDTO {
    groups: DSGroup[];
    users: Record<string, DSUser[]>;
}

interface InvitationDTO {
    Roles: string[];
    Email: string;
}

interface Invitation {
    id: number;
    invitationId: string;
    email: string;
    roles: string[];
    used: boolean;
}

interface UserInvitationCreationDTO {
    FirstName: string;
    LastName: string;
    Password: string;
}