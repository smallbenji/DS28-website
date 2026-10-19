using DS.Application.Data;
using DS.Domain;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using DS.Application.Services.Notifications;

namespace DS.Application.Services.Profile
{
    public class MeService(IDataDbContext dataDb, UserManager<User> userManager)
    {
        public async Task<ServiceResult<MeDto>> Index(MeActor actor)
        {
            if (!actor.IsAuthenticated)
            {
                return new ServiceResult<MeDto>(ServiceOutcome.Success, new MeDto
                {
                    IsAuthenticated = actor.IsAuthenticated
                });
            }

            var user = await GetCurrentUserAsync(actor.UserId);
            if (user == null) return new ServiceResult(ServiceOutcome.NotFound);

            var roles = actor.Groups;
            var appRoles = AppAccess.ResolveAppRoles(roles);
            var passkeys = (await userManager.GetPasskeysAsync(user)).ToDtoList();

            var positionTitle = await dataDb.OrgPositionHolders
                .Where(holder => holder.UserId == user.Id && holder.Position.DeletedAt == null)
                .OrderBy(holder => holder.PositionId)
                .Select(holder => holder.Position.Title)
                .FirstOrDefaultAsync();

            var profilePicture = user.ProfilePictureFileId.HasValue
                ? await dataDb.StoredFiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Id == user.ProfilePictureFileId.Value
                        && f.Status == StoredFileStatus.Ready
                        && f.DeletedAt == null)
                : null;

            var model = new MeDto
            {
                IsAuthenticated = actor.IsAuthenticated,
                Id = user.Id,
                Name = user.GetFullName(),
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                Title = positionTitle ?? AppAccess.ResolveGroupTitle(roles),
                Phone = user.PhoneNumber ?? string.Empty,
                MustEnableTwoFactor = await userManager.IsInRoleAsync(
                    user,
                    nameof(AppGroups.SysAdmin)
                ) && !user.TwoFactorEnabled,
                Roles = roles,
                AppRoles = appRoles,
                IsImpersonating = actor.IsImpersonating,
                Passkeys = passkeys,
                ProfilePicture = profilePicture != null ? new ImageReferenceDto(profilePicture) : null
            };

            return new ServiceResult<MeDto>(ServiceOutcome.Success, model);
        }

        public async Task<ServiceResult> UpdatePhone(MeActor actor, UpdatePhoneDto data)
        {
            var user = await GetCurrentUserAsync(actor.UserId);
            if (user == null) return new ServiceResult(ServiceOutcome.NotFound);

            user.PhoneNumber = data.Phone;

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded) return new ServiceResult(ServiceOutcome.Invalid, result.Errors);

            return new ServiceResult(ServiceOutcome.Success);
        }

        public async Task<ServiceResult<ImageReferenceDto>> UpdateProfilePicture(MeActor actor, UpdateProfilePictureDto data)
        {
            var user = await GetCurrentUserAsync(actor.UserId);
            if (user == null) return new ServiceResult(ServiceOutcome.NotFound);

            if (data.Image == null)
            {
                user.ProfilePictureFileId = null;

                var clearResult = await userManager.UpdateAsync(user);
                if (!clearResult.Succeeded) return new ServiceResult(ServiceOutcome.Invalid, clearResult.Errors);

                return new ServiceResult(ServiceOutcome.Success);
            }

            var image = await dataDb.StoredFiles
                .FirstOrDefaultAsync(f => f.PublicId == data.Image.PublicId
                    && f.Purpose == FilePurpose.ProfilePicture
                    && f.Status == StoredFileStatus.Ready
                    && f.DeletedAt == null
                    && f.UploadedByUserId == user.Id);
            if (image == null) return new ServiceResult(ServiceOutcome.Invalid, "Billedet findes ikke.");

            user.ProfilePictureFileId = image.Id;

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded) return new ServiceResult(ServiceOutcome.Invalid, result.Errors);

            return new ServiceResult<ImageReferenceDto>(ServiceOutcome.Success, new ImageReferenceDto(image));
        }

        public async Task<ServiceResult<List<NotificationPreferenceDto>>> GetNotifications(MeActor actor)
        {
            var user = await GetCurrentUserAsync(actor.UserId);
            if (user == null) return new ServiceResult(ServiceOutcome.NotFound);

            var roles = actor.Groups;
            var appRoles = AppAccess.ResolveAppRoles(roles);

            var subscribed = await dataDb.NotificationPreferences
                .Where(p => p.UserId == user.Id)
                .Select(p => p.NotificationType)
                .ToListAsync();

            var preferences = Enum.GetValues<NotificationType>()
                .Select(type => new NotificationPreferenceDto
                {
                    Type = type.ToString(),
                    CanSubscribe = NotificationPermissions.CanSubscribe(appRoles, type),
                    Subscribed = subscribed.Contains(type),
                })
                .ToList();

            return new ServiceResult<List<NotificationPreferenceDto>>(ServiceOutcome.Success, preferences);
        }

        public async Task<ServiceResult> UpdateNotifications(MeActor actor, UpdateNotificationPreferencesDto data)
        {
            var user = await GetCurrentUserAsync(actor.UserId);
            if (user == null) return new ServiceResult(ServiceOutcome.NotFound);

            var roles = actor.Groups;
            var appRoles = AppAccess.ResolveAppRoles(roles);

            var requested = new List<NotificationType>();
            foreach (var typeName in data.Types)
            {
                if (!Enum.TryParse<NotificationType>(typeName, out var type)) return new ServiceResult(ServiceOutcome.Invalid, $"Ukendt notifikationstype: {typeName}");
                if (!NotificationPermissions.CanSubscribe(appRoles, type)) return new ServiceResult(ServiceOutcome.Forbidden);

                requested.Add(type);
            }

            var existing = await dataDb.NotificationPreferences
                .Where(p => p.UserId == user.Id)
                .ToListAsync();

            dataDb.NotificationPreferences.RemoveRange(
                existing.Where(p => !requested.Contains(p.NotificationType)));

            foreach (var type in requested.Where(t => existing.All(p => p.NotificationType != t)))
            {
                dataDb.NotificationPreferences.Add(new UserNotificationPreference
                {
                    UserId = user.Id,
                    NotificationType = type,
                });
            }

            await dataDb.SaveChangesAsync();

            return new ServiceResult(ServiceOutcome.Success);
        }

        private async Task<User> GetCurrentUserAsync(string userId)
        {
            if (userId == null) return null;

            return await userManager.FindByIdAsync(userId);
        }
    }
}
