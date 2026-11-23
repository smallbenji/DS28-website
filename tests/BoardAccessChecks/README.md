# Board access checks

Run `bash tests/BoardAccessChecks/run.sh` from the repository root with .NET 10
and Docker available. The runner creates and removes a disposable PostgreSQL 16
database; it never uses the application database.

Checks cover migration of existing boards, automatic creator membership,
invitation chains, duplicate invitations, member removal and leaving (including
the rule that the last member stays, kept card assignments and per-member card
counts), administrator access, denied access for outsiders across board
operations, and visibility of shared-card placements.
Card color checks cover migration defaults, preserved colors across upgrades,
named board palette CRUD, board isolation, validation, creation, updates,
clearing, database constraints and shared colors across boards. Palette edits
and deletion must not recolor existing cards.

Tag checks cover migration, optional colors, case-insensitive name uniqueness,
multiple tag assignments, creation and deletion of tagged placements, legacy
updates preserving tags, validation, denied access, independent shared-card
tags, live events, and composite database foreign keys rejecting cross-board
assignments. Tag deletion must remove assignments without altering other boards.

Inline tag creation checks cover optional `newTagNames` on card creation and
updates, trimming, duplicate and existing-name reuse, color defaults, validation,
board isolation, access control, rollback of card/tag writes on database failure,
no live events after failure, and a successful retry without duplicate tags.

Remote MCP checks invoke `BoardTools` with a test authentication service and the
real board service/database. They cover tag creation, rename/recolour, deletion,
card tag selection, omitted/null/empty semantics, conflicts, invalid input,
shared-board isolation and denied access. They do not exercise the HTTP MCP
transport or OAuth login. Local stdio protocol tests are run separately with
`node --test tools/ds28-mcp/*.test.mjs`.

Epic checks cover marking and unmarking, one epic per card, one level only,
same-board targets, self-reference, placement scope on shared cards, progress
from done columns, live events, denied access, rejection on boards without
developer features, hidden but preserved data while the feature is off, and
release of child cards when an epic is unmarked or deleted. Remote MCP epic checks cover
`isEpic` and `epicCardId` on `create_card` and `update_card`, the epic lines in
`get_board`, and rejection before card creation on boards without developer
features.
