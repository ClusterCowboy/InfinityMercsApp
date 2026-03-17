using System.Text.Json;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Common;

namespace InfinityMercsApp.Services;

internal sealed class CompanyUnitDetailDisplayNameContext
{
    internal delegate bool TryParseIdDelegate(JsonElement element, out int id);

    private readonly IReadOnlyDictionary<int, ExtraDefinition> _extrasLookup;
    private readonly bool _showUnitsInInches;
    private readonly TryParseIdDelegate _tryParseId;

    /// <summary>
    /// Creates a reusable context for equipment/skill display-name calculations.
    /// </summary>
    public static CompanyUnitDetailDisplayNameContext Create(
        string? filtersJson,
        bool showUnitsInInches,
        TryParseIdDelegate tryParseId)
    {
        return new CompanyUnitDetailDisplayNameContext(
            BuildExtrasLookup(filtersJson, tryParseId),
            showUnitsInInches,
            tryParseId);
    }

    private CompanyUnitDetailDisplayNameContext(
        IReadOnlyDictionary<int, ExtraDefinition> extrasLookup,
        bool showUnitsInInches,
        TryParseIdDelegate tryParseId)
    {
        _extrasLookup = extrasLookup;
        _showUnitsInInches = showUnitsInInches;
        _tryParseId = tryParseId;
    }

