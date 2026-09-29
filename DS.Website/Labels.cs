using DS.Models;

namespace DS.Website
{
    public static class Labels
    {
        public static readonly Dictionary<District, string> DistrictNames = new()
        {
            { District.DANEHOF, "Danehof" },
            { District.FIONIA, "Fionia" }
        };

        public static readonly Dictionary<Gender, string> GenderNames = new()
        {
            { Gender.Male, "Mand" },
            { Gender.Female, "Kvinde" }
        };

        public static string For(District district)
        {
            return DistrictNames.TryGetValue(district, out var name) ? name : district.ToString();
        }

        public static string For(Gender gender)
        {
            return GenderNames.TryGetValue(gender, out var name) ? name : gender.ToString();
        }
    }
}
