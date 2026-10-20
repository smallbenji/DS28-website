using DS.Application.Data;
using DS.Domain.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace DS.Application.Services.Groups
{
    internal static class GroupLookup
    {
        public const int RegistrationSettingsId = 1;

        public static Task<User> GetUserWithGroupAsync(this UserManager<User> userManager, string userId)
        {
            return userManager.Users
                .Include(u => u.Group)
                .SingleOrDefaultAsync(u => u.Id == userId);
        }

        public static async Task<bool> IsPreSignupOpenAsync(this IDataDbContext dataDb, bool lockSettings)
        {
            if (lockSettings)
            {
                await dataDb.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT 1 FROM ds28.registration_settings WHERE id = {RegistrationSettingsId} FOR SHARE");
            }

            return await dataDb.RegistrationSettings.AnyAsync(
                s => s.Id == RegistrationSettingsId && s.IsPreSignupOpen);
        }

        public static Task LockGroupForUpdateAsync(this IDataDbContext dataDb, int groupId)
        {
            return dataDb.Database.ExecuteSqlInterpolatedAsync(
                $"SELECT 1 FROM ds28.scout_group WHERE id = {groupId} FOR UPDATE");
        }
    }
}
