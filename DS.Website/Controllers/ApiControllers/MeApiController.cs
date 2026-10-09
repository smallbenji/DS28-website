using DS.Models;
using DS.Data;
using DS.DTOs;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace DS.Website.Controllers
{
    [Route("/api/v1/me")]
    public class MeApiController(UserManager<User> userManager, DataDbContext dataDb) : Controller
    {
        public async Task<IActionResult> Index()
        {
            if (!HttpContext.User.Identity.IsAuthenticated)
            {
                return Ok(new MeDto
                {
                    IsAuthenticated = HttpContext.User.Identity.IsAuthenticated
                });
            }

            var user = await userManager.GetUserAsync(HttpContext.User);
            if (user == null) return NotFound();

            var roles = (await userManager.GetRolesAsync(user)).ToList();
            var appRoles = AppAccess.ResolveAppRoles(roles);
            var passkeys = (await userManager.GetPasskeysAsync(user)).ToDtoList();

            var profilePicture = user.ProfilePictureFileId.HasValue
                ? await dataDb.StoredFiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(f => f.Id == user.ProfilePictureFileId.Value
                        && f.Status == StoredFileStatus.Ready
                        && f.DeletedAt == null)
                : null;

            var model = new MeDto
            {
                IsAuthenticated = HttpContext.User.Identity.IsAuthenticated,
                Id = user.Id,
                Name = user.GetFullName(),
                FirstName = user.FirstName ?? string.Empty,
                LastName = user.LastName ?? string.Empty,
                Phone = user.PhoneNumber ?? string.Empty,
                MustEnableTwoFactor = await userManager.IsInRoleAsync(
                    user,
                    nameof(AppGroups.SysAdmin)
                ) && !user.TwoFactorEnabled,
                Roles = roles,
                AppRoles = appRoles,
                Passkeys = passkeys,
                ProfilePicture = profilePicture != null ? new ImageReferenceDto(profilePicture) : null
            };

            return Ok(model);
        }

        [HttpPut("phone")]
        public async Task<IActionResult> UpdatePhone([FromBody] UpdatePhoneDto data)
        {
            if (data == null) return BadRequest("Invalid request body.");

            var user = await userManager.GetUserAsync(HttpContext.User);
            if (user == null) return NotFound();

            user.PhoneNumber = data.Phone;

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok();
        }

        [HttpPut("profile-picture")]
        public async Task<IActionResult> UpdateProfilePicture([FromBody] UpdateProfilePictureDto data)
        {
            if (data == null) return BadRequest("Invalid request body.");

            var user = await userManager.GetUserAsync(HttpContext.User);
            if (user == null) return NotFound();

            if (data.Image == null)
            {
                user.ProfilePictureFileId = null;

                var clearResult = await userManager.UpdateAsync(user);
                if (!clearResult.Succeeded) return BadRequest(clearResult.Errors);

                return Ok();
            }

            var image = await dataDb.StoredFiles
                .FirstOrDefaultAsync(f => f.PublicId == data.Image.PublicId
                    && f.Purpose == FilePurpose.ProfilePicture
                    && f.Status == StoredFileStatus.Ready
                    && f.DeletedAt == null
                    && f.UploadedByUserId == user.Id);
            if (image == null) return BadRequest("Billedet findes ikke.");

            user.ProfilePictureFileId = image.Id;

            var result = await userManager.UpdateAsync(user);
            if (!result.Succeeded) return BadRequest(result.Errors);

            return Ok(new ImageReferenceDto(image));
        }

        [HttpGet("notifications")]
        public async Task<IActionResult> GetNotifications()
        {
            var user = await userManager.GetUserAsync(HttpContext.User);
            if (user == null) return NotFound();

            var roles = await userManager.GetRolesAsync(user);
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

            return Ok(preferences);
        }

        [HttpPut("notifications")]
        public async Task<IActionResult> UpdateNotifications([FromBody] UpdateNotificationPreferencesDto data)
        {
            if (data == null) return BadRequest("Invalid request body.");

            var user = await userManager.GetUserAsync(HttpContext.User);
            if (user == null) return NotFound();

            var roles = await userManager.GetRolesAsync(user);
            var appRoles = AppAccess.ResolveAppRoles(roles);

            var requested = new List<NotificationType>();
            foreach (var typeName in data.Types)
            {
                if (!Enum.TryParse<NotificationType>(typeName, out var type)) return BadRequest($"Ukendt notifikationstype: {typeName}");
                if (!NotificationPermissions.CanSubscribe(appRoles, type)) return Forbid();

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

            return Ok();
        }
    }
}
