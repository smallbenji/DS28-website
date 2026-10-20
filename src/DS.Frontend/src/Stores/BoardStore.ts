import BoardService from "@/Services/BoardService";
import { defineStore } from "pinia";
import { ref } from "vue";
import type {
    BoardColumnCreateDto,
    BoardColumnUpdateDto,
    BoardCreateDto,
    BoardDetailDto,
    BoardSummaryDto,
    BoardUpdateDto,
    WorkItemCreateDto,
    WorkItemUpdateDto
} from "@/types";

export const useBoardStore = defineStore("board", () => {
    const boardService = new BoardService();
    const Boards = ref<BoardSummaryDto[]>([]);
    const SelectedBoard = ref<BoardDetailDto | null>(null);

    async function GET_BOARDS() {
        const data = await boardService.getBoards();
        Boards.value = data;
        return data;
    }

    async function GET_BOARD(id: number) {
        const data = await boardService.getBoard(id);
        SelectedBoard.value = data;
        return data;
    }

    async function CREATE_BOARD(data: BoardCreateDto) {
        const id = await boardService.createBoard(data);
        if (id !== null) {
            await GET_BOARDS();
        }
        return id;
    }

    async function UPDATE_BOARD(id: number, data: BoardUpdateDto) {
        const success = await boardService.updateBoard(id, data);
        if (success) {
            await GET_BOARDS();
            await GET_BOARD(id);
        }
        return success;
    }

    async function DELETE_BOARD(id: number) {
        const success = await boardService.deleteBoard(id);
        if (success) {
            await GET_BOARDS();
        }
        return success;
    }

    async function ADD_COLUMN(boardId: number, data: BoardColumnCreateDto) {
        const id = await boardService.createColumn(boardId, data);
        if (id !== null) {
            await GET_BOARD(boardId);
        }
        return id;
    }

    async function UPDATE_COLUMN(boardId: number, columnId: number, data: BoardColumnUpdateDto) {
        const success = await boardService.updateColumn(boardId, columnId, data);
        if (success) {
            await GET_BOARD(boardId);
        }
        return success;
    }

    async function DELETE_COLUMN(boardId: number, columnId: number) {
        const success = await boardService.deleteColumn(boardId, columnId);
        if (success) {
            await GET_BOARD(boardId);
        }
        return success;
    }

    async function REORDER_COLUMNS(boardId: number, columnOrder: string[]) {
        const success = await boardService.reorderColumns(boardId, columnOrder);
        if (success) {
            await GET_BOARD(boardId);
        }
        return success;
    }

    async function ADD_CARD(boardId: number, data: WorkItemCreateDto) {
        const id = await boardService.createCard(boardId, data);
        if (id !== null) {
            await GET_BOARD(boardId);
        }
        return id;
    }

    async function UPDATE_CARD(boardId: number, cardId: number, data: WorkItemUpdateDto) {
        const success = await boardService.updateCard(boardId, cardId, data);
        if (success) {
            await GET_BOARD(boardId);
        }
        return success;
    }

    async function MOVE_CARD(boardId: number, cardId: number, columnId: number, position: number) {
        const success = await boardService.moveCard(boardId, cardId, { columnId, position });
        if (success) {
            await GET_BOARD(boardId);
        }
        return success;
    }

    async function REMOVE_CARD(boardId: number, cardId: number) {
        const success = await boardService.removeCard(boardId, cardId);
        if (success) {
            await GET_BOARD(boardId);
        }
        return success;
    }

    async function ADD_PLACEMENT(currentBoardId: number, targetBoardId: number, cardId: number, columnId: number) {
        const success = await boardService.createPlacement(targetBoardId, cardId, { columnId });
        if (success) {
            await GET_BOARD(currentBoardId);
        }
        return success;
    }

    async function REMOVE_PLACEMENT(currentBoardId: number, placementBoardId: number, cardId: number) {
        const success = await boardService.removeCard(placementBoardId, cardId);
        if (success) {
            await GET_BOARD(currentBoardId);
        }
        return success;
    }

    return {
        Boards,
        SelectedBoard,
        GET_BOARDS,
        GET_BOARD,
        CREATE_BOARD,
        UPDATE_BOARD,
        DELETE_BOARD,
        ADD_COLUMN,
        UPDATE_COLUMN,
        DELETE_COLUMN,
        REORDER_COLUMNS,
        ADD_CARD,
        UPDATE_CARD,
        MOVE_CARD,
        REMOVE_CARD,
        ADD_PLACEMENT,
        REMOVE_PLACEMENT
    };
});
