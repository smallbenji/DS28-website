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