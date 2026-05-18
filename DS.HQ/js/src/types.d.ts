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
}

interface DSUser {
    user: KcUser;
    groupNumber: string;
    roles: KcGroup[];
}

interface DSGroup {
    id: number;
    name: string;
}