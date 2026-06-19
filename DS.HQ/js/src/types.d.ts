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