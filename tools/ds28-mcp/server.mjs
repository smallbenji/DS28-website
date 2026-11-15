#!/usr/bin/env node
import { existsSync, readFileSync } from "node:fs";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { createInterface } from "node:readline";
import http from "node:http";
import https from "node:https";
import { formatBoard, formatBoards, parseEnvFile, resolveColumn, toHtml } from "./lib.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const envPath = join(here, ".env");

if (existsSync(envPath)) {
    for (const [key, value] of Object.entries(parseEnvFile(readFileSync(envPath, "utf8")))) {
        process.env[key] ??= value;
    }
}

const baseUrl = (process.env.DS28_MCP_URL ?? "https://localhost:44310").replace(/\/+$/, "");
const email = process.env.DS28_MCP_EMAIL;
const password = process.env.DS28_MCP_PASSWORD;
// shortcut: certifikatvalidering er slået fra som standard til den lokale udviklingsserver, sæt DS28_MCP_INSECURE_TLS=0 ved et rigtigt certifikat
const rejectUnauthorized = process.env.DS28_MCP_INSECURE_TLS === "0";
const safeMethods = new Set(["GET", "HEAD", "OPTIONS", "TRACE"]);

const cookies = new Map();
let antiforgeryToken = null;

function request(method, path, body) {
    return new Promise((resolve, reject) => {
        const url = new URL(baseUrl + path);
        const payload = body === undefined ? undefined : JSON.stringify(body);
        const headers = { Accept: "application/json" };

        if (payload !== undefined) {
            headers["Content-Type"] = "application/json";
            headers["Content-Length"] = Buffer.byteLength(payload);
        }
        if (cookies.size) {
            headers.Cookie = [...cookies].map(([name, value]) => `${name}=${value}`).join("; ");
        }
        if (antiforgeryToken && !safeMethods.has(method)) {
            headers.RequestVerificationToken = antiforgeryToken;
        }

        const transport = url.protocol === "https:" ? https : http;
        const req = transport.request(url, { method, headers, rejectUnauthorized }, response => {
            const chunks = [];
            response.on("data", chunk => chunks.push(chunk));
            response.on("end", () => {
                for (const header of response.headers["set-cookie"] ?? []) {
                    const pair = header.split(";")[0];
                    const index = pair.indexOf("=");
                    if (index > 0) cookies.set(pair.slice(0, index).trim(), pair.slice(index + 1));
                }

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

async function refreshAntiforgeryToken() {
    const response = await request("GET", "/api/v1/antiforgery");
    const token = parseJson(response.body)?.token;

    if (response.status !== 200 || !token) {
        throw new Error(`Kunne ikke hente antiforgery-token (HTTP ${response.status}).`);
    }

    antiforgeryToken = token;
}

async function login() {
    if (!email || !password) {
        throw new Error("Sæt DS28_MCP_EMAIL og DS28_MCP_PASSWORD i tools/ds28-mcp/.env.");
    }

    cookies.clear();
    antiforgeryToken = null;

    const response = await request("POST", "/api/v1/auth/login", { email, password });
    const data = parseJson(response.body);

    if (response.status !== 200) {
        throw new Error(`Login fejlede (HTTP ${response.status}): ${typeof data === "string" ? data : response.body}`);
    }
    if (data?.requiresTwoFactor) {
        throw new Error("Kontoen kræver 2FA. Brug en DS28-konto uden 2FA til MCP'en.");
    }

    await refreshAntiforgeryToken();
}

async function api(method, path, body) {
    if (!cookies.size) await login();

    let response = await request(method, path, body);
    const retry = response.status === 401
        || (response.status >= 300 && response.status < 400)
        || (response.status === 400 && !response.body);

    if (retry) {
        if (response.status === 400) await refreshAntiforgeryToken();
        else await login();

        response = await request(method, path, body);
    }

    if (!safeMethods.has(method)) antiforgeryToken = null;

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
        description: "Read one DS28 Kanban board: columns with ids and all cards with DS-number, column, priority, due date, assignees and description.",
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
        description: "Create a card on a DS28 Kanban board in a given column. Returns the new card number DS-<id>.",
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

            const id = await api("POST", `/api/v1/boards/${boardId}/cards`, {
                columnId,
                title: args.title,
                description: toHtml(args.description),
                priority,
                dueDate: args.dueDate ?? null,
            });

            return `Oprettet DS-${id} "${args.title}" i kolonnen "${columnTitle}" på tavle ${boardId}.`;
        },
    },
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
                instructions: "Værktøjer til at læse og oprette kort på DS28 Kanban-tavler. Brug list_boards, derefter get_board for kolonne-id'er, og create_card.",
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
