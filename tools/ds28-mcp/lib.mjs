export function parseEnvFile(text) {
    const values = {};

    for (const line of text.split(/\r?\n/)) {
        const match = /^\s*([A-Za-z_][A-Za-z0-9_]*)\s*=\s*(.*?)\s*$/.exec(line);
        if (!match) continue;

        values[match[1]] = match[2].replace(/^(["'])(.*)\1$/, "$2");
    }

    return values;
}

export function toHtml(text) {
    if (!text || !text.trim()) return null;
    if (/<[a-z][\s\S]*>/i.test(text)) return text;

    const escape = value => value
        .replace(/&/g, "&amp;")
        .replace(/</g, "&lt;")
        .replace(/>/g, "&gt;");

    return text
        .split(/\n{2,}/)
        .map(block => block.trim())
        .filter(Boolean)
        .map(block => `<p>${escape(block).replace(/\n/g, "<br>")}</p>`)
        .join("");
}

export function formatBoards(boards) {
    if (!boards.length) return "Ingen tavler fundet.";

    return boards
        .map(board => `- id ${board.id}: ${board.title}${board.isArchived ? " [arkiveret]" : ""} (${board.cardCount} kort)`)
        .join("\n");
}

export function resolveColumn(columns, columnId, columnTitle) {
    const known = columns.map(column => `${column.id}=${column.title}`).join(", ");

    if (columnId != null && columnId !== "") {
        const match = columns.find(column => String(column.id) === String(columnId));
        if (!match) throw new Error(`Kolonnen ${columnId} findes ikke. Kolonner: ${known}`);
        return Number(match.id);
    }

    const wanted = String(columnTitle ?? "").trim().toLowerCase();
    if (!wanted) throw new Error(`Angiv columnId eller column. Kolonner: ${known}`);

    const match = columns.find(column => column.title.trim().toLowerCase() === wanted);
    if (!match) throw new Error(`Kolonnen "${columnTitle}" findes ikke. Kolonner: ${known}`);

    return Number(match.id);
}

export function matchUser(users, wanted) {
    const query = String(wanted ?? "").trim().toLowerCase();
    if (!query) throw new Error("Angiv et navn eller en email som ansvarlig.");

    const exact = users.filter(user =>
        `${user.firstName} ${user.lastName}`.trim().toLowerCase() === query
        || String(user.email ?? "").toLowerCase() === query);

    if (exact.length === 1) return exact[0];

    const candidates = (exact.length ? exact : users)
        .map(user => `${user.firstName} ${user.lastName} <${user.email}>`)
        .join(", ");

    if (exact.length > 1) throw new Error(`"${wanted}" matcher flere brugere: ${candidates}`);
    if (candidates) throw new Error(`"${wanted}" matcher ingen brugere præcist (forslag: ${candidates}).`);
    throw new Error(`"${wanted}" matcher ingen brugere.`);
}

export function formatBoard(board) {
    const lines = [
        `Tavle: ${board.title} (id ${board.id})${board.isArchived ? " [arkiveret]" : ""}`,
    ];

    if (board.description) lines.push(board.description);

    lines.push("", "Kolonner:");
    for (const column of board.columns) {
        lines.push(`- id ${column.id}: ${column.title}${column.isDoneColumn ? " (færdig)" : ""}`);
    }

    if (board.colors?.length) {
        lines.push("", "Farver:");
        for (const color of board.colors) {
            lines.push(`- ${color.name}: ${color.color}`);
        }
    }

    lines.push("", "Kort:");
    if (!board.cards.length) lines.push("- (ingen)");

    for (const card of board.cards) {
        const column = board.columns.find(candidate => String(candidate.id) === String(card.columnId));
        const meta = [`kolonne ${column?.title ?? "?"} (id ${card.columnId})`];
        const priority = Number(card.priority ?? 0);
        const assignees = (card.assignees ?? []).map(user => `${user.firstName} ${user.lastName}`);

        if (priority > 0) meta.push("høj prioritet");
        if (priority < 0) meta.push("lav prioritet");
        if (card.dueDate) meta.push(`frist ${card.dueDate}`);
        if (card.color) meta.push(`farve ${card.color}`);
        if (assignees.length) meta.push(`ansvarlige: ${assignees.join(", ")}`);

        lines.push(`- DS-${card.id} ${card.title} | ${meta.join(" | ")}`);

        if (card.description) lines.push(`  ${card.description.replace(/\n/g, "\n  ")}`);
    }

    return lines.join("\n");
}
