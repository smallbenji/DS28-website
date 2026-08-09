export interface HomeViewModelDto {
    shortcuts: HQPanelEntryDto[];
}

export interface HQPanelEntryDto {
    title: string;
    url: string;
    icon: string[];
    requiredRole: string;
    requiredRoles: string[];
}
