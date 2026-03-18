using System.Text.Json;
using System.Text.Json.Nodes;
using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Domain.Utilities;

namespace InfinityMercsApp.Infrastructure.Providers;

/// <inheritdoc/>
public sealed class CohesiveCompanyFactionQueryProvider(IFactionProvider factionProvider) : ICohesiveCompanyFactionQueryProvider
{
    /// <inheritdoc/>
    public Task<CohesiveCompanyFactionQueryResult> GetFilterQuerySourceAsync(
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
            return Task.FromResult(new CohesiveCompanyFactionQueryResult());
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

        var mergedEntries = GetMergedMercsArmyList(normalizedFactionIds, cancellationToken);
        return Task.FromResult(new CohesiveCompanyFactionQueryResult
        {
            TypeLookup = typeLookup,
            CharacteristicsLookup = charsLookup,
            SkillsLookup = skillsLookup,
            EquipmentLookup = equipLookup,
            WeaponsLookup = weaponsLookup,
            AmmoLookup = ammoLookup,
            MergedMercsListEntries = mergedEntries
        });
    }

    private IReadOnlyList<MercsArmyListEntry> GetMergedMercsArmyList(
        IReadOnlyCollection<int> factionIds,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (factionIds.Count == 0)
        {
            return [];
        }

        var normalizedFactionIds = factionIds
            .Where(id => id > 0)
            .Distinct()
            .ToArray();
        if (normalizedFactionIds.Length == 0)
        {
            return [];
        }

        var resultSets = normalizedFactionIds
            .Select(factionProvider.GetResumeByFactionMercsOnly)
            .ToArray();
        var unitLookupByFaction = new Dictionary<int, IReadOnlyDictionary<int, Unit>>();
        for (var setIndex = 0; setIndex < resultSets.Length; setIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var factionId = normalizedFactionIds[setIndex];
            var unitIds = resultSets[setIndex]
                .Select(x => x.UnitId)
                .Where(id => id > 0)
                .Distinct()
                .ToArray();
            var lookup = unitIds.ToDictionary(
                id => id,
                id => factionProvider.GetUnit(factionId, id));
            unitLookupByFaction[factionId] = lookup
                .Where(x => x.Value is not null)
                .ToDictionary(x => x.Key, x => x.Value!);
        }

        var mergedByUnitName = new Dictionary<string, MutableMercsArmyListEntry>(StringComparer.OrdinalIgnoreCase);
        for (var setIndex = 0; setIndex < resultSets.Length; setIndex++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var factionId = normalizedFactionIds[setIndex];
            var rows = resultSets[setIndex];
            unitLookupByFaction.TryGetValue(factionId, out var unitLookup);
            foreach (var row in rows)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (string.IsNullOrWhiteSpace(row.Name))
                {
                    continue;
                }

                var key = row.Name.Trim();
                if (!mergedByUnitName.TryGetValue(key, out var merged))
                {
                    var unitRecord = TryGetUnit(unitLookup, row.UnitId);
                    mergedByUnitName[key] = new MutableMercsArmyListEntry
                    {
                        Resume = CloneResume(row),
                        ProfileGroupsJson = unitRecord?.ProfileGroupsJson,
                        SourceFactionIds = new HashSet<int> { factionId }
                    };
                    continue;
                }

                merged.SourceFactionIds.Add(factionId);

                var incomingUnitRecord = TryGetUnit(unitLookup, row.UnitId);
                merged.ProfileGroupsJson = MergeProfileGroupsJson(merged.ProfileGroupsJson, incomingUnitRecord?.ProfileGroupsJson);
            }
        }

        var sorted = ArmyUnitSort
            .OrderByUnitTypeAndName(mergedByUnitName.Values, x => x.Resume.Type, x => x.Resume.Name)
            .ToList();

