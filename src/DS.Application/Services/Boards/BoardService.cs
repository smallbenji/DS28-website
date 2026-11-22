using DS.Application.Data;
using DS.Application.Services.Email;
using DS.Domain;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Data.Common;
using System.Text;
using System.Text.RegularExpressions;

namespace DS.Application.Services.Boards
{
    public class BoardService(IDataDbContext dataDb, UserManager<User> userManager, IBoardEventPublisher boardEvents, EmailService emailService)
    {
        public async Task<ServiceResult<List<BoardSummaryDto>>> GetBoards(BoardActor actor)
        {
            var boards = await VisibleBoards(actor)
                .AsNoTracking()
                .OrderBy(board => board.Title)
                .Select(board => new BoardSummaryDto
                {
                    Id = board.Id,
                    Key = board.Key,
                    Title = board.Title,
                    Description = board.Description,
                    Icon = board.Icon,
                    Color = board.Color,
                    IsArchived = board.IsArchived,
                    CardCount = dataDb.BoardCards.Count(card => card.BoardId == board.Id),
                })
                .ToListAsync();

            return new ServiceResult<List<BoardSummaryDto>>(ServiceOutcome.Success, boards);
        }

        public async Task<ServiceResult<BoardDetailDto>> GetBoard(BoardActor actor, int id)
        {
            var board = await dataDb.Boards
                .AsNoTracking()
                .Include(board => board.Columns)
                .Include(board => board.Colors)
                .Include(board => board.Tags)
                .AsSplitQuery()
                .FirstOrDefaultAsync(board => board.Id == id && board.DeletedAt == null);
            if (board == null) return new ServiceResult(ServiceOutcome.NotFound);
            if (!await CanSeeBoardAsync(actor, board)) return new ServiceResult(ServiceOutcome.Forbidden);

            var cards = await LoadCardsAsync(id, board.IsDeveloperBoard);
            var workItemIds = cards.Select(card => card.WorkItemId).Distinct().ToList();
            var placements = await LoadPlacementsAsync(actor, workItemIds);
            var dependencies = await LoadDependenciesAsync(actor, workItemIds);

            return new ServiceResult<BoardDetailDto>(ServiceOutcome.Success, ToDetailDto(board, cards, placements, dependencies));
        }

        public async Task<ServiceResult<List<WorkItemSearchResultDto>>> SearchCards(BoardActor actor, string search)
        {
            if (string.IsNullOrWhiteSpace(search)) return new ServiceResult<List<WorkItemSearchResultDto>>(ServiceOutcome.Success, []);

            var searchTerm = search.Trim();
            var idMatch = Regex.Match(searchTerm, "^(?:ds-?)?(\\d{1,9})$", RegexOptions.IgnoreCase);
            int? searchId = idMatch.Success ? int.Parse(idMatch.Groups[1].Value) : null;

            var visibleBoardIds = VisibleBoards(actor).Select(board => board.Id);
            var items = await dataDb.WorkItems
                .AsNoTracking()
                .Where(item => dataDb.BoardCards.Any(card => card.WorkItemId == item.Id && visibleBoardIds.Contains(card.BoardId)))
                .Where(item => item.Id == searchId || EF.Functions.ILike(item.Title, $"%{searchTerm}%"))
                .OrderBy(item => item.Id == searchId ? 0 : 1)
                .ThenByDescending(item => item.UpdatedAt)
                .Take(10)
                .Select(item => new { item.Id, item.Title })
                .ToListAsync();

            var placements = await LoadPlacementsAsync(actor, items.Select(item => item.Id).ToList());
            var results = items
                .Select(item => new WorkItemSearchResultDto
                {
                    Id = item.Id,
                    Title = item.Title,
                    Placements = placements.TryGetValue(item.Id, out var itemPlacements) ? itemPlacements : [],
                })
                .ToList();

            return new ServiceResult<List<WorkItemSearchResultDto>>(ServiceOutcome.Success, results);
        }

