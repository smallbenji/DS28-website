using DS.Data;
using DS.DTOs;
using DS.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;

namespace DS.Website.Controllers
{
    [Authorize]
    [Route("api/v1/boards")]
    public class BoardApiController(DataDbContext dataDb, UserManager<User> userManager) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> GetBoards()
        {
            if (!CanView()) return Forbid();

            var boards = await dataDb.Boards
                .AsNoTracking()
                .Where(board => board.DeletedAt == null)
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
                    RequiredRole = board.RequiredRole,
                    CardCount = dataDb.BoardCards.Count(card => card.BoardId == board.Id),
                })
                .ToListAsync();

            return Ok(boards.Where(CanSeeBoard).ToList());
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetBoard(int id)
        {
            if (!CanView()) return Forbid();

            var board = await dataDb.Boards
                .AsNoTracking()
                .Include(board => board.Columns)
                .FirstOrDefaultAsync(board => board.Id == id && board.DeletedAt == null);
            if (board == null) return NotFound();
            if (!CanSeeBoard(board)) return Forbid();

            var cards = await LoadCardsAsync(id);
            var placements = await LoadPlacementsAsync(cards.Select(card => card.WorkItemId).Distinct().ToList());

            return Ok(ToDetailDto(board, cards, placements));
        }

        [HttpGet("{id:int}/columns")]
        public async Task<IActionResult> GetColumns(int id)
        {
            if (!CanView()) return Forbid();

            var board = await GetBoardAsync(id);
            if (board == null) return NotFound();
            if (!CanSeeBoard(board)) return Forbid();

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

            return Ok(columns);
        }

        [HttpGet("assignees")]
        public async Task<IActionResult> SearchAssignees([FromQuery] string search)
        {
            if (!CanManage()) return Forbid();
            if (string.IsNullOrWhiteSpace(search)) return Ok(new List<UserDto>());

            var searchTerm = search.Trim();
            var boardGroups = AppAccess.Matrix
                .Where(entry => entry.Value.Contains(nameof(AppRoles.BoardsView)))
                .Select(entry => entry.Key.ToString())
                .ToList();

            var roleIds = dataDb.Roles
                .Where(role => boardGroups.Contains(role.Name))
                .Select(role => role.Id)
                .ToList();

            var userIds = dataDb.UserRoles
                .Where(userRole => roleIds.Contains(userRole.RoleId))
                .Select(userRole => userRole.UserId)
                .ToList();

            var users = await dataDb.Users
                .AsNoTracking()
                .Where(user => userIds.Contains(user.Id))
                .Where(user => EF.Functions.ILike(user.FirstName + " " + user.LastName, $"%{searchTerm}%")
                    || EF.Functions.ILike(user.Email, $"%{searchTerm}%"))
                .OrderBy(user => user.FirstName)
                .ThenBy(user => user.LastName)
                .Take(10)
                .ToListAsync();

            return Ok(users.Select(user => new UserDto(user)).ToList());
        }

        [HttpPost]
        public async Task<IActionResult> CreateBoard([FromBody] BoardCreateDto data)
        {
            if (!CanManage()) return Forbid();
            if (data == null || string.IsNullOrWhiteSpace(data.Title)) return BadRequest("Titel er påkrævet.");

            if (!TryNormalizeRequiredRole(data.RequiredRole, out var requiredRole)) return BadRequest("Ukendt rolle.");
            if (!TryNormalizeShortText(data.Icon, 64, out var icon)) return BadRequest("Ikonet er for langt.");
            if (!TryNormalizeShortText(data.Color, 16, out var color)) return BadRequest("Farven er for lang.");

            var key = await GenerateKeyAsync(data.Title);

            var board = new Board
            {
                Key = key,
                Title = data.Title.Trim(),
                Description = data.Description,
                Icon = icon,
                Color = color,
                RequiredRole = requiredRole,
                CreatedByUserId = userManager.GetUserId(User),
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

            return Ok(board.Id);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdateBoard([FromBody] BoardUpdateDto data, int id)
        {
            if (!CanManage()) return Forbid();
            if (data == null || string.IsNullOrWhiteSpace(data.Title)) return BadRequest("Titel er påkrævet.");

            var board = await dataDb.Boards.FirstOrDefaultAsync(board => board.Id == id && board.DeletedAt == null);
            if (board == null) return NotFound();
            if (!CanSeeBoard(board)) return Forbid();

            if (!TryNormalizeRequiredRole(data.RequiredRole, out var requiredRole)) return BadRequest("Ukendt rolle.");
            if (!TryNormalizeShortText(data.Icon, 64, out var icon)) return BadRequest("Ikonet er for langt.");
            if (!TryNormalizeShortText(data.Color, 16, out var color)) return BadRequest("Farven er for lang.");

            board.Title = data.Title.Trim();
            board.Description = data.Description;
            board.Icon = icon;
            board.Color = color;
            board.RequiredRole = requiredRole;
            board.IsArchived = data.IsArchived;
            board.UpdatedAt = DateTime.UtcNow;

            await dataDb.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteBoard(int id)
        {
            if (!CanManage()) return Forbid();

            var board = await dataDb.Boards.FirstOrDefaultAsync(board => board.Id == id && board.DeletedAt == null);
            if (board == null) return NotFound();
            if (!CanSeeBoard(board)) return Forbid();

            board.DeletedAt = DateTime.UtcNow;
            board.UpdatedAt = DateTime.UtcNow;
            await dataDb.SaveChangesAsync();

            return Ok();
        }

        [HttpPost("{id:int}/columns")]
        public async Task<IActionResult> CreateColumn([FromBody] BoardColumnCreateDto data, int id)
        {
            if (!CanManage()) return Forbid();
            if (data == null || string.IsNullOrWhiteSpace(data.Title)) return BadRequest("Titel er påkrævet.");

            var board = await GetBoardAsync(id);
            if (board == null) return NotFound();
            if (!CanSeeBoard(board)) return Forbid();
            if (!IsValidWipLimit(data.WipLimit)) return BadRequest("WIP-grænsen skal være mindst 1.");

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

            return Ok(column.Id);
        }

        [HttpPut("{id:int}/columns/{columnId:int}")]
        public async Task<IActionResult> UpdateColumn([FromBody] BoardColumnUpdateDto data, int id, int columnId)
        {
            if (!CanManage()) return Forbid();
            if (data == null || string.IsNullOrWhiteSpace(data.Title)) return BadRequest("Titel er påkrævet.");

            var board = await GetBoardAsync(id);
            if (board == null) return NotFound();
            if (!CanSeeBoard(board)) return Forbid();
            if (!IsValidWipLimit(data.WipLimit)) return BadRequest("WIP-grænsen skal være mindst 1.");

            var column = await dataDb.BoardColumns.FirstOrDefaultAsync(column => column.Id == columnId && column.BoardId == id);
            if (column == null) return NotFound();

            column.Title = data.Title.Trim();
            column.WipLimit = data.WipLimit;
            column.IsDoneColumn = data.IsDoneColumn;
            column.UpdatedAt = DateTime.UtcNow;

            await dataDb.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("{id:int}/columns/{columnId:int}")]
        public async Task<IActionResult> DeleteColumn(int id, int columnId)
        {
            if (!CanManage()) return Forbid();

            var board = await GetBoardAsync(id);
            if (board == null) return NotFound();
            if (!CanSeeBoard(board)) return Forbid();

            var column = await dataDb.BoardColumns.FirstOrDefaultAsync(column => column.Id == columnId && column.BoardId == id);
            if (column == null) return NotFound();

            var columnCount = await dataDb.BoardColumns.CountAsync(column => column.BoardId == id);
            if (columnCount <= 1) return BadRequest("Boardet skal have mindst én kolonne.");

            var hasCards = await dataDb.BoardCards.AnyAsync(card => card.ColumnId == columnId);
            if (hasCards) return BadRequest("Kolonnen indeholder kort og kan ikke slettes.");

            dataDb.BoardColumns.Remove(column);
            await dataDb.SaveChangesAsync();

            return Ok();
        }

        [HttpPut("{id:int}/columns/reorder")]
        public async Task<IActionResult> ReorderColumns([FromBody] List<int> columnOrder, int id)
        {
            if (!CanManage()) return Forbid();

            var board = await GetBoardAsync(id);
            if (board == null) return NotFound();
            if (!CanSeeBoard(board)) return Forbid();

            if (columnOrder == null || columnOrder.Count == 0) return BadRequest("Rækkefølgen må ikke være tom.");

            var columns = await dataDb.BoardColumns
                .Where(column => column.BoardId == id)
                .ToListAsync();

            if (columnOrder.Count != columns.Count || columnOrder.Distinct().Count() != columnOrder.Count)
            {
                return BadRequest("Rækkefølgen skal indeholde hver kolonne én gang.");
            }

            if (columnOrder.Any(columnId => columns.All(column => column.Id != columnId)))
            {
                return BadRequest("Rækkefølgen indeholder ukendte kolonner.");
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

            return Ok();
        }

        [HttpPost("{id:int}/cards")]
        public async Task<IActionResult> CreateCard([FromBody] WorkItemCreateDto data, int id)
        {
            if (!CanManage()) return Forbid();
            if (data == null || string.IsNullOrWhiteSpace(data.Title)) return BadRequest("Titel er påkrævet.");

            var board = await GetBoardAsync(id);
            if (board == null) return NotFound();
            if (!CanSeeBoard(board)) return Forbid();

            var columnExists = await dataDb.BoardColumns.AnyAsync(column => column.Id == data.ColumnId && column.BoardId == id);
            if (!columnExists) return BadRequest("Kolonnen findes ikke på boardet.");

            if (!await IsValidAssigneeAsync(data.AssigneeUserId)) return BadRequest("Den ansvarlige bruger findes ikke.");
            if (!await IsValidOwnerPositionAsync(data.OwnerPositionId)) return BadRequest("Stillingen findes ikke.");

            var item = new WorkItem
            {
                Title = data.Title.Trim(),
                Description = data.Description,
                AssigneeUserId = NormalizeUserId(data.AssigneeUserId),
                OwnerPositionId = data.OwnerPositionId,
                DueDate = data.DueDate,
                Priority = data.Priority,
                CreatedByUserId = userManager.GetUserId(User),
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
                Position = lastPosition + 1,
            });

            await dataDb.SaveChangesAsync();

            return Ok(item.Id);
        }

        [HttpPut("{id:int}/cards/{workItemId:int}")]
        public async Task<IActionResult> UpdateCard([FromBody] WorkItemUpdateDto data, int id, int workItemId)
        {
            if (!CanManage()) return Forbid();
            if (data == null || string.IsNullOrWhiteSpace(data.Title)) return BadRequest("Titel er påkrævet.");

            var board = await GetBoardAsync(id);
            if (board == null) return NotFound();
            if (!CanSeeBoard(board)) return Forbid();

            var card = await dataDb.BoardCards
                .Include(card => card.WorkItem)
                .FirstOrDefaultAsync(card => card.BoardId == id && card.WorkItemId == workItemId && card.WorkItem.DeletedAt == null);
            if (card == null) return NotFound();

            if (!await IsValidAssigneeAsync(data.AssigneeUserId)) return BadRequest("Den ansvarlige bruger findes ikke.");
            if (!await IsValidOwnerPositionAsync(data.OwnerPositionId)) return BadRequest("Stillingen findes ikke.");

            var item = card.WorkItem;
            item.Title = data.Title.Trim();
            item.Description = data.Description;
            item.AssigneeUserId = NormalizeUserId(data.AssigneeUserId);
            item.OwnerPositionId = data.OwnerPositionId;
            item.DueDate = data.DueDate;
            item.Priority = data.Priority;
            item.UpdatedAt = DateTime.UtcNow;

            await dataDb.SaveChangesAsync();

            return Ok();
        }

        [HttpPut("{id:int}/cards/{workItemId:int}/move")]
        public async Task<IActionResult> MoveCard([FromBody] WorkItemMoveDto data, int id, int workItemId)
        {
            if (!CanManage()) return Forbid();
            if (data == null) return BadRequest();

            var board = await GetBoardAsync(id);
            if (board == null) return NotFound();
            if (!CanSeeBoard(board)) return Forbid();

            var card = await dataDb.BoardCards
                .FirstOrDefaultAsync(card => card.BoardId == id && card.WorkItemId == workItemId);
            if (card == null) return NotFound();

            var targetColumn = await dataDb.BoardColumns
                .FirstOrDefaultAsync(column => column.Id == data.ColumnId && column.BoardId == id);
            if (targetColumn == null) return BadRequest("Kolonnen findes ikke på boardet.");

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

            return Ok();
        }

        [HttpDelete("{id:int}/cards/{workItemId:int}")]
        public async Task<IActionResult> RemoveCard(int id, int workItemId)
        {
            if (!CanManage()) return Forbid();

            var board = await GetBoardAsync(id);
            if (board == null) return NotFound();
            if (!CanSeeBoard(board)) return Forbid();

            var card = await dataDb.BoardCards
                .FirstOrDefaultAsync(card => card.BoardId == id && card.WorkItemId == workItemId);
            if (card == null) return NotFound();

            dataDb.BoardCards.Remove(card);
            await dataDb.SaveChangesAsync();

            var stillPlaced = await dataDb.BoardCards.AnyAsync(candidate => candidate.WorkItemId == workItemId);
            if (!stillPlaced)
            {
                var item = await dataDb.WorkItems.FirstOrDefaultAsync(candidate => candidate.Id == workItemId && candidate.DeletedAt == null);
                if (item != null)
                {
                    item.DeletedAt = DateTime.UtcNow;
                    item.UpdatedAt = DateTime.UtcNow;
                    await dataDb.SaveChangesAsync();
                }
            }

            return Ok();
        }

        [HttpPost("{id:int}/cards/{workItemId:int}/placements")]
        public async Task<IActionResult> AddCardPlacement([FromBody] WorkItemPlacementCreateDto data, int id, int workItemId)
        {
            if (!CanManage()) return Forbid();
            if (data == null) return BadRequest();

            var board = await GetBoardAsync(id);
            if (board == null) return NotFound();
            if (!CanSeeBoard(board)) return Forbid();

            var item = await dataDb.WorkItems.FirstOrDefaultAsync(candidate => candidate.Id == workItemId && candidate.DeletedAt == null);
            if (item == null) return NotFound();

            var sourceBoards = await dataDb.BoardCards
                .Where(card => card.WorkItemId == workItemId)
                .Select(card => card.Board)
                .ToListAsync();
            if (!sourceBoards.Any(CanSeeBoard)) return Forbid();

            var columnExists = await dataDb.BoardColumns.AnyAsync(column => column.Id == data.ColumnId && column.BoardId == id);
            if (!columnExists) return BadRequest("Kolonnen findes ikke på boardet.");

            var alreadyPlaced = await dataDb.BoardCards.AnyAsync(card => card.BoardId == id && card.WorkItemId == workItemId);
            if (alreadyPlaced) return BadRequest("Kortet ligger allerede på boardet.");

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

            return Ok();
        }

        private async Task<List<BoardCard>> LoadCardsAsync(int boardId)
        {
            return await dataDb.BoardCards
                .AsNoTracking()
                .Where(card => card.BoardId == boardId && card.WorkItem.DeletedAt == null)
                .Include(card => card.WorkItem).ThenInclude(item => item.Assignee).ThenInclude(user => user.ProfilePicture)
                .Include(card => card.WorkItem).ThenInclude(item => item.OwnerPosition)
                .Include(card => card.WorkItem).ThenInclude(item => item.Links)
                .AsSplitQuery()
                .OrderBy(card => card.ColumnId).ThenBy(card => card.Position).ThenBy(card => card.Id)
                .ToListAsync();
        }

        private async Task<Dictionary<int, List<WorkItemPlacementDto>>> LoadPlacementsAsync(List<int> workItemIds)
        {
            if (workItemIds.Count == 0) return [];

            var placements = await dataDb.BoardCards
                .AsNoTracking()
                .Where(card => workItemIds.Contains(card.WorkItemId) && card.Board.DeletedAt == null)
                .OrderBy(card => card.Board.Title).ThenBy(card => card.Column.Position).ThenBy(card => card.Id)
                .Select(card => new
                {
                    card.WorkItemId,
                    card.BoardId,
                    BoardTitle = card.Board.Title,
                    BoardIcon = card.Board.Icon,
                    card.Board.RequiredRole,
                    card.ColumnId,
                    ColumnTitle = card.Column.Title,
                    card.Column.IsDoneColumn,
                })
                .ToListAsync();

            return placements
                .Where(placement => CanSeeRole(placement.RequiredRole))
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

            while (await dataDb.Boards.AnyAsync(board => board.Key == candidate))
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

        private async Task<bool> IsValidAssigneeAsync(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId)) return true;

            return await userManager.FindByIdAsync(userId.Trim()) != null;
        }

        private async Task<bool> IsValidOwnerPositionAsync(int? positionId)
        {
            if (positionId == null) return true;

            return await dataDb.OrgPositions.AnyAsync(position => position.Id == positionId && position.DeletedAt == null);
        }

        private bool CanView()
        {
            return User.IsInRole(nameof(AppRoles.BoardsView));
        }

        private bool CanManage()
        {
            return User.IsInRole(nameof(AppRoles.BoardsManage));
        }

        private bool CanSeeBoard(Board board)
        {
            return CanSeeRole(board.RequiredRole);
        }

        private bool CanSeeBoard(BoardSummaryDto board)
        {
            return CanSeeRole(board.RequiredRole);
        }

        private bool CanSeeRole(string requiredRole)
        {
            return CanView() && (requiredRole == null || User.IsInRole(requiredRole));
        }

        private static bool TryNormalizeRequiredRole(string role, out string result)
        {
            if (string.IsNullOrWhiteSpace(role))
            {
                result = null;
                return true;
            }

            var match = Enum.GetNames<AppRoles>()
                .FirstOrDefault(name => string.Equals(name, role.Trim(), StringComparison.OrdinalIgnoreCase));
            if (match == null)
            {
                result = null;
                return false;
            }

            result = match;
            return true;
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

        private static bool IsValidWipLimit(int? wipLimit)
        {
            return wipLimit == null || wipLimit >= 1;
        }

        private static string NormalizeUserId(string userId)
        {
            return string.IsNullOrWhiteSpace(userId) ? null : userId.Trim();
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

        private static BoardDetailDto ToDetailDto(Board board, List<BoardCard> cards, Dictionary<int, List<WorkItemPlacementDto>> placements)
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
                RequiredRole = board.RequiredRole,
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
                Cards = cards.Select(card => ToCardDto(card, placements)).ToList(),
            };
        }

        private static WorkItemDto ToCardDto(BoardCard card, Dictionary<int, List<WorkItemPlacementDto>> placements)
        {
            var item = card.WorkItem;

            return new WorkItemDto
            {
                Id = item.Id,
                Title = item.Title,
                Description = item.Description,
                ColumnId = card.ColumnId,
                Position = card.Position,
                AssigneeUserId = item.AssigneeUserId,
                AssigneeName = item.Assignee?.GetFullName(),
                AssigneeProfilePicture = item.Assignee?.ProfilePicture is { Status: StoredFileStatus.Ready, DeletedAt: null } picture
                    ? new ImageReferenceDto(picture)
                    : null,
                OwnerPositionId = item.OwnerPositionId,
                OwnerPositionTitle = item.OwnerPosition?.Title,
                DueDate = item.DueDate,
                Priority = item.Priority,
                Links = item.Links
                    .Select(link => new WorkItemLinkDto { EntityType = link.EntityType, EntityId = link.EntityId })
                    .ToList(),
                Placements = placements.TryGetValue(item.Id, out var itemPlacements) ? itemPlacements : [],
            };
        }
    }
}
