using System.ComponentModel;
using System.Globalization;
using System.Text;
using DS.Application.Services;
using DS.Application.Services.Boards;
using DS.Domain;
using DS.Domain.DTOs;
using DS.Domain.Models;
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
            var result = await boards.GetBoards(Actor());

            return BoardText.Boards(Unwrap(result));
        }

        [McpServerTool(Name = "get_board", ReadOnly = true)]
        [Description("Read one DS28 Kanban board: columns with ids, colour palette, and all cards with DS-number, column, priority, due date, colour, assignees and description.")]
        public async Task<string> GetBoard([Description("Board id from list_boards.")] int boardId)
        {
            var result = await boards.GetBoard(Actor(), boardId);

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
            [Description("Due date as YYYY-MM-DD.")] string dueDate = null)
        {
            if (!new[] { -1, 0, 1 }.Contains(priority)) throw new McpException("priority skal være -1, 0 eller 1.");
            if (string.IsNullOrWhiteSpace(title)) throw new McpException("title må ikke være tom.");

            var actor = Actor();
            var columns = Unwrap(await boards.GetColumns(actor, boardId));
            var columnIdValue = BoardInputs.ResolveColumn(columns, columnId, column);

            var data = new WorkItemCreateDto
            {
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
        [Description("Update an existing DS28 card. Fields that are omitted keep their current value. Empty values clear description, due date, colour or assignees.")]
        public async Task<string> UpdateCard(
            [Description("Board id from list_boards.")] int boardId,
            [Description("Card number from DS-<id>, for example 42 for DS-42.")] int cardId,
            [Description("New title.")] string title = null,
            [Description("Plain text or HTML. Empty string clears the description.")] string description = null,
            [Description("-1 low, 0 normal, 1 high.")] int? priority = null,
            [Description("Due date as YYYY-MM-DD. Empty string clears it.")] string dueDate = null,
            [Description("Hex colour from the board palette, for example \"#3366ff\". Empty string clears it.")] string color = null,
            [Description("Full names or emails of the responsible users. Replaces the current assignees; an empty list clears them.")] string[] assignees = null)
        {
            if (priority.HasValue && !new[] { -1, 0, 1 }.Contains(priority.Value)) throw new McpException("priority skal være -1, 0 eller 1.");

            var actor = Actor();
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

            if (changes.Count == 0) return $"DS-{cardId} er uændret på tavle {boardId}.";

            var data = new WorkItemUpdateDto
            {
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

        [McpServerTool(Name = "move_card")]
        [Description("Move a DS28 card to another column or position on the same board. Uses the card number from DS-<id>.")]
        public async Task<string> MoveCard(
            [Description("Board id from list_boards.")] int boardId,
            [Description("Card number from DS-<id>, for example 42 for DS-42.")] int cardId,
            [Description("Target column title from get_board. Use either column or columnId.")] string column = null,
            [Description("Target column id from get_board.")] int? columnId = null,
            [Description("Zero-based position in the target column. Defaults to the end.")] int? position = null)
        {
            var actor = Actor();
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
            var actor = Actor();
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

        private BoardActor Actor()
        {
            var user = httpContextAccessor.HttpContext?.User;
            if (user == null) throw new McpException("Ingen bruger er logget ind.");

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
                if (card.Assignees.Count > 0) meta.Add($"ansvarlige: {string.Join(", ", card.Assignees.Select(user => $"{user.FirstName} {user.LastName}"))}");

                text.Append($"- DS-{card.Id} {card.Title} | {string.Join(" | ", meta)}");
                text.AppendLine();

                if (!string.IsNullOrWhiteSpace(card.Description)) text.AppendLine($"  {card.Description}");
            }

            return text.ToString().TrimEnd();
        }
    }

    public static class BoardInputs
    {
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
