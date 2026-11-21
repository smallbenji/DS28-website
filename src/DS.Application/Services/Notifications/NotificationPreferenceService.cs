using DS.Application.Data;
using DS.Domain.DTOs;
using DS.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DS.Application.Services.Notifications
{
    public class NotificationPreferenceService(IDataDbContext dataDb)
    {
        public async Task<List<NotificationPreferenceDto>> GetPreferences(string userId, List<string> appRoles)
        {
            var subscribed = await dataDb.NotificationPreferences
                .Where(p => p.UserId == userId)
                .Select(p => p.NotificationType)
                .ToListAsync();

            return Enum.GetValues<NotificationType>()
                .Select(type => new NotificationPreferenceDto
                {
                    Type = type.ToString(),
                    CanSubscribe = NotificationPermissions.CanSubscribe(appRoles, type),
                    Subscribed = subscribed.Contains(type),
                })
                .ToList();
        }

        public async Task<ServiceResult> UpdatePreferences(string userId, List<string> appRoles, UpdateNotificationPreferencesDto data)
        {
            var requested = new List<NotificationType>();
            foreach (var typeName in data.Types)
            {
                if (!Enum.TryParse<NotificationType>(typeName, out var type)) return new ServiceResult(ServiceOutcome.Invalid, $"Ukendt notifikationstype: {typeName}");
                if (!NotificationPermissions.CanSubscribe(appRoles, type)) return new ServiceResult(ServiceOutcome.Forbidden);

                requested.Add(type);
            }

            var existing = await dataDb.NotificationPreferences
                .Where(p => p.UserId == userId)
                .ToListAsync();

            dataDb.NotificationPreferences.RemoveRange(
                existing.Where(p => !requested.Contains(p.NotificationType)));

            foreach (var type in requested.Where(t => existing.All(p => p.NotificationType != t)))
            {
                dataDb.NotificationPreferences.Add(new UserNotificationPreference
                {
                    UserId = userId,
                    NotificationType = type,
                });
            }

            await dataDb.SaveChangesAsync();

            return new ServiceResult(ServiceOutcome.Success);
        }
    }
}
