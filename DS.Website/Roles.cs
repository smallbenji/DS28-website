namespace DS.Website
{
    public enum AppGroups
    {
        SysAdmin,
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
        // Vi ændrer værdien til string[], så vi bruger lynhurtige kompiler-konstanter via nameof()
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
                AppGroups.User,
                [
                    nameof(AppRoles.UsersView)
                ]
            }
        };
    }

}