        public async Task<ServiceResult> UpdatePullRequestLink(BoardActor actor, int id, int workItemId, string url, bool remove)
        {
            var board = await GetBoardAsync(id);
            if (board == null) return new ServiceResult(ServiceOutcome.NotFound);
            if (!await CanSeeBoardAsync(actor, board)) return new ServiceResult(ServiceOutcome.Forbidden);
            if (!board.IsDeveloperBoard) return new ServiceResult(ServiceOutcome.Forbidden, "Tavlen har ikke udviklerfunktioner slået til.");

            var cardId = await dataDb.BoardCards
                .Where(card => card.BoardId == id && card.WorkItemId == workItemId && card.WorkItem.DeletedAt == null)
                .Select(card => (int?)card.Id)
                .SingleOrDefaultAsync();
            if (cardId == null) return new ServiceResult(ServiceOutcome.NotFound);

            const string invalidUrl = "Angiv en HTTPS-adresse til en GitHub-pull request.";
            if (url == null || url.Length > 512) return new ServiceResult(ServiceOutcome.Invalid, invalidUrl);

            var match = Regex.Match(url.Trim(), @"\A(?i:https://github\.com)/([a-zA-Z0-9-]+)/([a-zA-Z0-9_.-]+)/pull/([1-9][0-9]*)/?\z");
            if (!match.Success || match.Groups[2].Value is "." or "..") return new ServiceResult(ServiceOutcome.Invalid, invalidUrl);

            var canonicalUrl = $"https://github.com/{match.Groups[1].Value.ToLowerInvariant()}/{match.Groups[2].Value.ToLowerInvariant()}/pull/{match.Groups[3].Value}";
            if (remove)
            {
                var deleted = await dataDb.BoardCardPullRequests
                    .Where(link => link.BoardCardId == cardId.Value && link.Url == canonicalUrl)
                    .ExecuteDeleteAsync();
                if (deleted == 0) return new ServiceResult(ServiceOutcome.NotFound);
            }
            else
            {
                var link = new BoardCardPullRequest { BoardCardId = cardId.Value, Url = canonicalUrl };
                dataDb.BoardCardPullRequests.Add(link);
                try
                {
                    await dataDb.SaveChangesAsync();
                }
                catch (DbUpdateException exception) when (exception.InnerException is DbException { SqlState: "23505" or "23503" } databaseError)
                {
                    return databaseError.SqlState == "23505"
                        ? new ServiceResult(ServiceOutcome.Conflict, "PR-linket er allerede tilknyttet.")
                        : new ServiceResult(ServiceOutcome.NotFound);
                }
                finally
                {
                    dataDb.Entry(link).State = EntityState.Detached;
                }
            }

            await boardEvents.BoardChanged(id);

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> AddDependency(BoardActor actor, WorkItemDependencyCreateDto data, int id, int workItemId)
        {
            if (data == null) return new ServiceResult(ServiceOutcome.Invalid);

            var board = await GetBoardAsync(id);
            if (board == null) return new ServiceResult(ServiceOutcome.NotFound);
            if (!await CanSeeBoardAsync(actor, board)) return new ServiceResult(ServiceOutcome.Forbidden);

            var isPlaced = await dataDb.BoardCards.AnyAsync(card => card.BoardId == id && card.WorkItemId == workItemId && card.WorkItem.DeletedAt == null);
            if (!isPlaced) return new ServiceResult(ServiceOutcome.NotFound);

            var dependsOnId = data.DependsOnWorkItemId;
            if (dependsOnId == workItemId) return new ServiceResult(ServiceOutcome.Invalid, "Et kort kan ikke vente på sig selv.");

            var visibleBoardIds = VisibleBoards(actor).Select(candidate => candidate.Id);
            var canSeeDependency = await dataDb.BoardCards
                .AnyAsync(card => card.WorkItemId == dependsOnId && card.WorkItem.DeletedAt == null && visibleBoardIds.Contains(card.BoardId));
            if (!canSeeDependency) return new ServiceResult(ServiceOutcome.Invalid, "Kortet findes ikke, eller du har ikke adgang til det.");

            var alreadyWaiting = await dataDb.WorkItemDependencies
                .AnyAsync(dependency => dependency.WorkItemId == workItemId && dependency.DependsOnWorkItemId == dependsOnId);
            if (alreadyWaiting) return new ServiceResult(ServiceOutcome.Conflict, $"Kortet venter allerede på DS-{dependsOnId}.");

            if (await WaitsOnAsync(dependsOnId, workItemId))
            {
                return new ServiceResult(ServiceOutcome.Invalid, $"DS-{dependsOnId} venter allerede på dette kort, direkte eller gennem andre kort.");
            }

            var dependency = new WorkItemDependency { WorkItemId = workItemId, DependsOnWorkItemId = dependsOnId };
            dataDb.WorkItemDependencies.Add(dependency);
            try
            {
                await dataDb.SaveChangesAsync();
            }
            catch (DbUpdateException exception) when (exception.InnerException is DbException { SqlState: "23505" })
            {
                dataDb.Entry(dependency).State = EntityState.Detached;
                return new ServiceResult(ServiceOutcome.Conflict, $"Kortet venter allerede på DS-{dependsOnId}.");
            }
            await NotifyBoardsWithCardsAsync([workItemId, dependsOnId]);

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> RemoveDependency(BoardActor actor, int id, int workItemId, int otherWorkItemId)
        {
            var board = await GetBoardAsync(id);
            if (board == null) return new ServiceResult(ServiceOutcome.NotFound);
            if (!await CanSeeBoardAsync(actor, board)) return new ServiceResult(ServiceOutcome.Forbidden);

            var isPlaced = await dataDb.BoardCards.AnyAsync(card => card.BoardId == id && card.WorkItemId == workItemId && card.WorkItem.DeletedAt == null);
            if (!isPlaced) return new ServiceResult(ServiceOutcome.NotFound);

            var dependencies = await dataDb.WorkItemDependencies
                .Where(dependency => (dependency.WorkItemId == workItemId && dependency.DependsOnWorkItemId == otherWorkItemId)
                    || (dependency.WorkItemId == otherWorkItemId && dependency.DependsOnWorkItemId == workItemId))
                .ToListAsync();
            if (dependencies.Count == 0) return new ServiceResult(ServiceOutcome.NotFound);

            dataDb.WorkItemDependencies.RemoveRange(dependencies);
            await dataDb.SaveChangesAsync();
            await NotifyBoardsWithCardsAsync([workItemId, otherWorkItemId]);

            return new ServiceResult(ServiceOutcome.Success);
        }

        private async Task<bool> WaitsOnAsync(int workItemId, int targetWorkItemId)
        {
            var visited = new HashSet<int> { workItemId };
            var frontier = new List<int> { workItemId };

            while (frontier.Count > 0)
            {
                var next = await dataDb.WorkItemDependencies
                    .AsNoTracking()
                    .Where(dependency => frontier.Contains(dependency.WorkItemId))
                    .Select(dependency => dependency.DependsOnWorkItemId)
                    .Distinct()
                    .ToListAsync();
                if (next.Contains(targetWorkItemId)) return true;

                frontier = next.Where(visited.Add).ToList();
            }

            return false;
        }

        public async Task<ServiceResult<List<BoardColumnDto>>> GetColumns(BoardActor actor, int id)
        {
            var board = await GetBoardAsync(id);
            if (board == null) return new ServiceResult(ServiceOutcome.NotFound);
            if (!await CanSeeBoardAsync(actor, board)) return new ServiceResult(ServiceOutcome.Forbidden);

            var columns = await dataDb.BoardColumns
                .AsNoTracking()
                .Where(column => column.BoardId == id)
                .OrderBy(column => column.Position).ThenBy(column => column.Id)
                .Select(column => new BoardColumnDto
                {
                    Id = column.Id,
                    Title = column.Title,
                    Position = column.Position,
                    WipLimit = column.WipLimit,
                    IsDoneColumn = column.IsDoneColumn,
                })
                .ToListAsync();

            return new ServiceResult<List<BoardColumnDto>>(ServiceOutcome.Success, columns);
        }

        public async Task<ServiceResult<List<BoardMemberDto>>> SearchAssignees(BoardActor actor, string search, int boardId)
        {
            if (!await VisibleBoards(actor).AnyAsync(board => board.Id == boardId)) return new ServiceResult(ServiceOutcome.Forbidden);
            if (string.IsNullOrWhiteSpace(search)) return new ServiceResult<List<BoardMemberDto>>(ServiceOutcome.Success, new List<BoardMemberDto>());

            var searchTerm = search.Trim();
            var users = await dataDb.Users
                .AsNoTracking()
                .Where(user => EF.Functions.ILike(user.FirstName + " " + user.LastName, $"%{searchTerm}%")
                    || EF.Functions.ILike(user.Email, $"%{searchTerm}%"))
                .OrderBy(user => user.FirstName)
                .ThenBy(user => user.LastName)
                .Take(10)
                .ToListAsync();

            return new ServiceResult<List<BoardMemberDto>>(ServiceOutcome.Success, users.Select(user => ToMemberDto(user)).ToList());
        }

        public async Task<ServiceResult<List<BoardMemberDto>>> GetMembers(BoardActor actor, int id)
        {
            if (!await VisibleBoards(actor).AnyAsync(board => board.Id == id)) return new ServiceResult(ServiceOutcome.Forbidden);

            var members = await dataDb.BoardMembers
                .AsNoTracking()
                .Where(member => member.BoardId == id)
                .Select(member => new
                {
                    member.User,
                    AssignedCardCount = dataDb.BoardCards.Count(card => card.BoardId == id
                        && card.WorkItem.DeletedAt == null
                        && card.WorkItem.Assignees.Any(assignee => assignee.UserId == member.UserId)),
                })
                .OrderBy(member => member.User.FirstName).ThenBy(member => member.User.LastName)
                .ToListAsync();

            var result = members.Select(member => ToMemberDto(member.User, member.AssignedCardCount)).ToList();

            return new ServiceResult<List<BoardMemberDto>>(ServiceOutcome.Success, result);
        }

        public async Task<ServiceResult> AddMember(BoardActor actor, BoardMemberCreateDto data, int id)
        {
            if (!await VisibleBoards(actor).AnyAsync(board => board.Id == id)) return new ServiceResult(ServiceOutcome.Forbidden);

            if (data == null || string.IsNullOrWhiteSpace(data.UserId)) return new ServiceResult(ServiceOutcome.Invalid, "Vælg en bruger.");

            var user = await userManager.FindByIdAsync(data.UserId.Trim());
            if (user == null) return new ServiceResult(ServiceOutcome.NotFound);

            await dataDb.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ds28.board_member (board_id, user_id) VALUES ({id}, {user.Id}) ON CONFLICT DO NOTHING");
            await boardEvents.BoardChanged(id);

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> RemoveMember(BoardActor actor, int id, string userId)
        {
            if (!await VisibleBoards(actor).AnyAsync(board => board.Id == id)) return new ServiceResult(ServiceOutcome.Forbidden);

            var member = await dataDb.BoardMembers.SingleOrDefaultAsync(member => member.BoardId == id && member.UserId == userId);
            if (member == null) return new ServiceResult(ServiceOutcome.NotFound);

            var hasOtherMembers = await dataDb.BoardMembers.AnyAsync(other => other.BoardId == id && other.UserId != userId);
            if (!hasOtherMembers) return new ServiceResult(ServiceOutcome.Conflict, "Tavlen skal have mindst ét medlem.");

            dataDb.BoardMembers.Remove(member);
            await dataDb.SaveChangesAsync();
            await boardEvents.BoardChanged(id);

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult<int>> CreateBoard(BoardActor actor, BoardCreateDto data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Title)) return new ServiceResult(ServiceOutcome.Invalid, "Titel er påkrævet.");

            if (!TryNormalizeShortText(data.Icon, 64, out var icon)) return new ServiceResult(ServiceOutcome.Invalid, "Ikonet er for langt.");
            if (!TryNormalizeShortText(data.Color, 16, out var color)) return new ServiceResult(ServiceOutcome.Invalid, "Farven er for lang.");

            var key = await GenerateKeyAsync(data.Title);

            var board = new Board
            {
                Key = key,
                Title = data.Title.Trim(),
                Description = data.Description,
                Icon = icon,
                Color = color,
                CreatedByUserId = actor.UserId,
                Members = [new BoardMember { UserId = actor.UserId }],
                Colors = CreateDefaultColors(),
            };

            var columns = NormalizeColumnTitles(data.Columns);
            for (var index = 0; index < columns.Count; index++)
            {
                board.Columns.Add(new BoardColumn
                {
                    Title = columns[index],
                    Position = index,
                    IsDoneColumn = index == columns.Count - 1,
                });
            }

            dataDb.Boards.Add(board);
            await dataDb.SaveChangesAsync();

            var creator = await dataDb.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Id == actor.UserId);
            emailService.QueueBoardCreatedNotificationMail(board, creator);

            return new ServiceResult<int>(ServiceOutcome.Success, board.Id);
        }

        public async Task<ServiceResult<int>> CreateTag(BoardActor actor, BoardTagUpdateDto data, int id)
        {
            if (!await VisibleBoards(actor).AnyAsync(board => board.Id == id)) return new ServiceResult(ServiceOutcome.Forbidden);

            if (data == null || !TryNormalizeShortText(data.Name, 64, out var name) || name == null)
            {
                return new ServiceResult(ServiceOutcome.Invalid, "Tagget skal have et navn på højst 64 tegn.");
            }
            if (!TryNormalizeCardColor(data.Color, out var color)) return new ServiceResult(ServiceOutcome.Invalid, "Ugyldig tagfarve.");

            if (await TagNameExists(id, name)) return new ServiceResult(ServiceOutcome.Conflict, "Et tag med dette navn findes allerede på tavlen.");

            var tag = new BoardTag { BoardId = id, Name = name, Color = color };
            dataDb.BoardTags.Add(tag);
            try
            {
                await dataDb.SaveChangesAsync();
            }
            catch (DbUpdateException exception) when (exception.InnerException is DbException { SqlState: "23505" })
            {
                dataDb.Entry(tag).State = EntityState.Detached;
                return new ServiceResult(ServiceOutcome.Conflict, "Et tag med dette navn findes allerede på tavlen.");
            }
            await boardEvents.BoardChanged(id);

            return new ServiceResult<int>(ServiceOutcome.Success, tag.Id);
        }

        public async Task<ServiceResult> UpdateTag(BoardActor actor, BoardTagUpdateDto data, int id, int tagId)
        {
            if (!await VisibleBoards(actor).AnyAsync(board => board.Id == id)) return new ServiceResult(ServiceOutcome.Forbidden);

            if (data == null || !TryNormalizeShortText(data.Name, 64, out var name) || name == null)
            {
                return new ServiceResult(ServiceOutcome.Invalid, "Tagget skal have et navn på højst 64 tegn.");
            }
            if (!TryNormalizeCardColor(data.Color, out var color)) return new ServiceResult(ServiceOutcome.Invalid, "Ugyldig tagfarve.");

            var tag = await dataDb.BoardTags.FirstOrDefaultAsync(tag => tag.Id == tagId && tag.BoardId == id);
            if (tag == null) return new ServiceResult(ServiceOutcome.NotFound);
            if (await TagNameExists(id, name, tagId)) return new ServiceResult(ServiceOutcome.Conflict, "Et tag med dette navn findes allerede på tavlen.");

            tag.Name = name;
            tag.Color = color;
            try
            {
                await dataDb.SaveChangesAsync();
            }
            catch (DbUpdateException exception) when (exception.InnerException is DbException { SqlState: "23505" })
            {
                await dataDb.Entry(tag).ReloadAsync();
                return new ServiceResult(ServiceOutcome.Conflict, "Et tag med dette navn findes allerede på tavlen.");
            }
            await boardEvents.BoardChanged(id);

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> DeleteTag(BoardActor actor, int id, int tagId)
        {
            if (!await VisibleBoards(actor).AnyAsync(board => board.Id == id)) return new ServiceResult(ServiceOutcome.Forbidden);

            var tag = await dataDb.BoardTags.FirstOrDefaultAsync(tag => tag.Id == tagId && tag.BoardId == id);
            if (tag == null) return new ServiceResult(ServiceOutcome.NotFound);

            dataDb.BoardTags.Remove(tag);
            await dataDb.SaveChangesAsync();
            await boardEvents.BoardChanged(id);

            return new ServiceResult(ServiceOutcome.Success);
        }

        private async Task<bool> TagNameExists(int boardId, string name, int? exceptId = null)
        {
            return await dataDb.BoardTags.AnyAsync(tag => tag.BoardId == boardId && tag.Id != exceptId && tag.Name.ToLower() == name.ToLower());
        }

        private static bool TryNormalizeTagNames(List<string> values, out List<string> names)
        {
            names = [];
            foreach (var value in values ?? [])
            {
                if (!TryNormalizeShortText(value, 64, out var name) || name == null) return false;
                if (!names.Contains(name, StringComparer.OrdinalIgnoreCase)) names.Add(name);
            }
            return true;
        }

        private async Task<List<BoardCardTag>> NewTagLinks(int boardId, List<int> selectedIds, List<string> names)
        {
            if (names.Count == 0) return [];

            var normalizedNames = names.Select(name => name.ToLowerInvariant()).ToList();
            var existing = await dataDb.BoardTags
                .Where(tag => tag.BoardId == boardId && normalizedNames.Contains(tag.Name.ToLower()))
                .ToListAsync();
            var links = new List<BoardCardTag>();
            foreach (var name in names)
            {
                var tag = existing.FirstOrDefault(tag => string.Equals(tag.Name, name, StringComparison.OrdinalIgnoreCase));
                if (tag != null && selectedIds.Contains(tag.Id)) continue;

                tag ??= new BoardTag { BoardId = boardId, Name = name };
                links.Add(new BoardCardTag { BoardId = boardId, BoardTag = tag });
            }
            return links;
        }

        private async Task<bool> SaveCardTags(bool hasNewNames)
        {
            try
            {
                await dataDb.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException exception) when (hasNewNames && exception.InnerException is DbException { SqlState: "23505" })
            {
                dataDb.ChangeTracker.Clear();
                return false;
            }
        }

        private async Task<bool> AreValidTagsAsync(int boardId, List<int> tagIds)
        {
            return tagIds.Count == 0 || await dataDb.BoardTags.CountAsync(tag => tag.BoardId == boardId && tagIds.Contains(tag.Id)) == tagIds.Count;
        }

        public async Task<ServiceResult<int>> CreateColor(BoardActor actor, BoardColorUpdateDto data, int id)
        {
            if (!await VisibleBoards(actor).AnyAsync(board => board.Id == id)) return new ServiceResult(ServiceOutcome.Forbidden);

            if (data == null || !TryNormalizeShortText(data.Name, 64, out var name) || name == null) return new ServiceResult(ServiceOutcome.Invalid, "Farven skal have et navn på højst 64 tegn.");
            if (!TryNormalizeCardColor(data.Color, out var color) || color == null) return new ServiceResult(ServiceOutcome.Invalid, "Vælg en gyldig farve.");

            var boardColor = new BoardColor { BoardId = id, Name = name, Color = color };
            dataDb.BoardColors.Add(boardColor);
            await dataDb.SaveChangesAsync();
            await boardEvents.BoardChanged(id);

            return new ServiceResult<int>(ServiceOutcome.Success, boardColor.Id);
        }

        public async Task<ServiceResult> UpdateColor(BoardActor actor, BoardColorUpdateDto data, int id, int colorId)
        {
            if (!await VisibleBoards(actor).AnyAsync(board => board.Id == id)) return new ServiceResult(ServiceOutcome.Forbidden);

            if (data == null || !TryNormalizeShortText(data.Name, 64, out var name) || name == null) return new ServiceResult(ServiceOutcome.Invalid, "Farven skal have et navn på højst 64 tegn.");
            if (!TryNormalizeCardColor(data.Color, out var color) || color == null) return new ServiceResult(ServiceOutcome.Invalid, "Vælg en gyldig farve.");

            var boardColor = await dataDb.BoardColors.FirstOrDefaultAsync(candidate => candidate.Id == colorId && candidate.BoardId == id);
            if (boardColor == null) return new ServiceResult(ServiceOutcome.NotFound);

            boardColor.Name = name;
            boardColor.Color = color;
            await dataDb.SaveChangesAsync();
            await boardEvents.BoardChanged(id);

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> DeleteColor(BoardActor actor, int id, int colorId)
        {
            if (!await VisibleBoards(actor).AnyAsync(board => board.Id == id)) return new ServiceResult(ServiceOutcome.Forbidden);

            var boardColor = await dataDb.BoardColors.FirstOrDefaultAsync(candidate => candidate.Id == colorId && candidate.BoardId == id);
            if (boardColor == null) return new ServiceResult(ServiceOutcome.NotFound);

            dataDb.BoardColors.Remove(boardColor);
            await dataDb.SaveChangesAsync();
            await boardEvents.BoardChanged(id);

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> UpdateBoard(BoardActor actor, BoardUpdateDto data, int id)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Title)) return new ServiceResult(ServiceOutcome.Invalid, "Titel er påkrævet.");

            var board = await dataDb.Boards.FirstOrDefaultAsync(board => board.Id == id && board.DeletedAt == null);
            if (board == null) return new ServiceResult(ServiceOutcome.NotFound);
            if (!await CanSeeBoardAsync(actor, board)) return new ServiceResult(ServiceOutcome.Forbidden);

            if (!TryNormalizeShortText(data.Icon, 64, out var icon)) return new ServiceResult(ServiceOutcome.Invalid, "Ikonet er for langt.");
            if (!TryNormalizeShortText(data.Color, 16, out var color)) return new ServiceResult(ServiceOutcome.Invalid, "Farven er for lang.");

            if (data.IsDeveloperBoard.HasValue && !actor.IsSysAdmin) return new ServiceResult(ServiceOutcome.Forbidden);

            board.Title = data.Title.Trim();
            board.Description = data.Description;
            board.Icon = icon;
            board.Color = color;
            board.IsArchived = data.IsArchived;
            if (data.IsDeveloperBoard.HasValue) board.IsDeveloperBoard = data.IsDeveloperBoard.Value;
            board.UpdatedAt = DateTime.UtcNow;

            await dataDb.SaveChangesAsync();
            await boardEvents.BoardChanged(id);

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> DeleteBoard(BoardActor actor, int id)
        {
            var board = await dataDb.Boards.FirstOrDefaultAsync(board => board.Id == id && board.DeletedAt == null);
            if (board == null) return new ServiceResult(ServiceOutcome.NotFound);
            if (!await CanSeeBoardAsync(actor, board)) return new ServiceResult(ServiceOutcome.Forbidden);

            board.DeletedAt = DateTime.UtcNow;
            board.UpdatedAt = DateTime.UtcNow;
            await dataDb.SaveChangesAsync();
            await boardEvents.BoardChanged(id);
            await NotifyBoardsWithCardsAsync(await dataDb.BoardCards.Where(card => card.BoardId == id).Select(card => card.WorkItemId).ToListAsync());

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult<int>> CreateColumn(BoardActor actor, BoardColumnCreateDto data, int id)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Title)) return new ServiceResult(ServiceOutcome.Invalid, "Titel er påkrævet.");

            var board = await GetBoardAsync(id);
            if (board == null) return new ServiceResult(ServiceOutcome.NotFound);
            if (!await CanSeeBoardAsync(actor, board)) return new ServiceResult(ServiceOutcome.Forbidden);
            if (!IsValidWipLimit(data.WipLimit)) return new ServiceResult(ServiceOutcome.Invalid, "WIP-grænsen skal være mindst 1.");

            var lastPosition = await dataDb.BoardColumns
                .Where(column => column.BoardId == id)
                .Select(column => (int?)column.Position)
                .MaxAsync() ?? -1;

            var column = new BoardColumn
            {
                BoardId = id,
                Title = data.Title.Trim(),
                Position = lastPosition + 1,
                WipLimit = data.WipLimit,
                IsDoneColumn = data.IsDoneColumn,
            };

            dataDb.BoardColumns.Add(column);
            await dataDb.SaveChangesAsync();
            await boardEvents.BoardChanged(id);

            return new ServiceResult<int>(ServiceOutcome.Success, column.Id);
        }

