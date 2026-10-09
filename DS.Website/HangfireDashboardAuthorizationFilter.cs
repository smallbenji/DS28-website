using Hangfire.Dashboard;

namespace DS.Website
{
    public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            return context.GetHttpContext().User?.IsInRole(nameof(AppRoles.AdminAccess)) == true;
        }
    }
}
