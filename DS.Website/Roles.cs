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
    }

}