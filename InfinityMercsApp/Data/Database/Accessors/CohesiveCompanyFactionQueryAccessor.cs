using InfinityMercsApp.Infrastructure.Providers;

namespace InfinityMercsApp.Data.Database;

public sealed class CohesiveCompanyFactionQueryAccessor(IFactionProvider factionProvider, IMercsArmyListProvider mercsArmyListProvider) : ICohesiveCompanyFactionQueryAccessor
{
    public async Task<CohesiveCompanyFactionQueryResult> GetFilterQuerySourceAsync(
        IReadOnlyCollection<int> factionIds,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var normalizedFactionIds = factionIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();
        if (normalizedFactionIds.Length == 0)
        {
            return new CohesiveCompanyFactionQueryResult();
        }

        var typeLookup = new Dictionary<int, string>();
        var charsLookup = new Dictionary<int, string>();
        var skillsLookup = new Dictionary<int, string>();
        var equipLookup = new Dictionary<int, string>();
        var weaponsLookup = new Dictionary<int, string>();
        var ammoLookup = new Dictionary<int, string>();

        foreach (var factionId in normalizedFactionIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var snapshot = factionProvider.GetFactionSnapshot(factionId);
            var filtersJson = snapshot?.FiltersJson;
            if (string.IsNullOrWhiteSpace(filtersJson))
            {
                continue;
            }

            MergeLookup(typeLookup, ArmyJsonLookup.BuildIdNameLookup(filtersJson, "type"));
            MergeLookup(charsLookup, ArmyJsonLookup.BuildIdNameLookup(filtersJson, "chars"));
            MergeLookup(skillsLookup, ArmyJsonLookup.BuildIdNameLookup(filtersJson, "skills"));
            MergeLookup(equipLookup, ArmyJsonLookup.BuildIdNameLookup(filtersJson, "equip"));
            MergeLookup(weaponsLookup, ArmyJsonLookup.BuildIdNameLookup(filtersJson, "weapons"));
            MergeLookup(ammoLookup, ArmyJsonLookup.BuildIdNameLookup(filtersJson, "ammunition"));
        }

        var mergedEntries = mercsArmyListProvider.GetMergedMercsArmyList(normalizedFactionIds);
        return new CohesiveCompanyFactionQueryResult
        {
            TypeLookup = typeLookup,
            CharacteristicsLookup = charsLookup,
            SkillsLookup = skillsLookup,
            EquipmentLookup = equipLookup,
            WeaponsLookup = weaponsLookup,
            AmmoLookup = ammoLookup,
            MergedMercsListEntries = mergedEntries
        };
    }

    private static void MergeLookup(Dictionary<int, string> target, IReadOnlyDictionary<int, string> source)
    {
        foreach (var pair in source)
        {
            if (target.ContainsKey(pair.Key) || string.IsNullOrWhiteSpace(pair.Value))
            {
                continue;
            }

            target[pair.Key] = pair.Value.Trim();
        }
    }

    private static class ArmyJsonLookup
    {
        public static IReadOnlyDictionary<int, string> BuildIdNameLookup(string? filtersJson, string key)
        {
            if (string.IsNullOrWhiteSpace(filtersJson))
            {
                return new Dictionary<int, string>();
            }

            var map = new Dictionary<int, string>();
            try
            {
                using var doc = System.Text.Json.JsonDocument.Parse(filtersJson);
                if (doc.RootElement.ValueKind != System.Text.Json.JsonValueKind.Object ||
                    !doc.RootElement.TryGetProperty(key, out var array) ||
                    array.ValueKind != System.Text.Json.JsonValueKind.Array)
                {
                    return map;
                }

                foreach (var item in array.EnumerateArray())
                {
                    if (!TryParseId(item, out var id))
                    {
                        continue;
                    }

                    var name = item.TryGetProperty("name", out var nameElement)
                        ? (nameElement.GetString() ?? string.Empty)
                        : string.Empty;
                    if (string.IsNullOrWhiteSpace(name))
                    {
                        continue;
                    }

                    map[id] = name.Trim();
                }
            }
            catch
            {
                return new Dictionary<int, string>();
            }

            return map;
        }

        private static bool TryParseId(System.Text.Json.JsonElement element, out int id)
        {
            id = 0;
            if (element.TryGetProperty("id", out var idElement))
            {
                return TryParseInt(idElement, out id);
            }

            return false;
        }

        private static bool TryParseInt(System.Text.Json.JsonElement element, out int value)
        {
            value = 0;
            if (element.ValueKind == System.Text.Json.JsonValueKind.Number)
            {
                return element.TryGetInt32(out value);
            }

            if (element.ValueKind == System.Text.Json.JsonValueKind.String)
            {
                return int.TryParse(element.GetString(), out value);
            }

            return false;
        }
    }
}
