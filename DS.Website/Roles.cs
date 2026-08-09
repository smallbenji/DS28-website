namespace DS.Website
{
    public enum AppGroups
    {
        SysAdmin,
        CampAdmin,
        EventAdmin,
        ActivityAdmin,
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
                AppGroups.User,
                [
                    nameof(AppRoles.UsersView)
                ]
            }
        };
    }

}