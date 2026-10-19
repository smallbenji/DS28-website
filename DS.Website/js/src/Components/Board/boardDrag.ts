import { inject, type InjectionKey, type Ref } from 'vue';

export type BoardDropTarget = {
    kind: 'card' | 'column' | 'append';
    id: number;
    after: boolean;
};

export type BoardDragState = {
    kind: 'card' | 'column';
    id: number;
    target: BoardDropTarget | null;
};

export const boardDragKey: InjectionKey<Ref<BoardDragState | null>> = Symbol('boardDrag');

export function useBoardDrag() {
    const drag = inject(boardDragKey);
    if (!drag) throw new Error('Board drag state is missing.');

    return drag;
}
