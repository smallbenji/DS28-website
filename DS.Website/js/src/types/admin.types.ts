import type { HQPanelEntryDto } from './home.types';

export interface AdminViewModelDto {
    sections: AdminSectionDto[];
    signupProgress: SignupProgressDto[];
}

export interface AdminSectionDto {
    title: string;
    icon: string;
    entries: HQPanelEntryDto[];
}

export interface SignupProgressDto {
    key: string;
    label: string;
    current: number;
    target: number;
    status: string;
}
