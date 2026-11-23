import assert from "node:assert/strict";
import test from "node:test";
import { formatBoard, formatBoards, matchUser, parseCliArguments, parseEnvFile, resolveColumn, resolveTagIds, toHtml, validateEpicArguments } from "./lib.mjs";

test("parseEnvFile læser nøgler, ignorerer kommentarer og fjerner citationstegn", () => {
    const values = parseEnvFile(`
# kommentar
DS28_MCP_URL=https://localhost:44310
DS28_MCP_TOKEN="hemmeligt=sjaldsynt"
`);

    assert.deepEqual(values, {
        DS28_MCP_TOKEN: "hemmeligt=sjaldsynt",
        DS28_MCP_URL: "https://localhost:44310",
    });
});

test("toHtml laver afsnit af ren tekst og lader HTML være", () => {
    assert.equal(toHtml("Hej\n\nVerden\nigen"), "<p>Hej</p><p>Verden<br>igen</p>");
    assert.equal(toHtml("<p>findes</p>"), "<p>findes</p>");
    assert.equal(toHtml("   "), null);
    assert.equal(toHtml("a < b"), "<p>a &lt; b</p>");
});

test("resolveColumn finder kolonne på id eller titel uden hensyn til store bogstaver", () => {
    const columns = [{ id: 4, title: "I gang" }, { id: 7, title: "Færdig" }];

    assert.equal(resolveColumn(columns, 7, undefined), 7);
    assert.equal(resolveColumn(columns, undefined, " i GANG "), 4);
    assert.throws(() => resolveColumn(columns, 9, undefined), /findes ikke/);
    assert.throws(() => resolveColumn(columns, undefined, "Ukendt"), /findes ikke/);
    assert.throws(() => resolveColumn(columns, undefined, ""), /Angiv/);
});

test("formatBoards viser id, titel og arkivering", () => {
    const text = formatBoards([
        { id: 2, title: "Vejfest", isArchived: false, cardCount: 5 },
        { id: 3, title: "Gammel", isArchived: true, cardCount: 0 },
    ]);

    assert.match(text, /id 2: Vejfest \(5 kort\)/);
    assert.match(text, /Gammel \[arkiveret\]/);
    assert.equal(formatBoards([]), "Ingen tavler fundet.");
});

test("matchUser kræver et entydigt navn eller en email", () => {
    const users = [
        { id: "u-1", firstName: "Ada", lastName: "Lovelace", email: "ada@example.com" },
        { id: "u-2", firstName: "Ada", lastName: "Lovelace Jr", email: "ada.jr@example.com" },
    ];

    assert.equal(matchUser(users, " ada lovelace ").id, "u-1");
    assert.equal(matchUser(users, "ADA@EXAMPLE.COM").id, "u-1");
    assert.throws(() => matchUser(users, "Ada"), /forslag: Ada Lovelace/);
    assert.throws(() => matchUser([users[0], { ...users[0], id: "u-3" }], "Ada Lovelace"), /flere brugere/);
    assert.throws(() => matchUser(users, "Niels"), /ingen brugere/);
    assert.throws(() => matchUser(users, "  "), /Angiv/);
});

