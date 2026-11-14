using System.Globalization;
using System.Text.Json.Nodes;

namespace DS.RegistrationImporter
{
    internal static class RegistrationNormalizer
    {
        private static readonly (string Category, string Field, string? LeaderField)[] Categories =
        [
            ("family", "FamilieSpejder", "Familie"), ("beaver", "Baever", "Baever"),
            ("wolf", "Ulve", "Ulve"), ("junior", "Junior", "Juniorer"), ("trop", "Trop", "Trop"),
            ("senior", "Senior", "Senior"), ("rover", "Klan", "Klan"), ("leader", "Leder", null)
        ];
        private static readonly string[] Days = ["LørdagStart", "Søndag", "Mandag", "Tirsdag", "Onsdag", "Torsdag", "Fredag", "LørdagSlut"];

        internal static RegistrationData Normalize(List<SourceDocument> groups, List<SourceDocument> presignups, List<SourceDocument> signups)
        {
            var preRows = new List<ImportRow>();
            var signupRows = new List<ImportRow>();
            var skipped = new List<string>();
            foreach (var document in presignups)
            {
                var data = document.Data;
                var group = FindGroup(groups, "GruppeUID", data["_Gruppe"]?["GruppeUID"]?.ToString());
                foreach (var (category, field, _) in Categories)
                {
                    var guests = data["venskabsgruppe"]?.GetValue<bool>() == true ? Count(data["Venskabsgruppe" + field] ?? JsonValue.Create(0)) : 0;
                    preRows.Add(CreateRow(group, document.Id, category, Count(data[field]), guests, 0));
                }
            }

            foreach (var document in signups)
            {
                var data = document.Data;
                var leaderBreakdown = Categories.Where(item => item.LeaderField != null).Aggregate(0, (total, item) => checked(total + Count(data["Ledere"]?[item.LeaderField!] ?? JsonValue.Create(0))));
                if (string.IsNullOrWhiteSpace(data["email"]?.ToString()))
                {
                    var patrolCount = Categories.Aggregate(0, (total, item) => checked(total + Patrols(data, item.Category, item.Field).Aggregate(0, (sum, patrol) => checked(sum + Count(patrol!["antal"])))));
                    if (patrolCount != 0 || leaderBreakdown != 0) throw new ImportValidationException("En endelig tilmelding med deltagere mangler gruppekobling.");
                    skipped.Add(document.Id);
                    continue;
                }

                var group = FindGroup(groups, "email", data["email"]!.ToString());
                foreach (var (category, field, _) in Categories)
                {
                    var patrols = Patrols(data, category, field);
                    var totals = new int[9];
                    var patrolKeys = new HashSet<string>();
                    foreach (var patrol in patrols)
                    {
                        var key = patrol!["key"]?.ToString();
                        if (string.IsNullOrWhiteSpace(key) || !patrolKeys.Add(key)) throw new ImportValidationException("Manglende eller dubleret patruljenøgle i samme gren.");
                        var size = Count(patrol["antal"]);
                        totals[0] = checked(totals[0] + size);
                        for (var day = 0; day < Days.Length; day++)
                        {
                            var present = patrol["dage"]?[Days[day]];
                            if (present is not JsonValue dayValue || !dayValue.TryGetValue<bool>(out var isPresent)) throw new ImportValidationException("En patrulje mangler en gyldig fremmødedag.");
                            if (isPresent) totals[day + 1] = checked(totals[day + 1] + size);
                        }
                    }

                    if (category == "leader")
                    {
                        if (patrols.Count > 0 && leaderBreakdown > 0) throw new ImportValidationException("Både lederpatruljer og ledertotaler findes; afklar overlap før import.");
                        totals[0] = checked(totals[0] + leaderBreakdown);
                    }

                    var dayCount = category == "leader" && patrols.Count == 0 ? 1 : 9;
                    for (var day = 0; day < dayCount; day++) signupRows.Add(CreateRow(group, document.Id, category, totals[day], 0, day));
                }
            }

            return new RegistrationData(preRows, signupRows, skipped);
        }

        internal static int Count(JsonNode? value)
        {
            if (value is JsonValue text && text.TryGetValue<string>(out var input))
            {
                if (input == "") return 0;
                if (input.Length > 0 && input.All(char.IsAsciiDigit) && int.TryParse(input, NumberStyles.None, CultureInfo.InvariantCulture, out var count)) return count;
            }
            else if (value is JsonValue number && number.TryGetValue<long>(out var numeric) && numeric >= 0 && numeric <= int.MaxValue)
            {
                return (int)numeric;
            }
            else if (value is JsonValue integer && integer.TryGetValue<int>(out var intValue) && intValue >= 0)
            {
                return intValue;
            }

            throw new ImportValidationException("Deltagerantal skal være et ikke-negativt heltal.");
        }

        private static JsonArray Patrols(JsonObject data, string category, string field)
        {
            var node = data[category == "beaver" ? "Bæver" : field];
            if (node == null) return new JsonArray();
            return node as JsonArray ?? throw new ImportValidationException("Patruljefelt er ikke en liste.");
        }

        private static JsonObject FindGroup(List<SourceDocument> groups, string field, string? value)
        {
            var matches = groups.Where(group => !string.IsNullOrWhiteSpace(value) && group.Data[field]?.ToString() == value).ToList();
            if (matches.Count != 1) throw new ImportValidationException($"En kildetilmelding kunne ikke kobles entydigt til Grupper via {field}.");
            return matches[0].Data;
        }

        private static ImportRow CreateRow(JsonObject group, string documentId, string category, int count, int guests, int day)
        {
            var number = group["GruppeNummer"]?.ToString();
            var name = group["GruppeNavn"]?.GetValue<string>();
            if (string.IsNullOrWhiteSpace(number) || string.IsNullOrWhiteSpace(name)) throw new ImportValidationException("En kildegruppe mangler navn eller gruppenummer.");
            return new ImportRow(number, name, category, count, guests, day, documentId);
        }
    }
}
