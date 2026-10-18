import type { AxiosResponse } from "axios";
import axios from "axios";
import type {
    BoardColumnCreateDto,
    BoardColumnDto,
    BoardColumnUpdateDto,
    BoardCreateDto,
    BoardDetailDto,
    BoardSummaryDto,
    BoardUpdateDto,
    BoardMemberDto,
    WorkItemCreateDto,
    WorkItemMoveDto,
    WorkItemPlacementCreateDto,
    WorkItemUpdateDto
} from "@/types";

export default class BoardService {
    public async getBoards(): Promise<BoardSummaryDto[]> {
        try {
            const response: AxiosResponse<BoardSummaryDto[]> = await axios({
                url: "/api/v1/boards",
                method: "GET"
            });
            return Array.isArray(response.data) ? response.data : [];
        } catch {
            return [];
        }
    }

    public async searchAssignees(search: string, boardId: number): Promise<BoardMemberDto[]> {
        try {
            const response: AxiosResponse<BoardMemberDto[]> = await axios({
                url: "/api/v1/boards/assignees",
                method: "GET",
                params: { search, boardId }
            });
            return Array.isArray(response.data) ? response.data : [];
        } catch {
            return [];
        }
    }

    public async getBoard(id: number): Promise<BoardDetailDto | null> {
        try {
            const response: AxiosResponse<BoardDetailDto> = await axios({
                url: `/api/v1/boards/${id}`,
                method: "GET"
            });
            return response.data ? response.data : null;
        } catch {
            return null;
        }
    }

    public async getMembers(boardId: number): Promise<BoardMemberDto[]> {
        const response = await axios.get<BoardMemberDto[]>(`/api/v1/boards/${boardId}/members`);
        return response.data;
    }

    public async addMember(boardId: number, userId: string): Promise<void> {
        await axios.post(`/api/v1/boards/${boardId}/members`, { userId });
    }

    public async getColumns(boardId: number): Promise<BoardColumnDto[]> {
        try {
            const response: AxiosResponse<BoardColumnDto[]> = await axios({
                url: `/api/v1/boards/${boardId}/columns`,
                method: "GET"
            });
            return Array.isArray(response.data) ? response.data : [];
        } catch {
            return [];
        }
    }

    public async createBoard(data: BoardCreateDto): Promise<number | null> {
        try {
            const response: AxiosResponse<number> = await axios({
                url: "/api/v1/boards",
                method: "POST",
                data
            });
            const id = Number(response.data);
            return Number.isFinite(id) ? id : null;
        } catch {
            return null;
        }
    }

    public async updateBoard(id: number, data: BoardUpdateDto): Promise<boolean> {
        try {
            await axios({
                url: `/api/v1/boards/${id}`,
                method: "PUT",
                data
            });
            return true;
        } catch {
            return false;
        }
    }

    public async deleteBoard(id: number): Promise<boolean> {
        try {
            await axios({
                url: `/api/v1/boards/${id}`,
                method: "DELETE"
            });
            return true;
        } catch {
            return false;
        }
    }

    public async createColumn(boardId: number, data: BoardColumnCreateDto): Promise<number | null> {
        try {
            const response: AxiosResponse<number> = await axios({
                url: `/api/v1/boards/${boardId}/columns`,
                method: "POST",
                data
            });
            const id = Number(response.data);
            return Number.isFinite(id) ? id : null;
        } catch {
            return null;
        }
    }

    public async updateColumn(boardId: number, columnId: number, data: BoardColumnUpdateDto): Promise<boolean> {
        try {
            await axios({
                url: `/api/v1/boards/${boardId}/columns/${columnId}`,
                method: "PUT",
                data
            });
            return true;
        } catch {
            return false;
        }
    }

    public async reorderColumns(boardId: number, columnOrder: string[]): Promise<boolean> {
        try {
            await axios({
                url: `/api/v1/boards/${boardId}/columns/reorder`,
                method: "PUT",
                data: columnOrder.map((columnId) => Number(columnId))
            });
            return true;
        } catch {
            return false;
        }
    }

    public async deleteColumn(boardId: number, columnId: number): Promise<boolean> {
        try {
            await axios({
                url: `/api/v1/boards/${boardId}/columns/${columnId}`,
                method: "DELETE"
            });
            return true;
        } catch {
            return false;
        }
    }

    public async createCard(boardId: number, data: WorkItemCreateDto): Promise<number | null> {
        try {
            const response: AxiosResponse<number> = await axios({
                url: `/api/v1/boards/${boardId}/cards`,
                method: "POST",
                data
            });
            const id = Number(response.data);
            return Number.isFinite(id) ? id : null;
        } catch {
            return null;
        }
    }

    public async updateCard(boardId: number, cardId: number, data: WorkItemUpdateDto): Promise<boolean> {
        try {
            await axios({
                url: `/api/v1/boards/${boardId}/cards/${cardId}`,
                method: "PUT",
                data
            });
            return true;
        } catch {
            return false;
        }
    }

    public async moveCard(boardId: number, cardId: number, data: WorkItemMoveDto): Promise<boolean> {
        try {
            await axios({
                url: `/api/v1/boards/${boardId}/cards/${cardId}/move`,
                method: "PUT",
                data
            });
            return true;
        } catch {
            return false;
        }
    }

    public async removeCard(boardId: number, cardId: number): Promise<boolean> {
        try {
            await axios({
                url: `/api/v1/boards/${boardId}/cards/${cardId}`,
                method: "DELETE"
            });
            return true;
        } catch {
            return false;
        }
    }

    public async createPlacement(boardId: number, cardId: number, data: WorkItemPlacementCreateDto): Promise<boolean> {
        try {
            await axios({
                url: `/api/v1/boards/${boardId}/cards/${cardId}/placements`,
                method: "POST",
                data
            });
            return true;
        } catch {
            return false;
        }
    }
}
