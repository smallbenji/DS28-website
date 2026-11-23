import type {
    BoardColumnCreateDto,
    BoardColorUpdateDto,
    BoardTagUpdateDto,
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
    WorkItemSearchResultDto,
    WorkItemUpdateDto
} from "@/types";
import { apiDelete, apiGet, apiPost, apiPut, safeApiCall } from "@/lib/api";

export default class BoardService {
    public async addPullRequest(boardId: number, cardId: number, url: string): Promise<void> {
        await apiPost(`/api/v1/boards/${boardId}/cards/${cardId}/pull-requests`, { url });
    }

    public async removePullRequest(boardId: number, cardId: number, url: string): Promise<void> {
        await apiDelete(`/api/v1/boards/${boardId}/cards/${cardId}/pull-requests`, { params: { url } });
    }

    public async setEpicMarking(boardId: number, cardId: number, isEpic: boolean): Promise<void> {
        const path = `/api/v1/boards/${boardId}/cards/${cardId}/epic`;
        if (isEpic) {
            await apiPut(path);
        } else {
            await apiDelete(path);
        }
    }

    public async setCardEpic(boardId: number, cardId: number, epicWorkItemId: number): Promise<void> {
        await apiPut(`/api/v1/boards/${boardId}/cards/${cardId}/epic-link`, { epicWorkItemId });
    }

    public async removeCardEpic(boardId: number, cardId: number): Promise<void> {
        await apiDelete(`/api/v1/boards/${boardId}/cards/${cardId}/epic-link`);
    }

    public async addAssignee(boardId: number, cardId: number, userId: string): Promise<void> {
        await apiPost(`/api/v1/boards/${boardId}/cards/${cardId}/assignees`, { userId });
    }

    public async removeAssignee(boardId: number, cardId: number, userId: string): Promise<void> {
        await apiDelete(`/api/v1/boards/${boardId}/cards/${cardId}/assignees/${encodeURIComponent(userId)}`);
    }

    public async setCompletion(boardId: number, cardId: number, completed: boolean): Promise<void> {
        const path = `/api/v1/boards/${boardId}/cards/${cardId}/completion`;
        if (completed) {
            await apiPut(path);
        } else {
            await apiDelete(path);
        }
    }

    public async getBoards(): Promise<BoardSummaryDto[]> {
        return safeApiCall(async () => {
            const data = await apiGet<BoardSummaryDto[]>("/api/v1/boards");
            return Array.isArray(data) ? data : [];
        }, []);
    }

    public async searchAssignees(search: string, boardId: number): Promise<BoardMemberDto[]> {
        return safeApiCall(async () => {
            const data = await apiGet<BoardMemberDto[]>("/api/v1/boards/assignees", { params: { search, boardId } });
            return Array.isArray(data) ? data : [];
        }, []);
    }

    public async getBoard(id: number): Promise<BoardDetailDto | null> {
        return safeApiCall(async () => {
            const data = await apiGet<BoardDetailDto>(`/api/v1/boards/${id}`);
            return data ? data : null;
        }, null);
    }

    public async getMembers(boardId: number): Promise<BoardMemberDto[]> {
        return await apiGet<BoardMemberDto[]>(`/api/v1/boards/${boardId}/members`);
    }

    public async createTag(boardId: number, data: BoardTagUpdateDto): Promise<void> {
        await apiPost<void>(`/api/v1/boards/${boardId}/tags`, data);
    }

    public async updateTag(boardId: number, tagId: number, data: BoardTagUpdateDto): Promise<void> {
        await apiPut<void>(`/api/v1/boards/${boardId}/tags/${tagId}`, data);
    }

    public async deleteTag(boardId: number, tagId: number): Promise<void> {
        await apiDelete<void>(`/api/v1/boards/${boardId}/tags/${tagId}`);
    }

    public async createColor(boardId: number, data: BoardColorUpdateDto): Promise<void> {
        await apiPost<void>(`/api/v1/boards/${boardId}/colors`, data);
    }

    public async updateColor(boardId: number, colorId: number, data: BoardColorUpdateDto): Promise<void> {
        await apiPut<void>(`/api/v1/boards/${boardId}/colors/${colorId}`, data);
    }

    public async deleteColor(boardId: number, colorId: number): Promise<void> {
        await apiDelete<void>(`/api/v1/boards/${boardId}/colors/${colorId}`);
    }

    public async addMember(boardId: number, userId: string): Promise<void> {
        await apiPost<void>(`/api/v1/boards/${boardId}/members`, { userId });
    }

