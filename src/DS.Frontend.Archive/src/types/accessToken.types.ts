export interface AccessTokenDto {
    id: number;
    name: string;
    createdAt: string;
    expiresAt: string | null;
}

export interface AccessTokenCreatedDto extends AccessTokenDto {
    token: string;
}
