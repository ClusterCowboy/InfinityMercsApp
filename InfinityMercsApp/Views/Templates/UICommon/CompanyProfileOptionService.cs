using System.Globalization;
using System.Text.Json;
using InfinityMercsApp.Views.Templates.NewCompany;

namespace InfinityMercsApp.Views.Templates.UICommon;

public static class CompanyProfileOptionService
{
    /// <summary>
    /// Reads SWC from an option as normalized text.
    /// </summary>
    public static string ReadOptionSwc(JsonElement option)
    {
        if (option.TryGetProperty("swc", out var swcElement))
        {
            if (swcElement.ValueKind == JsonValueKind.Number && swcElement.TryGetDecimal(out var swcNumber))
            {
                return swcNumber.ToString(CultureInfo.InvariantCulture);
            }

            if (swcElement.ValueKind == JsonValueKind.String)
            {
                return swcElement.GetString() ?? "-";
            }
        }

        return "-";
    }

    /// <summary>
    /// Reads option cost from points/cost/pts fields.
    /// </summary>
    public static string ReadOptionCost(JsonElement option)
    {
        if (option.TryGetProperty("points", out var pointsElement))
        {
            if (pointsElement.ValueKind == JsonValueKind.Number && pointsElement.TryGetInt32(out var intCost))
            {
                return intCost.ToString(CultureInfo.InvariantCulture);
            }

            if (pointsElement.ValueKind == JsonValueKind.String)
            {
                var points = pointsElement.GetString();
                return string.IsNullOrWhiteSpace(points) ? "-" : points;
            }
        }

        if (option.TryGetProperty("cost", out var costElement))
        {
            if (costElement.ValueKind == JsonValueKind.Number && costElement.TryGetInt32(out var costNumber))
            {
                return costNumber.ToString(CultureInfo.InvariantCulture);
            }

            if (costElement.ValueKind == JsonValueKind.String)
            {
                var cost = costElement.GetString();
                return string.IsNullOrWhiteSpace(cost) ? "-" : cost;
            }
        }

        if (option.TryGetProperty("pts", out var ptsElement))
        {
            if (ptsElement.ValueKind == JsonValueKind.Number && ptsElement.TryGetInt32(out var ptsNumber))
            {
                return ptsNumber.ToString(CultureInfo.InvariantCulture);
            }

            if (ptsElement.ValueKind == JsonValueKind.String)
            {
                var points = ptsElement.GetString();
                return string.IsNullOrWhiteSpace(points) ? "-" : points;
            }
        }

        return "-";
    }

    /// <summary>
    /// Adjusts cost when duplicated peripherals are collapsed for display.
    /// </summary>
    public static string ReadAdjustedOptionCost(JsonElement profileGroupsRoot, JsonElement group, JsonElement option)
    {
        var baseCostText = ReadOptionCost(option);
        if (!int.TryParse(baseCostText, NumberStyles.Integer, CultureInfo.InvariantCulture, out var baseCost))
        {
            return baseCostText;
        }

        var totalPeripheralCount = GetDisplayPeripheralEntriesForOption(profileGroupsRoot, group, option)
            .Sum(ReadEntryQuantity);
        if (totalPeripheralCount <= 1)
        {
            return baseCostText;
        }

        var minis = ReadOptionMinis(option);
        if (minis <= 1 || minis <= totalPeripheralCount)
        {
            return baseCostText;
        }

        if (baseCost <= 0 || baseCost % minis != 0)
        {
            return baseCostText;
        }

        var removedPeripheralCount = totalPeripheralCount - 1;
        var perModelCost = baseCost / minis;
        var deduction = removedPeripheralCount * perModelCost;
        var adjustedCost = Math.Max(0, baseCost - deduction);
        return adjustedCost.ToString(CultureInfo.InvariantCulture);
    }

    /// <summary>
    /// Reads the minis/model count declared on an option.
    /// </summary>
    public static int ReadOptionMinis(JsonElement option)
    {
        if (!option.TryGetProperty("minis", out var minisElement))
        {
            return 0;
        }

        if (minisElement.ValueKind == JsonValueKind.Number && minisElement.TryGetInt32(out var minisNumber))
        {
            return Math.Max(0, minisNumber);
        }

        if (minisElement.ValueKind == JsonValueKind.String &&
            int.TryParse(minisElement.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var minisText))
        {
            return Math.Max(0, minisText);
        }

        return 0;
    }

    /// <summary>
    /// Reads entry quantity, defaulting to one.
    /// </summary>
    public static int ReadEntryQuantity(JsonElement entry)
    {
        if (entry.ValueKind != JsonValueKind.Object)
        {
            return 1;
        }

        if (entry.TryGetProperty("q", out var quantityElement))
        {
            if (quantityElement.ValueKind == JsonValueKind.Number && quantityElement.TryGetInt32(out var quantityNumber))
            {
                return Math.Max(1, quantityNumber);
            }

            if (quantityElement.ValueKind == JsonValueKind.String &&
                int.TryParse(quantityElement.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out var quantityText))
            {
                return Math.Max(1, quantityText);
            }
        }

        return 1;
    }

    /// <summary>
    /// Returns peripheral entries for an option, with controller fallback.
    /// </summary>
    public static IReadOnlyList<JsonElement> GetDisplayPeripheralEntriesForOption(
        JsonElement profileGroupsRoot,
        JsonElement group,
        JsonElement option)
    {
        var optionEntries = GetFilteredOptionPeripheralEntries(profileGroupsRoot, group, option).ToList();
        if (optionEntries.Count > 0)
        {
            return optionEntries;
        }

        return GetControllerPeripheralEntries(group).ToList();
    }