        public async Task<ServiceResult> UpdateColumn(BoardActor actor, BoardColumnUpdateDto data, int id, int columnId)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Title)) return new ServiceResult(ServiceOutcome.Invalid, "Titel er påkrævet.");

            var board = await GetBoardAsync(id);
            if (board == null) return new ServiceResult(ServiceOutcome.NotFound);
            if (!await CanSeeBoardAsync(actor, board)) return new ServiceResult(ServiceOutcome.Forbidden);
            if (!IsValidWipLimit(data.WipLimit)) return new ServiceResult(ServiceOutcome.Invalid, "WIP-grænsen skal være mindst 1.");

            var column = await dataDb.BoardColumns.FirstOrDefaultAsync(column => column.Id == columnId && column.BoardId == id);
            if (column == null) return new ServiceResult(ServiceOutcome.NotFound);

            var doneChanged = column.IsDoneColumn != data.IsDoneColumn;

            column.Title = data.Title.Trim();
            column.WipLimit = data.WipLimit;
            column.IsDoneColumn = data.IsDoneColumn;
            column.UpdatedAt = DateTime.UtcNow;

            await dataDb.SaveChangesAsync();
            await boardEvents.BoardChanged(id);
            if (doneChanged) await NotifyBoardsWithCardsAsync(await dataDb.BoardCards.Where(card => card.ColumnId == columnId).Select(card => card.WorkItemId).ToListAsync());

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> DeleteColumn(BoardActor actor, int id, int columnId)
        {
            var board = await GetBoardAsync(id);
            if (board == null) return new ServiceResult(ServiceOutcome.NotFound);
            if (!await CanSeeBoardAsync(actor, board)) return new ServiceResult(ServiceOutcome.Forbidden);

            var column = await dataDb.BoardColumns.FirstOrDefaultAsync(column => column.Id == columnId && column.BoardId == id);
            if (column == null) return new ServiceResult(ServiceOutcome.NotFound);

            var columnCount = await dataDb.BoardColumns.CountAsync(column => column.BoardId == id);
            if (columnCount <= 1) return new ServiceResult(ServiceOutcome.Invalid, "Boardet skal have mindst én kolonne.");

            var hasCards = await dataDb.BoardCards.AnyAsync(card => card.ColumnId == columnId);
            if (hasCards) return new ServiceResult(ServiceOutcome.Invalid, "Kolonnen indeholder kort og kan ikke slettes.");

            dataDb.BoardColumns.Remove(column);
            await dataDb.SaveChangesAsync();
            await boardEvents.BoardChanged(id);

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> ReorderColumns(BoardActor actor, List<int> columnOrder, int id)
        {
            var board = await GetBoardAsync(id);
            if (board == null) return new ServiceResult(ServiceOutcome.NotFound);
            if (!await CanSeeBoardAsync(actor, board)) return new ServiceResult(ServiceOutcome.Forbidden);

            if (columnOrder == null || columnOrder.Count == 0) return new ServiceResult(ServiceOutcome.Invalid, "Rækkefølgen må ikke være tom.");

            var columns = await dataDb.BoardColumns
                .Where(column => column.BoardId == id)
                .ToListAsync();

            if (columnOrder.Count != columns.Count || columnOrder.Distinct().Count() != columnOrder.Count)
            {
                return new ServiceResult(ServiceOutcome.Invalid, "Rækkefølgen skal indeholde hver kolonne én gang.");
            }

            if (columnOrder.Any(columnId => columns.All(column => column.Id != columnId)))
            {
                return new ServiceResult(ServiceOutcome.Invalid, "Rækkefølgen indeholder ukendte kolonner.");
            }

            await using var transaction = await dataDb.Database.BeginTransactionAsync();

            for (var index = 0; index < columns.Count; index++)
            {
                columns[index].Position = -1 - index;
            }
            await dataDb.SaveChangesAsync();

            for (var index = 0; index < columnOrder.Count; index++)
            {
                columns.First(column => column.Id == columnOrder[index]).Position = index;
            }
            await dataDb.SaveChangesAsync();

            await transaction.CommitAsync();
            await boardEvents.BoardChanged(id);

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult<int>> CreateCard(BoardActor actor, WorkItemCreateDto data, int id)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Title)) return new ServiceResult(ServiceOutcome.Invalid, "Titel er påkrævet.");

            var board = await GetBoardAsync(id);
            if (board == null) return new ServiceResult(ServiceOutcome.NotFound);
            if (!await CanSeeBoardAsync(actor, board)) return new ServiceResult(ServiceOutcome.Forbidden);

            var columnExists = await dataDb.BoardColumns.AnyAsync(column => column.Id == data.ColumnId && column.BoardId == id);
            if (!columnExists) return new ServiceResult(ServiceOutcome.Invalid, "Kolonnen findes ikke på boardet.");

            if (!TryNormalizeCardColor(data.Color, out var color)) return new ServiceResult(ServiceOutcome.Invalid, "Ugyldig kortfarve.");
            var assigneeUserIds = NormalizeUserIds(data.AssigneeUserIds);
            if (!await AreValidAssigneesAsync(assigneeUserIds)) return new ServiceResult(ServiceOutcome.Invalid, "En eller flere ansvarlige brugere findes ikke.");
            if (!await IsValidOwnerPositionAsync(data.OwnerPositionId)) return new ServiceResult(ServiceOutcome.Invalid, "Stillingen findes ikke.");

            if (!TryNormalizeTagNames(data.NewTagNames, out var newTagNames)) return new ServiceResult(ServiceOutcome.Invalid, "Tagget skal have et navn på højst 64 tegn.");

            var tagIds = (data.TagIds ?? []).Distinct().ToList();
            if (!await AreValidTagsAsync(id, tagIds)) return new ServiceResult(ServiceOutcome.Invalid, "Et eller flere tags findes ikke på tavlen.");

            var tagLinks = await NewTagLinks(id, tagIds, newTagNames);

            var item = new WorkItem
            {
                Title = data.Title.Trim(),
                Description = data.Description,
                Color = color,
                Assignees = assigneeUserIds.Select(userId => new WorkItemAssignee { UserId = userId }).ToList(),
                OwnerPositionId = data.OwnerPositionId,
                DueDate = data.DueDate,
                Priority = data.Priority,
                CreatedByUserId = actor.UserId,
                Links = NormalizeLinks(data.Links),
            };

            dataDb.WorkItems.Add(item);

            var lastPosition = await dataDb.BoardCards
                .Where(card => card.BoardId == id && card.ColumnId == data.ColumnId)
                .Select(card => (int?)card.Position)
                .MaxAsync() ?? -1;

            dataDb.BoardCards.Add(new BoardCard
            {
                BoardId = id,
                ColumnId = data.ColumnId,
                WorkItem = item,
                Tags = tagIds.Select(tagId => new BoardCardTag { BoardId = id, BoardTagId = tagId }).Concat(tagLinks).ToList(),
                Position = lastPosition + 1,
            });

            if (!await SaveCardTags(newTagNames.Count > 0)) return new ServiceResult(ServiceOutcome.Conflict, "Tavlens tags er ændret. Prøv at gemme kortet igen.");
            await boardEvents.BoardChanged(id);

            return new ServiceResult<int>(ServiceOutcome.Success, item.Id);
        }

        public async Task<ServiceResult> UpdateCard(BoardActor actor, WorkItemUpdateDto data, int id, int workItemId)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Title)) return new ServiceResult(ServiceOutcome.Invalid, "Titel er påkrævet.");

            var board = await GetBoardAsync(id);
            if (board == null) return new ServiceResult(ServiceOutcome.NotFound);
            if (!await CanSeeBoardAsync(actor, board)) return new ServiceResult(ServiceOutcome.Forbidden);

            var card = await dataDb.BoardCards
                .Include(card => card.Tags)
                .Include(card => card.WorkItem).ThenInclude(item => item.Assignees)
                .FirstOrDefaultAsync(card => card.BoardId == id && card.WorkItemId == workItemId && card.WorkItem.DeletedAt == null);
            if (card == null) return new ServiceResult(ServiceOutcome.NotFound);

            if (!TryNormalizeCardColor(data.Color, out var color)) return new ServiceResult(ServiceOutcome.Invalid, "Ugyldig kortfarve.");
            var assigneeUserIds = data.AssigneeUserIds == null ? null : NormalizeUserIds(data.AssigneeUserIds);
            if (assigneeUserIds != null && !await AreValidAssigneesAsync(assigneeUserIds)) return new ServiceResult(ServiceOutcome.Invalid, "En eller flere ansvarlige brugere findes ikke.");
            if (!await IsValidOwnerPositionAsync(data.OwnerPositionId)) return new ServiceResult(ServiceOutcome.Invalid, "Stillingen findes ikke.");

            if (!TryNormalizeTagNames(data.NewTagNames, out var newTagNames)) return new ServiceResult(ServiceOutcome.Invalid, "Tagget skal have et navn på højst 64 tegn.");

            var tagIds = data.TagIds?.Distinct().ToList();
            if (tagIds != null && !await AreValidTagsAsync(id, tagIds))
            {
                return new ServiceResult(ServiceOutcome.Invalid, "Et eller flere tags findes ikke på tavlen.");
            }

            var tagLinks = await NewTagLinks(id, tagIds ?? card.Tags.Select(link => link.BoardTagId).ToList(), newTagNames);

            if (tagIds != null)
            {
                card.Tags.RemoveAll(link => !tagIds.Contains(link.BoardTagId));
                var existingTagIds = card.Tags.Select(link => link.BoardTagId).ToHashSet();
                card.Tags.AddRange(tagIds.Where(tagId => !existingTagIds.Contains(tagId))
                    .Select(tagId => new BoardCardTag { BoardId = id, BoardTagId = tagId }));
            }

            card.Tags.AddRange(tagLinks);

            var item = card.WorkItem;
            item.Title = data.Title.Trim();
            item.Description = data.Description;
            item.Color = color;
            if (assigneeUserIds != null)
            {
                item.Assignees.RemoveAll(assignee => !assigneeUserIds.Contains(assignee.UserId));
                var existingUserIds = item.Assignees.Select(assignee => assignee.UserId).ToHashSet();
                item.Assignees.AddRange(assigneeUserIds.Where(userId => !existingUserIds.Contains(userId))
                    .Select(userId => new WorkItemAssignee { UserId = userId }));
            }

            item.OwnerPositionId = data.OwnerPositionId;
            item.DueDate = data.DueDate;
            item.Priority = data.Priority;
            item.UpdatedAt = DateTime.UtcNow;

            if (!await SaveCardTags(newTagNames.Count > 0)) return new ServiceResult(ServiceOutcome.Conflict, "Tavlens tags er ændret. Prøv at gemme kortet igen.");
            await NotifyBoardsWithCardAsync(workItemId);

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> AddAssignee(BoardActor actor, WorkItemAssigneeCreateDto data, int id, int workItemId)
        {
            if (string.IsNullOrWhiteSpace(data?.UserId)) return new ServiceResult(ServiceOutcome.Invalid, "Vælg en bruger.");

            var board = await GetBoardAsync(id);
            if (board == null) return new ServiceResult(ServiceOutcome.NotFound);
            if (!await CanSeeBoardAsync(actor, board)) return new ServiceResult(ServiceOutcome.Forbidden);
            if (!await CardExistsAsync(id, workItemId)) return new ServiceResult(ServiceOutcome.NotFound);

            var userId = data.UserId.Trim();
            if (!await AreValidAssigneesAsync([userId])) return new ServiceResult(ServiceOutcome.Invalid, "Brugeren findes ikke.");
            var inserted = await dataDb.Database.ExecuteSqlInterpolatedAsync($"INSERT INTO ds28.work_item_assignee (work_item_id, user_id) VALUES ({workItemId}, {userId}) ON CONFLICT DO NOTHING");
            if (inserted > 0) await NotifyBoardsWithCardAsync(workItemId);

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> RemoveAssignee(BoardActor actor, int id, int workItemId, string userId)
        {
            var board = await GetBoardAsync(id);
            if (board == null) return new ServiceResult(ServiceOutcome.NotFound);
            if (!await CanSeeBoardAsync(actor, board)) return new ServiceResult(ServiceOutcome.Forbidden);
            if (!await CardExistsAsync(id, workItemId)) return new ServiceResult(ServiceOutcome.NotFound);

            var assignment = await dataDb.WorkItemAssignees.FirstOrDefaultAsync(assignee => assignee.WorkItemId == workItemId && assignee.UserId == userId);
            if (assignment == null) return new ServiceResult(ServiceOutcome.Success);

            dataDb.WorkItemAssignees.Remove(assignment);
            try
            {
                await dataDb.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                dataDb.Entry(assignment).State = EntityState.Detached;
                return new ServiceResult(ServiceOutcome.Success);
            }

            await NotifyBoardsWithCardAsync(workItemId);

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> SetCompletion(BoardActor actor, int id, int workItemId, bool completed)
        {
            var board = await GetBoardAsync(id);
            if (board == null) return new ServiceResult(ServiceOutcome.NotFound);
            if (!await CanSeeBoardAsync(actor, board)) return new ServiceResult(ServiceOutcome.Forbidden);
            if (!await CardExistsAsync(id, workItemId)) return new ServiceResult(ServiceOutcome.NotFound);

            var assignment = await dataDb.WorkItemAssignees.FirstOrDefaultAsync(assignee => assignee.WorkItemId == workItemId && assignee.UserId == actor.UserId);
            if (assignment == null) return new ServiceResult(ServiceOutcome.Invalid, "Du er ikke ansvarlig på kortet.");
            if (assignment.CompletedAt.HasValue == completed) return new ServiceResult(ServiceOutcome.Success);

            assignment.CompletedAt = completed ? DateTime.UtcNow : null;
            try
            {
                await dataDb.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                dataDb.Entry(assignment).State = EntityState.Detached;
                return new ServiceResult(ServiceOutcome.Invalid, "Du er ikke ansvarlig på kortet.");
            }

            await NotifyBoardsWithCardAsync(workItemId);

            return new ServiceResult(ServiceOutcome.Success);
        }

        private async Task<bool> CardExistsAsync(int boardId, int workItemId)
        {
            return await dataDb.BoardCards.AnyAsync(card => card.BoardId == boardId && card.WorkItemId == workItemId && card.WorkItem.DeletedAt == null);
        }

        public async Task<ServiceResult> MoveCard(BoardActor actor, WorkItemMoveDto data, int id, int workItemId)
        {
            if (data == null) return new ServiceResult(ServiceOutcome.Invalid);

            var board = await GetBoardAsync(id);
            if (board == null) return new ServiceResult(ServiceOutcome.NotFound);
            if (!await CanSeeBoardAsync(actor, board)) return new ServiceResult(ServiceOutcome.Forbidden);

            var card = await dataDb.BoardCards
                .FirstOrDefaultAsync(card => card.BoardId == id && card.WorkItemId == workItemId);
            if (card == null) return new ServiceResult(ServiceOutcome.NotFound);

            var targetColumn = await dataDb.BoardColumns
                .FirstOrDefaultAsync(column => column.Id == data.ColumnId && column.BoardId == id);
            if (targetColumn == null) return new ServiceResult(ServiceOutcome.Invalid, "Kolonnen findes ikke på boardet.");

            var sourceColumnId = card.ColumnId;

            var sourceCards = await dataDb.BoardCards
                .Where(candidate => candidate.BoardId == id && candidate.ColumnId == sourceColumnId && candidate.Id != card.Id)
                .OrderBy(candidate => candidate.Position).ThenBy(candidate => candidate.Id)
                .ToListAsync();

            if (sourceColumnId == targetColumn.Id)
            {
                var position = Math.Clamp(data.Position, 0, sourceCards.Count);
                sourceCards.Insert(position, card);

                for (var index = 0; index < sourceCards.Count; index++) sourceCards[index].Position = index;
            }
            else
            {
                var targetCards = await dataDb.BoardCards
                    .Where(candidate => candidate.BoardId == id && candidate.ColumnId == targetColumn.Id)
                    .OrderBy(candidate => candidate.Position).ThenBy(candidate => candidate.Id)
                    .ToListAsync();

                card.ColumnId = targetColumn.Id;

                var position = Math.Clamp(data.Position, 0, targetCards.Count);
                targetCards.Insert(position, card);

                for (var index = 0; index < targetCards.Count; index++) targetCards[index].Position = index;
                for (var index = 0; index < sourceCards.Count; index++) sourceCards[index].Position = index;
            }

            await dataDb.SaveChangesAsync();
            await boardEvents.BoardChanged(id);
            if (sourceColumnId != targetColumn.Id) await NotifyBoardsWithCardAsync(workItemId);

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> RemoveCard(BoardActor actor, int id, int workItemId)
        {
            var board = await GetBoardAsync(id);
            if (board == null) return new ServiceResult(ServiceOutcome.NotFound);
            if (!await CanSeeBoardAsync(actor, board)) return new ServiceResult(ServiceOutcome.Forbidden);

            var card = await dataDb.BoardCards
                .FirstOrDefaultAsync(card => card.BoardId == id && card.WorkItemId == workItemId);
            if (card == null) return new ServiceResult(ServiceOutcome.NotFound);

            dataDb.BoardCards.Remove(card);
            await dataDb.SaveChangesAsync();

            var relatedWorkItemIds = new List<int>();
            var stillPlaced = await dataDb.BoardCards.AnyAsync(candidate => candidate.WorkItemId == workItemId);
            if (!stillPlaced)
            {
                var item = await dataDb.WorkItems.FirstOrDefaultAsync(candidate => candidate.Id == workItemId && candidate.DeletedAt == null);
                if (item != null)
                {
                    var dependencies = await dataDb.WorkItemDependencies
                        .Where(dependency => dependency.WorkItemId == workItemId || dependency.DependsOnWorkItemId == workItemId)
                        .ToListAsync();
                    relatedWorkItemIds = dependencies
                        .Select(dependency => dependency.WorkItemId == workItemId ? dependency.DependsOnWorkItemId : dependency.WorkItemId)
                        .ToList();

                    dataDb.WorkItemDependencies.RemoveRange(dependencies);
                    item.DeletedAt = DateTime.UtcNow;
                    item.UpdatedAt = DateTime.UtcNow;
                    await dataDb.SaveChangesAsync();
                }
            }

            await boardEvents.BoardChanged(id);
            await NotifyBoardsWithCardsAsync([workItemId, .. relatedWorkItemIds]);

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> AddCardPlacement(BoardActor actor, WorkItemPlacementCreateDto data, int id, int workItemId)
        {
            if (data == null) return new ServiceResult(ServiceOutcome.Invalid);

            var board = await GetBoardAsync(id);
            if (board == null) return new ServiceResult(ServiceOutcome.NotFound);
            if (!await CanSeeBoardAsync(actor, board)) return new ServiceResult(ServiceOutcome.Forbidden);

            var item = await dataDb.WorkItems.FirstOrDefaultAsync(candidate => candidate.Id == workItemId && candidate.DeletedAt == null);
            if (item == null) return new ServiceResult(ServiceOutcome.NotFound);

            var visibleBoardIds = VisibleBoards(actor).Select(candidate => candidate.Id);
            var canSeeSource = await dataDb.BoardCards
                .AnyAsync(card => card.WorkItemId == workItemId && visibleBoardIds.Contains(card.BoardId));
            if (!canSeeSource) return new ServiceResult(ServiceOutcome.Forbidden);

            var columnExists = await dataDb.BoardColumns.AnyAsync(column => column.Id == data.ColumnId && column.BoardId == id);
            if (!columnExists) return new ServiceResult(ServiceOutcome.Invalid, "Kolonnen findes ikke på boardet.");

            var alreadyPlaced = await dataDb.BoardCards.AnyAsync(card => card.BoardId == id && card.WorkItemId == workItemId);
            if (alreadyPlaced) return new ServiceResult(ServiceOutcome.Invalid, "Kortet ligger allerede på boardet.");

            var lastPosition = await dataDb.BoardCards
                .Where(card => card.BoardId == id && card.ColumnId == data.ColumnId)
                .Select(card => (int?)card.Position)
                .MaxAsync() ?? -1;

            dataDb.BoardCards.Add(new BoardCard
            {
                BoardId = id,
                ColumnId = data.ColumnId,
                WorkItemId = workItemId,
                Position = lastPosition + 1,
            });

            await dataDb.SaveChangesAsync();
            await NotifyBoardsWithCardAsync(workItemId);

            return new ServiceResult(ServiceOutcome.Success);
        }

        private async Task NotifyBoardsWithCardAsync(int workItemId)
        {
            await NotifyBoardsWithCardsAsync([workItemId]);
        }

        private async Task NotifyBoardsWithCardsAsync(List<int> workItemIds)
        {
            if (workItemIds.Count == 0) return;

            var dependencies = await dataDb.WorkItemDependencies
                .AsNoTracking()
                .Where(dependency => workItemIds.Contains(dependency.WorkItemId) || workItemIds.Contains(dependency.DependsOnWorkItemId))
                .Select(dependency => new { dependency.WorkItemId, dependency.DependsOnWorkItemId })
                .ToListAsync();
            var affectedIds = workItemIds
                .Concat(dependencies.Select(dependency => dependency.WorkItemId))
                .Concat(dependencies.Select(dependency => dependency.DependsOnWorkItemId))
                .Distinct()
                .ToList();

            var boardIds = await dataDb.BoardCards
                .AsNoTracking()
                .Where(card => affectedIds.Contains(card.WorkItemId))
                .Select(card => card.BoardId)
                .Distinct()
                .ToListAsync();

            foreach (var boardId in boardIds) await boardEvents.BoardChanged(boardId);
        }

        private async Task<Dictionary<int, CardDependencies>> LoadDependenciesAsync(BoardActor actor, List<int> workItemIds)
        {
            if (workItemIds.Count == 0) return [];

            var edges = await dataDb.WorkItemDependencies
                .AsNoTracking()
                .Where(dependency => workItemIds.Contains(dependency.WorkItemId) || workItemIds.Contains(dependency.DependsOnWorkItemId))
                .Select(dependency => new { dependency.WorkItemId, dependency.DependsOnWorkItemId })
                .ToListAsync();
            if (edges.Count == 0) return [];

            var relatedIds = edges.SelectMany(edge => new[] { edge.WorkItemId, edge.DependsOnWorkItemId }).Distinct().ToList();
            var titles = await dataDb.WorkItems
                .AsNoTracking()
                .Where(item => relatedIds.Contains(item.Id))
                .ToDictionaryAsync(item => item.Id, item => item.Title);
            var unfinishedIds = (await dataDb.BoardCards
                .AsNoTracking()
                .Where(card => relatedIds.Contains(card.WorkItemId) && card.Board.DeletedAt == null && !card.Column.IsDoneColumn)
                .Select(card => card.WorkItemId)
                .Distinct()
                .ToListAsync())
                .ToHashSet();
            var placements = await LoadPlacementsAsync(actor, relatedIds);

            WorkItemDependencyDto ToDependencyDto(int relatedId)
            {
                var isVisible = placements.TryGetValue(relatedId, out var relatedPlacements);

                return new WorkItemDependencyDto
                {
                    Id = relatedId,
                    IsVisible = isVisible,
                    Title = isVisible ? titles[relatedId] : null,
                    IsDone = !unfinishedIds.Contains(relatedId),
                    Placements = isVisible ? relatedPlacements : [],
                };
            }

            var result = workItemIds.ToDictionary(workItemId => workItemId, _ => new CardDependencies([], []));
            foreach (var edge in edges.Where(edge => titles.ContainsKey(edge.WorkItemId) && titles.ContainsKey(edge.DependsOnWorkItemId)))
            {
                if (result.TryGetValue(edge.WorkItemId, out var waiting)) waiting.WaitingOn.Add(ToDependencyDto(edge.DependsOnWorkItemId));
                if (result.TryGetValue(edge.DependsOnWorkItemId, out var blocking)) blocking.Blocking.Add(ToDependencyDto(edge.WorkItemId));
            }

            foreach (var dependencies in result.Values)
            {
                dependencies.WaitingOn.Sort(CompareDependencies);
                dependencies.Blocking.Sort(CompareDependencies);
            }

            return result;
        }

        private static int CompareDependencies(WorkItemDependencyDto first, WorkItemDependencyDto second)
        {
            return (first.IsDone, first.Id).CompareTo((second.IsDone, second.Id));
        }

        private sealed record CardDependencies(List<WorkItemDependencyDto> WaitingOn, List<WorkItemDependencyDto> Blocking);

        private async Task<List<BoardCard>> LoadCardsAsync(int boardId, bool includePullRequests)
        {
            var cards = dataDb.BoardCards
                .AsNoTracking()
                .Where(card => card.BoardId == boardId && card.WorkItem.DeletedAt == null)
                .Include(card => card.WorkItem).ThenInclude(item => item.Assignees).ThenInclude(assignee => assignee.User).ThenInclude(user => user.ProfilePicture)
                .Include(card => card.WorkItem).ThenInclude(item => item.OwnerPosition)
                .Include(card => card.WorkItem).ThenInclude(item => item.Links)
                .Include(card => card.Tags).ThenInclude(link => link.BoardTag)
                .AsQueryable();

            if (includePullRequests) cards = cards.Include(card => card.PullRequests);

            return await cards.AsSplitQuery()
                .OrderBy(card => card.ColumnId).ThenBy(card => card.Position).ThenBy(card => card.Id)
                .ToListAsync();
        }

        private async Task<Dictionary<int, List<WorkItemPlacementDto>>> LoadPlacementsAsync(BoardActor actor, List<int> workItemIds)
        {
            if (workItemIds.Count == 0) return [];

            var visibleBoardIds = VisibleBoards(actor).Select(board => board.Id);
            var placements = await dataDb.BoardCards
                .AsNoTracking()
                .Where(card => workItemIds.Contains(card.WorkItemId) && visibleBoardIds.Contains(card.BoardId))
                .OrderBy(card => card.Board.Title).ThenBy(card => card.Column.Position).ThenBy(card => card.Id)
                .Select(card => new
                {
                    card.WorkItemId,
                    card.BoardId,
                    BoardTitle = card.Board.Title,
                    BoardIcon = card.Board.Icon,
                    card.ColumnId,
                    ColumnTitle = card.Column.Title,
                    card.Column.IsDoneColumn,
                })
                .ToListAsync();

            return placements
                .GroupBy(placement => placement.WorkItemId)
                .ToDictionary(
                    group => group.Key,
                    group => group.Select(placement => new WorkItemPlacementDto
                    {
                        BoardId = placement.BoardId,
                        BoardTitle = placement.BoardTitle,
                        BoardIcon = placement.BoardIcon,
                        ColumnId = placement.ColumnId,
                        ColumnTitle = placement.ColumnTitle,
                        IsDoneColumn = placement.IsDoneColumn,
                    }).ToList());
        }

        private async Task<Board> GetBoardAsync(int id)
        {
            return await dataDb.Boards.FirstOrDefaultAsync(board => board.Id == id && board.DeletedAt == null);
        }

        private async Task<string> GenerateKeyAsync(string title)
        {
            var baseKey = Slugify(title);
            if (baseKey.Length == 0) baseKey = "board";
            if (baseKey.Length > 64) baseKey = baseKey[..64];

            var candidate = baseKey;
            var suffix = 2;

            while (await dataDb.Boards.IgnoreQueryFilters().AnyAsync(board => board.Key == candidate))
            {
                var suffixText = $"-{suffix++}";
                var clipped = baseKey.Length + suffixText.Length > 64
                    ? baseKey[..(64 - suffixText.Length)]
                    : baseKey;
                candidate = clipped + suffixText;
            }

            return candidate;
        }

        private static string Slugify(string value)
        {
            var builder = new StringBuilder();
            var lastWasSeparator = false;

            foreach (var character in value.Trim().ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(character))
                {
                    builder.Append(character);
                    lastWasSeparator = false;
                }
                else if (builder.Length > 0 && !lastWasSeparator)
                {
                    builder.Append('-');
                    lastWasSeparator = true;
                }
            }

            return builder.ToString().TrimEnd('-');
        }

        private async Task<bool> AreValidAssigneesAsync(List<string> userIds)
        {
            if (userIds.Count == 0) return true;

            return await dataDb.Users.CountAsync(user => userIds.Contains(user.Id)) == userIds.Count;
        }

        private async Task<bool> IsValidOwnerPositionAsync(int? positionId)
        {
            if (positionId == null) return true;

            return await dataDb.OrgPositions.AnyAsync(position => position.Id == positionId && position.DeletedAt == null);
        }

        private async Task<bool> CanSeeBoardAsync(BoardActor actor, Board board)
        {
            return await VisibleBoards(actor).AnyAsync(candidate => candidate.Id == board.Id);
        }

        public IQueryable<Board> VisibleBoards(BoardActor actor)
        {
            var boards = dataDb.Boards.Where(board => board.DeletedAt == null);
            if (actor.CanSeeAllBoards) return boards;

            var userId = actor.UserId;
            return boards.Where(board => board.Members.Any(member => member.UserId == userId));
        }

        private static BoardMemberDto ToMemberDto(User user, int? assignedCardCount = null)
        {
            return new BoardMemberDto
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                AssignedCardCount = assignedCardCount,
            };
        }

        private static bool TryNormalizeShortText(string value, int maxLength, out string result)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                result = null;
                return true;
            }

            result = value.Trim();
            return result.Length <= maxLength;
        }

        private static bool TryNormalizeCardColor(string value, out string result)
        {
            result = string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToLowerInvariant();
            return result == null || Regex.IsMatch(result, "^#[0-9a-f]{6}$");
        }

        private static List<BoardColor> CreateDefaultColors()
        {
            return
            [
                new() { Name = "Blå", Color = "#dbeafe" },
                new() { Name = "Grøn", Color = "#dcfce7" },
                new() { Name = "Gul", Color = "#fef3c7" },
                new() { Name = "Rød", Color = "#fee2e2" },
                new() { Name = "Lilla", Color = "#ede9fe" },
                new() { Name = "Turkis", Color = "#ccfbf1" },
                new() { Name = "Orange", Color = "#ffedd5" },
                new() { Name = "Pink", Color = "#fce7f3" },
                new() { Name = "Grå", Color = "#e5e7eb" },
                new() { Name = "Brun", Color = "#e7d5c5" },
            ];
        }

        private static bool IsValidWipLimit(int? wipLimit)
        {
            return wipLimit == null || wipLimit >= 1;
        }

        private static List<string> NormalizeUserIds(List<string> userIds)
        {
            return (userIds ?? [])
                .Where(userId => !string.IsNullOrWhiteSpace(userId))
                .Select(userId => userId.Trim())
                .Distinct()
                .ToList();
        }

        private static List<string> NormalizeColumnTitles(List<string> titles)
        {
            var result = (titles ?? [])
                .Where(title => !string.IsNullOrWhiteSpace(title))
                .Select(title => title.Trim())
                .ToList();

            return result.Count > 0 ? result : ["Backlog", "I gang", "Færdig"];
        }

        private static List<WorkItemLink> NormalizeLinks(List<WorkItemLinkDto> links)
        {
            if (links == null) return [];

            return links
                .Where(link => link != null && !string.IsNullOrWhiteSpace(link.EntityType) && link.EntityId > 0)
                .Select(link => new WorkItemLink { EntityType = link.EntityType.Trim(), EntityId = link.EntityId })
                .DistinctBy(link => (link.EntityType, link.EntityId))
                .ToList();
        }

        private static BoardDetailDto ToDetailDto(Board board, List<BoardCard> cards, Dictionary<int, List<WorkItemPlacementDto>> placements, Dictionary<int, CardDependencies> dependencies)
        {
            return new BoardDetailDto
            {
                Id = board.Id,
                Key = board.Key,
                Title = board.Title,
                Description = board.Description,
                Icon = board.Icon,
                Color = board.Color,
                IsArchived = board.IsArchived,
                IsDeveloperBoard = board.IsDeveloperBoard,
                Columns = board.Columns
                    .OrderBy(column => column.Position).ThenBy(column => column.Id)
                    .Select(column => new BoardColumnDto
                    {
                        Id = column.Id,
                        Title = column.Title,
                        Position = column.Position,
                        WipLimit = column.WipLimit,
                        IsDoneColumn = column.IsDoneColumn,
                    })
                    .ToList(),
                Cards = cards.Select(card => ToCardDto(card, placements, dependencies)).ToList(),
                Tags = board.Tags.OrderBy(tag => tag.Name).ThenBy(tag => tag.Id)
                    .Select(tag => new BoardTagDto { Id = tag.Id, Name = tag.Name, Color = tag.Color }).ToList(),
                Colors = board.Colors.OrderBy(color => color.Id)
                    .Select(color => new BoardColorDto { Id = color.Id, Name = color.Name, Color = color.Color })
                    .ToList(),
            };
        }

        private static WorkItemDto ToCardDto(BoardCard card, Dictionary<int, List<WorkItemPlacementDto>> placements, Dictionary<int, CardDependencies> dependencies)
        {
            var item = card.WorkItem;
            dependencies.TryGetValue(item.Id, out var itemDependencies);

            return new WorkItemDto
            {
                PullRequests = card.PullRequests.Select(link => link.Url).Order().ToList(),
                Tags = card.Tags.OrderBy(link => link.BoardTag.Name).ThenBy(link => link.BoardTagId)
                    .Select(link => new BoardTagDto { Id = link.BoardTagId, Name = link.BoardTag.Name, Color = link.BoardTag.Color }).ToList(),
                Id = item.Id,
                Title = item.Title,
                Description = item.Description,
                Color = item.Color,
                ColumnId = card.ColumnId,
                Position = card.Position,
                Assignees = item.Assignees.OrderBy(assignee => assignee.User.FirstName)
                    .ThenBy(assignee => assignee.User.LastName).ThenBy(assignee => assignee.UserId)
                    .Select(assignee => new WorkItemAssigneeDto(assignee)).ToList(),
                OwnerPositionId = item.OwnerPositionId,
                OwnerPositionTitle = item.OwnerPosition?.Title,
                DueDate = item.DueDate,
                Priority = item.Priority,
                Links = item.Links
                    .Select(link => new WorkItemLinkDto { EntityType = link.EntityType, EntityId = link.EntityId })
                    .ToList(),
                Placements = placements.TryGetValue(item.Id, out var itemPlacements) ? itemPlacements : [],
                WaitingOn = itemDependencies?.WaitingOn ?? [],
                Blocking = itemDependencies?.Blocking ?? [],
            };
        }
    }
}
