namespace DS.Website
{
    public enum AppGroups
    {
        SysAdmin,
        CampAdmin,
        EventAdmin,
        ActivityAdmin,
        ActivityUser,
        FinanceAdmin,
        PRAdmin,
        FoodAdmin,
        User,
    }

    public enum AppRoles
    {
        UsersView,
        UsersCreate,
        UsersLock,
        UsersDelete,
        UsersResetPassword,

        GroupsView,
        GroupsCreate,
        GroupsDelete,

        ActivityView,
        ActivityAdmin,

        AuditLogView,

        WordPressEditor,
        WordPressAdmin,
    }

    public static class AppAccess
    {
        public static readonly Dictionary<AppGroups, string[]> Matrix = new()
        {
            {
                AppGroups.SysAdmin,
                [
                    nameof(AppRoles.UsersView),
                    nameof(AppRoles.UsersCreate),
                    nameof(AppRoles.UsersLock),
                    nameof(AppRoles.UsersDelete),
                    nameof(AppRoles.UsersResetPassword),

                    nameof(AppRoles.GroupsView),
                    nameof(AppRoles.GroupsCreate),
                    nameof(AppRoles.GroupsDelete),

                    nameof(AppRoles.WordPressAdmin),

                    nameof(AppRoles.ActivityView),
                    nameof(AppRoles.ActivityAdmin),
                ]
            },
            {
                AppGroups.PRAdmin,
                [
                    nameof(AppRoles.WordPressAdmin)
                ]
            },
            {
                AppGroups.CampAdmin,
                [
                    nameof(AppRoles.UsersView),
                    nameof(AppRoles.UsersResetPassword),

                    nameof(AppRoles.GroupsView),
                    nameof(AppRoles.GroupsCreate),

                    nameof(AppRoles.WordPressEditor),
                    nameof(AppRoles.ActivityAdmin),
                    nameof(AppRoles.ActivityView),
                ]
            },
            {
                AppGroups.EventAdmin,
                [
                    nameof(AppRoles.WordPressEditor),
                ]
            },
            {
                AppGroups.ActivityAdmin,
                [
                    nameof(AppRoles.ActivityView),
                    nameof(AppRoles.ActivityAdmin),
                    nameof(AppRoles.WordPressEditor),
                ]
            },
            {
                AppGroups.FinanceAdmin,
                [
                    nameof(AppRoles.WordPressEditor),
                ]
            },
            {
                AppGroups.ActivityUser,
                [
                    nameof(AppRoles.ActivityView),
                ]
            },
            {
                AppGroups.User,
                [
                    // nameof(AppRoles.UsersView)
                ]
            }
        };

        public static readonly Dictionary<AppGroups, AppGroups[]> AssignableGroups = new()
        {
            {
                AppGroups.SysAdmin,
                [
                    AppGroups.SysAdmin,
                    AppGroups.CampAdmin,
                    AppGroups.EventAdmin,
                    AppGroups.ActivityAdmin,
                    AppGroups.ActivityUser,
                    AppGroups.FinanceAdmin,
                    AppGroups.PRAdmin,
                    AppGroups.FoodAdmin,
                    AppGroups.User,
                ]
            },
            {
                AppGroups.CampAdmin,
                [
                    AppGroups.EventAdmin,
                    AppGroups.ActivityAdmin,
                    AppGroups.ActivityUser,
                    AppGroups.FinanceAdmin,
                    AppGroups.PRAdmin,
                    AppGroups.FoodAdmin,
                    AppGroups.User,
                ]
            },
        };

        public static bool CanAssignRole(IEnumerable<string> userGroups, string targetGroup)
        {
            return userGroups.Any(g =>
                Enum.TryParse<AppGroups>(g, out var group) &&
                AssignableGroups.TryGetValue(group, out var assignable) &&
                assignable.Any(a => a.ToString() == targetGroup));
        }

        public static List<string> ResolveAppRoles(IEnumerable<string> roleNames)
        {
            return roleNames
                .SelectMany(roleName =>
                    Enum.TryParse<AppGroups>(roleName, out var group) &&
                    Matrix.TryGetValue(group, out var subRoles)
                        ? subRoles
                        : [])
                .Distinct()
                .ToList();
        }
    }

}