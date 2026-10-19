using DS.Domain;
using DS.Domain.Models;

namespace DS.Application.Services.Notifications
{
    public static class NotificationPermissions
    {
        public static readonly Dictionary<NotificationType, string[]> AllowedAppRoles = new()
        {
            {
                NotificationType.NewUser,
                [nameof(AppRoles.UsersView)]
            }
        };

        public static bool CanSubscribe(IEnumerable<string> appRoles, NotificationType type)
        {
            return AllowedAppRoles.TryGetValue(type, out var allowed)
                && allowed.Any(appRoles.Contains);
        }
    }
}
