using DS.Models;

namespace DS.Website
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
