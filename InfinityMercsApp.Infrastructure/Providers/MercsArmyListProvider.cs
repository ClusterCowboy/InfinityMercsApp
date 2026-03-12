using InfinityMercsApp.Domain.Sorting;
using InfinityMercsApp.Infrastructure.Models.Database.Army;
using System.Text.Json.Nodes;

namespace InfinityMercsApp.Infrastructure.Providers;

/// <inheritdoc/>
public sealed class MercsArmyListProvider(IFactionProvider factionProvider) : IMercsArmyListProvider
{
    /// <inheritdoc/>
    public IReadOnlyList<MercsArmyListEntry> GetMergedMercsArmyList(
        int factionId)
    {
        return GetMergedMercsArmyList([factionId]);
    }

    /// <inheritdoc/>
    public IReadOnlyList<MercsArmyListEntry> GetMergedMercsArmyList(
        int firstFactionId,
        int secondFactionId)
    {
        return GetMergedMercsArmyList([firstFactionId, secondFactionId]);
    }

    /// <inheritdoc/>
    public IReadOnlyList<MercsArmyListEntry> GetMergedMercsArmyList(IReadOnlyCollection<int> factionIds)
    {
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
            .Select(id => factionProvider.GetResumeByFactionMercsOnly(id))
            .ToArray();

        var unitLookupByFaction = new Dictionary<int, IReadOnlyDictionary<int, Unit>>();

        for (var setIndex = 0; setIndex < resultSets.Length; setIndex++)
        {
            var factionId = normalizedFactionIds[setIndex];
            var unitIds = resultSets[setIndex]
                .Select(x => x.UnitId)
                .Where(id => id > 0)
                .Distinct()
                .ToArray();
            unitLookupByFaction[factionId] = factionProvider.GetUnitsByFactionAndIds(
                factionId,
                unitIds);
        }

        var mergedByUnitName = new Dictionary<string, MutableMercsArmyListEntry>(StringComparer.OrdinalIgnoreCase);
        for (var setIndex = 0; setIndex < resultSets.Length; setIndex++)
        {
            var factionId = normalizedFactionIds[setIndex];
            var rows = resultSets[setIndex];
            unitLookupByFaction.TryGetValue(factionId, out var unitLookup);
            foreach (var row in rows)
            {
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

    /// <inheritdoc/>
    public IReadOnlyList<Resume> GetResumeByFactionMercsOnly(int factionId)
    {
        return GetMergedMercsArmyList(factionId)
            .Select(x => x.Resume)
            .ToList();
    }

    /// <inheritdoc/>
    public IReadOnlyList<Resume> GetResumeByFactionMercsOnly(
        int firstFactionId,
        int secondFactionId)
    {
        return GetMergedMercsArmyList(firstFactionId, secondFactionId)
            .Select(x => x.Resume)
            .ToList();
    }

    public IReadOnlyList<Resume> GetResumeByFactionMercsOnly(IReadOnlyCollection<int> factionIds)
    {
        return GetMergedMercsArmyList(factionIds)
            .Select(x => x.Resume)
            .ToList();
    }

    // TODO: Probably best done using IClonable or similar
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

            var incomingClone = CloneJsonObject(incomingProfile);
            var key = GetProfileKey(incomingClone);
            if (!existingByKey.TryGetValue(key, out var existingProfile))
            {
                existingByKey[key] = incomingClone;
                targetProfiles.Add(incomingClone);
                continue;
            }

            var existingAva = TryReadInt(existingProfile["ava"]);
            var incomingAva = TryReadInt(incomingClone["ava"]);
            var maxAva = Math.Max(existingAva, incomingAva);
            if (maxAva > 0)
            {
                existingProfile["ava"] = maxAva;
            }
        }
    }

    private static void MergeGroupOptions(JsonObject targetGroup, JsonObject incomingGroup)
    {
        var targetOptions = targetGroup["options"] as JsonArray ?? new JsonArray();
        targetGroup["options"] = targetOptions;

        var optionKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var optionNode in targetOptions)
        {
            if (optionNode is JsonObject option)
            {
                optionKeys.Add(GetOptionKey(option));
            }
        }

        if (incomingGroup["options"] is not JsonArray incomingOptions)
        {
            return;
        }

        foreach (var optionNode in incomingOptions)
        {
            if (optionNode is not JsonObject incomingOption)
            {
                continue;
            }

            var incomingClone = CloneJsonObject(incomingOption);
            var key = GetOptionKey(incomingClone);
            if (!optionKeys.Add(key))
            {
                continue;
            }

            targetOptions.Add(incomingClone);
        }
    }

    private static string GetGroupKey(JsonObject group)
    {
        var id = TryReadInt(group["id"]);
        if (id > 0)
        {
            return $"id:{id}";
        }

        var isc = group["isc"]?.GetValue<string>()?.Trim() ?? string.Empty;
        var category = TryReadInt(group["category"]);
        return $"isc:{isc}|cat:{category}";
    }

    private static string GetProfileKey(JsonObject profile)
    {
        var id = TryReadInt(profile["id"]);
        if (id > 0)
        {
            return $"id:{id}";
        }

        var name = profile["name"]?.GetValue<string>()?.Trim() ?? string.Empty;
        return $"name:{name}";
    }

    private static string GetOptionKey(JsonObject option)
    {
        var id = TryReadInt(option["id"]);
        if (id > 0)
        {
            return $"id:{id}";
        }

        var name = option["name"]?.GetValue<string>()?.Trim() ?? string.Empty;
        var swc = option["swc"]?.ToJsonString() ?? string.Empty;
        var points = option["points"]?.ToJsonString() ?? string.Empty;
        var weapons = BuildIdArraySignature(option["weapons"] as JsonArray);
        return $"name:{name}|swc:{swc}|pts:{points}|w:{weapons}";
    }

    private static string BuildIdArraySignature(JsonArray? array)
    {
        if (array is null || array.Count == 0)
        {
            return string.Empty;
        }

        var ids = new List<int>();
        foreach (var node in array)
        {
            if (node is not JsonObject item)
            {
                continue;
            }

            var id = TryReadInt(item["id"]);
            if (id > 0)
            {
                ids.Add(id);
            }
        }

        ids.Sort();
        return string.Join(",", ids);
    }

    private static int TryReadInt(JsonNode? node)
    {
        if (node is null)
        {
            return 0;
        }

        if (node is JsonValue value)
        {
            if (value.TryGetValue<int>(out var intValue))
            {
                return intValue;
            }

            if (value.TryGetValue<string>(out var stringValue) &&
                int.TryParse(stringValue, out var parsed))
            {
                return parsed;
            }
        }

        return 0;
    }

    private static JsonObject CloneJsonObject(JsonObject source)
    {
        return JsonNode.Parse(source.ToJsonString())!.AsObject();
    }

    private static Unit? TryGetUnit(IReadOnlyDictionary<int, Unit>? unitLookup, int unitId)
    {
        if (unitLookup is null || unitId <= 0)
        {
            return null;
        }

        return unitLookup.TryGetValue(unitId, out var row) ? row : null;
    }

    // TODO: Punt this to another file for clarity's sake.
    private sealed class MutableMercsArmyListEntry
    {
        public required Resume Resume { get; init; }

        public string? ProfileGroupsJson { get; set; }

        public required HashSet<int> SourceFactionIds { get; init; }
    }
}
