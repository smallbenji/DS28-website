export interface OrgColorOption {
    value: string | null;
    label: string;
    hex: string | null;
}

export const ORG_COLORS: OrgColorOption[] = [
    { value: null, label: 'Ingen farve', hex: null },
    { value: '#1f6fb2', label: 'Blå', hex: '#1f6fb2' },
    { value: '#2f9e44', label: 'Grøn', hex: '#2f9e44' },
    { value: '#e0a51c', label: 'Gul', hex: '#e0a51c' },
    { value: '#d95740', label: 'Rød', hex: '#d95740' },
    { value: '#8e5bd1', label: 'Lilla', hex: '#8e5bd1' },
    { value: '#0f9d9d', label: 'Turkis', hex: '#0f9d9d' },
];

export function colorLabel(color: string | null | undefined): string {
    const option = ORG_COLORS.find((candidate) => candidate.value === (color ?? null));
    return option?.label ?? 'Ingen farve';
}

export function tintHex(hex: string | null | undefined): string | undefined {
    if (!hex) return undefined;

    const match = /^#([0-9a-f]{2})([0-9a-f]{2})([0-9a-f]{2})$/i.exec(hex);
    if (!match) return undefined;

    const channels = [match[1], match[2], match[3]].map((part) => {
        const value = parseInt(part, 16);
        return Math.round(value + (255 - value) * 0.88).toString(16).padStart(2, '0');
    });

    return `#${channels.join('')}`;
}