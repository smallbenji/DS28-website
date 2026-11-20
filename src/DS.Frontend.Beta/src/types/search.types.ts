export type SearchResultType = 'board' | 'group' | 'activity';

export interface SearchResultDto {
    type: SearchResultType;
    id: number;
    title: string;
    subtitle: string | null;
    url: string;
}