test("formatBoard viser farvepalette og kortets farve", () => {
    const text = formatBoard({
        id: 1,
        title: "Vejfest",
        columns: [{ id: 10, title: "Backlog" }],
        colors: [{ id: 3, name: "Blå", color: "#3366ff" }],
        cards: [{ id: 42, title: "Fejl", columnId: 10, color: "#3366ff", priority: 0, assignees: [] }],
    });

    assert.match(text, /Farver:\n- Blå: #3366ff/);
    assert.match(text, /DS-42 Fejl \| kolonne Backlog \(id 10\) \| farve #3366ff/);
});

test("formatBoard markerer klarmeldte ansvarlige", () => {
    const text = formatBoard({
        id: 1,
        title: "Vejfest",
        columns: [{ id: 10, title: "Backlog" }],
        cards: [{
            id: 42,
            title: "Fejl",
            columnId: 10,
            priority: 0,
            assignees: [
                { id: "u-1", firstName: "Ada", lastName: "Lovelace", completedAt: "2026-11-21T10:00:00Z" },
                { id: "u-2", firstName: "Alan", lastName: "Turing", completedAt: null },
            ],
        }],
    });

    assert.match(text, /ansvarlige: Ada Lovelace \(klarmeldt\), Alan Turing$/m);
});


test("tags valideres mod tavlen, dubletter fjernes og tom liste bevares", () => {
    const tags = [{ id: "1", name: "v0.2.13" }, { id: 2, name: "Fix" }];
    assert.deepEqual(resolveTagIds(tags, [2, 1, 2]), [2, 1]);
    assert.deepEqual(resolveTagIds(tags, []), []);
    for (const value of [null, "1", ["1"], [0], [-1], [1.5]]) assert.throws(() => resolveTagIds(tags, value), /positive heltal/);
    assert.throws(() => resolveTagIds(tags, [3]), /findes ikke på tavlen/);
});

test("formatBoard viser tagdefinitioner, valgfri farve og kortets tag-id'er", () => {
    const tags = [{ id: 1, name: "v0.2.13", color: "#dbeafe" }, { id: 2, name: "Fix", color: null }];
    const text = formatBoard({ id: 1, title: "IT", columns: [], tags, cards: [{ id: 42, title: "Fejl", tags }] });
    assert.match(text, /Tags:\n- id 1: v0.2.13 \| farve #dbeafe\n- id 2: Fix \| farve ingen/);
    assert.match(text, /tags: v0.2.13 \(id 1\), Fix \(id 2\)/);
});

test("formatBoard viser epic-fremdrift og kortets epic", () => {
    const text = formatBoard({
        id: 1,
        title: "IT",
        columns: [{ id: 10, title: "Backlog" }],
        cards: [
            { id: 17, title: "Epics", columnId: 10, isEpic: true, epicProgress: { done: 3, total: 7 } },
            { id: 42, title: "Fejl", columnId: 10, epicCardId: 17 },
            { id: 43, title: "Skjult", columnId: 10, epicCardId: 99 },
        ],
    });

    assert.match(text, /DS-17 Epics \| kolonne Backlog \(id 10\) \| epic: 3 af 7 færdige$/m);
    assert.match(text, /DS-42 Fejl \| kolonne Backlog \(id 10\) \| epic: DS-17 Epics$/m);
    assert.match(text, /DS-43 Skjult \| kolonne Backlog \(id 10\) \| epic: DS-99$/m);
});

test("validateEpicArguments kræver udviklertavle og en epic på samme tavle", () => {
    const board = { isDeveloperBoard: true, cards: [{ id: "17", isEpic: true }, { id: "42", isEpic: false }] };

    assert.doesNotThrow(() => validateEpicArguments({ isDeveloperBoard: false }, 42, null, null));
    assert.doesNotThrow(() => validateEpicArguments(board, 42, false, 17));
    assert.doesNotThrow(() => validateEpicArguments(board, 42, true, 0));
    assert.throws(() => validateEpicArguments({ ...board, isDeveloperBoard: false }, 42, true, null), /udviklerfunktioner/);
    assert.throws(() => validateEpicArguments(board, 42, true, 17), /både være epic/);
    assert.throws(() => validateEpicArguments(board, 17, null, 17), /sin egen epic/);
    assert.throws(() => validateEpicArguments(board, 43, null, 42), /DS-42 er ikke markeret som epic/);
    assert.throws(() => validateEpicArguments(board, 43, null, 99), /DS-99 findes ikke på tavlen/);
    assert.throws(() => validateEpicArguments(board, 43, null, -1), /DS-nummer eller 0/);
    assert.throws(() => validateEpicArguments(board, 43, "ja", null), /true eller false/);
});

test("parseCliArguments omsætter navn=værdi efter værktøjets skema", () => {
    const schema = {
        properties: {
            boardId: { type: "integer" },
            cardId: { type: "integer" },
            title: { type: "string" },
            dueDate: { type: ["string", "null"] },
            tagIds: { type: ["array", "null"], items: { type: "integer" } },
            assignees: { type: "array", items: { type: "string" } },
        },
    };

    assert.deepEqual(parseCliArguments(schema, ["boardId=1", "cardId=DS-42", "title=42 = svaret", "dueDate=null", "tagIds=3,4", "assignees=[\"Ada Lovelace\"]"]), {
        boardId: 1,
        cardId: 42,
        title: "42 = svaret",
        dueDate: null,
        tagIds: [3, 4],
        assignees: ["Ada Lovelace"],
    });
    assert.deepEqual(parseCliArguments(schema, ["assignees="]), { assignees: [] });
    assert.deepEqual(parseCliArguments({ properties: { completed: { type: "boolean" } } }, ["completed=false"]), { completed: false });
    assert.throws(() => parseCliArguments({ properties: { completed: { type: "boolean" } } }, ["completed=ja"]), /true eller false/);
    assert.throws(() => parseCliArguments(schema, ["boardId=en"]), /helt tal/);
    assert.throws(() => parseCliArguments(schema, ["ukendt=1"]), /Ukendt argument "ukendt"/);
    assert.throws(() => parseCliArguments(schema, ["boardId"]), /navn=værdi/);
});