    public async removeMember(boardId: number, userId: string): Promise<void> {
        await apiDelete<void>(`/api/v1/boards/${boardId}/members/${encodeURIComponent(userId)}`);
    }

    public async getColumns(boardId: number): Promise<BoardColumnDto[]> {
        return safeApiCall(async () => {
            const data = await apiGet<BoardColumnDto[]>(`/api/v1/boards/${boardId}/columns`);
            return Array.isArray(data) ? data : [];
        }, []);
    }

    public async createBoard(data: BoardCreateDto): Promise<number | null> {
        return safeApiCall(async () => {
            const id = Number(await apiPost<number>("/api/v1/boards", data));
            return Number.isFinite(id) ? id : null;
        }, null);
    }

    public async updateBoard(id: number, data: BoardUpdateDto): Promise<boolean> {
        return safeApiCall(async () => {
            await apiPut<void>(`/api/v1/boards/${id}`, data);
            return true;
        }, false);
    }

    public async deleteBoard(id: number): Promise<boolean> {
        return safeApiCall(async () => {
            await apiDelete<void>(`/api/v1/boards/${id}`);
            return true;
        }, false);
    }

    public async createColumn(boardId: number, data: BoardColumnCreateDto): Promise<number | null> {
        return safeApiCall(async () => {
            const id = Number(await apiPost<number>(`/api/v1/boards/${boardId}/columns`, data));
            return Number.isFinite(id) ? id : null;
        }, null);
    }

    public async updateColumn(boardId: number, columnId: number, data: BoardColumnUpdateDto): Promise<boolean> {
        return safeApiCall(async () => {
            await apiPut<void>(`/api/v1/boards/${boardId}/columns/${columnId}`, data);
            return true;
        }, false);
    }

    public async reorderColumns(boardId: number, columnOrder: number[]): Promise<boolean> {
        return safeApiCall(async () => {
            await apiPut<void>(`/api/v1/boards/${boardId}/columns/reorder`, columnOrder.map((columnId) => Number(columnId)));
            return true;
        }, false);
    }

    public async deleteColumn(boardId: number, columnId: number): Promise<boolean> {
        return safeApiCall(async () => {
            await apiDelete<void>(`/api/v1/boards/${boardId}/columns/${columnId}`);
            return true;
        }, false);
    }

    public async createCard(boardId: number, data: WorkItemCreateDto): Promise<number | null> {
        return safeApiCall(async () => {
            const id = Number(await apiPost<number>(`/api/v1/boards/${boardId}/cards`, data));
            return Number.isFinite(id) ? id : null;
        }, null);
    }

    public async updateCard(boardId: number, cardId: number, data: WorkItemUpdateDto): Promise<boolean> {
        return safeApiCall(async () => {
            await apiPut<void>(`/api/v1/boards/${boardId}/cards/${cardId}`, data);
            return true;
        }, false);
    }

    public async moveCard(boardId: number, cardId: number, data: WorkItemMoveDto): Promise<boolean> {
        return safeApiCall(async () => {
            await apiPut<void>(`/api/v1/boards/${boardId}/cards/${cardId}/move`, data);
            return true;
        }, false);
    }

    public async removeCard(boardId: number, cardId: number): Promise<boolean> {
        return safeApiCall(async () => {
            await apiDelete<void>(`/api/v1/boards/${boardId}/cards/${cardId}`);
            return true;
        }, false);
    }

    public async createPlacement(boardId: number, cardId: number, data: WorkItemPlacementCreateDto): Promise<boolean> {
        return safeApiCall(async () => {
            await apiPost<void>(`/api/v1/boards/${boardId}/cards/${cardId}/placements`, data);
            return true;
        }, false);
    }

    public async searchCards(search: string): Promise<WorkItemSearchResultDto[]> {
        return safeApiCall(async () => {
            const data = await apiGet<WorkItemSearchResultDto[]>("/api/v1/boards/cards/search", { params: { search } });
            return Array.isArray(data) ? data : [];
        }, []);
    }

    public async addDependency(boardId: number, cardId: number, dependsOnWorkItemId: number): Promise<void> {
        await apiPost<void>(`/api/v1/boards/${boardId}/cards/${cardId}/dependencies`, { dependsOnWorkItemId });
    }

    public async removeDependency(boardId: number, cardId: number, otherCardId: number): Promise<boolean> {
        return safeApiCall(async () => {
            await apiDelete<void>(`/api/v1/boards/${boardId}/cards/${cardId}/dependencies/${otherCardId}`);
            return true;
        }, false);
    }
}