    /// <summary>
    /// Resolves ordered unique display names from option/profile entry arrays.
    /// </summary>
    public List<string> GetOrderedIdDisplayNamesFromEntries(
        IEnumerable<JsonElement> entries,
        IReadOnlyDictionary<int, string> lookup)
    {
        var names = new List<string>();
        foreach (var entry in entries)
        {
            if (!_tryParseId(entry, out var id))
            {
                continue;
            }

            var baseName = lookup.TryGetValue(id, out var resolvedName) ? resolvedName : id.ToString();
            names.Add(BuildEntryDisplayName(baseName, entry));
        }

        return names
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Resolves ordered display names while aggregating quantities.
    /// </summary>
    public List<string> GetCountedDisplayNamesFromEntries(
        IEnumerable<JsonElement> entries,
        IReadOnlyDictionary<int, string> lookup)
    {
        var counts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entries)
        {
            if (!_tryParseId(entry, out var id))
            {
                continue;
            }

            var baseName = lookup.TryGetValue(id, out var resolvedName) ? resolvedName : id.ToString();
            var displayName = BuildEntryDisplayName(baseName, entry);
            if (string.IsNullOrWhiteSpace(displayName))
            {
                continue;
            }

            var quantity = CompanyProfileOptionService.ReadEntryQuantity(entry);
            counts[displayName] = counts.TryGetValue(displayName, out var existing)
                ? existing + quantity
                : quantity;
        }

        return counts
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => $"{x.Key} ({x.Value})")
            .ToList();
    }

    /// <summary>
    /// Computes display names common to all profiles for a property (for example equip/skills).
    /// </summary>
    public List<string> ComputeCommonDisplayNamesFromProfiles(
        string? profileGroupsJson,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup)
    {
        if (string.IsNullOrWhiteSpace(profileGroupsJson))
        {
            return [];
        }

        HashSet<string>? common = null;
        try
        {
            using var doc = JsonDocument.Parse(profileGroupsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            foreach (var group in doc.RootElement.EnumerateArray())
            {
                if (!group.TryGetProperty("profiles", out var profilesElement) || profilesElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var profile in profilesElement.EnumerateArray())
                {
                    var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (profile.TryGetProperty(propertyName, out var arr) && arr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var entry in arr.EnumerateArray())
                        {
                            if (!_tryParseId(entry, out var id))
                            {
                                continue;
                            }

                            var baseName = lookup.TryGetValue(id, out var resolvedName) ? resolvedName : id.ToString();
                            set.Add(BuildEntryDisplayName(baseName, entry));
                        }
                    }

                    if (common is null)
                    {
                        common = set;
                    }
                    else
                    {
                        common.IntersectWith(set);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage ComputeCommonDisplayNamesFromProfiles failed for '{propertyName}': {ex.Message}");
            return [];
        }

        if (common is null || common.Count == 0)
        {
            return [];
        }

        return common
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Computes display names common to the provided options after include expansion.
    /// </summary>
    public List<string> IntersectDisplayNamesWithIncludes(
        JsonElement profileGroupsRoot,
        IReadOnlyList<JsonElement> options,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup)
    {
        HashSet<string>? intersection = null;
        foreach (var option in options)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in CompanyProfileOptionService.GetOptionEntriesWithIncludes(profileGroupsRoot, option, propertyName))
            {
                if (!_tryParseId(entry, out var id))
                {
                    continue;
                }

                var baseName = lookup.TryGetValue(id, out var resolvedName) ? resolvedName : id.ToString();
                names.Add(BuildEntryDisplayName(baseName, entry));
            }

            if (intersection is null)
            {
                intersection = names;
            }
            else
            {
                intersection.IntersectWith(names);
            }
        }

        if (intersection is null || intersection.Count == 0)
        {
            return [];
        }

        return intersection
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>
    /// Loads the extras lookup used to format entry suffixes.
    /// </summary>
    private static Dictionary<int, ExtraDefinition> BuildExtrasLookup(
        string? filtersJson,
        TryParseIdDelegate tryParseId)
    {
        var map = new Dictionary<int, ExtraDefinition>();
        if (string.IsNullOrWhiteSpace(filtersJson))
        {
            return map;
        }

        try
        {
            using var doc = JsonDocument.Parse(filtersJson);
            if (!doc.RootElement.TryGetProperty("extras", out var section) || section.ValueKind != JsonValueKind.Array)
            {
                return map;
            }

            foreach (var entry in section.EnumerateArray())
            {
                if (!entry.TryGetProperty("id", out var idElement) || !tryParseId(idElement, out var id))
                {
                    continue;
                }

                if (!entry.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var name = nameElement.GetString();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var type = entry.TryGetProperty("type", out var typeElement) && typeElement.ValueKind == JsonValueKind.String
                    ? (typeElement.GetString() ?? string.Empty)
                    : string.Empty;

                map[id] = new ExtraDefinition(name, type);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage BuildExtrasLookup failed: {ex.Message}");
        }

        return map;
    }

    /// <summary>
    /// Builds an entry display string with any formatted extras.
    /// </summary>
    private string BuildEntryDisplayName(string baseName, JsonElement entry)
    {
        if (entry.ValueKind != JsonValueKind.Object)
        {
            return baseName;
        }

        if (!entry.TryGetProperty("extra", out var extraElement) || extraElement.ValueKind != JsonValueKind.Array)
        {
            return baseName;
        }

        var extras = new List<string>();
        foreach (var extraEntry in extraElement.EnumerateArray())
        {
            if (!_tryParseId(extraEntry, out var extraId))
            {
                continue;
            }

            if (_extrasLookup.TryGetValue(extraId, out var definition) && !string.IsNullOrWhiteSpace(definition.Name))
            {
                extras.Add(FormatExtraDisplay(definition));
            }
            else
            {
                extras.Add(extraId.ToString());
            }
        }

        var distinctExtras = extras
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return distinctExtras.Count == 0
            ? baseName
            : $"{baseName} ({string.Join(", ", distinctExtras)})";
    }

    /// <summary>
    /// Formats an extra value for display, including range-unit conversion for distances.
    /// </summary>
    private string FormatExtraDisplay(ExtraDefinition definition)
    {
        if (!string.Equals(definition.Type, "DISTANCE", StringComparison.OrdinalIgnoreCase))
        {
            return definition.Name;
        }

        return CompanySelectionSharedUtilities.ConvertDistanceText(definition.Name, _showUnitsInInches);
    }

    private readonly record struct ExtraDefinition(string Name, string Type);
}