    /// <summary>
    /// Detects whether a profile group represents a controller/peripheral group.
    /// </summary>
    public static bool IsControllerGroup(JsonElement profileGroupsRoot, JsonElement group)
    {
        if (GetControllerPeripheralIds(group).Count > 0)
        {
            return true;
        }

        if (!group.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var option in optionsElement.EnumerateArray())
        {
            if (GetOptionEntriesWithIncludes(profileGroupsRoot, option, "peripheral").Any())
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Collects peripheral entries directly from group profiles.
    /// </summary>
    private static IEnumerable<JsonElement> GetControllerPeripheralEntries(JsonElement group)
    {
        if (!group.TryGetProperty("profiles", out var profilesElement) || profilesElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var collected = new List<JsonElement>();
        foreach (var profile in profilesElement.EnumerateArray())
        {
            if (profile.TryGetProperty("peripheral", out var peripheralElement) &&
                peripheralElement.ValueKind == JsonValueKind.Array &&
                peripheralElement.GetArrayLength() > 0)
            {
                collected.AddRange(peripheralElement.EnumerateArray().ToList());
            }
        }

        return collected;
    }

    /// <summary>
    /// Builds the set of controller peripheral ids for filtering option peripherals.
    /// </summary>
    private static HashSet<int> GetControllerPeripheralIds(JsonElement group)
    {
        var ids = new HashSet<int>();
        foreach (var entry in GetControllerPeripheralEntries(group))
        {
            if (TryParseId(entry, out var id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    /// <summary>
    /// Filters option peripheral entries to controller-supported ids when present.
    /// </summary>
    private static IEnumerable<JsonElement> GetFilteredOptionPeripheralEntries(
        JsonElement profileGroupsRoot,
        JsonElement group,
        JsonElement option)
    {
        var allowedIds = GetControllerPeripheralIds(group);
        var optionEntries = GetOptionEntriesWithIncludes(profileGroupsRoot, option, "peripheral").ToList();

        if (allowedIds.Count == 0)
        {
            return optionEntries;
        }

        return optionEntries
            .Where(entry => TryParseId(entry, out var id) && allowedIds.Contains(id))
            .ToList();
    }

    /// <summary>
    /// Returns option property entries plus recursively included option entries.
    /// </summary>
    public static IEnumerable<JsonElement> GetOptionEntriesWithIncludes(
        JsonElement profileGroupsRoot,
        JsonElement option,
        string propertyName)
    {
        var collected = new List<JsonElement>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectOptionEntriesWithIncludes(profileGroupsRoot, option, propertyName, collected, visited, null);
        return collected;
    }

    /// <summary>
    /// Recursively collects entries across include references while preventing cycles.
    /// </summary>
    private static void CollectOptionEntriesWithIncludes(
        JsonElement profileGroupsRoot,
        JsonElement option,
        string propertyName,
        List<JsonElement> target,
        HashSet<string> visited,
        (int GroupId, int OptionId)? includeRef)
    {
        var key = includeRef.HasValue
            ? $"{includeRef.Value.GroupId}:{includeRef.Value.OptionId}"
            : option.GetRawText().GetHashCode().ToString();
        if (!visited.Add(key))
        {
            return;
        }

        if (option.TryGetProperty(propertyName, out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in arr.EnumerateArray())
            {
                target.Add(entry);
            }
        }

        if (!option.TryGetProperty("includes", out var includesElement) || includesElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var include in includesElement.EnumerateArray())
        {
            if (include.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (!TryParseIncludeReference(include, out var includeGroupId, out var includeOptionId))
            {
                continue;
            }

            var includedOption = FindIncludedOption(profileGroupsRoot, includeGroupId, includeOptionId);
            if (includedOption.HasValue)
            {
                CollectOptionEntriesWithIncludes(
                    profileGroupsRoot,
                    includedOption.Value,
                    propertyName,
                    target,
                    visited,
                    (includeGroupId, includeOptionId));
            }
        }
    }

    /// <summary>
    /// Parses include group/option ids from an include object.
    /// </summary>
    private static bool TryParseIncludeReference(JsonElement include, out int groupId, out int optionId)
    {
        groupId = 0;
        optionId = 0;

        if (!include.TryGetProperty("group", out var groupElement) || !TryParseId(groupElement, out groupId))
        {
            return false;
        }

        if (!include.TryGetProperty("option", out var optionElement) || !TryParseId(optionElement, out optionId))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Finds a referenced included option by group and option id.
    /// </summary>
    private static JsonElement? FindIncludedOption(JsonElement profileGroupsRoot, int groupId, int optionId)
    {
        if (profileGroupsRoot.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var group in profileGroupsRoot.EnumerateArray())
        {
            if (!group.TryGetProperty("id", out var groupIdElement) ||
                !TryParseId(groupIdElement, out var parsedGroupId) ||
                parsedGroupId != groupId)
            {
                continue;
            }

            if (!group.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var candidateOption in optionsElement.EnumerateArray())
            {
                if (!candidateOption.TryGetProperty("id", out var optionIdElement) ||
                    !TryParseId(optionIdElement, out var parsedOptionId) ||
                    parsedOptionId != optionId)
                {
                    continue;
                }

                return candidateOption;
            }
        }

        return null;
    }

    /// <summary>
    /// Parses ids from number/string/object id payloads.
    /// </summary>
    private static bool TryParseId(JsonElement element, out int id)
    {
        return CompanySelectionSharedUtilities.TryParseId(element, out id);
    }
}
