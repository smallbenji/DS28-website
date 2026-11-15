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
const moved = [];
const updated = [];
const deleted = [];
const tagsCreated = [];
const tagsUpdated = [];
const tagsDeleted = [];

function startStubServer() {
    const server = http.createServer((request, response) => {
        const send = (status, body) => {
            response.writeHead(status, { "Content-Type": "application/json" });
            response.end(body === undefined ? "" : JSON.stringify(body));
        };

        if (request.headers.authorization !== `Bearer ${bearerToken}`) {
            return send(401, undefined);
        }

        const readBody = callback => {
            let body = "";
            request.on("data", chunk => { body += chunk; });
            request.on("end", () => callback(body ? JSON.parse(body) : undefined));
        };

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
                        color: "#3366ff",
                        columnId: "10",
                        priority: "1",
                        dueDate: "2026-11-20",
                        assignees: [{ id: "u-1", firstName: "Ada", lastName: "Lovelace" }],
                        tags: [{ id: "1", name: "v0.2.13", color: "#dbeafe" }],
                        placements: [{ boardId: "1", columnId: "10" }],
                    },
                ],
                colors: [{ id: "3", name: "Blå", color: "#3366ff" }],
                tags: [{ id: "1", name: "v0.2.13", color: "#dbeafe" }, { id: "2", name: "Fix", color: null }],
            });
        }
        if (request.url === "/api/v1/boards/2" || request.url.startsWith("/api/v1/boards/2/tags")) return send(403);
        if (request.method === "POST" && request.url === "/api/v1/boards/1/tags") {
            return readBody(body => {
                if (body.name === "v0.2.13") return send(409, "Tagget findes allerede.");
                tagsCreated.push(body);
                send(200, 3);
            });
        }
        if (request.method === "PUT" && request.url === "/api/v1/boards/1/tags/1") {
            return readBody(body => { tagsUpdated.push(body); send(200); });
        }
        if (request.method === "DELETE" && request.url === "/api/v1/boards/1/tags/1") {
            tagsDeleted.push(1);
            return send(200);
        }
        if (request.method === "GET" && request.url.startsWith("/api/v1/boards/assignees")) {
            return send(200, [
                { id: "u-1", firstName: "Ada", lastName: "Lovelace", email: "ada@example.com" },
                { id: "u-2", firstName: "Ada", lastName: "Lovelace Jr", email: "ada.jr@example.com" },
            ]);
        }
        if (request.method === "POST" && request.url === "/api/v1/boards/1/cards") {
            return readBody(body => {
                created.push(body);
                send(200, "42");
            });
        }
        if (request.method === "PUT" && request.url === "/api/v1/boards/1/cards/42") {
            return readBody(body => {
                updated.push(body);
                send(204, undefined);
            });
        }
        if (request.method === "DELETE" && request.url === "/api/v1/boards/1/cards/42") {
            return readBody(body => {
                deleted.push(body);
                send(204, undefined);
            });
        }
        if (request.method === "PUT" && request.url === "/api/v1/boards/1/cards/42/move") {
            return readBody(body => {
                moved.push(body);
                send(204, undefined);
            });
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
        assert.deepEqual(tools.result.tools.map(tool => tool.name), ["list_boards", "get_board", "create_card", "move_card", "update_card", "delete_card", "create_tag", "update_tag", "delete_tag"]);

        const boards = await client.call("tools/call", { name: "list_boards", arguments: {} });
        assert.match(boards.result.content[0].text, /id 1: Vejfest/);
        assert.match(boards.result.content[0].text, /Gammel \[arkiveret\]/);

        const board = await client.call("tools/call", { name: "get_board", arguments: { boardId: 1 } });
        const boardText = board.result.content[0].text;
        assert.match(boardText, /DS-42 Fejl i tilmelding/);
        assert.match(boardText, /høj prioritet/);
        assert.match(boardText, /Ada Lovelace/);
        assert.match(boardText, /farve #3366ff/);
        assert.match(boardText, /Blå: #3366ff/);
        assert.match(boardText, /Ret <strong>fejlen<\/strong>/);

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

        const movedCard = await client.call("tools/call", {
            name: "move_card",
            arguments: { boardId: 1, cardId: 42, column: "færdig" },
        });
        assert.match(movedCard.result.content[0].text, /Flyttede DS-42 til kolonnen "Færdig" på tavle 1 som nr. 1/);
        assert.deepEqual(moved[0], { columnId: 11, position: 0 });

        const updatedCard = await client.call("tools/call", {
            name: "update_card",
            arguments: { boardId: 1, cardId: 42, title: "Fejl i tilmeldingen", color: "#ffcc00", assignees: ["ada@example.com"] },
        });
        assert.match(updatedCard.result.content[0].text, /Opdaterede DS-42 på tavle 1: titel, farve\./);
        assert.deepEqual(updated[0], {
            title: "Fejl i tilmeldingen",
            description: "<p>Ret <strong>fejlen</strong></p>",
            color: "#ffcc00",
            assigneeUserIds: ["u-1"],
            ownerPositionId: null,
            dueDate: "2026-11-20",
            priority: 1,
        });

        const ambiguous = await client.call("tools/call", {
            name: "update_card",
            arguments: { boardId: 1, cardId: 42, assignees: ["Ada"] },
        });
        assert.equal(ambiguous.result.isError, true);
        assert.match(ambiguous.result.content[0].text, /forslag: Ada Lovelace <ada@example.com>/);

        const cleared = await client.call("tools/call", {
            name: "update_card",
            arguments: { boardId: 1, cardId: 42, dueDate: "", description: "" },
        });
        assert.match(cleared.result.content[0].text, /beskrivelse, frist/);
        assert.equal(updated[1].dueDate, null);
        assert.equal(updated[1].description, null);
        assert.equal(updated[1].assigneeUserIds.length, 1);

        const removedCard = await client.call("tools/call", { name: "delete_card", arguments: { boardId: 1, cardId: 42 } });
        assert.match(removedCard.result.content[0].text, /Slettede DS-42 fra tavle 1\./);
        assert.equal(deleted.length, 1);

        const missingCard = await client.call("tools/call", {
            name: "move_card",
            arguments: { boardId: 1, cardId: 999, column: "Færdig" },
        });
        assert.equal(missingCard.result.isError, true);
        assert.match(missingCard.result.content[0].text, /findes ikke/);
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


test("MCP understøtter tags med bevaret valg, rydning, validering og tagadministration", async () => {
    const server = await startStubServer();
    const client = startClient(`http://127.0.0.1:${server.address().port}`);
    const call = async (name, args) => (await client.call("tools/call", { name, arguments: { boardId: 1, ...args } })).result;
    try {
        const board = await call("get_board", {});
        assert.match(board.content[0].text, /id 1: v0.2.13 \| farve #dbeafe/);
        assert.match(board.content[0].text, /tags: v0.2.13 \(id 1\)/);
        const createCount = created.length;
        const tagged = await call("create_card", { title: "Tagged", columnId: 10, tagIds: [2, 1, 2] });
        assert.equal(tagged.isError, undefined);
        assert.deepEqual(created[createCount].tagIds, [2, 1]);
        for (const tagIds of [[99], ["1"], [-1]]) {
            assert.equal((await call("create_card", { title: "Invalid", columnId: 10, tagIds })).isError, true);
        }
        assert.equal(created.length, createCount + 1);
        let before = updated.length;
        await call("update_card", { cardId: 42, title: "Keep tags" });
        assert.equal(Object.hasOwn(updated[before], "tagIds"), false);
        before = updated.length;
        await call("update_card", { cardId: 42, title: "Keep tags", tagIds: null });
        assert.equal(Object.hasOwn(updated[before], "tagIds"), false);
        before = updated.length;
        await call("update_card", { cardId: 42, tagIds: [] });
        assert.deepEqual(updated[before].tagIds, []);
        before = updated.length;
        const selected = await call("update_card", { cardId: 42, tagIds: [2, 1, 2] });
        assert.match(selected.content[0].text, /tags/);
        assert.deepEqual(updated[before].tagIds, [2, 1]);
        before = updated.length;
        const unchanged = await call("update_card", { cardId: 42, tagIds: [1, 1] });
        assert.match(unchanged.content[0].text, /uændret/);
        assert.equal(updated.length, before);
        assert.equal((await call("update_card", { cardId: 42, title: "Invalid", tagIds: [99] })).isError, true);
        assert.equal(updated.length, before);

        const tag = await call("create_tag", { name: " v0.2.14 " });
        assert.match(tag.content[0].text, /tag 3 "v0.2.14"/);
        assert.deepEqual(tagsCreated.at(-1), { name: "v0.2.14", color: null });
        await call("update_tag", { tagId: 1, name: "v0.2.15" });
        assert.deepEqual(tagsUpdated.at(-1), { name: "v0.2.15", color: "#dbeafe" });
        await call("update_tag", { tagId: 1, color: "" });
        assert.deepEqual(tagsUpdated.at(-1), { name: "v0.2.13", color: "" });
        await call("update_tag", { tagId: 1, color: null });
        assert.equal(tagsUpdated.at(-1).color, "#dbeafe");
        const deletedTag = await call("delete_tag", { tagId: 1 });
        assert.match(deletedTag.content[0].text, /fjernede det fra tavlens kort/);
        assert.equal(tagsDeleted.at(-1), 1);
        const updates = tagsUpdated.length;
        assert.equal((await call("update_tag", { tagId: 99, name: "Hidden" })).isError, true);
        assert.equal(tagsUpdated.length, updates);
        assert.equal((await call("create_tag", { name: "v0.2.13" })).isError, true);
        for (const name of ["create_tag", "update_tag", "delete_tag"]) {
            assert.equal((await call(name, { boardId: 2, tagId: 1, name: "Denied" })).isError, true);
        }
    } finally {
        client.child.kill();
        server.close();
    }
});
