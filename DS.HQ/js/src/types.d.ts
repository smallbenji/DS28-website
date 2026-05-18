interface KcUser {
    id: string;
    firstName: string;
    lastName: string;
    userName: string;
    email: string;
}

interface DSUser {
    user: KcUser;
    groupNumber: string;
    roles: any[];
}

interface DSGroup {
    id: number;
    name: string;
}