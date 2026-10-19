namespace DS.Domain
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
        PersonalAssistant,
        User,
    }

    public enum AppRoles
    {
        UsersView,
        UsersCreate,
        UsersLock,
        UsersDelete,
        UsersResetPassword,
        UsersEditProfilePicture,

        GroupsView,
        GroupsCreate,
        GroupsDelete,
        PreSignupManage,

        OrgView,
        OrgManage,

        ActivityView,
        ActivityAdmin,

        AuditLogView,
        ExportsView,
        AdminAccess,
        OpenIddictManage,

        WordPressEditor,
        WordPressAdmin,

        HangfireView,
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
                    nameof(AppRoles.UsersEditProfilePicture),

                    nameof(AppRoles.GroupsView),
                    nameof(AppRoles.GroupsCreate),
                    nameof(AppRoles.GroupsDelete),
                    nameof(AppRoles.PreSignupManage),

                    nameof(AppRoles.OrgView),
                    nameof(AppRoles.OrgManage),

                    nameof(AppRoles.WordPressAdmin),

                    nameof(AppRoles.ActivityView),
                    nameof(AppRoles.ActivityAdmin),

                    nameof(AppRoles.AuditLogView),
                    nameof(AppRoles.ExportsView),
                    nameof(AppRoles.AdminAccess),
                    nameof(AppRoles.OpenIddictManage),

                    nameof(AppRoles.HangfireView),
                ]
            },
            {
                AppGroups.PRAdmin,
                [
                    nameof(AppRoles.WordPressAdmin),

                    nameof(AppRoles.OrgView),
                    nameof(AppRoles.OrgManage),
                ]
            },
            {
                AppGroups.CampAdmin,
                [
                    nameof(AppRoles.UsersView),
                    nameof(AppRoles.UsersResetPassword),

                    nameof(AppRoles.GroupsView),
                    nameof(AppRoles.GroupsCreate),

                    nameof(AppRoles.OrgView),
                    nameof(AppRoles.OrgManage),

                    nameof(AppRoles.WordPressEditor),
                    nameof(AppRoles.PreSignupManage),
                    nameof(AppRoles.ActivityAdmin),
                    nameof(AppRoles.ActivityView),

                    nameof(AppRoles.AuditLogView),
                    nameof(AppRoles.ExportsView),
                    nameof(AppRoles.AdminAccess),
                ]
            },
            {
                AppGroups.EventAdmin,
                [
                    nameof(AppRoles.ActivityView),
                    nameof(AppRoles.ActivityAdmin),
                    nameof(AppRoles.WordPressEditor),

                    nameof(AppRoles.OrgView),
                    nameof(AppRoles.OrgManage),
                ]
            },
            {
                AppGroups.ActivityAdmin,
                [
                    nameof(AppRoles.ActivityView),
                    nameof(AppRoles.ActivityAdmin),
                    nameof(AppRoles.WordPressEditor),

                    nameof(AppRoles.OrgView),
                    nameof(AppRoles.OrgManage),
                ]
            },
            {
                AppGroups.FinanceAdmin,
                [
                    nameof(AppRoles.WordPressEditor),

                    nameof(AppRoles.OrgView),
                    nameof(AppRoles.OrgManage),
                ]
            },
            {
                AppGroups.ActivityUser,
                [
                    nameof(AppRoles.ActivityView),

                    nameof(AppRoles.OrgView),
                    nameof(AppRoles.OrgManage),
                ]
            },
            {
                AppGroups.PersonalAssistant,
                [
                    nameof(AppRoles.OrgView)
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
                    AppGroups.PersonalAssistant,
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
                    AppGroups.PersonalAssistant,
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

        public static string GetGroupTitle(AppGroups group)
        {
            return group switch
            {
                AppGroups.SysAdmin => "Systemadministrator",
                AppGroups.CampAdmin => "Lejrchef",
                AppGroups.EventAdmin => "Eventansvarlig",
                AppGroups.ActivityAdmin => "Aktivitetsansvarlig",
                AppGroups.ActivityUser => "Aktivitets-teammedlem",
                AppGroups.FinanceAdmin => "Kasserer",
                AppGroups.PRAdmin => "PR-ansvarlig",
                AppGroups.FoodAdmin => "Madansvarlig",
                AppGroups.PersonalAssistant => "Personlig assistent",
                AppGroups.User => "Deltager",
                _ => string.Empty,
            };
        }

        public static string ResolveGroupTitle(IEnumerable<string> roleNames)
        {
            foreach (var group in Enum.GetValues<AppGroups>())
            {
                if (roleNames.Contains(group.ToString()))
                {
                    return GetGroupTitle(group);
                }
            }

            return string.Empty;
        }
    }

}
