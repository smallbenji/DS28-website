using System.Text.Json;

namespace DS.Website
{
    public static class OpenIddictClientPolicy
    {
        public const string RequiredRolesProperty = "ds28:required_app_roles";
        private static readonly HashSet<string> KnownRoles = new(Enum.GetNames<AppRoles>(), StringComparer.Ordinal);

        public static bool IsValid(string[] requiredRoles)
        {
            return requiredRoles != null && requiredRoles.Length > 0 && requiredRoles.Length <= KnownRoles.Count
                && requiredRoles.All(role => role != null && KnownRoles.Contains(role));
        }

        public static string[] ReadRequiredRoles(IReadOnlyDictionary<string, JsonElement> properties)
        {
            if (!properties.TryGetValue(RequiredRolesProperty, out var value)) return [nameof(AppRoles.WordPressEditor), nameof(AppRoles.WordPressAdmin)];

            if (value.ValueKind != JsonValueKind.Array) return [];

            var roles = new List<string>();
            foreach (var item in value.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.String) return [];
                roles.Add(item.GetString());
            }

            return IsValid(roles.ToArray()) ? roles.Distinct(StringComparer.Ordinal).ToArray() : [];
        }
    }
}
