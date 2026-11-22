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
            },
            {
                NotificationType.BoardCreated,
                [nameof(AppRoles.BoardViewAll)]
            }
        };

        public static bool CanSubscribe(IEnumerable<string> appRoles, NotificationType type)
        {
            return AllowedAppRoles.TryGetValue(type, out var allowed)
                && allowed.Any(appRoles.Contains);
        }

        public static List<string> AllowedGroups(NotificationType type)
        {
            return Enum.GetValues<AppGroups>()
                .Select(group => group.ToString())
                .Where(group => CanSubscribe(AppAccess.ResolveAppRoles([group]), type))
                .ToList();
        }
    }
}
