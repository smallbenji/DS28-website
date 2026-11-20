import { inject, type InjectionKey, type Ref } from 'vue';

export type BoardDropTarget =
    | { kind: 'card'; columnId: number; index: number }
    | { kind: 'column'; columnId: number; after: boolean };

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

export function isSameDropTarget(a: BoardDropTarget | null, b: BoardDropTarget | null) {
    if (!a || !b) return a === b;
    if (a.kind === 'card' && b.kind === 'card') return a.columnId === b.columnId && a.index === b.index;
    if (a.kind === 'column' && b.kind === 'column') return a.columnId === b.columnId && a.after === b.after;

    return false;
}
