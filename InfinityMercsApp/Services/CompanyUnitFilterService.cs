using System.Globalization;
using System.Text.Json;
using InfinityMercsApp.Views.Controls;

namespace InfinityMercsApp.Services;

public static class CompanyUnitFilterService
{
    private static readonly IReadOnlyDictionary<int, string> EmptyLookup = new Dictionary<int, string>();

    public static void AddFilterOptionsFromVisibleProfilesAndOptions(
        string profileGroupsJson,
        IReadOnlyDictionary<int, string> charsLookup,
        IReadOnlyDictionary<int, string> skillsLookup,
        IReadOnlyDictionary<int, string> equipLookup,
        IReadOnlyDictionary<int, string> weaponsLookup,
        IReadOnlyDictionary<int, string> ammoLookup,
        bool requireLieutenant,
        bool requireZeroSwc,
        int? maxCost,
        bool includeProfileValues,
        HashSet<string> characteristics,
        HashSet<string> skills,
        HashSet<string> equipment,
        HashSet<string> weapons,
        HashSet<string> ammo)
    {
        try
        {
            using var doc = JsonDocument.Parse(profileGroupsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var group in doc.RootElement.EnumerateArray())
            {
                var groupHasVisibleOption = false;
                if (group.TryGetProperty("options", out var optionsElement) && optionsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var option in optionsElement.EnumerateArray())
                    {
                        if (requireLieutenant && !IsLieutenantOption(option, skillsLookup))
                        {
                            continue;
                        }

                        if (requireZeroSwc && IsPositiveSwc(CompanyProfileOptionService.ReadOptionSwc(option)))
                        {
                            continue;
                        }

                        var optionCost = ParseCostValue(CompanyProfileOptionService.ReadAdjustedOptionCost(doc.RootElement, group, option));
                        if (maxCost.HasValue && optionCost > maxCost.Value)
                        {
                            continue;
                        }

                        groupHasVisibleOption = true;
                        AddLookupValuesFromEntries(CompanyProfileOptionService.GetOptionEntriesWithIncludes(doc.RootElement, option, "chars"), charsLookup, characteristics);
                        AddLookupValuesFromEntries(CompanyProfileOptionService.GetOptionEntriesWithIncludes(doc.RootElement, option, "skills"), skillsLookup, skills);
                        AddLookupValuesFromEntries(CompanyProfileOptionService.GetOptionEntriesWithIncludes(doc.RootElement, option, "equip"), equipLookup, equipment);
                        AddLookupValuesFromEntries(CompanyProfileOptionService.GetOptionEntriesWithIncludes(doc.RootElement, option, "weapons"), weaponsLookup, weapons);
                        AddLookupValuesFromEntries(CompanyProfileOptionService.GetOptionEntriesWithIncludes(doc.RootElement, option, "ammunition"), ammoLookup, ammo);
                        AddLookupValuesFromEntries(CompanyProfileOptionService.GetOptionEntriesWithIncludes(doc.RootElement, option, "ammo"), ammoLookup, ammo);
                    }
                }

                if (!groupHasVisibleOption)
                {
                    continue;
                }

                if (!includeProfileValues || !group.TryGetProperty("profiles", out var profilesElement) || profilesElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var profile in profilesElement.EnumerateArray())
                {
                    AddLookupValuesFromContainerArray(profile, "chars", charsLookup, characteristics);
                    AddLookupValuesFromContainerArray(profile, "skills", skillsLookup, skills);
                    AddLookupValuesFromContainerArray(profile, "equip", equipLookup, equipment);
                    AddLookupValuesFromContainerArray(profile, "weapons", weaponsLookup, weapons);
                    AddLookupValuesFromContainerArray(profile, "ammunition", ammoLookup, ammo);
                    AddLookupValuesFromContainerArray(profile, "ammo", ammoLookup, ammo);
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CompanyUnitFilterService AddFilterOptionsFromVisibleProfilesAndOptions failed: {ex.Message}");
        }
    }

    public static bool UnitHasVisibleOptionWithFilter(
        string? profileGroupsJson,
        IReadOnlyDictionary<int, string> skillsLookup,
        IReadOnlyDictionary<int, string> charsLookup,
        IReadOnlyDictionary<int, string> equipLookup,
        IReadOnlyDictionary<int, string> weaponsLookup,
        IReadOnlyDictionary<int, string> ammoLookup,
        UnitFilterCriteria criteria,
        bool requireLieutenant,
        bool requireZeroSwc,
        int? maxCost = null,
        Func<string?, bool>? optionNamePredicate = null)
    {
        if (string.IsNullOrWhiteSpace(profileGroupsJson))
        {
            return false;
        }

        var filterQuery = criteria.ToQuery();

        try
        {
            using var doc = JsonDocument.Parse(profileGroupsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var group in doc.RootElement.EnumerateArray())
            {
                if (!group.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var option in optionsElement.EnumerateArray())
                {
                    var optionName = option.TryGetProperty("name", out var optionNameElement) &&
                                     optionNameElement.ValueKind == JsonValueKind.String
                        ? optionNameElement.GetString()
                        : null;
                    if (optionNamePredicate is not null && !optionNamePredicate(optionName))
                    {
                        continue;
                    }

                    if (requireLieutenant && !IsLieutenantOption(option, skillsLookup))
                    {
                        continue;
                    }

                    if (requireZeroSwc && IsPositiveSwc(CompanyProfileOptionService.ReadOptionSwc(option)))
                    {
                        continue;
                    }

                    var optionCost = ParseCostValue(CompanyProfileOptionService.ReadAdjustedOptionCost(doc.RootElement, group, option));
                    if (maxCost.HasValue && optionCost > maxCost.Value)
                    {
                        continue;
                    }

                    if (filterQuery.MinPoints.HasValue && optionCost < filterQuery.MinPoints.Value)
                    {
                        continue;
                    }

                    if (filterQuery.MaxPoints.HasValue && optionCost > filterQuery.MaxPoints.Value)
                    {
                        continue;
                    }

                    if (!OptionMatchesUnitFilter(
                            doc.RootElement,
                            group,
                            option,
                            charsLookup,
                            skillsLookup,
                            equipLookup,
                            weaponsLookup,
                            ammoLookup,
                            filterQuery))
                    {
                        continue;
                    }

                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CompanyUnitFilterService UnitHasVisibleOptionWithFilter failed: {ex.Message}");
        }

        return false;
    }

    public static bool UnitHasVisibleOption(
        string? profileGroupsJson,
        IReadOnlyDictionary<int, string> skillsLookup,
        bool requireLieutenant,
        bool requireZeroSwc,
        int? maxCost = null)
    {
        return UnitHasVisibleOptionWithFilter(
            profileGroupsJson,
            skillsLookup,
            EmptyLookup,
            EmptyLookup,
            EmptyLookup,
            EmptyLookup,
            UnitFilterCriteria.None,
            requireLieutenant,
            requireZeroSwc,
            maxCost);
    }

    public static bool UnitHasLieutenantOption(
        string? profileGroupsJson,
        IReadOnlyDictionary<int, string> skillsLookup)
    {
        if (string.IsNullOrWhiteSpace(profileGroupsJson))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(profileGroupsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var group in doc.RootElement.EnumerateArray())
            {
                if (!group.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var option in optionsElement.EnumerateArray())
                {
                    if (IsLieutenantOption(option, skillsLookup))
                    {
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CompanyUnitFilterService UnitHasLieutenantOption failed: {ex.Message}");
        }

        return false;
    }

    public static bool MatchesClassificationFilter(
        UnitFilterCriteria criteria,
        int? unitType,
        IReadOnlyDictionary<int, string> typeLookup)
    {
        var classificationTerm = criteria.ToQuery().GetTerm(UnitFilterField.Classification);
        if (classificationTerm is null || classificationTerm.Values.Count == 0)
        {
            return true;
        }

        if (!unitType.HasValue || typeLookup.Count == 0)
        {
            return false;
        }

        if (!typeLookup.TryGetValue(unitType.Value, out var typeName))
        {
            return false;
        }

        return classificationTerm.MatchMode == UnitFilterMatchMode.All
            ? classificationTerm.Values.All(value => string.Equals(typeName, value, StringComparison.OrdinalIgnoreCase))
            : classificationTerm.Values.Any(value => string.Equals(typeName, value, StringComparison.OrdinalIgnoreCase));
    }

    private static void AddLookupValuesFromContainerArray(
        JsonElement container,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup,
        HashSet<string> target)
    {
        if (!container.TryGetProperty(propertyName, out var arrayElement) || arrayElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        AddLookupValuesFromEntries(arrayElement.EnumerateArray(), lookup, target);
    }

    private static void AddLookupValuesFromEntries(
        IEnumerable<JsonElement> entries,
        IReadOnlyDictionary<int, string> lookup,
        HashSet<string> target)
    {
        foreach (var entry in entries)
        {
            if (!TryParseId(entry, out var id) || !lookup.TryGetValue(id, out var name) || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            target.Add(name.Trim());
        }
    }

    private static bool OptionMatchesUnitFilter(
        JsonElement profileGroupsRoot,
        JsonElement profileGroup,
        JsonElement option,
        IReadOnlyDictionary<int, string> charsLookup,
        IReadOnlyDictionary<int, string> skillsLookup,
        IReadOnlyDictionary<int, string> equipLookup,
        IReadOnlyDictionary<int, string> weaponsLookup,
        IReadOnlyDictionary<int, string> ammoLookup,
        UnitFilterQuery filterQuery)
    {
        foreach (var term in filterQuery.Terms)
        {
            if (term.Field == UnitFilterField.Classification || term.Values.Count == 0)
            {
                continue;
            }

            var matches = term.Field switch
            {
                UnitFilterField.Characteristics => TermMatchesOptionOrGroup(
                    term,
                    profileGroupsRoot,
                    profileGroup,
                    option,
                    "chars",
                    charsLookup),
                UnitFilterField.Skills => TermMatchesOptionOrGroup(
                    term,
                    profileGroupsRoot,
                    profileGroup,
                    option,
                    "skills",
                    skillsLookup),
                UnitFilterField.Equipment => TermMatchesOptionOrGroup(
                    term,
                    profileGroupsRoot,
                    profileGroup,
                    option,
                    "equip",
                    equipLookup),
                UnitFilterField.Weapons => TermMatchesOptionOrGroup(
                    term,
                    profileGroupsRoot,
                    profileGroup,
                    option,
                    "weapons",
                    weaponsLookup),
                UnitFilterField.Ammo => TermMatchesOptionOnly(
                    term,
                    profileGroupsRoot,
                    option,
                    ammoLookup,
                    ["ammunition", "ammo"]),
                _ => true
            };

            if (!matches)
            {
                return false;
            }
        }

        return true;
    }

    private static bool TermMatchesOptionOrGroup(
        UnitFilterTerm term,
        JsonElement profileGroupsRoot,
        JsonElement profileGroup,
        JsonElement option,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup)
    {
        return term.MatchMode == UnitFilterMatchMode.All
            ? term.Values.All(value => OptionOrGroupContainsLookupName(profileGroupsRoot, profileGroup, option, propertyName, lookup, value))
            : term.Values.Any(value => OptionOrGroupContainsLookupName(profileGroupsRoot, profileGroup, option, propertyName, lookup, value));
    }

    private static bool TermMatchesOptionOnly(
        UnitFilterTerm term,
        JsonElement profileGroupsRoot,
        JsonElement option,
        IReadOnlyDictionary<int, string> lookup,
        IEnumerable<string> propertyNames)
    {
        return term.MatchMode == UnitFilterMatchMode.All
            ? term.Values.All(value => OptionContainsAnyLookupName(profileGroupsRoot, option, propertyNames, lookup, value))
            : term.Values.Any(value => OptionContainsAnyLookupName(profileGroupsRoot, option, propertyNames, lookup, value));
    }

    private static bool OptionOrGroupContainsLookupName(
        JsonElement profileGroupsRoot,
        JsonElement profileGroup,
        JsonElement option,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup,
        string expectedValue)
    {
        return OptionContainsLookupName(profileGroupsRoot, option, propertyName, lookup, expectedValue) ||
               GroupProfilesContainLookupName(profileGroup, propertyName, lookup, expectedValue);
    }

    private static bool OptionContainsAnyLookupName(
        JsonElement profileGroupsRoot,
        JsonElement option,
        IEnumerable<string> propertyNames,
        IReadOnlyDictionary<int, string> lookup,
        string expectedValue)
    {
        foreach (var propertyName in propertyNames)
        {
            if (OptionContainsLookupName(profileGroupsRoot, option, propertyName, lookup, expectedValue))
            {
                return true;
            }
        }

        return false;
    }

    private static bool OptionContainsLookupName(
        JsonElement profileGroupsRoot,
        JsonElement option,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup,
        string expectedValue)
    {
        if (lookup.Count == 0 || string.IsNullOrWhiteSpace(expectedValue))
        {
            return false;
        }

        foreach (var entry in CompanyProfileOptionService.GetOptionEntriesWithIncludes(profileGroupsRoot, option, propertyName))
        {
            if (!TryParseId(entry, out var id) || !lookup.TryGetValue(id, out var name))
            {
                continue;
            }

            if (string.Equals(name, expectedValue, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool GroupProfilesContainLookupName(
        JsonElement profileGroup,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup,
        string expectedValue)
    {
        if (lookup.Count == 0 || string.IsNullOrWhiteSpace(expectedValue))
        {
            return false;
        }

        if (!profileGroup.TryGetProperty("profiles", out var profilesElement) || profilesElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var profile in profilesElement.EnumerateArray())
        {
            if (!profile.TryGetProperty(propertyName, out var valuesElement) || valuesElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var entry in valuesElement.EnumerateArray())
            {
                if (!TryParseId(entry, out var id) || !lookup.TryGetValue(id, out var name))
                {
                    continue;
                }

                if (string.Equals(name, expectedValue, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static bool IsLieutenantOption(JsonElement option, IReadOnlyDictionary<int, string> skillsLookup)
    {
        if (HasLieutenantOrder(option))
        {
            return true;
        }

        if (option.TryGetProperty("name", out var nameElement) &&
            nameElement.ValueKind == JsonValueKind.String &&
            nameElement.GetString()?.Contains("lieutenant", StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        if (!option.TryGetProperty("skills", out var skillsElement) || skillsElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var entry in skillsElement.EnumerateArray())
        {
            if (!TryParseId(entry, out var id))
            {
                continue;
            }

            if (skillsLookup.TryGetValue(id, out var name) &&
                name.Contains("lieutenant", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool HasLieutenantOrder(JsonElement option)
    {
        if (!option.TryGetProperty("orders", out var ordersElement) || ordersElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var order in ordersElement.EnumerateArray())
        {
            if (!order.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            if (string.Equals(typeElement.GetString(), "LIEUTENANT", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsPositiveSwc(string swc)
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

    private static bool TryParseId(JsonElement element, out int id)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var numberId))
        {
            id = numberId;
            return true;
        }

        if (element.ValueKind == JsonValueKind.String &&
            int.TryParse(element.GetString(), out var stringId))
        {
            id = stringId;
            return true;
        }

        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("id", out var idElement))
        {
            return TryParseId(idElement, out id);
        }

        id = 0;
        return false;
    }

    public static int ParseCostValue(string? cost)
    {
        if (string.IsNullOrWhiteSpace(cost))
        {
            return 0;
        }

        if (int.TryParse(cost, out var parsed))
        {
            return parsed;
        }

        var match = System.Text.RegularExpressions.Regex.Match(cost, "\\d+");
        return match.Success && int.TryParse(match.Value, out var fallback) ? fallback : 0;
    }

    public static bool TryGetPeripheralUnitCost(JsonElement profileGroupsRoot, string peripheralName, out int peripheralUnitCost)
    {
        peripheralUnitCost = 0;
        if (profileGroupsRoot.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var expected = NormalizeComparisonToken(peripheralName);
        if (string.IsNullOrWhiteSpace(expected))
        {
            return false;
        }

        foreach (var group in profileGroupsRoot.EnumerateArray())
        {
            var groupIsc = group.TryGetProperty("isc", out var groupIscElement) && groupIscElement.ValueKind == JsonValueKind.String
                ? groupIscElement.GetString() ?? string.Empty
                : string.Empty;
            var groupMatch = NormalizeComparisonToken(groupIsc) == expected;

            if (!groupMatch &&
                group.TryGetProperty("profiles", out var profilesElement) &&
                profilesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var profile in profilesElement.EnumerateArray())
                {
                    var profileName = profile.TryGetProperty("name", out var profileNameElement) && profileNameElement.ValueKind == JsonValueKind.String
                        ? profileNameElement.GetString() ?? string.Empty
                        : string.Empty;
                    if (NormalizeComparisonToken(profileName) == expected)
                    {
                        groupMatch = true;
                        break;
                    }
                }
            }

            if (!groupMatch &&
                group.TryGetProperty("options", out var matchOptionsElement) &&
                matchOptionsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var option in matchOptionsElement.EnumerateArray())
                {
                    var optionName = option.TryGetProperty("name", out var optionNameElement) && optionNameElement.ValueKind == JsonValueKind.String
                        ? optionNameElement.GetString() ?? string.Empty
                        : string.Empty;
                    if (NormalizeComparisonToken(optionName) == expected)
                    {
                        groupMatch = true;
                        break;
                    }
                }
            }

            if (!groupMatch ||
                !group.TryGetProperty("options", out var optionsElement) ||
                optionsElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var option in optionsElement.EnumerateArray())
            {
                var optionCost = ParseCostValue(CompanyProfileOptionService.ReadOptionCost(option));
                if (optionCost <= 0)
                {
                    continue;
                }

                var minis = Math.Max(1, CompanyProfileOptionService.ReadOptionMinis(option));
                peripheralUnitCost = Math.Max(1, optionCost / minis);
                return true;
            }
        }

        return false;
    }

    private static string NormalizeComparisonToken(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return System.Text.RegularExpressions.Regex.Replace(value, @"[^a-z0-9]", string.Empty, System.Text.RegularExpressions.RegexOptions.IgnoreCase).ToLowerInvariant();
    }
}
