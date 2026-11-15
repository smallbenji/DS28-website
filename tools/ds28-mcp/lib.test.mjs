import assert from "node:assert/strict";
import test from "node:test";
import { formatBoards, parseEnvFile, resolveColumn, toHtml } from "./lib.mjs";

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
