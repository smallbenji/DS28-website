import assert from "node:assert/strict";
import test from "node:test";
import { spawn } from "node:child_process";
import { createInterface } from "node:readline";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import http from "node:http";

const here = dirname(fileURLToPath(import.meta.url));
const bearerToken = "ds28_test-token";
const created = [];

function startStubServer() {
    const server = http.createServer((request, response) => {
        const send = (status, body) => {
            response.writeHead(status, { "Content-Type": "application/json" });
            response.end(body === undefined ? "" : JSON.stringify(body));
        };

        if (request.headers.authorization !== `Bearer ${bearerToken}`) {
            return send(401, undefined);
        }

        if (request.method === "GET" && request.url === "/api/v1/boards") {
            return send(200, [
                { id: "1", title: "Vejfest", isArchived: false, cardCount: "2" },
                { id: "2", title: "Gammel", isArchived: true, cardCount: "0" },
            ]);
        }
        if (request.method === "GET" && request.url === "/api/v1/boards/1/columns") {
            return send(200, [
                { id: "10", title: "Backlog", isDoneColumn: false },
                { id: "11", title: "Færdig", isDoneColumn: true },
            ]);
        }
        if (request.method === "GET" && request.url === "/api/v1/boards/1") {
            return send(200, {
                id: "1",
                title: "Vejfest",
                isArchived: false,
                columns: [
                    { id: "10", title: "Backlog", isDoneColumn: false },
                    { id: "11", title: "Færdig", isDoneColumn: true },
                ],
                cards: [
                    {
                        id: "42",
                        title: "Fejl i tilmelding",
                        description: "<p>Ret <strong>fejlen</strong></p>",
                        columnId: "10",
                        priority: "1",
                        dueDate: "2026-11-20",
                        assignees: [{ firstName: "Ada", lastName: "Lovelace" }],
                    },
                ],
            });
        }
        if (request.method === "POST" && request.url === "/api/v1/boards/1/cards") {
            let body = "";
            request.on("data", chunk => { body += chunk; });
            request.on("end", () => {
                created.push(JSON.parse(body));
                send(200, "42");
            });
            return;
        }

        send(404, {});
    });

    return new Promise(resolve => {
        server.listen(0, "127.0.0.1", () => resolve(server));
    });
}

function startClient(url, token = bearerToken) {
    const child = spawn(process.execPath, [join(here, "server.mjs")], {
        env: {
            ...process.env,
            DS28_MCP_URL: url,
            DS28_MCP_TOKEN: token,
            DS28_MCP_INSECURE_TLS: "0",
        },
        stdio: ["pipe", "pipe", "inherit"],
    });

    const waiting = new Map();
    let nextId = 0;

    createInterface({ input: child.stdout }).on("line", line => {
        const message = JSON.parse(line);
        waiting.get(message.id)?.(message);
        waiting.delete(message.id);
    });

    const call = (method, params) => new Promise((resolve, reject) => {
        const id = ++nextId;
        waiting.set(id, resolve);
        child.stdin.write(`${JSON.stringify({ jsonrpc: "2.0", id, method, params })}\n`);
        setTimeout(() => reject(new Error(`Intet svar på ${method}`)), 5000).unref();
    });

    return { child, call };
}

test("MCP-serveren læser tavler og opretter kort gennem DS28-API'et", async () => {
    const server = await startStubServer();
    const url = `http://127.0.0.1:${server.address().port}`;
    const client = startClient(url);

    try {
        const init = await client.call("initialize", { protocolVersion: "2025-06-18" });
        assert.equal(init.result.protocolVersion, "2025-06-18");

        const tools = await client.call("tools/list", {});
        assert.deepEqual(tools.result.tools.map(tool => tool.name), ["list_boards", "get_board", "create_card"]);

        const boards = await client.call("tools/call", { name: "list_boards", arguments: {} });
        assert.match(boards.result.content[0].text, /id 1: Vejfest/);
        assert.match(boards.result.content[0].text, /Gammel \[arkiveret\]/);

        const board = await client.call("tools/call", { name: "get_board", arguments: { boardId: 1 } });
        const boardText = board.result.content[0].text;
        assert.match(boardText, /DS-42 Fejl i tilmelding/);
        assert.match(boardText, /høj prioritet/);
        assert.match(boardText, /Ada Lovelace/);
        assert.match(boardText, /Ret fejlen/);

        const createdCard = await client.call("tools/call", {
            name: "create_card",
            arguments: {
                boardId: 1,
                column: "backlog",
                title: "Ny opgave",
                description: "Linje et\n\nLinje to",
                priority: 1,
                dueDate: "2026-12-01",
            },
        });
        assert.match(createdCard.result.content[0].text, /DS-42 "Ny opgave" i kolonnen "Backlog"/);
        assert.deepEqual(created[0], {
            columnId: 10,
            title: "Ny opgave",
            description: "<p>Linje et</p><p>Linje to</p>",
            priority: 1,
            dueDate: "2026-12-01",
        });

        const unknown = await client.call("tools/call", { name: "nope", arguments: {} });
        assert.equal(unknown.result.isError, true);
    } finally {
        client.child.kill();
        server.close();
    }
});

test("MCP-serveren kræver et gyldigt adgangstoken", async () => {
    const server = await startStubServer();
    const url = `http://127.0.0.1:${server.address().port}`;
    const withoutToken = startClient(url, "");
    const wrongToken = startClient(url, "ds28_forkert");

    try {
        const missing = await withoutToken.call("tools/call", { name: "list_boards", arguments: {} });
        assert.equal(missing.result.isError, true);
        assert.match(missing.result.content[0].text, /DS28_MCP_TOKEN/);

        const rejected = await wrongToken.call("tools/call", { name: "list_boards", arguments: {} });
        assert.equal(rejected.result.isError, true);
        assert.match(rejected.result.content[0].text, /ugyldigt, udløbet eller tilbagekaldt/);
    } finally {
        withoutToken.child.kill();
        wrongToken.child.kill();
        server.close();
    }
});
