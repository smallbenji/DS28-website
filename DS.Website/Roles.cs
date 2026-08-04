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
        
        GroupView,
        GroupCreate,
        GroupDelete,

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

                    nameof(AppRoles.GroupView),
                    nameof(AppRoles.GroupCreate),
                    nameof(AppRoles.GroupDelete),
                ]
            },
            {
                AppGroups.CampAdmin,
                [
                    nameof(AppRoles.UsersView),

                    nameof(AppRoles.GroupView),
                    nameof(AppRoles.GroupCreate),
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