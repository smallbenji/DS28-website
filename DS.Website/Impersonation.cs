using System.Security.Claims;
using DS.Domain;
using Microsoft.AspNetCore.DataProtection;

namespace DS.Website
{
    public static class Impersonation
    {
        public const string CookieName = "DS.Impersonation";
        public const string CookiePurpose = "DS28.Impersonation";
        public const string OriginClaimType = "ds28:imp:origin";
        public const string ActiveClaimType = "ds28:imp:active";

        public static string ProtectGroups(IDataProtectionProvider provider, IEnumerable<string> groups)
        {
            var protector = provider.CreateProtector(CookiePurpose);
            return protector.Protect(string.Join(',', groups));
        }

        public static List<string> ResolveCookieGroups(IDataProtectionProvider provider, string cookieValue)
        {
            if (string.IsNullOrWhiteSpace(cookieValue)) return null;

            var protector = provider.CreateProtector(CookiePurpose);

            string value;
            try
            {
                value = protector.Unprotect(cookieValue);
            }
            catch
            {
                return null;
            }

            var groups = value
                .Split(',')
                .Where(g => Enum.TryParse<AppGroups>(g, out _))
                .Distinct()
                .ToList();

            return groups.Count > 0 ? groups : null;
        }
    }
}