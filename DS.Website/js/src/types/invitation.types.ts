export interface UserInvitationDto {
    id: number;
    invitationId: string;
    email: string;
    roles: string[];
    used: boolean;
}

export interface UserInvitationCreationDto {
    firstName: string;
    lastName: string;
    password: string;
}
