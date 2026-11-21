#!/usr/bin/env node
import { spawn } from "node:child_process";
import { createInterface } from "node:readline";
import { dirname, join } from "node:path";
import { fileURLToPath } from "node:url";
import { parseCliArguments } from "./lib.mjs";

const here = dirname(fileURLToPath(import.meta.url));
const [toolName, ...pairs] = process.argv.slice(2);

const server = spawn(process.execPath, [join(here, "server.mjs")], { stdio: ["pipe", "pipe", "inherit"] });
const waiting = new Map();
let nextId = 0;

createInterface({ input: server.stdout }).on("line", line => {
    const message = JSON.parse(line);
    waiting.get(message.id)?.(message);
    waiting.delete(message.id);
});

function call(method, params) {
    return new Promise(resolve => {
        const id = ++nextId;
        waiting.set(id, resolve);
        server.stdin.write(`${JSON.stringify({ jsonrpc: "2.0", id, method, params })}\n`);
    });
}

function describe(tool) {
    const required = tool.inputSchema.required ?? [];
    const parameters = Object.entries(tool.inputSchema.properties ?? {})
        .map(([name, property]) => `    ${name}${required.includes(name) ? "" : " (valgfri)"}: ${property.description ?? ""}`);

    return [`  ${tool.name}`, `    ${tool.description}`, ...parameters].join("\n");
}

async function run() {
    await call("initialize", { protocolVersion: "2025-06-18", capabilities: {}, clientInfo: { name: "ds28-cli", version: "1.0.0" } });
    const { result: { tools } } = await call("tools/list", {});

    if (!toolName || toolName === "help" || toolName === "--help") {
        console.log("Brug: node tools/ds28-mcp/cli.mjs <værktøj> navn=værdi ...\n\nVærktøjer:");
        console.log(tools.map(describe).join("\n\n"));
        return 0;
    }

    const tool = tools.find(candidate => candidate.name === toolName);
    if (!tool) {
        console.error(`Ukendt værktøj "${toolName}". Kør uden argumenter for at se listen.`);
        return 1;
    }

    const response = await call("tools/call", { name: tool.name, arguments: parseCliArguments(tool.inputSchema, pairs) });
    const text = response.result.content.map(part => part.text).join("\n");

    if (response.result.isError) {
        console.error(text);
        return 1;
    }

    console.log(text);
    return 0;
}

run()
    .then(code => { process.exitCode = code; })
    .catch(error => {
        console.error(error.message);
        process.exitCode = 1;
    })
    .finally(() => server.kill());
