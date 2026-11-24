#!/usr/bin/env node
import { existsSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { createInterface } from "node:readline";
import http from "node:http";
import https from "node:https";
import { formatBoard, formatBoards, formatCardSearch, matchUser, parseEnvFile, resolveColumn, resolveTagIds, toHtml, validateEpicArguments } from "./lib.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const envPath = join(here, ".env");

if (existsSync(envPath)) {
    for (const [key, value] of Object.entries(parseEnvFile(readFileSync(envPath, "utf8")))) {
        process.env[key] ??= value;
    }
}

const baseUrl = (process.env.DS28_MCP_URL ?? "https://localhost:44310").replace(/\/+$/, "");
const token = process.env.DS28_MCP_TOKEN;
// shortcut: certifikatvalidering er slået fra som standard til den lokale udviklingsserver, sæt DS28_MCP_INSECURE_TLS=0 ved et rigtigt certifikat
const rejectUnauthorized = process.env.DS28_MCP_INSECURE_TLS === "0";

function request(method, path, body) {
    return new Promise((resolve, reject) => {
        const url = new URL(baseUrl + path);
        const payload = body === undefined ? undefined : JSON.stringify(body);
        const headers = { Accept: "application/json" };

        if (token) headers.Authorization = `Bearer ${token}`;
        if (payload !== undefined) {
            headers["Content-Type"] = "application/json";
            headers["Content-Length"] = Buffer.byteLength(payload);
        }

        const transport = url.protocol === "https:" ? https : http;
        const req = transport.request(url, { method, headers, rejectUnauthorized }, response => {
            const chunks = [];
            response.on("data", chunk => chunks.push(chunk));
            response.on("end", () => {
                resolve({
                    status: response.statusCode,
                    location: response.headers.location,
                    body: Buffer.concat(chunks).toString("utf8"),
                });
            });
        });

        req.on("error", reject);
        if (payload !== undefined) req.write(payload);
        req.end();
    });
}

function parseJson(text) {
    try {
        return text ? JSON.parse(text) : null;
    } catch {
        return null;
    }
}

async function api(method, path, body) {
    if (!token) {
        throw new Error("Sæt DS28_MCP_TOKEN i tools/ds28-mcp/.env. Opret tokenet under profilen i DS28.");
    }

    const response = await request(method, path, body);

    if (response.status === 401) {
        throw new Error("Adgangstokenet er ugyldigt, udløbet eller tilbagekaldt.");
    }
    if (response.status === 403) {
        throw new Error("Ingen adgang: kontoen er ikke medlem af tavlen og er ikke SysAdmin/CampAdmin.");
    }
    if (response.status >= 400) {
        const data = parseJson(response.body);
        throw new Error(`DS28 svarede HTTP ${response.status}: ${typeof data === "string" ? data : response.body || "uden fejltekst"}`);
    }

    return parseJson(response.body);
}

const tools = [
    {
        name: "list_boards",
        description: "List DS28 Kanban boards the logged-in user can see, with board id and card count.",
        inputSchema: { type: "object", properties: {}, additionalProperties: false },
        handler: async () => formatBoards(await api("GET", "/api/v1/boards")),
    },
    {
        name: "get_board",
        description: "Read one DS28 Kanban board: columns with ids, colour palette, board tags with ids/names/colours and all cards with DS-number, column, priority, due date, colour, assignees, assigned tags, the cards each card waits on or blocks, PR links and epics on developer boards (epic progress, or the epic a card belongs to), and description.",
        inputSchema: {
            type: "object",
            properties: { boardId: { type: "integer", description: "Board id from list_boards." } },
            required: ["boardId"],
            additionalProperties: false,
        },
        handler: async args => {
            const boardId = Number(args.boardId);
            if (!Number.isInteger(boardId)) throw new Error("boardId skal være et helt tal.");

            return formatBoard(await api("GET", `/api/v1/boards/${boardId}`));
        },
    },
    {
        name: "create_card",
        description: "Create a card on a DS28 Kanban board in a given column. Returns the new card number DS-<id>. On developer boards the card can be marked as an epic or put under an existing epic.",
        inputSchema: {
            type: "object",
            properties: {
                boardId: { type: "integer", description: "Board id from list_boards." },
                title: { type: "string", description: "Card title." },
                columnId: { type: "integer", description: "Column id from get_board. Use either columnId or column." },
                column: { type: "string", description: "Column title from get_board, for example \"Backlog\". Use either columnId or column." },
                description: { type: "string", description: "Plain text or HTML. Plain text becomes paragraphs." },
                priority: { type: "integer", enum: [-1, 0, 1], description: "-1 low, 0 normal, 1 high. Defaults to 0." },
                dueDate: { type: "string", description: "Due date as YYYY-MM-DD." },
                tagIds: { type: ["array", "null"], items: { type: "integer", minimum: 1 }, description: "Tag ids from this board in get_board. Omitted/null means no tags on a new card." },
                isEpic: { type: "boolean", description: "Developer boards only: true marks the new card as an epic. Defaults to false." },
                epicCardId: { type: ["integer", "null"], minimum: 0, description: "Developer boards only: card number of an epic on this board, for example 17 for DS-17, to put the new card under. Omitted, null or 0 means no epic." },
            },
            required: ["boardId", "title"],
            additionalProperties: false,
        },
        handler: async args => {
            const boardId = Number(args.boardId);
            if (!Number.isInteger(boardId)) throw new Error("boardId skal være et helt tal.");

            const priority = Number(args.priority ?? 0);
            if (![-1, 0, 1].includes(priority)) throw new Error("priority skal være -1, 0 eller 1.");

            if (args.dueDate && !/^\d{4}-\d{2}-\d{2}$/.test(args.dueDate)) {
                throw new Error("dueDate skal være YYYY-MM-DD.");
            }

            const columns = await api("GET", `/api/v1/boards/${boardId}/columns`);
            const columnId = resolveColumn(columns, args.columnId, args.column);
            const columnTitle = columns.find(column => Number(column.id) === columnId).title;

            const isEpic = args.isEpic ?? false;
            const epicCardId = args.epicCardId || null;
            const board = args.tagIds == null && !isEpic && epicCardId == null ? null : await api("GET", `/api/v1/boards/${boardId}`);
            if (board) validateEpicArguments(board, undefined, isEpic || null, epicCardId);

            const tagIds = args.tagIds == null ? undefined : resolveTagIds(board.tags, args.tagIds);
            const id = await api("POST", `/api/v1/boards/${boardId}/cards`, {
                ...(tagIds === undefined ? {} : { tagIds }),
                columnId,
                title: args.title,
                description: toHtml(args.description),
                priority,
                dueDate: args.dueDate ?? null,
            });

            const result = `Oprettet DS-${id} "${args.title}" i kolonnen "${columnTitle}" på tavle ${boardId}.`;
            try {
                if (isEpic) await api("PUT", `/api/v1/boards/${boardId}/cards/${id}/epic`);
                if (epicCardId != null) await api("PUT", `/api/v1/boards/${boardId}/cards/${id}/epic-link`, { epicWorkItemId: epicCardId });
            } catch (error) {
                throw new Error(`${result} Epic blev ikke sat: ${error.message}`);
            }

            if (isEpic) return `${result} Markeret som epic.`;
            if (epicCardId != null) return `${result} Lagt under epic DS-${epicCardId}.`;
            return result;
        },
    },
    {
        name: "move_card",
        description: "Move a DS28 card to another column or position on the same board. Uses the card number from DS-<id>.",
        inputSchema: {
            type: "object",
            properties: {
                boardId: { type: "integer", description: "Board id from list_boards." },
                cardId: { type: "integer", description: "Card number from DS-<id>, for example 42 for DS-42." },
                columnId: { type: "integer", description: "Target column id from get_board. Use either columnId or column." },
                column: { type: "string", description: "Target column title from get_board. Use either columnId or column." },
                position: { type: "integer", minimum: 0, description: "Zero-based position in the target column. Defaults to the end." },
            },
            required: ["boardId", "cardId"],
            additionalProperties: false,
        },
        handler: async args => {
            const boardId = Number(args.boardId);
            if (!Number.isInteger(boardId)) throw new Error("boardId skal være et helt tal.");

            const cardId = Number(args.cardId);
            if (!Number.isInteger(cardId)) throw new Error("cardId skal være et helt tal, fx 42 for DS-42.");

            const board = await api("GET", `/api/v1/boards/${boardId}`);
            if (!board.cards.some(card => Number(card.id) === cardId)) {
                throw new Error(`Kortet DS-${cardId} findes ikke på tavle ${boardId}.`);
            }

            const columnId = resolveColumn(board.columns, args.columnId, args.column);
            const columnTitle = board.columns.find(column => Number(column.id) === columnId).title;
            const cardsInColumn = board.cards.filter(card => Number(card.columnId) === columnId && Number(card.id) !== cardId).length;
            const position = args.position === undefined ? cardsInColumn : Math.min(Number(args.position), cardsInColumn);
            if (!Number.isInteger(position)) throw new Error("position skal være et helt tal fra 0.");

            await api("PUT", `/api/v1/boards/${boardId}/cards/${cardId}/move`, { columnId, position });

            return `Flyttede DS-${cardId} til kolonnen "${columnTitle}" på tavle ${boardId} som nr. ${position + 1}.`;
        },
    },
    {
        name: "update_card",
        description: "Update an existing DS28 card. Fields that are omitted keep their current value. Empty values clear description, due date, color or assignees. Omitted/null tagIds preserve tags; [] clears tags on this board. On developer boards isEpic marks or unmarks an epic, and epicCardId puts the card under an epic or removes it with 0.",
        inputSchema: {
            type: "object",
            properties: {
                boardId: { type: "integer", description: "Board id from list_boards." },
                cardId: { type: "integer", description: "Card number from DS-<id>, for example 42 for DS-42." },
                title: { type: "string", description: "New title." },
                description: { type: "string", description: "Plain text or HTML. Empty string clears the description." },
                priority: { type: "integer", enum: [-1, 0, 1], description: "-1 low, 0 normal, 1 high." },
                dueDate: { type: ["string", "null"], description: "Due date as YYYY-MM-DD, or null/empty to clear it." },
                color: { type: ["string", "null"], description: "Hex color from the board palette, for example \"#3366ff\". Null or empty clears the color." },
                tagIds: { type: ["array", "null"], items: { type: "integer", minimum: 1 }, description: "Tag ids from this board in get_board. Omitted/null preserves tags; [] clears tags on this board only." },
                assignees: {
                    type: "array",
                    items: { type: "string" },
                    description: "Full names or emails of the responsible users. Replaces the current assignees; an empty list clears them.",
                },
                isEpic: { type: ["boolean", "null"], description: "Developer boards only: true marks the card as an epic, false unmarks it and releases its cards. Omitted/null preserves it." },
                epicCardId: { type: ["integer", "null"], minimum: 0, description: "Developer boards only: card number of an epic on this board, for example 17 for DS-17. 0 removes the card from its epic; omitted/null preserves it." },
            },
            required: ["boardId", "cardId"],
            additionalProperties: false,
        },
        handler: async args => {
            const boardId = Number(args.boardId);
            if (!Number.isInteger(boardId)) throw new Error("boardId skal være et helt tal.");

            const cardId = Number(args.cardId);
            if (!Number.isInteger(cardId)) throw new Error("cardId skal være et helt tal, fx 42 for DS-42.");

            const board = await api("GET", `/api/v1/boards/${boardId}`);
            const card = board.cards.find(candidate => Number(candidate.id) === cardId);
            if (!card) throw new Error(`Kortet DS-${cardId} findes ikke på tavle ${boardId}.`);

            const changes = [];

            const title = args.title === undefined ? card.title : String(args.title).trim();
            if (!title) throw new Error("title må ikke være tom.");
            if (title !== card.title) changes.push("titel");

            const description = args.description === undefined ? card.description : toHtml(args.description ?? "");
            if ((description ?? null) !== (card.description ?? null)) changes.push("beskrivelse");

            const currentPriority = Number(card.priority ?? 0);
            const priority = args.priority === undefined ? currentPriority : Number(args.priority);
            if (![-1, 0, 1].includes(priority)) throw new Error("priority skal være -1, 0 eller 1.");
            if (priority !== currentPriority) changes.push("prioritet");

            let dueDate = card.dueDate ?? null;
            if (args.dueDate !== undefined) {
                dueDate = args.dueDate ? String(args.dueDate) : null;
                if (dueDate !== null && !/^\d{4}-\d{2}-\d{2}$/.test(dueDate)) {
                    throw new Error("dueDate skal være YYYY-MM-DD eller tom.");
                }
            }
            if (dueDate !== (card.dueDate ?? null)) changes.push("frist");

            const color = args.color === undefined ? (card.color ?? null) : (args.color ? String(args.color) : null);
            if (color !== (card.color ?? null)) changes.push("farve");

            let assigneeUserIds = (card.assignees ?? []).map(user => user.id);
            if (args.assignees !== undefined) {
                if (!Array.isArray(args.assignees)) throw new Error("assignees skal være en liste af navne eller emails.");

                assigneeUserIds = [];
                for (const wanted of args.assignees) {
                    const users = await api("GET", `/api/v1/boards/assignees?search=${encodeURIComponent(String(wanted))}&boardId=${boardId}`);
                    const user = matchUser(users, wanted);
                    if (!assigneeUserIds.includes(user.id)) assigneeUserIds.push(user.id);
                }

                const before = (card.assignees ?? []).map(user => user.id).sort().join(",");
                if ([...assigneeUserIds].sort().join(",") !== before) changes.push("ansvarlige");
            }

            const tagIds = args.tagIds == null ? undefined : resolveTagIds(board.tags, args.tagIds);
            if (tagIds !== undefined && [...tagIds].sort((a, b) => a - b).join(",") !== (card.tags ?? []).map(tag => Number(tag.id)).sort((a, b) => a - b).join(",")) changes.push("tags");

            const isEpic = args.isEpic ?? null;
            const epicCardId = args.epicCardId ?? null;
            validateEpicArguments(board, cardId, isEpic, epicCardId);

            const currentEpicCardId = card.epicCardId == null ? null : Number(card.epicCardId);
            const removeEpicLink = epicCardId === 0 && currentEpicCardId !== null;
            const unmarkEpic = isEpic === false && Boolean(card.isEpic);
            const setEpicLink = epicCardId > 0 && epicCardId !== currentEpicCardId;
            const markEpic = isEpic === true && !card.isEpic;
            const fieldChanges = changes.length;

            if (unmarkEpic || markEpic) changes.push("epic-markering");
            if (removeEpicLink || setEpicLink) changes.push("epic");

            if (!changes.length) return `DS-${cardId} er uændret på tavle ${boardId}.`;

            if (fieldChanges) {
                await api("PUT", `/api/v1/boards/${boardId}/cards/${cardId}`, {
                    ...(tagIds === undefined ? {} : { tagIds }),
                    title,
                    description,
                    color,
                    ...(args.assignees === undefined ? {} : { assigneeUserIds }),
                    dueDate,
                    priority,
                });
            }

            const cardPath = `/api/v1/boards/${boardId}/cards/${cardId}`;
            if (removeEpicLink) await api("DELETE", `${cardPath}/epic-link`);
            if (unmarkEpic) await api("DELETE", `${cardPath}/epic`);
            if (setEpicLink) await api("PUT", `${cardPath}/epic-link`, { epicWorkItemId: epicCardId });
            if (markEpic) await api("PUT", `${cardPath}/epic`);

            return `Opdaterede DS-${cardId} på tavle ${boardId}: ${changes.join(", ")}.`;
        },
    },
    {
        name: "delete_card",
        description: "Remove a DS28 card from a board. The card itself is soft-deleted when this was its last board.",
        inputSchema: {
            type: "object",
            properties: {
                boardId: { type: "integer", description: "Board id from list_boards." },
                cardId: { type: "integer", description: "Card number from DS-<id>, for example 42 for DS-42." },
            },
            required: ["boardId", "cardId"],
            additionalProperties: false,
        },
        handler: async args => {
            const boardId = Number(args.boardId);
            if (!Number.isInteger(boardId)) throw new Error("boardId skal være et helt tal.");

            const cardId = Number(args.cardId);
            if (!Number.isInteger(cardId)) throw new Error("cardId skal være et helt tal, fx 42 for DS-42.");

            const board = await api("GET", `/api/v1/boards/${boardId}`);
            const card = board.cards.find(candidate => Number(candidate.id) === cardId);
            if (!card) throw new Error(`Kortet DS-${cardId} findes ikke på tavle ${boardId}.`);

            await api("DELETE", `/api/v1/boards/${boardId}/cards/${cardId}`);

            const otherBoards = (card.placements ?? []).length - 1;
            return otherBoards > 0
                ? `Fjernede DS-${cardId} fra tavle ${boardId}. Kortet ligger fortsat på ${otherBoards} andre tavler.`
                : `Slettede DS-${cardId} fra tavle ${boardId}.`;
        },
    },
    {
        name: "create_tag",
        description: "Create a tag on one DS28 board with a unique name and optional hex colour. Returns its tag id. Does not assign it to cards.",
        inputSchema: {
            type: "object",
            properties: {
                boardId: { type: "integer", description: "Board id from list_boards." },
                name: { type: "string", maxLength: 64, description: "Tag name, unique on this board ignoring case." },
                color: { type: ["string", "null"], description: "Optional hex colour. Omitted, null or empty means no colour." },
            },
            required: ["boardId", "name"],
            additionalProperties: false,
        },
        handler: async args => {
            const boardId = Number(args.boardId);
            if (!Number.isInteger(boardId)) throw new Error("boardId skal være et helt tal.");
            if (typeof args.name !== "string" || !args.name.trim() || args.name.trim().length > 64) throw new Error("Tagget skal have et navn på højst 64 tegn.");
            const name = args.name.trim();
            const tagId = await api("POST", `/api/v1/boards/${boardId}/tags`, { name, color: args.color ?? null });
            return `Oprettet tag ${tagId} "${name}" på tavle ${boardId}.`;
        },
    },
    {
        name: "update_tag",
        description: "Rename or recolour a tag on a DS28 board. Changes apply to every card using it on this board. Omitted/null fields are preserved; empty color removes the colour.",
        inputSchema: {
            type: "object",
            properties: {
                boardId: { type: "integer", description: "Board id from list_boards." },
                tagId: { type: "integer", description: "Tag id from this board in get_board." },
                name: { type: ["string", "null"], maxLength: 64, description: "New unique name. Omitted/null preserves it." },
                color: { type: ["string", "null"], description: "New hex colour. Omitted/null preserves it; empty string removes it." },
            },
            required: ["boardId", "tagId"],
            additionalProperties: false,
        },
        handler: async args => {
            const boardId = Number(args.boardId), tagId = Number(args.tagId);
            if (!Number.isInteger(boardId) || !Number.isInteger(tagId)) throw new Error("boardId og tagId skal være heltal.");
            const board = await api("GET", `/api/v1/boards/${boardId}`);
            const tag = (board.tags ?? []).find(tag => Number(tag.id) === tagId);
            if (!tag) throw new Error("Tagget findes ikke på tavlen.");
            await api("PUT", `/api/v1/boards/${boardId}/tags/${tagId}`, { name: args.name ?? tag.name, color: args.color ?? tag.color });
            return `Opdaterede tag ${tagId} på tavle ${boardId}.`;
        },
    },
    {
        name: "delete_tag",
        description: "Delete a tag from a DS28 board and remove it from all cards on that board. Cards and tags on other boards are preserved.",
        inputSchema: {
            type: "object",
            properties: {
                boardId: { type: "integer", description: "Board id from list_boards." },
                tagId: { type: "integer", description: "Tag id from this board in get_board." },
            },
            required: ["boardId", "tagId"],
            additionalProperties: false,
        },
        handler: async args => {
            const boardId = Number(args.boardId), tagId = Number(args.tagId);
            if (!Number.isInteger(boardId) || !Number.isInteger(tagId)) throw new Error("boardId og tagId skal være heltal.");
            await api("DELETE", `/api/v1/boards/${boardId}/tags/${tagId}`);
            return `Slettede tag ${tagId} fra tavle ${boardId} og fjernede det fra tavlens kort.`;
        },
    },
    {
        name: "search_cards",
        description: "Search DS28 cards on boards the logged-in user can see, by card number (\"DS-42\" or \"42\") or part of the title. Returns up to 10 cards with their boards and columns.",
        inputSchema: {
            type: "object",
            properties: { search: { type: "string", description: "Card number such as DS-42 or 42, or part of the title." } },
            required: ["search"],
            additionalProperties: false,
        },
        handler: async args => {
            const search = String(args.search ?? "").trim();
            if (!search) throw new Error("search må ikke være tom.");

            return formatCardSearch(await api("GET", `/api/v1/boards/cards/search?search=${encodeURIComponent(search)}`));
        },
    },
    {
        name: "add_dependency",
        description: "Make a DS28 card wait on another card, for example a frontend card waiting on a backend card. The other card may be on another board the user can see. Rejects self-dependencies, duplicates and cycles.",
        inputSchema: {
            type: "object",
            properties: {
                boardId: { type: "integer", description: "Board id from list_boards where the waiting card is placed." },
                cardId: { type: "integer", description: "Number of the waiting card from DS-<id>, for example 42 for DS-42." },
                dependsOnCardId: { type: "integer", description: "Number of the card it waits on, from get_board or search_cards." },
            },
            required: ["boardId", "cardId", "dependsOnCardId"],
            additionalProperties: false,
        },
        handler: async args => {
            const boardId = Number(args.boardId), cardId = Number(args.cardId), dependsOnCardId = Number(args.dependsOnCardId);
            if (![boardId, cardId, dependsOnCardId].every(Number.isInteger)) throw new Error("boardId, cardId og dependsOnCardId skal være heltal.");

            await api("POST", `/api/v1/boards/${boardId}/cards/${cardId}/dependencies`, { dependsOnWorkItemId: dependsOnCardId });
            return `DS-${cardId} venter nu på DS-${dependsOnCardId}.`;
        },
    },
    {
        name: "remove_dependency",
        description: "Remove the dependency between two DS28 cards, whichever card waits on the other. Works even when the other card is on a board the user cannot see.",
        inputSchema: {
            type: "object",
            properties: {
                boardId: { type: "integer", description: "Board id from list_boards where cardId is placed." },
                cardId: { type: "integer", description: "Card number from DS-<id> on this board." },
                otherCardId: { type: "integer", description: "Number of the other card in the dependency." },
            },
            required: ["boardId", "cardId", "otherCardId"],
            additionalProperties: false,
        },
        handler: async args => {
            const boardId = Number(args.boardId), cardId = Number(args.cardId), otherCardId = Number(args.otherCardId);
            if (![boardId, cardId, otherCardId].every(Number.isInteger)) throw new Error("boardId, cardId og otherCardId skal være heltal.");

            await api("DELETE", `/api/v1/boards/${boardId}/cards/${cardId}/dependencies/${otherCardId}`);
            return `Fjernede afhængigheden mellem DS-${cardId} og DS-${otherCardId}.`;
        },
    },
    {
        name: "set_completion",
        description: "Mark your own part of a DS28 card as done, or withdraw it. Other assignees are unaffected and the card does not move.",
        inputSchema: {
            type: "object",
            properties: {
                boardId: { type: "integer", description: "Board id from list_boards." },
                cardId: { type: "integer", description: "Card number from DS-<id>, for example 42 for DS-42." },
                completed: { type: "boolean", description: "true marks your part as done, false withdraws it." },
            },
            required: ["boardId", "cardId", "completed"],
            additionalProperties: false,
        },
        handler: async ({ boardId, cardId, completed }) => {
            if (![boardId, cardId].every(Number.isInteger)) throw new Error("boardId og cardId skal være heltal.");
            if (typeof completed !== "boolean") throw new Error("completed skal være true eller false.");

            await api(completed ? "PUT" : "DELETE", `/api/v1/boards/${boardId}/cards/${cardId}/completion`);
            return completed ? `Klarmeldte din del af DS-${cardId}.` : `Trak klarmeldingen af DS-${cardId} tilbage.`;
        },
    },
    ...["add", "remove"].map(action => ({
        name: `${action}_pull_request`,
        description: action === "add"
            ? "Add a GitHub pull request to a card on a developer board. Existing links are preserved."
            : "Remove one GitHub pull request link from a card on a developer board.",
        inputSchema: {
            type: "object",
            properties: {
                boardId: { type: "integer", description: "Board id of the developer board." },
                cardId: { type: "integer", description: "Card number from DS-<id> on this board." },
                url: { type: "string", description: "HTTPS URL of the GitHub pull request." },
            },
            required: ["boardId", "cardId", "url"],
            additionalProperties: false,
        },
        handler: async ({ boardId, cardId, url }) => {
            if (![boardId, cardId].every(Number.isInteger)) throw new Error("boardId og cardId skal være heltal.");
            if (typeof url !== "string") throw new Error("url skal være en tekst.");

            const path = `/api/v1/boards/${boardId}/cards/${cardId}/pull-requests`;
            if (action === "add") {
                await api("POST", path, { url });
                return `Tilknyttede PR-link til DS-${cardId}.`;
            }

            await api("DELETE", `${path}?${new URLSearchParams({ url })}`);
            return `Fjernede PR-link fra DS-${cardId}.`;
        },
    })),
];

async function callTool(params) {
    const tool = tools.find(candidate => candidate.name === params.name);
    if (!tool) return { content: [{ type: "text", text: `Ukendt værktøj: ${params.name}` }], isError: true };

    try {
        return { content: [{ type: "text", text: await tool.handler(params.arguments ?? {}) }] };
    } catch (error) {
        return { content: [{ type: "text", text: error.message }], isError: true };
    }
}

function write(message) {
    process.stdout.write(`${JSON.stringify(message)}\n`);
}

function respond(id, result) {
    write({ jsonrpc: "2.0", id, result });
}

async function handleLine(line) {
    const message = parseJson(line);
    if (!message || message.id === undefined) return;

    switch (message.method) {
        case "initialize":
            return respond(message.id, {
                protocolVersion: message.params?.protocolVersion ?? "2025-06-18",
                capabilities: { tools: {} },
                serverInfo: { name: "ds28-boards", version: "1.0.0" },
                instructions: "Værktøjer til at læse og ændre kort på DS28 Kanban-tavler. Brug list_boards, derefter get_board for kolonne-id'er, kortnumre, farver og tag-id'er. Brug create_card, update_card, move_card eller delete_card til kort og create_tag, update_tag eller delete_tag til tavlens tags. Udeladte/null tagIds bevares ved opdatering; [] fjerner tags på denne tavle. Brug search_cards til at finde kort på tværs af tavler og add_dependency eller remove_dependency til at styre, hvilke kort et kort venter på. På udviklertavler styrer isEpic og epicCardId i create_card og update_card epics, og add_pull_request eller remove_pull_request styrer PR-links.",
            });
        case "ping":
            return respond(message.id, {});
        case "tools/list":
            return respond(message.id, {
                tools: tools.map(({ name, description, inputSchema }) => ({ name, description, inputSchema })),
            });
        case "tools/call":
            return respond(message.id, await callTool(message.params ?? {}));
        default:
            write({ jsonrpc: "2.0", id: message.id, error: { code: -32601, message: `Ukendt metode: ${message.method}` } });
    }
}

let queue = Promise.resolve();

createInterface({ input: process.stdin }).on("line", line => {
    if (!line.trim()) return;

    queue = queue.then(() => handleLine(line)).catch(error => {
        process.stderr.write(`ds28-mcp: ${error.stack ?? error}\n`);
    });
});
