export interface OpenIddictClientUpdateDto {
    clientId: string;
    displayName: string;
    clientType: 'public' | 'confidential';
    redirectUris: string[];
    scopes: string[];
    requiredRoles: string[];
    requirePkce: boolean;
}

export interface OpenIddictClientDto extends OpenIddictClientUpdateDto {
    id: string;
}

export interface OpenIddictClientSavedDto {
    client: OpenIddictClientDto;
    clientSecret: string | null;
}
