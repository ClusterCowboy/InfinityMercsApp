using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using InfinityMercsApp.Domain.Models.Army;
using InfinityMercsApp.Infrastructure.Repositories;
using DbMetadataFaction = InfinityMercsApp.Infrastructure.Models.Database.Metadata.Faction;

namespace InfinityMercsApp.Infrastructure.Providers;

/// <inheritdoc/>
public sealed class InspiringCompanyFactionGenerator(
    ISQLiteRepository sqliteRepository,
    IFactionProvider factionProvider,
    IArmyImportProvider armyImportProvider) : IInspiringCompanyFactionGenerator
{
    public const int InspiringCompanyFactionId = 2002;

    /// <inheritdoc/>
    public async Task GenerateAsync(CancellationToken cancellationToken = default)
    {
        Console.WriteLine("[InspiringGen] GenerateAsync started.");

        var factionIds = factionProvider.GetStoredFactionIds()
            .Where(id => id != InspiringCompanyFactionId)
            .ToList();

        if (factionIds.Count == 0)
        {
            Console.WriteLine("[InspiringGen] No stored factions found. Aborting.");
            return;
        }

        Console.WriteLine($"[InspiringGen] Found {factionIds.Count} source factions to scan.");

        var mergedUnits = new Dictionary<string, SyntheticUnitEntry>(StringComparer.OrdinalIgnoreCase);
        foreach (var factionId in factionIds)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var beforeCount = mergedUnits.Count;
            CollectQualifyingUnits(factionId, mergedUnits);
            var added = mergedUnits.Count - beforeCount;
            if (added > 0)
            {
                Console.WriteLine($"[InspiringGen] Faction {factionId}: +{added} new unit(s) (total: {mergedUnits.Count})");
            }
        }

        if (mergedUnits.Count == 0)
        {
            Console.WriteLine("[InspiringGen] No qualifying Irregular units found across all factions.");
            return;
        }

        Console.WriteLine($"[InspiringGen] Total qualifying units: {mergedUnits.Count}");
        foreach (var entry in mergedUnits.Values.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase).Take(10))
        {
            Console.WriteLine($"[InspiringGen]   - {entry.Name} (AVA={entry.MaxAva}, sources=[{string.Join(",", entry.SourceFactionIds)}])");
        }

        if (mergedUnits.Count > 10)
        {
            Console.WriteLine($"[InspiringGen]   ... and {mergedUnits.Count - 10} more");
        }

        var syntheticFaction = BuildSyntheticFaction(mergedUnits, factionIds);
        Console.WriteLine($"[InspiringGen] Built synthetic faction: {syntheticFaction.Units.Count} units, {syntheticFaction.Resume.Count} resumes");

        var newVersion = ComputeVersionHash(syntheticFaction);
        var existing = factionProvider.GetFactionSnapshot(InspiringCompanyFactionId);
        if (existing is not null && existing.Version == newVersion)
        {
            Console.WriteLine($"[InspiringGen] No changes detected (version {newVersion}), skipping reimport.");
            return;
        }

        Console.WriteLine($"[InspiringGen] Importing faction {InspiringCompanyFactionId} (version {newVersion}, previous: {existing?.Version ?? "none"})...");

        await armyImportProvider.ImportAsync(InspiringCompanyFactionId, syntheticFaction, cancellationToken);

        InsertMetadataFactionEntry();

        Console.WriteLine($"[InspiringGen] Successfully generated synthetic faction with {mergedUnits.Count} units (version {newVersion}).");
    }

    private void CollectQualifyingUnits(
        int factionId,
        Dictionary<string, SyntheticUnitEntry> mergedUnits)
    {
        var resumes = factionProvider.GetResumeByFactionMercsOnly(factionId);
        var units = factionProvider.GetUnitsByFaction(factionId);
        var unitLookup = units.ToDictionary(u => u.UnitId);

        var noNameCount = 0;
        var noUnitMatchCount = 0;
        var noProfileGroupsCount = 0;
        var noIrregularCount = 0;
        var qualifiedCount = 0;

        foreach (var resume in resumes)
        {
            if (string.IsNullOrWhiteSpace(resume.Name))
            {
                noNameCount++;
                continue;
            }

            if (!unitLookup.TryGetValue(resume.UnitId, out var unit))
            {
                noUnitMatchCount++;
                continue;
            }

            if (string.IsNullOrWhiteSpace(unit.ProfileGroupsJson))
            {
                noProfileGroupsCount++;
                continue;
            }

            var filteredJson = FilterForIrregularOrder(unit.ProfileGroupsJson);
            if (filteredJson is null)
            {
                noIrregularCount++;
                continue;
            }

            qualifiedCount++;
            var key = resume.Name.Trim();
            if (mergedUnits.TryGetValue(key, out var existing))
            {
                existing.SourceFactionIds.Add(factionId);
                var incomingMaxAva = GetMaxAva(filteredJson);
                if (incomingMaxAva > existing.MaxAva)
                {
                    existing.MaxAva = incomingMaxAva;
                    existing.ProfileGroupsJson = UpdateAvaInProfileGroupsJson(filteredJson, incomingMaxAva);
                }
            }
            else
            {
                var maxAva = GetMaxAva(filteredJson);
                mergedUnits[key] = new SyntheticUnitEntry
                {
                    Name = resume.Name,
                    Resume = resume,
                    ProfileGroupsJson = filteredJson,
                    OptionsJson = unit.OptionsJson,
                    FiltersJson = unit.FiltersJson,
                    Isc = unit.Isc,
                    IscAbbr = unit.IscAbbr,
                    Slug = unit.Slug,
                    MaxAva = maxAva,
                    SourceFactionId = factionId,
                    SourceUnitId = resume.UnitId,
                    SourceFactionIds = { factionId }
                };
            }
        }

        if (resumes.Count > 0)
        {
            Console.WriteLine($"[InspiringGen]   Faction {factionId}: {resumes.Count} resumes, {units.Count} units | "
                + $"noName={noNameCount}, noUnitMatch={noUnitMatchCount}, noProfileGroups={noProfileGroupsCount}, "
                + $"noIrregular={noIrregularCount}, qualified={qualifiedCount}");
        }
    }

    /// <summary>
    /// Filters profile groups to only include groups where at least one option
    /// has an Irregular order type. Also filters out options with positive SWC.
    /// </summary>
    private static string? FilterForIrregularOrder(string profileGroupsJson)
    {
        JsonArray? groups;
        try
        {
            groups = JsonNode.Parse(profileGroupsJson) as JsonArray;
        }
        catch
        {
            return null;
        }

        if (groups is null || groups.Count == 0)
        {
            return null;
        }

        var result = new JsonArray();
        foreach (var node in groups)
        {
            if (node is not JsonObject group)
            {
                continue;
            }

            var options = group["options"] as JsonArray;
            if (options is null || options.Count == 0)
            {
                continue;
            }

            // Check if any option in this group has an Irregular order.
            if (!GroupHasIrregularOption(options))
            {
                continue;
            }

            // Keep only options that have Irregular order and no positive SWC.
            var qualifyingOptions = new JsonArray();
            foreach (var optNode in options)
            {
                if (optNode is not JsonObject option)
                {
                    continue;
                }

                if (!OptionHasIrregularOrder(option))
                {
                    continue;
                }

                var swcValue = option["swc"];
                if (swcValue is not null && IsPositiveSwc(swcValue.ToString()))
                {
                    continue;
                }

                qualifyingOptions.Add(option.DeepClone());
            }

            if (qualifyingOptions.Count == 0)
            {
                continue;
            }

            var filteredGroup = group.DeepClone() as JsonObject;
            if (filteredGroup is null)
            {
                continue;
            }

            filteredGroup["options"] = qualifyingOptions;
            result.Add(filteredGroup);
        }

        return result.Count > 0 ? result.ToJsonString() : null;
    }

    private static bool GroupHasIrregularOption(JsonArray options)
    {
        foreach (var optNode in options)
        {
            if (optNode is JsonObject option && OptionHasIrregularOrder(option))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Checks if an option has an order entry with type "IRREGULAR".
    /// Order structure: option.orders[] where each order has a "type" string.
    /// </summary>
    private static bool OptionHasIrregularOrder(JsonObject option)
    {
        var orders = option["orders"] as JsonArray;
        if (orders is null || orders.Count == 0)
        {
            return false;
        }

        foreach (var orderNode in orders)
        {
            if (orderNode is not JsonObject order)
            {
                continue;
            }

            var typeNode = order["type"];
            if (typeNode is null)
            {
                continue;
            }

            var typeStr = typeNode.ToString();
            if (string.Equals(typeStr, "IRREGULAR", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPositiveSwc(string? swc)
    {
        if (string.IsNullOrWhiteSpace(swc) || swc == "-")
        {
            return false;
        }

        return decimal.TryParse(
                   swc,
                   NumberStyles.Number,
                   CultureInfo.InvariantCulture,
                   out var value)
               && value > 0m;
    }

    private static int GetMaxAva(string profileGroupsJson)
    {
        var maxAva = 0;
        try
        {
            var groups = JsonNode.Parse(profileGroupsJson) as JsonArray;
            if (groups is null)
            {
                return maxAva;
            }

            foreach (var node in groups)
            {
                if (node is not JsonObject group)
                {
                    continue;
                }

                var profiles = group["profiles"] as JsonArray;
                if (profiles is null)
                {
                    continue;
                }

                foreach (var profileNode in profiles)
                {
                    if (profileNode is not JsonObject profile)
                    {
                        continue;
                    }

                    var avaNode = profile["ava"];
                    if (avaNode is null)
                    {
                        continue;
                    }

                    if (avaNode is JsonValue avaValue)
                    {
                        if (avaValue.TryGetValue(out int intAva) && intAva > maxAva)
                        {
                            maxAva = intAva;
                        }
                        else if (avaValue.TryGetValue(out string? strAva) &&
                                 int.TryParse(strAva, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedAva) &&
                                 parsedAva > maxAva)
                        {
                            maxAva = parsedAva;
                        }
                    }
                }
            }
        }
        catch
        {
            // Ignore malformed JSON.
        }

        return maxAva;
    }

    private static string UpdateAvaInProfileGroupsJson(string profileGroupsJson, int maxAva)
    {
        try
        {
            var groups = JsonNode.Parse(profileGroupsJson) as JsonArray;
            if (groups is null)
            {
                return profileGroupsJson;
            }

            foreach (var node in groups)
            {
                if (node is not JsonObject group)
                {
                    continue;
                }

                var profiles = group["profiles"] as JsonArray;
                if (profiles is null)
                {
                    continue;
                }

                foreach (var profileNode in profiles)
                {
                    if (profileNode is not JsonObject profile)
                    {
                        continue;
                    }

                    profile["ava"] = maxAva;
                }
            }

            return groups.ToJsonString();
        }
        catch
        {
            return profileGroupsJson;
        }
    }

    private ArmyImportFaction BuildSyntheticFaction(
        Dictionary<string, SyntheticUnitEntry> mergedUnits,
        IReadOnlyList<int> sourceFactionIds)
    {
        var mergedFiltersJson = BuildMergedFiltersJson(sourceFactionIds);

        var syntheticUnits = new List<ArmyImportUnit>();
        var syntheticResumes = new List<ArmyImportResume>();
        var unitId = 1;

        foreach (var entry in mergedUnits.Values.OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            var finalJson = UpdateAvaInProfileGroupsJson(entry.ProfileGroupsJson, entry.MaxAva);

            var sourceMetadata = new JsonObject
            {
                ["sourceFactionId"] = entry.SourceFactionId,
                ["sourceUnitId"] = entry.SourceUnitId,
                ["sourceFactionIds"] = new JsonArray(entry.SourceFactionIds.Order().Select(id => (JsonNode)JsonValue.Create(id)).ToArray())
            };
            var factionsJson = sourceMetadata.ToJsonString();

            syntheticUnits.Add(new ArmyImportUnit
            {
                Id = unitId,
                IdArmy = null,
                Canonical = null,
                Isc = entry.Isc,
                IscAbbr = entry.IscAbbr,
                Name = entry.Name,
                Slug = entry.Slug,
                ProfileGroupsJson = finalJson,
                OptionsJson = entry.OptionsJson,
                FiltersJson = entry.FiltersJson,
                FactionsJson = factionsJson
            });

            syntheticResumes.Add(new ArmyImportResume
            {
                Id = unitId,
                IdArmy = entry.Resume.IdArmy,
                Isc = entry.Resume.Isc,
                Name = entry.Resume.Name,
                Slug = entry.Resume.Slug,
                Logo = $"{entry.SourceFactionId}-{entry.SourceUnitId}",
                Type = entry.Resume.Type,
                Category = entry.Resume.Category
            });

            unitId++;
        }

        return new ArmyImportFaction
        {
            Version = string.Empty,
            Units = syntheticUnits,
            Resume = syntheticResumes,
            FiltersJson = mergedFiltersJson,
            ReinforcementsJson = null,
            FireteamsJson = null,
            RelationsJson = null,
            SpecopsJson = null,
            FireteamChartJson = null,
            RawJson = string.Empty
        };
    }

    private string? BuildMergedFiltersJson(IReadOnlyList<int> sourceFactionIds)
    {
        var mergedType = new Dictionary<int, JsonObject>();
        var mergedChars = new Dictionary<int, JsonObject>();
        var mergedSkills = new Dictionary<int, JsonObject>();
        var mergedEquip = new Dictionary<int, JsonObject>();
        var mergedWeapons = new Dictionary<int, JsonObject>();
        var mergedAmmo = new Dictionary<int, JsonObject>();
        var mergedCategory = new Dictionary<int, JsonObject>();
        var mergedPeripheral = new Dictionary<int, JsonObject>();
        var mergedExtras = new Dictionary<int, JsonObject>();

        foreach (var factionId in sourceFactionIds)
        {
            var snapshot = factionProvider.GetFactionSnapshot(factionId);
            if (string.IsNullOrWhiteSpace(snapshot?.FiltersJson))
            {
                continue;
            }

            try
            {
                using var doc = JsonDocument.Parse(snapshot.FiltersJson);
                if (doc.RootElement.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                MergeFilterSection(doc.RootElement, "type", mergedType);
                MergeFilterSection(doc.RootElement, "chars", mergedChars);
                MergeFilterSection(doc.RootElement, "skills", mergedSkills);
                MergeFilterSection(doc.RootElement, "equip", mergedEquip);
                MergeFilterSection(doc.RootElement, "weapons", mergedWeapons);
                MergeFilterSection(doc.RootElement, "ammunition", mergedAmmo);
                MergeFilterSection(doc.RootElement, "category", mergedCategory);
                MergeFilterSection(doc.RootElement, "peripheral", mergedPeripheral);
                MergeFilterSection(doc.RootElement, "extras", mergedExtras);
            }
            catch
            {
                // Skip malformed filters.
            }
        }

        var result = new JsonObject
        {
            ["type"] = ToJsonArray(mergedType),
            ["chars"] = ToJsonArray(mergedChars),
            ["skills"] = ToJsonArray(mergedSkills),
            ["equip"] = ToJsonArray(mergedEquip),
            ["weapons"] = ToJsonArray(mergedWeapons),
            ["ammunition"] = ToJsonArray(mergedAmmo),
            ["category"] = ToJsonArray(mergedCategory),
            ["peripheral"] = ToJsonArray(mergedPeripheral),
            ["extras"] = ToJsonArray(mergedExtras)
        };

        return result.ToJsonString();
    }

    private static void MergeFilterSection(JsonElement root, string key, Dictionary<int, JsonObject> target)
    {
        if (!root.TryGetProperty(key, out var array) || array.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (!item.TryGetProperty("id", out var idElement))
            {
                continue;
            }

            int id;
            if (idElement.ValueKind == JsonValueKind.Number && idElement.TryGetInt32(out var intId))
            {
                id = intId;
            }
            else if (idElement.ValueKind == JsonValueKind.String &&
                     int.TryParse(idElement.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedId))
            {
                id = parsedId;
            }
            else
            {
                continue;
            }

            if (target.ContainsKey(id))
            {
                continue;
            }

            target[id] = JsonNode.Parse(item.GetRawText()) as JsonObject ?? new JsonObject();
        }
    }

    private static JsonArray ToJsonArray(Dictionary<int, JsonObject> items)
    {
        var array = new JsonArray();
        foreach (var item in items.Values)
        {
            array.Add(item.DeepClone());
        }

        return array;
    }

    private static string ComputeVersionHash(ArmyImportFaction faction)
    {
        var sb = new StringBuilder();

        foreach (var unit in faction.Units.OrderBy(u => u.Name, StringComparer.OrdinalIgnoreCase))
        {
            sb.Append(unit.Name);
            sb.Append('|');
            sb.Append(unit.ProfileGroupsJson ?? string.Empty);
            sb.Append('|');
        }

        foreach (var resume in faction.Resume.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase))
        {
            sb.Append(resume.Name);
            sb.Append('|');
            sb.Append(resume.Type?.ToString() ?? string.Empty);
            sb.Append('|');
        }

        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(sb.ToString()));
        return Convert.ToHexString(bytes)[..16].ToLowerInvariant();
    }

    private void InsertMetadataFactionEntry()
    {
        sqliteRepository.Delete<DbMetadataFaction>(x => x.Id == InspiringCompanyFactionId);
        sqliteRepository.Insert(new[]
        {
            new DbMetadataFaction
            {
                Id = InspiringCompanyFactionId,
                ParentId = InspiringCompanyFactionId,
                Name = "Inspiring Company",
                Slug = "inspiring-company",
                Discontinued = false,
                Logo = "SVGCache/MercsIcons/noun-leadership-7195245.svg"
            }
        });
    }

    private sealed class SyntheticUnitEntry
    {
        public required string Name { get; init; }
        public required Resume Resume { get; init; }
        public required string ProfileGroupsJson { get; set; }
        public required string? OptionsJson { get; init; }
        public required string? FiltersJson { get; init; }
        public string? Isc { get; init; }
        public string? IscAbbr { get; init; }
        public string? Slug { get; init; }
        public int MaxAva { get; set; }
        public int SourceFactionId { get; init; }
        public int SourceUnitId { get; init; }
        public HashSet<int> SourceFactionIds { get; init; } = [];
    }
}
