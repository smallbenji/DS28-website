using System.ComponentModel;
using System.Globalization;
using System.Text;
using DS.Application.Services;
using DS.Application.Services.Boards;
using DS.Domain;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace DS.Backend.Mcp
{
    [McpServerToolType]
    public class BoardTools(BoardService boards, IHttpContextAccessor httpContextAccessor, UserManager<User> userManager)
    {
        [McpServerTool(Name = "list_boards", ReadOnly = true)]
        [Description("List DS28 Kanban boards the logged-in user can see, with board id and card count.")]
        public async Task<string> ListBoards()
        {
            var result = await boards.GetBoards(await ActorAsync());

            return BoardText.Boards(Unwrap(result));
        }

        [McpServerTool(Name = "get_board", ReadOnly = true)]
        [Description("Read one DS28 Kanban board: columns with ids, colour palette, board tags with ids/names/colours, and all cards with DS-number, column, priority, due date, colour, assignees, assigned tags, the cards each card waits on or blocks, PR links on developer boards, and description.")]
        public async Task<string> GetBoard([Description("Board id from list_boards.")] int boardId)
        {
            var result = await boards.GetBoard(await ActorAsync(), boardId);

            return BoardText.Board(Unwrap(result));
        }

        [McpServerTool(Name = "create_card")]
        [Description("Create a card on a DS28 Kanban board in a given column. Returns the new card number DS-<id>.")]
        public async Task<string> CreateCard(
            [Description("Board id from list_boards.")] int boardId,
            [Description("Card title.")] string title,
            [Description("Column title from get_board, for example \"Backlog\". Use either column or columnId.")] string column = null,
            [Description("Column id from get_board. Use either column or columnId.")] int? columnId = null,
            [Description("Plain text or HTML. Plain text becomes paragraphs.")] string description = null,
            [Description("-1 low, 0 normal, 1 high. Defaults to 0.")] int priority = 0,
            [Description("Due date as YYYY-MM-DD.")] string dueDate = null,
            [Description("Tag ids from this board in get_board. Replaces the selection; omitted or null means no tags on a new card.")] int[] tagIds = null)
        {
            if (!new[] { -1, 0, 1 }.Contains(priority)) throw new McpException("priority skal være -1, 0 eller 1.");
            if (string.IsNullOrWhiteSpace(title)) throw new McpException("title må ikke være tom.");

            var actor = await ActorAsync();
            var columns = Unwrap(await boards.GetColumns(actor, boardId));
            var columnIdValue = BoardInputs.ResolveColumn(columns, columnId, column);

            var selectedTagIds = tagIds == null ? [] : BoardInputs.ResolveTagIds(Unwrap(await boards.GetBoard(actor, boardId)).Tags, tagIds);

            var data = new WorkItemCreateDto
            {
                TagIds = selectedTagIds,
                ColumnId = columnIdValue,
                Title = title.Trim(),
                Description = BoardInputs.ToHtml(description),
                Priority = priority,
                DueDate = BoardInputs.ParseDueDate(dueDate),
            };

            var created = await boards.CreateCard(actor, data, boardId);
            if (created.Outcome != ServiceOutcome.Success) throw new McpException(Describe(created.Outcome, created.Error));

            var columnTitle = columns.First(candidate => candidate.Id == columnIdValue).Title;
            return $"Oprettet DS-{created.Value} \"{title.Trim()}\" i kolonnen \"{columnTitle}\" på tavle {boardId}.";
        }

        [McpServerTool(Name = "update_card")]
        [Description("Update an existing DS28 card. Fields that are omitted keep their current value. Empty values clear description, due date, colour or assignees. Omitted/null tagIds preserve tags; [] clears tags on this board.")]
        public async Task<string> UpdateCard(
            [Description("Board id from list_boards.")] int boardId,
            [Description("Card number from DS-<id>, for example 42 for DS-42.")] int cardId,
            [Description("New title.")] string title = null,
            [Description("Plain text or HTML. Empty string clears the description.")] string description = null,
            [Description("-1 low, 0 normal, 1 high.")] int? priority = null,
            [Description("Due date as YYYY-MM-DD. Empty string clears it.")] string dueDate = null,
            [Description("Hex colour from the board palette, for example \"#3366ff\". Empty string clears it.")] string color = null,
            [Description("Full names or emails of the responsible users. Replaces the current assignees; an empty list clears them.")] string[] assignees = null,
            [Description("Tag ids from this board in get_board. Omitted or null preserves the selection; [] removes all tags on this board only.")] int[] tagIds = null)
        {
            if (priority.HasValue && !new[] { -1, 0, 1 }.Contains(priority.Value)) throw new McpException("priority skal være -1, 0 eller 1.");

            var actor = await ActorAsync();
            var board = Unwrap(await boards.GetBoard(actor, boardId));
            var card = board.Cards.FirstOrDefault(candidate => candidate.Id == cardId);
            if (card == null) throw new McpException($"Kortet DS-{cardId} findes ikke på tavle {boardId}.");

            var changes = new List<string>();

            var newTitle = title == null ? card.Title : title.Trim();
            if (string.IsNullOrEmpty(newTitle)) throw new McpException("title må ikke være tom.");
            if (newTitle != card.Title) changes.Add("titel");

            var newDescription = description == null ? card.Description : BoardInputs.ToHtml(description);
            if (newDescription != card.Description) changes.Add("beskrivelse");

            var newPriority = priority ?? card.Priority;
            if (newPriority != card.Priority) changes.Add("prioritet");

            var newDueDate = dueDate == null ? card.DueDate : BoardInputs.ParseDueDate(dueDate);
            if (newDueDate != card.DueDate) changes.Add("frist");

            var newColor = color == null ? card.Color : (string.IsNullOrEmpty(color) ? null : color);
            if (newColor != card.Color) changes.Add("farve");

            var assigneeIds = card.Assignees.Select(user => user.Id).ToList();
            if (assignees != null)
            {
                assigneeIds = [];
                foreach (var wanted in assignees)
                {
                    var candidates = Unwrap(await boards.SearchAssignees(actor, wanted, boardId));
                    var user = BoardInputs.MatchUser(candidates, wanted);
                    if (!assigneeIds.Contains(user.Id)) assigneeIds.Add(user.Id);
                }

                if (!assigneeIds.Order().SequenceEqual(card.Assignees.Select(user => user.Id).Order())) changes.Add("ansvarlige");
            }

            var selectedTagIds = tagIds == null ? null : BoardInputs.ResolveTagIds(board.Tags, tagIds);
            if (selectedTagIds != null && !selectedTagIds.Order().SequenceEqual(card.Tags.Select(tag => tag.Id).Order())) changes.Add("tags");

            if (changes.Count == 0) return $"DS-{cardId} er uændret på tavle {boardId}.";

            var data = new WorkItemUpdateDto
            {
                TagIds = selectedTagIds,
                Title = newTitle,
                Description = newDescription,
                Color = newColor,
                AssigneeUserIds = assigneeIds,
                OwnerPositionId = card.OwnerPositionId,
                DueDate = newDueDate,
                Priority = newPriority,
            };

            var updated = await boards.UpdateCard(actor, data, boardId, cardId);
            if (updated.Outcome != ServiceOutcome.Success) throw new McpException(Describe(updated.Outcome, updated.Error));

            return $"Opdaterede DS-{cardId} på tavle {boardId}: {string.Join(", ", changes)}.";
        }

        [McpServerTool(Name = "create_tag")]
        [Description("Create a tag on one DS28 board with a unique name and optional hex colour. Returns its tag id. Does not assign it to cards.")]
        public async Task<string> CreateTag(
            [Description("Board id from list_boards.")] int boardId,
            [Description("Tag name, up to 64 characters; unique on this board ignoring case.")] string name,
            [Description("Optional hex colour, for example #dbeafe. Omitted, null or empty means no colour.")] string color = null)
        {
            var created = await boards.CreateTag(await ActorAsync(), new BoardTagUpdateDto { Name = name, Color = color }, boardId);
            var tagId = Unwrap(created);

            return $"Oprettet tag {tagId} \"{name.Trim()}\" på tavle {boardId}.";
        }

        [McpServerTool(Name = "update_tag")]
        [Description("Rename or recolour a tag on a DS28 board. Changes apply to every card using this tag on this board. Omitted/null fields are preserved; empty color removes the colour.")]
        public async Task<string> UpdateTag(
            [Description("Board id from list_boards.")] int boardId,
            [Description("Tag id from this board in get_board.")] int tagId,
            [Description("New unique name, up to 64 characters. Omitted or null preserves the name.")] string name = null,
            [Description("New hex colour. Omitted or null preserves it; empty string removes it.")] string color = null)
        {
            var actor = await ActorAsync();
            var board = Unwrap(await boards.GetBoard(actor, boardId));
            var tag = board.Tags.FirstOrDefault(tag => tag.Id == tagId);
            if (tag == null) throw new McpException("Tagget findes ikke på tavlen.");

            var updated = await boards.UpdateTag(actor, new BoardTagUpdateDto { Name = name ?? tag.Name, Color = color ?? tag.Color }, boardId, tagId);
            if (updated.Outcome != ServiceOutcome.Success) throw new McpException(Describe(updated.Outcome, updated.Error));

            return $"Opdaterede tag {tagId} på tavle {boardId}.";
        }

        [McpServerTool(Name = "delete_tag")]
        [Description("Delete a tag from a DS28 board and remove it from all cards on that board. Cards and tags on other boards are preserved.")]
        public async Task<string> DeleteTag(
            [Description("Board id from list_boards.")] int boardId,
            [Description("Tag id from this board in get_board.")] int tagId)
        {
            var removed = await boards.DeleteTag(await ActorAsync(), boardId, tagId);
            if (removed.Outcome != ServiceOutcome.Success) throw new McpException(Describe(removed.Outcome, removed.Error));

            return $"Slettede tag {tagId} fra tavle {boardId} og fjernede det fra tavlens kort.";
        }

        [McpServerTool(Name = "move_card")]
        [Description("Move a DS28 card to another column or position on the same board. Uses the card number from DS-<id>.")]
        public async Task<string> MoveCard(
            [Description("Board id from list_boards.")] int boardId,
            [Description("Card number from DS-<id>, for example 42 for DS-42.")] int cardId,
            [Description("Target column title from get_board. Use either column or columnId.")] string column = null,
            [Description("Target column id from get_board.")] int? columnId = null,
            [Description("Zero-based position in the target column. Defaults to the end.")] int? position = null)
        {
            var actor = await ActorAsync();
            var board = Unwrap(await boards.GetBoard(actor, boardId));
            var card = board.Cards.FirstOrDefault(candidate => candidate.Id == cardId);
            if (card == null) throw new McpException($"Kortet DS-{cardId} findes ikke på tavle {boardId}.");

            var targetColumnId = BoardInputs.ResolveColumn(board.Columns, columnId, column);
            var lastPosition = board.Cards.Count(candidate => candidate.ColumnId == targetColumnId && candidate.Id != cardId);
            var targetPosition = position == null ? lastPosition : Math.Clamp(position.Value, 0, lastPosition);

            var moved = await boards.MoveCard(actor, new WorkItemMoveDto { ColumnId = targetColumnId, Position = targetPosition }, boardId, cardId);
            if (moved.Outcome != ServiceOutcome.Success) throw new McpException(Describe(moved.Outcome, moved.Error));

            var columnTitle = board.Columns.First(candidate => candidate.Id == targetColumnId).Title;
            return $"Flyttede DS-{cardId} til kolonnen \"{columnTitle}\" på tavle {boardId} som nr. {targetPosition + 1}.";
        }

        [McpServerTool(Name = "delete_card")]
        [Description("Remove a DS28 card from a board. The card itself is soft-deleted when this was its last board.")]
        public async Task<string> DeleteCard(
            [Description("Board id from list_boards.")] int boardId,
            [Description("Card number from DS-<id>, for example 42 for DS-42.")] int cardId)
        {
            var actor = await ActorAsync();
            var board = Unwrap(await boards.GetBoard(actor, boardId));
            var card = board.Cards.FirstOrDefault(candidate => candidate.Id == cardId);
            if (card == null) throw new McpException($"Kortet DS-{cardId} findes ikke på tavle {boardId}.");

            var removed = await boards.RemoveCard(actor, boardId, cardId);
            if (removed.Outcome != ServiceOutcome.Success) throw new McpException(Describe(removed.Outcome, removed.Error));

            var otherBoards = card.Placements.Count - 1;
            return otherBoards > 0
                ? $"Fjernede DS-{cardId} fra tavle {boardId}. Kortet ligger fortsat på {otherBoards} andre tavler."
                : $"Slettede DS-{cardId} fra tavle {boardId}.";
        }

        [McpServerTool(Name = "search_cards", ReadOnly = true)]
        [Description("Search DS28 cards on boards the logged-in user can see, by card number (\"DS-42\" or \"42\") or part of the title. Returns up to 10 cards with their boards and columns.")]
        public async Task<string> SearchCards([Description("Card number such as DS-42 or 42, or part of the title.")] string search)
        {
            if (string.IsNullOrWhiteSpace(search)) throw new McpException("search må ikke være tom.");

            var result = await boards.SearchCards(await ActorAsync(), search);

            return BoardText.SearchResults(Unwrap(result));
        }

        [McpServerTool(Name = "add_dependency")]
        [Description("Make a DS28 card wait on another card, for example a frontend card waiting on a backend card. The other card may be on another board the user can see. Rejects self-dependencies, duplicates and cycles.")]
        public async Task<string> AddDependency(
            [Description("Board id from list_boards where the waiting card is placed.")] int boardId,
            [Description("Number of the waiting card from DS-<id>, for example 42 for DS-42.")] int cardId,
            [Description("Number of the card it waits on, from get_board or search_cards.")] int dependsOnCardId)
        {
            var added = await boards.AddDependency(await ActorAsync(), new WorkItemDependencyCreateDto { DependsOnWorkItemId = dependsOnCardId }, boardId, cardId);
            if (added.Outcome != ServiceOutcome.Success) throw new McpException(Describe(added.Outcome, added.Error));

            return $"DS-{cardId} venter nu på DS-{dependsOnCardId}.";
        }

        [McpServerTool(Name = "remove_dependency")]
        [Description("Remove the dependency between two DS28 cards, whichever card waits on the other. Works even when the other card is on a board the user cannot see.")]
        public async Task<string> RemoveDependency(
            [Description("Board id from list_boards where cardId is placed.")] int boardId,
            [Description("Card number from DS-<id> on this board.")] int cardId,
            [Description("Number of the other card in the dependency.")] int otherCardId)
        {
            var removed = await boards.RemoveDependency(await ActorAsync(), boardId, cardId, otherCardId);
            if (removed.Outcome == ServiceOutcome.NotFound) throw new McpException($"Der er ingen afhængighed mellem DS-{cardId} og DS-{otherCardId} på tavle {boardId}.");
            if (removed.Outcome != ServiceOutcome.Success) throw new McpException(Describe(removed.Outcome, removed.Error));

            return $"Fjernede afhængigheden mellem DS-{cardId} og DS-{otherCardId}.";
        }

        [McpServerTool(Name = "add_pull_request")]
        [Description("Add a GitHub pull request to a card on a developer board. Existing links are preserved.")]
        public async Task<string> AddPullRequest(
            [Description("Board id of the developer board.")] int boardId,
            [Description("Card number from DS-<id> on this board.")] int cardId,
            [Description("HTTPS URL of the GitHub pull request.")] string url)
        {
            var result = await boards.UpdatePullRequestLink(await ActorAsync(), boardId, cardId, url, remove: false);
            if (result.Outcome != ServiceOutcome.Success) throw new McpException(Describe(result.Outcome, result.Error));

            return $"Tilknyttede PR-link til DS-{cardId}.";
        }

        [McpServerTool(Name = "remove_pull_request")]
        [Description("Remove one GitHub pull request link from a card on a developer board.")]
        public async Task<string> RemovePullRequest(
            [Description("Board id of the developer board.")] int boardId,
            [Description("Card number from DS-<id> on this board.")] int cardId,
            [Description("HTTPS URL of the GitHub pull request.")] string url)
        {
            var result = await boards.UpdatePullRequestLink(await ActorAsync(), boardId, cardId, url, remove: true);
            if (result.Outcome != ServiceOutcome.Success) throw new McpException(Describe(result.Outcome, result.Error));

            return $"Fjernede PR-link fra DS-{cardId}.";
        }

        private async Task<BoardActor> ActorAsync()
        {
            var context = httpContextAccessor.HttpContext;
            if (context == null) throw new McpException("Ingen bruger er logget ind.");

            var authentication = await context.AuthenticateAsync(McpAuthenticationHandler.SchemeName);
            var user = authentication.Principal;
            if (user == null) throw new McpException("Ingen bruger er logget ind.");

            if (!await McpAccessHandler.HasAccess(userManager, user)) throw new McpException("MCP-adgang kræver rollen McpUser.");

            var canSeeAllBoards = user.IsInRole(nameof(AppGroups.SysAdmin)) || user.IsInRole(nameof(AppGroups.CampAdmin));
            return new BoardActor(userManager.GetUserId(user), canSeeAllBoards);
        }

        private static T Unwrap<T>(ServiceResult<T> result)
        {
            if (result.Outcome == ServiceOutcome.Success) return result.Value;

            throw new McpException(Describe(result.Outcome, result.Error));
        }

        private static string Describe(ServiceOutcome outcome, object error)
        {
            return outcome switch
            {
                ServiceOutcome.Forbidden => "Ingen adgang til tavlen.",
                ServiceOutcome.NotFound => "Tavlen eller kortet findes ikke.",
                ServiceOutcome.Conflict => error?.ToString() ?? "Ændringen er i konflikt med den nuværende tilstand.",
                _ => error?.ToString() ?? "Anmodningen er ugyldig.",
            };
        }
    }

    public static class BoardText
    {
        public static string Boards(IEnumerable<BoardSummaryDto> boards)
        {
            var lines = boards.Select(board =>
                $"- id {board.Id}: {board.Title}{(board.IsArchived ? " [arkiveret]" : "")} ({board.CardCount} kort)").ToList();

            return lines.Count == 0 ? "Ingen tavler fundet." : string.Join("\n", lines);
        }

        public static string Board(BoardDetailDto board)
        {
            var text = new StringBuilder();
            text.AppendLine($"Tavle: {board.Title} (id {board.Id}){(board.IsArchived ? " [arkiveret]" : "")}");

            if (board.IsDeveloperBoard) text.AppendLine("Udviklertavle");
            if (!string.IsNullOrWhiteSpace(board.Description)) text.AppendLine(board.Description);

            text.AppendLine().AppendLine("Kolonner:");
            foreach (var column in board.Columns)
            {
                text.AppendLine($"- id {column.Id}: {column.Title}{(column.IsDoneColumn ? " (færdig)" : "")}");
            }

            if (board.Colors.Count > 0)
            {
                text.AppendLine().AppendLine("Farver:");
                foreach (var color in board.Colors)
                {
                    text.AppendLine($"- {color.Name}: {color.Color}");
                }
            }

            text.AppendLine().AppendLine("Tags:");
            if (board.Tags.Count == 0) text.AppendLine("- (ingen)");
            foreach (var tag in board.Tags)
            {
                text.AppendLine($"- id {tag.Id}: {tag.Name} | farve {tag.Color ?? "ingen"}");
            }

            text.AppendLine().AppendLine("Kort:");
            if (board.Cards.Count == 0) text.AppendLine("- (ingen)");

            foreach (var card in board.Cards)
            {
                var column = board.Columns.FirstOrDefault(candidate => candidate.Id == card.ColumnId);
                var meta = new List<string> { $"kolonne {column?.Title ?? "?"} (id {card.ColumnId})" };

                if (card.Priority > 0) meta.Add("høj prioritet");
                if (card.Priority < 0) meta.Add("lav prioritet");
                if (card.DueDate.HasValue) meta.Add($"frist {card.DueDate.Value.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)}");
                if (!string.IsNullOrEmpty(card.Color)) meta.Add($"farve {card.Color}");
                if (card.Tags.Count > 0) meta.Add($"tags: {string.Join(", ", card.Tags.Select(tag => $"{tag.Name} (id {tag.Id})"))}");
                if (card.Assignees.Count > 0) meta.Add($"ansvarlige: {string.Join(", ", card.Assignees.Select(user => $"{user.FirstName} {user.LastName}"))}");
                if (card.PullRequests.Count > 0) meta.Add($"PR-links: {string.Join(", ", card.PullRequests)}");
                if (card.WaitingOn.Count > 0) meta.Add($"venter på: {string.Join(", ", card.WaitingOn.Select(Dependency))}");
                if (card.Blocking.Count > 0) meta.Add($"blokerer: {string.Join(", ", card.Blocking.Select(Dependency))}");

                text.Append($"- DS-{card.Id} {card.Title} | {string.Join(" | ", meta)}");
                text.AppendLine();

                if (!string.IsNullOrWhiteSpace(card.Description)) text.AppendLine($"  {card.Description}");
            }

            return text.ToString().TrimEnd();
        }

        public static string SearchResults(IEnumerable<WorkItemSearchResultDto> cards)
        {
            var lines = cards.Select(card =>
            {
                var placements = card.Placements.Select(placement => $"{placement.BoardTitle} (id {placement.BoardId}), kolonne {placement.ColumnTitle}{(placement.IsDoneColumn ? " (færdig)" : "")}");
                return $"- DS-{card.Id} {card.Title} | {string.Join("; ", placements)}";
            }).ToList();

            return lines.Count == 0 ? "Ingen kort fundet." : string.Join("\n", lines);
        }

        private static string Dependency(WorkItemDependencyDto dependency)
        {
            var name = dependency.IsVisible ? $"DS-{dependency.Id} {dependency.Title}" : $"DS-{dependency.Id} (skjult kort)";
            return $"{name} ({(dependency.IsDone ? "færdig" : "ikke færdig")})";
        }
    }

    public static class BoardInputs
    {
        public static List<int> ResolveTagIds(IEnumerable<BoardTagDto> tags, IEnumerable<int> tagIds)
        {
            var result = tagIds.Distinct().ToList();
            var knownIds = tags.Select(tag => tag.Id).ToHashSet();
            if (result.Any(id => id <= 0 || !knownIds.Contains(id))) throw new McpException("Et eller flere tags findes ikke på tavlen. Brug tag-id'er fra get_board.");

            return result;
        }

        public static string ToHtml(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;
            if (System.Text.RegularExpressions.Regex.IsMatch(text, "<[a-z][\\s\\S]*>", System.Text.RegularExpressions.RegexOptions.IgnoreCase)) return text;

            var blocks = System.Text.RegularExpressions.Regex.Split(text, "\\n{2,}")
                .Select(block => block.Trim())
                .Where(block => block.Length > 0)
                .Select(block => $"<p>{System.Net.WebUtility.HtmlEncode(block).Replace("\n", "<br>")}</p>");

            return string.Concat(blocks);
        }

        public static DateOnly? ParseDueDate(string value)
        {
            if (string.IsNullOrEmpty(value)) return null;
            if (!DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dueDate))
            {
                throw new McpException("dueDate skal være YYYY-MM-DD eller tom.");
            }

            return dueDate;
        }

        public static int ResolveColumn(IEnumerable<BoardColumnDto> columns, int? columnId, string columnTitle)
        {
            var known = string.Join(", ", columns.Select(column => $"{column.Id}={column.Title}"));

            if (columnId.HasValue)
            {
                var match = columns.FirstOrDefault(column => column.Id == columnId.Value);
                if (match == null) throw new McpException($"Kolonnen {columnId} findes ikke. Kolonner: {known}");

                return match.Id;
            }

            var wanted = columnTitle?.Trim().ToLowerInvariant() ?? "";
            if (wanted.Length == 0) throw new McpException($"Angiv column eller columnId. Kolonner: {known}");

            var byTitle = columns.FirstOrDefault(column => column.Title.Trim().ToLowerInvariant() == wanted);
            if (byTitle == null) throw new McpException($"Kolonnen \"{columnTitle}\" findes ikke. Kolonner: {known}");

            return byTitle.Id;
        }

        public static BoardMemberDto MatchUser(IReadOnlyCollection<BoardMemberDto> users, string wanted)
        {
            var query = wanted?.Trim().ToLowerInvariant() ?? "";
            if (query.Length == 0) throw new McpException("Angiv et navn eller en email som ansvarlig.");

            var exact = users
                .Where(user => $"{user.FirstName} {user.LastName}".Trim().ToLowerInvariant() == query
                    || (user.Email ?? "").ToLowerInvariant() == query)
                .ToList();

            if (exact.Count == 1) return exact[0];

            var candidates = string.Join(", ", (exact.Count > 0 ? exact : users).Select(user => $"{user.FirstName} {user.LastName} <{user.Email}>"));

            if (exact.Count > 1) throw new McpException($"\"{wanted}\" matcher flere brugere: {candidates}");
            if (candidates.Length > 0) throw new McpException($"\"{wanted}\" matcher ingen brugere præcist (forslag: {candidates}).");

            throw new McpException($"\"{wanted}\" matcher ingen brugere.");
        }
    }
}
