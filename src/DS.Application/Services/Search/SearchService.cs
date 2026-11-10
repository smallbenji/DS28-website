using DS.Application.Data;
using DS.Application.Services.Boards;
using DS.Domain.DTOs;
using Microsoft.EntityFrameworkCore;

namespace DS.Application.Services.Search
{
    public class SearchService(IDataDbContext dataDb, BoardService boardService)
    {
        public const int MinimumTermLength = 2;
        private const int ResultsPerType = 5;

        public async Task<ServiceResult<List<SearchResultDto>>> Search(SearchActor actor, string term)
        {
            var searchTerm = term?.Trim() ?? "";
            if (searchTerm.Length < MinimumTermLength) return new ServiceResult<List<SearchResultDto>>(ServiceOutcome.Success, []);

            var pattern = $"%{EscapeLikePattern(searchTerm)}%";
            var results = new List<SearchResultDto>();

            if (actor.CanViewBoards) results.AddRange(await SearchBoards(actor, pattern));

            results.AddRange(await SearchGroups(actor, pattern));

            if (actor.CanViewActivities) results.AddRange(await SearchActivities(actor, pattern));

            return new ServiceResult<List<SearchResultDto>>(ServiceOutcome.Success, results);
        }

        private async Task<List<SearchResultDto>> SearchBoards(SearchActor actor, string pattern)
        {
            var boards = await boardService.VisibleBoards(new BoardActor(actor.UserId, actor.CanSeeAllBoards))
                .AsNoTracking()
                .Where(board => EF.Functions.ILike(board.Title, pattern))
                .OrderBy(board => board.IsArchived)
                .ThenBy(board => board.Title)
                .Take(ResultsPerType)
                .Select(board => new { board.Id, board.Title, board.Description })
                .ToListAsync();

            return boards
                .Select(board => new SearchResultDto
                {
                    Type = SearchResultTypes.Board,
                    Id = board.Id,
                    Title = board.Title,
                    Subtitle = board.Description,
                    Url = $"/board/{board.Id}"
                })
                .ToList();
        }

        private async Task<List<SearchResultDto>> SearchGroups(SearchActor actor, string pattern)
        {
            if (actor.CanSeeAllGroups)
            {
                var groups = await dataDb.Groups
                    .AsNoTracking()
                    .Where(group => EF.Functions.ILike(group.Name, pattern))
                    .OrderBy(group => group.Name)
                    .Take(ResultsPerType)
                    .Select(group => new { group.Id, group.Name })
                    .ToListAsync();

                return groups
                    .Select(group => new SearchResultDto
                    {
                        Type = SearchResultTypes.Group,
                        Id = group.Id,
                        Title = group.Name,
                        Url = $"/admin/groups/{group.Id}"
                    })
                    .ToList();
            }

            var ownGroup = await dataDb.Users
                .AsNoTracking()
                .Where(user => user.Id == actor.UserId && user.Group != null && EF.Functions.ILike(user.Group.Name, pattern))
                .Select(user => new { user.Group.Id, user.Group.Name })
                .FirstOrDefaultAsync();
            if (ownGroup == null) return [];

            return
            [
                new SearchResultDto
                {
                    Type = SearchResultTypes.Group,
                    Id = ownGroup.Id,
                    Title = ownGroup.Name,
                    Url = "/group"
                }
            ];
        }

        private async Task<List<SearchResultDto>> SearchActivities(SearchActor actor, string pattern)
        {
            var activities = dataDb.Activities
                .AsNoTracking()
                .Where(activity => EF.Functions.ILike(activity.Name, pattern));

            if (!actor.CanSeeAllActivities)
            {
                activities = activities.Where(activity => activity.ActivityTeam.Memberships.Any(membership => membership.UserId == actor.UserId));
            }

            var matches = await activities
                .OrderBy(activity => activity.Name)
                .Take(ResultsPerType)
                .Select(activity => new { activity.Id, activity.Name, TeamName = activity.ActivityTeam.Name })
                .ToListAsync();

            return matches
                .Select(activity => new SearchResultDto
                {
                    Type = SearchResultTypes.Activity,
                    Id = activity.Id,
                    Title = activity.Name,
                    Subtitle = activity.TeamName,
                    Url = $"/activity/{activity.Id}"
                })
                .ToList();
        }

        private static string EscapeLikePattern(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("%", "\\%")
                .Replace("_", "\\_");
        }
    }
}
