namespace DS.Website
{
    public enum AppGroups
    {
        SysAdmin,
        CampAdmin,
        EventAdmin,
        ActivityAdmin,
        FinanceAdmin,
        User,
    }

    public enum AppRoles
    {
        UsersView,
        UsersCreate,
        UsersLock,
        UsersDelete,
        
        GroupsView,
        GroupsCreate,
        GroupsDelete,

        AuditLogView,
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

                    nameof(AppRoles.GroupsView),
                    nameof(AppRoles.GroupsCreate),
                    nameof(AppRoles.GroupsDelete),
                ]
            },
            {
                AppGroups.CampAdmin,
                [
                    nameof(AppRoles.UsersView),

                    nameof(AppRoles.GroupsView),
                    nameof(AppRoles.GroupsCreate),
                ]
            },
            {
                AppGroups.EventAdmin,
                [
                    
                ]
            },
            {
                AppGroups.ActivityAdmin,
                [
                    
                ]
            },
            {
                AppGroups.FinanceAdmin,
                [
                    
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