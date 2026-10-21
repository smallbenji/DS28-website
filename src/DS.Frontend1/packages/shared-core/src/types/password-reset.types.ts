export interface ResetPasswordLinkDto {
    link: string;
    email: string;
}

export interface ResetPasswordDto {
    userId: string;
    token: string;
    newPassword: string;
}
