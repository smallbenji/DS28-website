interface KcUser {
    id: string;
    firstName: string;
    lastName: string;
}

interface DSUser {
    user: KcUser;
    groupNumber: string;
    roles: any[];
}