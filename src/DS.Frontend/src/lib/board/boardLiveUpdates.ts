import { onBeforeUnmount } from "vue";
import type { HubConnection } from "@microsoft/signalr";

export function useBoardLiveUpdates(boardId: number, onBoardChanged: () => void) {
    let connection: HubConnection | null = null;

    async function connect() {
        const { HubConnectionBuilder } = await import("@microsoft/signalr");
        const hub = new HubConnectionBuilder()
            .withUrl("/api/v1/boards/hub")
            .withAutomaticReconnect()
            .build();

        hub.on("boardChanged", (changedBoardId: number) => {
            if (Number(changedBoardId) === boardId) onBoardChanged();
        });
        hub.onreconnected(() => {
            void hub.invoke("JoinBoard", boardId);
        });

        connection = hub;
        try {
            await hub.start();
            await hub.invoke("JoinBoard", boardId);
        } catch {
            connection = null;
        }
    }

    onBeforeUnmount(() => {
        void connection?.stop();
    });

    return { connect };
}
