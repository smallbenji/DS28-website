using DS.Domain;
using Hangfire.Dashboard;

namespace DS.Backend
{
    public class HangfireDashboardAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            return context.GetHttpContext().User?.IsInRole(nameof(AppRoles.HangfireView)) == true;
        }
    }
}
