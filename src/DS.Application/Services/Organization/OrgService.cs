using DS.Application.Data;
using DS.Application.Services.Email;
using DS.Domain;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DS.Application.Services.Organization
{
    public class OrgService(IDataDbContext dataDb, UserManager<User> userManager, EmailService emailService, IUserAccessCache accessCache)
    {
        public async Task<ServiceResult<List<OrgPositionDto>>> GetPositions(OrgActor actor)
        {
            if (!(actor.IsSystemAdministrator || actor.CanView)) return new ServiceResult(ServiceOutcome.Forbidden);

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

            return new ServiceResult<List<OrgPositionDto>>(ServiceOutcome.Success, positions.Select(ToDto).ToList());
        }

        public async Task<ServiceResult<List<string>>> GetAssignableGroups(OrgActor actor)
        {
            if (!(actor.IsSystemAdministrator || actor.CanView)) return new ServiceResult(ServiceOutcome.Forbidden);

            var groups = actor.IsSystemAdministrator
                ? Enum.GetNames<AppGroups>().ToList()
                : await GetCurrentGroupsAsync(actor);

            return new ServiceResult<List<string>>(ServiceOutcome.Success, groups);
        }

        public async Task<ServiceResult<int>> CreatePosition(OrgActor actor, OrgPositionCreateDto data)
        {
            if (!await CanManageParentAsync(actor, data.ParentPositionId)) return new ServiceResult(ServiceOutcome.Forbidden);

            if (data.ParentPositionId != null && await dataDb.OrgPositions
                .AnyAsync(parent => parent.Id == data.ParentPositionId && parent.PaForPositionId != null))
            {
                return new ServiceResult(ServiceOutcome.Invalid, "Der kan ikke oprettes understillinger under en personlig assistent.");
            }

            var groups = NormalizeGroups(data.Groups);
            if (groups.Contains(nameof(AppGroups.SysAdmin))) return new ServiceResult(ServiceOutcome.Invalid, "SysAdmin kan ikke tildeles en almindelig stilling.");
            if (!await CanAssignGroupsAsync(actor, groups)) return new ServiceResult(ServiceOutcome.Forbidden);

            if (!TryNormalizeColor(data.Color, out var color)) return new ServiceResult(ServiceOutcome.Invalid, "Ugyldig farve.");

            var position = new OrgPosition
            {
                Title = data.Title.Trim(),
                ParentPositionId = data.ParentPositionId,
                Color = color,
                Groups = groups.Select(group => new OrgPositionGroup { GroupName = group }).ToList(),
            };

            dataDb.OrgPositions.Add(position);
            await dataDb.SaveChangesAsync();

            return new ServiceResult<int>(ServiceOutcome.Success, position.Id);
        }

        public async Task<ServiceResult<int>> CreatePa(OrgActor actor, int id)
        {
            var position = await dataDb.OrgPositions
                .FirstOrDefaultAsync(position => position.Id == id && position.DeletedAt == null);
            if (position == null) return new ServiceResult(ServiceOutcome.NotFound);

            if (position.PaForPositionId != null) return new ServiceResult(ServiceOutcome.Invalid, "Den personlige assistent kan ikke have sin egen assistent.");
            if (!await CanManagePositionAsync(actor, position)) return new ServiceResult(ServiceOutcome.Forbidden);

            var hasPa = await dataDb.OrgPositions
                .AnyAsync(pa => pa.PaForPositionId == id && pa.DeletedAt == null);
            if (hasPa) return new ServiceResult(ServiceOutcome.Invalid, "Stillingen har allerede en personlig assistent.");

            var pa = new OrgPosition
            {
                Title = "Personlig assistent",
                PaForPositionId = id,
                Groups = [new OrgPositionGroup { GroupName = nameof(AppGroups.PersonalAssistant) }],
            };

            dataDb.OrgPositions.Add(pa);
            await dataDb.SaveChangesAsync();

            return new ServiceResult<int>(ServiceOutcome.Success, pa.Id);
        }

        public async Task<ServiceResult> UpdatePosition(OrgActor actor, OrgPositionUpdateDto data, int id)
        {
            var position = await dataDb.OrgPositions
                .FirstOrDefaultAsync(position => position.Id == id && position.DeletedAt == null);
            if (position == null) return new ServiceResult(ServiceOutcome.NotFound);

            if (position.IsSystem) return new ServiceResult(ServiceOutcome.Invalid, "Systemstillingen kan ikke redigeres.");
            if (position.PaForPositionId != null) return new ServiceResult(ServiceOutcome.Invalid, "Den personlige assistent kan ikke redigeres.");
            if (!await CanManagePositionAsync(actor, position)) return new ServiceResult(ServiceOutcome.Forbidden);

            var groups = NormalizeGroups(data.Groups);
            if (groups.Contains(nameof(AppGroups.SysAdmin))) return new ServiceResult(ServiceOutcome.Invalid, "SysAdmin kan ikke tildeles en almindelig stilling.");
            if (!await CanAssignGroupsAsync(actor, groups)) return new ServiceResult(ServiceOutcome.Forbidden);

            if (!TryNormalizeColor(data.Color, out var color)) return new ServiceResult(ServiceOutcome.Invalid, "Ugyldig farve.");

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

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> MovePosition(OrgActor actor, OrgPositionMoveDto data, int id)
        {
            var position = await dataDb.OrgPositions
                .FirstOrDefaultAsync(position => position.Id == id && position.DeletedAt == null);
            if (position == null) return new ServiceResult(ServiceOutcome.NotFound);

            if (position.IsSystem && !actor.IsSystemAdministrator) return new ServiceResult(ServiceOutcome.Forbidden);
            if (position.PaForPositionId != null) return new ServiceResult(ServiceOutcome.Invalid, "Den personlige assistent er knyttet til en stilling og kan ikke flyttes.");
            if (!await CanManagePositionAsync(actor, position)) return new ServiceResult(ServiceOutcome.Forbidden);

            var parentPositionId = data?.ParentPositionId;
            if (!await CanManageParentAsync(actor, parentPositionId)) return new ServiceResult(ServiceOutcome.Forbidden);

            if (parentPositionId == id) return new ServiceResult(ServiceOutcome.Invalid, "En stilling kan ikke være sin egen overstilling.");
            if (parentPositionId != null && await IsDescendantAsync(id, parentPositionId.Value))
            {
                return new ServiceResult(ServiceOutcome.Invalid, "En stilling kan ikke flyttes ind under en af sine egne understillinger.");
            }

            position.ParentPositionId = parentPositionId;
            position.UpdatedAt = DateTime.UtcNow;
            await dataDb.SaveChangesAsync();

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> DeletePosition(OrgActor actor, int id)
        {
            var position = await dataDb.OrgPositions
                .Include(position => position.Holders)
                .FirstOrDefaultAsync(position => position.Id == id && position.DeletedAt == null);
            if (position == null) return new ServiceResult(ServiceOutcome.NotFound);

            if (position.IsSystem) return new ServiceResult(ServiceOutcome.Invalid, "Systemstillingen kan ikke slettes.");
            if (!await CanManagePositionAsync(actor, position)) return new ServiceResult(ServiceOutcome.Forbidden);

            var hasChildren = await dataDb.OrgPositions.AnyAsync(p => p.ParentPositionId == id && p.DeletedAt == null);
            if (hasChildren) return new ServiceResult(ServiceOutcome.Invalid, "Stillingen har understillinger og kan ikke slettes.");

            if (position.Holders.Count > 0) return new ServiceResult(ServiceOutcome.Invalid, "Stillingen har personer tilknyttet og kan ikke slettes.");

            var paHolderIds = await dataDb.OrgPositionHolders
                .Where(holder => holder.Position.PaForPositionId == id)
                .Select(holder => holder.UserId)
                .ToListAsync();

            var personalAssistants = await dataDb.OrgPositions
                .Where(pa => pa.PaForPositionId == id)
                .ToListAsync();
            dataDb.OrgPositions.RemoveRange(personalAssistants);

            dataDb.OrgPositions.Remove(position);
            await dataDb.SaveChangesAsync();

            foreach (var userId in paHolderIds)
            {
                accessCache.Evict(userId);
            }

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> AddHolder(OrgActor actor, OrgPositionHolderAddDto data, int id)
        {
            var position = await dataDb.OrgPositions
                .FirstOrDefaultAsync(position => position.Id == id && position.DeletedAt == null);
            if (position == null) return new ServiceResult(ServiceOutcome.NotFound);

            if (position.IsSystem && !actor.IsSystemAdministrator) return new ServiceResult(ServiceOutcome.Forbidden);
            if (!await CanManagePositionAsync(actor, position)) return new ServiceResult(ServiceOutcome.Forbidden);

            var user = await userManager.FindByIdAsync(data.UserId);
            if (user == null) return new ServiceResult(ServiceOutcome.NotFound, "Brugeren findes ikke.");

            var exists = await dataDb.OrgPositionHolders.AnyAsync(holder => holder.PositionId == id && holder.UserId == data.UserId);
            if (exists) return new ServiceResult(ServiceOutcome.Invalid, "Brugeren er allerede tilknyttet stillingen.");

            dataDb.OrgPositionHolders.Add(new OrgPositionHolder { PositionId = id, UserId = data.UserId });
            await dataDb.SaveChangesAsync();

            accessCache.Evict(data.UserId);

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> RemoveHolder(OrgActor actor, int id, string userId)
        {
            var position = await dataDb.OrgPositions
                .Include(position => position.Holders)
                .FirstOrDefaultAsync(position => position.Id == id && position.DeletedAt == null);
            if (position == null) return new ServiceResult(ServiceOutcome.NotFound);

            if (position.IsSystem && !actor.IsSystemAdministrator) return new ServiceResult(ServiceOutcome.Forbidden);
            if (!await CanManagePositionAsync(actor, position)) return new ServiceResult(ServiceOutcome.Forbidden);
            if (position.IsSystem && position.Holders.Count <= 1) return new ServiceResult(ServiceOutcome.Invalid, "Systemstillingen skal have mindst én indehaver.");

            var holder = position.Holders.FirstOrDefault(holder => holder.UserId == userId);
            if (holder == null) return new ServiceResult(ServiceOutcome.NotFound);

            dataDb.OrgPositionHolders.Remove(holder);
            await dataDb.SaveChangesAsync();

            accessCache.Evict(userId);

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult> Invite(OrgActor actor, OrgInviteDto data, int id)
        {
            var position = await dataDb.OrgPositions
                .Include(position => position.Groups)
                .FirstOrDefaultAsync(position => position.Id == id && position.DeletedAt == null);
            if (position == null) return new ServiceResult(ServiceOutcome.NotFound);

            if (!await CanManagePositionAsync(actor, position)) return new ServiceResult(ServiceOutcome.Forbidden);

            var email = data.Email.Trim();

            var existing = await userManager.FindByEmailAsync(email);
            if (existing != null) return new ServiceResult(ServiceOutcome.Invalid, "Bruger findes allerede. Tilknyt i stedet brugeren direkte.");

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

            return new ServiceResult(ServiceOutcome.Success);
        }

        private async Task<List<string>> GetCurrentGroupsAsync(OrgActor actor)
        {
            var user = await userManager.FindByIdAsync(actor.UserId);
            if (user == null) return [];

            var direct = (await userManager.GetRolesAsync(user)).ToList();

            var fromPositions = await OrgPositionAccess.GetEffectiveGroups(dataDb, user.Id).ToListAsync();

            return direct.Concat(fromPositions).Distinct().ToList();
        }

        private async Task<bool> CanManageParentAsync(OrgActor actor, int? parentPositionId)
        {
            if (actor.IsSystemAdministrator) return true;
            if (parentPositionId == null) return false;

            var userId = actor.UserId;
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

        private async Task<bool> CanManagePositionAsync(OrgActor actor, OrgPosition position)
        {
            if (actor.IsSystemAdministrator) return true;

            var managedPositionId = position.PaForPositionId ?? position.ParentPositionId;
            return await CanManageParentAsync(actor, managedPositionId);
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

        private async Task<bool> CanAssignGroupsAsync(OrgActor actor, List<string> groups)
        {
            if (groups.Count == 0) return true;
            if (actor.IsSystemAdministrator) return true;

            var current = await GetCurrentGroupsAsync(actor);

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
                accessCache.Evict(userId);
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
                    .Select(x => new UserSummaryDto(x.User))
                    .ToList(),
            };
        }
    }
}
