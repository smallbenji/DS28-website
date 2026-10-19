using DS.Application.Services;
using DS.Domain;
using DS.Domain.Data;
using DS.Domain.DTOs;
using DS.Domain.Models;
using DS.Website.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace DS.Website.Controllers
{
    [Authorize]
    [Route("api/v1/org")]
    public class OrgApiController(
        DataDbContext dataDb,
        UserManager<User> userManager,
        EmailService emailService,
        IMemoryCache memoryCache) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> GetPositions()
        {
            if (!await HasViewAccessAsync()) return Forbid();

            var positions = await dataDb.OrgPositions
                .AsNoTracking()
                .Where(position => position.DeletedAt == null)
                .Include(position => position.Groups)
                .Include(position => position.Holders)
                    .ThenInclude(holder => holder.User)
                        .ThenInclude(user => user.ProfilePicture)
                .AsSplitQuery()
                .OrderBy(position => position.Id)
                .ToListAsync();

            return Ok(positions.Select(ToDto).ToList());
        }

        [HttpGet("assignable-groups")]
        public async Task<IActionResult> GetAssignableGroups()
        {
            if (!await HasViewAccessAsync()) return Forbid();

            var groups = IsSysAdmin()
                ? Enum.GetNames<AppGroups>().ToList()
                : await GetCurrentGroupsAsync();

            return Ok(groups);
        }

        [HttpPost]
        public async Task<IActionResult> CreatePosition([FromBody] OrgPositionCreateDto data)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Title)) return BadRequest("Titel er påkrævet.");

            if (!await CanManageParentAsync(data.ParentPositionId)) return Forbid();

            if (data.ParentPositionId != null && await dataDb.OrgPositions
                .AnyAsync(parent => parent.Id == data.ParentPositionId && parent.PaForPositionId != null))
            {
                return BadRequest("Der kan ikke oprettes understillinger under en personlig assistent.");
            }

            var groups = NormalizeGroups(data.Groups);
            if (groups.Contains(nameof(AppGroups.SysAdmin))) return BadRequest("SysAdmin kan ikke tildeles en almindelig stilling.");
            if (!await CanAssignGroupsAsync(groups)) return Forbid();

            if (!TryNormalizeColor(data.Color, out var color)) return BadRequest("Ugyldig farve.");

            var position = new OrgPosition
            {
                Title = data.Title.Trim(),
                ParentPositionId = data.ParentPositionId,
                Color = color,
                Groups = groups.Select(group => new OrgPositionGroup { GroupName = group }).ToList(),
            };

            dataDb.OrgPositions.Add(position);
            await dataDb.SaveChangesAsync();

            return Ok(position.Id);
        }

        [HttpPost("{id:int}/pa")]
        public async Task<IActionResult> CreatePa(int id)
        {
            var position = await dataDb.OrgPositions
                .FirstOrDefaultAsync(position => position.Id == id && position.DeletedAt == null);
            if (position == null) return NotFound();

            if (position.PaForPositionId != null) return BadRequest("Den personlige assistent kan ikke have sin egen assistent.");
            if (!await CanManagePositionAsync(position)) return Forbid();

            var hasPa = await dataDb.OrgPositions
                .AnyAsync(pa => pa.PaForPositionId == id && pa.DeletedAt == null);
            if (hasPa) return BadRequest("Stillingen har allerede en personlig assistent.");

            var pa = new OrgPosition
            {
                Title = "Personlig assistent",
                PaForPositionId = id,
                Groups = [new OrgPositionGroup { GroupName = nameof(AppGroups.PersonalAssistant) }],
            };

            dataDb.OrgPositions.Add(pa);
            await dataDb.SaveChangesAsync();

            return Ok(pa.Id);
        }

        [HttpPut("{id:int}")]
        public async Task<IActionResult> UpdatePosition([FromBody] OrgPositionUpdateDto data, int id)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Title)) return BadRequest("Titel er påkrævet.");

            var position = await dataDb.OrgPositions
                .FirstOrDefaultAsync(position => position.Id == id && position.DeletedAt == null);
            if (position == null) return NotFound();

            if (position.IsSystem) return BadRequest("Systemstillingen kan ikke redigeres.");
            if (position.PaForPositionId != null) return BadRequest("Den personlige assistent kan ikke redigeres.");
            if (!await CanManagePositionAsync(position)) return Forbid();

            var groups = NormalizeGroups(data.Groups);
            if (groups.Contains(nameof(AppGroups.SysAdmin))) return BadRequest("SysAdmin kan ikke tildeles en almindelig stilling.");
            if (!await CanAssignGroupsAsync(groups)) return Forbid();

            if (!TryNormalizeColor(data.Color, out var color)) return BadRequest("Ugyldig farve.");

            position.Title = data.Title.Trim();
            position.Color = color;
            position.UpdatedAt = DateTime.UtcNow;

            await dataDb.OrgPositionGroups
                .Where(group => group.PositionId == id)
                .ExecuteDeleteAsync();

            foreach (var group in groups)
            {
                dataDb.OrgPositionGroups.Add(new OrgPositionGroup { PositionId = id, GroupName = group });
            }

            await dataDb.SaveChangesAsync();

            await EvictHoldersAsync(id);

            return Ok();
        }

        [HttpPut("{id:int}/parent")]
        public async Task<IActionResult> MovePosition([FromBody] OrgPositionMoveDto data, int id)
        {
            var position = await dataDb.OrgPositions
                .FirstOrDefaultAsync(position => position.Id == id && position.DeletedAt == null);
            if (position == null) return NotFound();

            if (position.IsSystem && !IsSysAdmin()) return Forbid();
            if (position.PaForPositionId != null) return BadRequest("Den personlige assistent er knyttet til en stilling og kan ikke flyttes.");
            if (!await CanManagePositionAsync(position)) return Forbid();

            var parentPositionId = data?.ParentPositionId;
            if (!await CanManageParentAsync(parentPositionId)) return Forbid();

            if (parentPositionId == id) return BadRequest("En stilling kan ikke være sin egen overstilling.");
            if (parentPositionId != null && await IsDescendantAsync(id, parentPositionId.Value))
            {
                return BadRequest("En stilling kan ikke flyttes ind under en af sine egne understillinger.");
            }

            position.ParentPositionId = parentPositionId;
            position.UpdatedAt = DateTime.UtcNow;
            await dataDb.SaveChangesAsync();

            return Ok();
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeletePosition(int id)
        {
            var position = await dataDb.OrgPositions
                .Include(position => position.Holders)
                .FirstOrDefaultAsync(position => position.Id == id && position.DeletedAt == null);
            if (position == null) return NotFound();

            if (position.IsSystem) return BadRequest("Systemstillingen kan ikke slettes.");
            if (!await CanManagePositionAsync(position)) return Forbid();

            var hasChildren = await dataDb.OrgPositions.AnyAsync(p => p.ParentPositionId == id && p.DeletedAt == null);
            if (hasChildren) return BadRequest("Stillingen har understillinger og kan ikke slettes.");

            if (position.Holders.Count > 0) return BadRequest("Stillingen har personer tilknyttet og kan ikke slettes.");

            var paHolderIds = await dataDb.OrgPositionHolders
                .Where(holder => holder.Position.PaForPositionId == id)
                .Select(holder => holder.UserId)
                .ToListAsync();

            dataDb.OrgPositions.Remove(position);
            await dataDb.SaveChangesAsync();

            foreach (var userId in paHolderIds)
            {
                ClaimsTransformer.Evict(memoryCache, userId);
            }

            return Ok();
        }

        [HttpPost("{id:int}/holders")]
        public async Task<IActionResult> AddHolder([FromBody] OrgPositionHolderAddDto data, int id)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.UserId)) return BadRequest("Bruger-id er påkrævet.");

            var position = await dataDb.OrgPositions
                .FirstOrDefaultAsync(position => position.Id == id && position.DeletedAt == null);
            if (position == null) return NotFound();

            if (position.IsSystem && !IsSysAdmin()) return Forbid();
            if (!await CanManagePositionAsync(position)) return Forbid();

            var user = await userManager.FindByIdAsync(data.UserId);
            if (user == null) return NotFound("Brugeren findes ikke.");

            var exists = await dataDb.OrgPositionHolders.AnyAsync(holder => holder.PositionId == id && holder.UserId == data.UserId);
            if (exists) return BadRequest("Brugeren er allerede tilknyttet stillingen.");

            dataDb.OrgPositionHolders.Add(new OrgPositionHolder { PositionId = id, UserId = data.UserId });
            await dataDb.SaveChangesAsync();

            ClaimsTransformer.Evict(memoryCache, data.UserId);

            return Ok();
        }

        [HttpDelete("{id:int}/holders/{userId}")]
        public async Task<IActionResult> RemoveHolder(int id, string userId)
        {
            var position = await dataDb.OrgPositions
                .Include(position => position.Holders)
                .FirstOrDefaultAsync(position => position.Id == id && position.DeletedAt == null);
            if (position == null) return NotFound();

            if (position.IsSystem && !IsSysAdmin()) return Forbid();
            if (!await CanManagePositionAsync(position)) return Forbid();
            if (position.IsSystem && position.Holders.Count <= 1) return BadRequest("Systemstillingen skal have mindst én indehaver.");

            var holder = position.Holders.FirstOrDefault(holder => holder.UserId == userId);
            if (holder == null) return NotFound();

            dataDb.OrgPositionHolders.Remove(holder);
            await dataDb.SaveChangesAsync();

            ClaimsTransformer.Evict(memoryCache, userId);

            return Ok();
        }

        [HttpPost("{id:int}/invite")]
        public async Task<IActionResult> Invite([FromBody] OrgInviteDto data, int id)
        {
            if (data == null || string.IsNullOrWhiteSpace(data.Email)) return BadRequest("Email er påkrævet.");

            var position = await dataDb.OrgPositions
                .Include(position => position.Groups)
                .FirstOrDefaultAsync(position => position.Id == id && position.DeletedAt == null);
            if (position == null) return NotFound();

            if (!await CanManagePositionAsync(position)) return Forbid();

            var email = data.Email.Trim();

            var existing = await userManager.FindByEmailAsync(email);
            if (existing != null) return BadRequest("Bruger findes allerede. Tilknyt i stedet brugeren direkte.");

            var invitation = new UserInvitation
            {
                InvitationId = Guid.NewGuid(),
                Email = email,
                Roles = position.Groups.Select(group => group.GroupName).ToList(),
                OrgPositionId = position.Id,
            };

            dataDb.Invitations.Add(invitation);
            await dataDb.SaveChangesAsync();

            emailService.QueueInvitationMail(invitation);

            return Ok();
        }

        private bool IsSysAdmin()
        {
            return User.IsInRole(nameof(AppGroups.SysAdmin));
        }

        private async Task<List<string>> GetCurrentGroupsAsync()
        {
            var user = await userManager.GetUserAsync(User);
            if (user == null) return [];

            var direct = (await userManager.GetRolesAsync(user)).ToList();

            var fromPositions = await OrgPositionAccess.GetEffectiveGroups(dataDb, user.Id).ToListAsync();

            return direct.Concat(fromPositions).Distinct().ToList();
        }

        private async Task<bool> HasViewAccessAsync()
        {
            return IsSysAdmin() || User.IsInRole(nameof(AppRoles.OrgView));
        }

        private async Task<bool> CanManageParentAsync(int? parentPositionId)
        {
            if (IsSysAdmin()) return true;
            if (parentPositionId == null) return false;

            var userId = userManager.GetUserId(User);
            var positionIds = await GetAncestorPositionIdsAsync(parentPositionId.Value);

            return await dataDb.OrgPositionHolders
                .AnyAsync(holder => holder.UserId == userId && positionIds.Contains(holder.PositionId));
        }

        private async Task<List<int>> GetAncestorPositionIdsAsync(int positionId)
        {
            var result = new List<int> { positionId };

            var parents = await dataDb.OrgPositions
                .Where(position => position.DeletedAt == null && position.ParentPositionId != null)
                .ToDictionaryAsync(
                    position => position.Id,
                    position => position.ParentPositionId.Value);

            var current = positionId;
            var visited = new HashSet<int>();
            while (parents.TryGetValue(current, out var parentId))
            {
                if (!visited.Add(parentId)) break;

                result.Add(parentId);
                current = parentId;
            }

            return result;
        }

        private async Task<bool> CanManagePositionAsync(OrgPosition position)
        {
            if (IsSysAdmin()) return true;

            var managedPositionId = position.PaForPositionId ?? position.ParentPositionId;
            return await CanManageParentAsync(managedPositionId);
        }

        private async Task<bool> IsDescendantAsync(int ancestorId, int candidateId)
        {
            var parents = await dataDb.OrgPositions
                .Where(position => position.DeletedAt == null && position.ParentPositionId != null)
                .ToDictionaryAsync(
                    position => position.Id,
                    position => position.ParentPositionId.Value);

            var current = candidateId;
            var visited = new HashSet<int>();

            while (parents.TryGetValue(current, out var parentId))
            {
                if (parentId == ancestorId) return true;
                if (!visited.Add(parentId)) return false;

                current = parentId;
            }

            return false;
        }

        private async Task<bool> CanAssignGroupsAsync(List<string> groups)
        {
            if (groups.Count == 0) return true;
            if (IsSysAdmin()) return true;

            var current = await GetCurrentGroupsAsync();

            return groups.All(current.Contains);
        }

        private async Task EvictHoldersAsync(int positionId)
        {
            var userIds = await dataDb.OrgPositionHolders
                .Where(holder => holder.PositionId == positionId
                    || holder.Position.PaForPositionId == positionId)
                .Select(holder => holder.UserId)
                .Distinct()
                .ToListAsync();

            foreach (var userId in userIds)
            {
                ClaimsTransformer.Evict(memoryCache, userId);
            }
        }

        private static readonly HashSet<string> AllowedColors = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "#1f6fb2",
            "#2f9e44",
            "#e0a51c",
            "#d95740",
            "#8e5bd1",
            "#0f9d9d",
        };

        private static bool TryNormalizeColor(string color, out string result)
        {
            if (string.IsNullOrWhiteSpace(color))
            {
                result = null;
                return true;
            }

            var normalized = color.Trim().ToLowerInvariant();
            if (!AllowedColors.Contains(normalized))
            {
                result = null;
                return false;
            }

            result = normalized;
            return true;
        }

        private static List<string> NormalizeGroups(IEnumerable<string> groups)
        {
            if (groups == null) return [];

            return groups
                .Where(group => !string.IsNullOrWhiteSpace(group))
                .Select(group => group.Trim())
                .Distinct()
                .ToList();
        }

        private static OrgPositionDto ToDto(OrgPosition position)
        {
            return new OrgPositionDto
            {
                Id = position.Id,
                Title = position.Title,
                ParentPositionId = position.ParentPositionId,
                PaForPositionId = position.PaForPositionId,
                IsSystem = position.IsSystem,
                Color = position.Color,
                Groups = position.Groups
                    .Select(group => group.GroupName)
                    .OrderBy(group => group)
                    .ToList(),
                Holders = position.Holders
                    .Where(holder => holder.User != null)
                    .Select(holder => new OrgPositionHolderDto
                    {
                        UserId = holder.UserId,
                        Name = holder.User.GetFullName(),
                        ProfilePicture = holder.User.ProfilePicture != null
                            && holder.User.ProfilePicture.Status == StoredFileStatus.Ready
                            && holder.User.ProfilePicture.DeletedAt == null
                            ? new ImageReferenceDto(holder.User.ProfilePicture)
                            : null,
                    })
                    .ToList(),
            };
        }
    }
}