        return sorted
            .Select(x => new MercsArmyListEntry
            {
                Resume = x.Resume,
                ProfileGroupsJson = x.ProfileGroupsJson,
                SourceFactionIds = x.SourceFactionIds.OrderBy(id => id).ToArray()
            })
            .ToList();
    }

    private static Unit? TryGetUnit(IReadOnlyDictionary<int, Unit>? lookup, int unitId)
    {
        if (lookup is null || unitId <= 0 || !lookup.TryGetValue(unitId, out var row))
        {
            return null;
        }

        return row;
    }

    private static Resume CloneResume(Resume source)
    {
        return new Resume
        {
            ResumeKey = source.ResumeKey,
            FactionId = source.FactionId,
            UnitId = source.UnitId,
            IdArmy = source.IdArmy,
            Isc = source.Isc,
            Name = source.Name,
            Slug = source.Slug,
            Logo = source.Logo,
            Type = source.Type,
            Category = source.Category
        };
    }

    private static string? MergeProfileGroupsJson(string? currentJson, string? incomingJson)
    {
        if (string.IsNullOrWhiteSpace(currentJson))
        {
            return string.IsNullOrWhiteSpace(incomingJson) ? null : incomingJson;
        }

        if (string.IsNullOrWhiteSpace(incomingJson))
        {
            return currentJson;
        }

        JsonArray? currentGroups;
        JsonArray? incomingGroups;
        try
        {
            currentGroups = JsonNode.Parse(currentJson) as JsonArray;
            incomingGroups = JsonNode.Parse(incomingJson) as JsonArray;
        }
        catch
        {
            // Fall back to current payload if either JSON blob is malformed.
            return currentJson;
        }

        if (currentGroups is null)
        {
            return incomingJson;
        }

        if (incomingGroups is null)
        {
            return currentJson;
        }

        var mergedGroups = new JsonArray();
        var byGroupKey = new Dictionary<string, JsonObject>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in currentGroups)
        {
            if (node is not JsonObject group)
            {
                continue;
            }

            var clone = CloneJsonObject(group);
            var key = GetGroupKey(clone);
            byGroupKey[key] = clone;
            mergedGroups.Add(clone);
        }

        foreach (var node in incomingGroups)
        {
            if (node is not JsonObject incomingGroup)
            {
                continue;
            }

            var incomingClone = CloneJsonObject(incomingGroup);
            var key = GetGroupKey(incomingClone);
            if (!byGroupKey.TryGetValue(key, out var existingGroup))
            {
                byGroupKey[key] = incomingClone;
                mergedGroups.Add(incomingClone);
                continue;
            }

            MergeGroupProfiles(existingGroup, incomingClone);
            MergeGroupOptions(existingGroup, incomingClone);
        }

        return mergedGroups.ToJsonString();
    }

    private static void MergeGroupProfiles(JsonObject targetGroup, JsonObject incomingGroup)
    {
        var targetProfiles = targetGroup["profiles"] as JsonArray ?? new JsonArray();
        targetGroup["profiles"] = targetProfiles;

        var existingByKey = new Dictionary<string, JsonObject>(StringComparer.OrdinalIgnoreCase);
        foreach (var profileNode in targetProfiles)
        {
            if (profileNode is not JsonObject profile)
            {
                continue;
            }

            existingByKey[GetProfileKey(profile)] = profile;
        }

        if (incomingGroup["profiles"] is not JsonArray incomingProfiles)
        {
            return;
        }

        foreach (var profileNode in incomingProfiles)
        {
            if (profileNode is not JsonObject incomingProfile)
            {
                continue;
            }

            var clone = CloneJsonObject(incomingProfile);
            var key = GetProfileKey(clone);
            if (existingByKey.ContainsKey(key))
            {
                continue;
            }

            targetProfiles.Add(clone);
            existingByKey[key] = clone;
        }
    }

    private static void MergeGroupOptions(JsonObject targetGroup, JsonObject incomingGroup)
    {
        var targetOptions = targetGroup["options"] as JsonArray ?? new JsonArray();
        targetGroup["options"] = targetOptions;

        var existingHashes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var optionNode in targetOptions)
        {
            if (optionNode is null)
            {
                continue;
            }

            existingHashes.Add(optionNode.ToJsonString());
        }

        if (incomingGroup["options"] is not JsonArray incomingOptions)
        {
            return;
        }

        foreach (var optionNode in incomingOptions)
        {
            if (optionNode is null)
            {
                continue;
            }

            var serialized = optionNode.ToJsonString();
            if (!existingHashes.Add(serialized))
            {
                continue;
            }

            targetOptions.Add(optionNode.DeepClone());
        }
    }

    private static string GetGroupKey(JsonObject group)
    {
        var id = group["id"]?.ToString() ?? string.Empty;
        var name = group["name"]?.ToString() ?? string.Empty;
        return $"{id}:{name}".Trim().ToLowerInvariant();
    }

    private static string GetProfileKey(JsonObject profile)
    {
        var id = profile["id"]?.ToString() ?? string.Empty;
        var idArmy = profile["idArmy"]?.ToString() ?? string.Empty;
        var name = profile["name"]?.ToString() ?? string.Empty;
        return $"{id}:{idArmy}:{name}".Trim().ToLowerInvariant();
    }

    private static JsonObject CloneJsonObject(JsonObject source)
    {
        return JsonNode.Parse(source.ToJsonString()) as JsonObject ?? new JsonObject();
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

    private sealed class MutableMercsArmyListEntry
    {
        public required Resume Resume { get; init; }
        public string? ProfileGroupsJson { get; set; }
        public required HashSet<int> SourceFactionIds { get; init; }
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
                using var doc = JsonDocument.Parse(filtersJson);
                if (doc.RootElement.ValueKind != JsonValueKind.Object ||
                    !doc.RootElement.TryGetProperty(key, out var array) ||
                    array.ValueKind != JsonValueKind.Array)
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

        private static bool TryParseId(JsonElement element, out int id)
        {
            id = 0;
            if (element.TryGetProperty("id", out var idElement))
            {
                return TryParseInt(idElement, out id);
            }

            return false;
        }

        private static bool TryParseInt(JsonElement element, out int value)
        {
            value = 0;
            if (element.ValueKind == JsonValueKind.Number)
            {
                return element.TryGetInt32(out value);
            }

            if (element.ValueKind == JsonValueKind.String)
            {
                return int.TryParse(element.GetString(), out value);
            }

            return false;
        }
    }
}
