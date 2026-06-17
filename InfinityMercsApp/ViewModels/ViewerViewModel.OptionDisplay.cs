using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using InfinityMercsApp.Domain.Utilities;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;
using Resume = InfinityMercsApp.Domain.Models.Army.Resume;

namespace InfinityMercsApp.ViewModels;

public partial class ViewerViewModel
{
    private static string BuildNamedSummary(string label, IEnumerable<string> values)
    {
        var list = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return list.Count == 0
            ? $"{label}: -"
            : $"{label}: {string.Join(", ", list)}";
    }

    private static FormattedString BuildNamedSummaryFormatted(
        string label,
        IEnumerable<string> values,
        IReadOnlyDictionary<int, string>? equipLookup,
        IReadOnlyDictionary<int, string>? links,
        Color? textColor)
    {
        var list = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var formatted = new FormattedString();
        var labelSpan = new Span { Text = $"{label}: " };
        if (textColor is not null)
        {
            labelSpan.TextColor = textColor;
        }

        formatted.Spans.Add(labelSpan);

        if (list.Count == 0)
        {
            var emptySpan = new Span { Text = "-" };
            if (textColor is not null)
            {
                emptySpan.TextColor = textColor;
            }

            formatted.Spans.Add(emptySpan);
            return formatted;
        }

        for (var i = 0; i < list.Count; i++)
        {
            var value = list[i];
            var valueColor = textColor ?? Colors.White;
            var link = TryResolveLinkForDisplayName(value, equipLookup, links);
            AppendWithLieutenantHighlight(formatted, value, valueColor, link);
            if (i < list.Count - 1)
            {
                var separatorSpan = new Span { Text = ", " };
                if (textColor is not null)
                {
                    separatorSpan.TextColor = textColor;
                }

                formatted.Spans.Add(separatorSpan);
            }
        }

        return formatted;
    }

    private static string? TryResolveLinkForDisplayName(
        string displayName,
        IReadOnlyDictionary<int, string>? nameLookup,
        IReadOnlyDictionary<int, string>? links)
    {
        if (string.IsNullOrWhiteSpace(displayName) || nameLookup is null || links is null)
        {
            return null;
        }

        foreach (var pair in nameLookup)
        {
            if (!links.ContainsKey(pair.Key))
            {
                continue;
            }

            if (string.Equals(pair.Value, displayName, StringComparison.OrdinalIgnoreCase))
            {
                return links[pair.Key];
            }
        }

        var trimmed = displayName;
        var parenIndex = displayName.IndexOf(" (", StringComparison.Ordinal);
        if (parenIndex > 0)
        {
            trimmed = displayName[..parenIndex];
        }

        foreach (var pair in nameLookup)
        {
            if (!links.ContainsKey(pair.Key))
            {
                continue;
            }

            if (string.Equals(pair.Value, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return links[pair.Key];
            }
        }

        return null;
    }

    private static string? TryResolveFirstLinkByPredicate(
        IReadOnlyDictionary<int, string> nameLookup,
        IReadOnlyDictionary<int, string> links,
        Func<string, bool> predicate)
    {
        foreach (var pair in nameLookup)
        {
            if (!links.TryGetValue(pair.Key, out var url))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            if (predicate(pair.Value))
            {
                return url;
            }
        }

        return null;
    }

    private static string BuildOptionConfigurationSummary(
        JsonElement option,
        IReadOnlyDictionary<int, string> weaponsLookup,
        IReadOnlyDictionary<int, string> equipLookup,
        IReadOnlyDictionary<int, string> skillsLookup)
    {
        var weapons = GetOrderedNames(option, "weapons", weaponsLookup);
        var equip = GetOrderedNames(option, "equip", equipLookup);
        var skills = GetOrderedNames(option, "skills", skillsLookup);

        var primary = weapons.Count > 0 ? string.Join(", ", weapons) : string.Empty;
        var extras = equip.Concat(skills).ToList();

        if (string.IsNullOrWhiteSpace(primary) && extras.Count == 0)
        {
            return "-";
        }

        if (string.IsNullOrWhiteSpace(primary))
        {
            return string.Join(", ", extras);
        }

        if (extras.Count == 0)
        {
            return primary;
        }

        return $"{primary} + {string.Join(", ", extras)}";
    }

    private static bool IsMeleeWeaponName(string weaponName) =>
        Regex.IsMatch(
            weaponName,
            @"\bccw\b|\bda ccw\b|\bap ccw\b|\bknife\b|\bsword\b|\bmonofilament\b|\bviral ccw\b|\bpistols?\b|\bclose combat weapon\b|\bcc\s*weapon\b|\bc\.?\s*c\.?\s*weapon\b|\bpara\s*cc\s*weapon\b",
            RegexOptions.IgnoreCase);

    private static string JoinOrDash(IEnumerable<string> values)
    {
        var list = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return list.Count == 0 ? "-" : string.Join(Environment.NewLine, list);
    }

    private static FormattedString BuildNameFormatted(string name)
    {
        var formatted = new FormattedString();
        if (string.IsNullOrWhiteSpace(name))
        {
            formatted.Spans.Add(new Span { Text = string.Empty });
            return formatted;
        }

        const string token = "Lieutenant";
        var start = 0;
        while (true)
        {
            var index = name.IndexOf(token, start, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                if (start < name.Length)
                {
                    formatted.Spans.Add(new Span { Text = name[start..] });
                }

                break;
            }

            if (index > start)
            {
                formatted.Spans.Add(new Span { Text = name[start..index] });
            }

            formatted.Spans.Add(new Span
            {
                Text = name.Substring(index, token.Length),
                TextColor = Color.FromArgb("#C084FC")
            });
            start = index + token.Length;
        }

        return formatted;
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

            var type = typeElement.GetString();
            if (string.Equals(type, "LIEUTENANT", StringComparison.OrdinalIgnoreCase))
            {
                return true;
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

        foreach (var skill in GetOrderedIdNames(option, "skills", skillsLookup))
        {
            if (skill.Name.Contains("lieutenant", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool MatchesClassificationFilter(
        Resume unit,
        IReadOnlyDictionary<int, string> typeLookup,
        UnitFilterCriteria criteria)
    {
        var classificationTerm = criteria.ToQuery().GetTerm(UnitFilterField.Classification);
        if (classificationTerm is null || classificationTerm.Values.Count == 0)
        {
            return true;
        }

        if (!unit.Type.HasValue || typeLookup.Count == 0)
        {
            return false;
        }

        if (!typeLookup.TryGetValue(unit.Type.Value, out var typeName))
        {
            return false;
        }

        return classificationTerm.MatchMode == UnitFilterMatchMode.All
            ? classificationTerm.Values.All(value => string.Equals(typeName, value, StringComparison.OrdinalIgnoreCase))
            : classificationTerm.Values.Any(value => string.Equals(typeName, value, StringComparison.OrdinalIgnoreCase));
    }

    private static bool UnitHasVisibleOptionWithFilter(
        string? profileGroupsJson,
        IReadOnlyDictionary<int, string> skillsLookup,
        IReadOnlyDictionary<int, string> charsLookup,
        IReadOnlyDictionary<int, string> equipLookup,
        IReadOnlyDictionary<int, string> weaponsLookup,
        IReadOnlyDictionary<int, string> ammoLookup,
        UnitFilterCriteria criteria,
        bool requireLieutenant,
        bool requireZeroSwc,
        int? maxCost = null)
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
                    if (requireLieutenant && !IsLieutenantOption(option, skillsLookup))
                    {
                        continue;
                    }

                    if (requireZeroSwc && IsPositiveSwc(ReadOptionSwc(option)))
                    {
                        continue;
                    }

                    var optionCost = ParseCostValue(ReadAdjustedOptionCost(doc.RootElement, group, option));
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
            Console.Error.WriteLine($"UnitHasVisibleOptionWithFilter failed: {ex.Message}");
        }

        return false;
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

        foreach (var entry in GetOptionEntriesWithIncludes(profileGroupsRoot, option, propertyName))
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

    private static string BuildOptionDisplayName(
        JsonElement option,
        string baseName,
        IReadOnlyDictionary<int, string> equipLookup,
        IReadOnlyDictionary<int, string> skillsLookup)
    {
        var details = new List<string>();
        var normalizedBase = baseName.ToLowerInvariant();

        foreach (var skillName in GetOrderedNames(option, "skills", skillsLookup))
        {
            if (IsNameDetailTag(skillName) && !normalizedBase.Contains(skillName.ToLowerInvariant()))
            {
                details.Add(skillName);
            }
        }

        foreach (var equipName in GetOrderedNames(option, "equip", equipLookup))
        {
            if (IsNameDetailTag(equipName) && !normalizedBase.Contains(equipName.ToLowerInvariant()))
            {
                details.Add(equipName);
            }
        }

        if (option.TryGetProperty("orders", out var ordersElement) && ordersElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var order in ordersElement.EnumerateArray())
            {
                if (!order.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var type = typeElement.GetString();
                if (string.Equals(type, "LIEUTENANT", StringComparison.OrdinalIgnoreCase) &&
                    !normalizedBase.Contains("lieutenant"))
                {
                    details.Add("Lieutenant");
                }
            }
        }

        var distinctDetails = details
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (distinctDetails.Count == 0)
        {
            return baseName;
        }

        return $"{baseName} ({string.Join(", ", distinctDetails)})";
    }

    private static bool IsNameDetailTag(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Contains("forward observer", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("hacker", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("hacking device", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("specialist", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("paramedic", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("doctor", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("engineer", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("lieutenant", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("nco", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("chain of command", StringComparison.OrdinalIgnoreCase);
    }

    private static List<(int Id, string Name)> BuildConfigurationSkillEntries(IEnumerable<(int Id, string Name)> rawEntries)
    {
        var normalized = new List<(int Id, string Name)>();
        foreach (var entry in rawEntries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                continue;
            }

            var skillName = entry.Name.Trim();
            if (!IsCommonSpecOpsSkill(skillName))
            {
                normalized.Add((entry.Id, skillName));
                continue;
            }

            var lieutenantDetail = ExtractLieutenantSkillDetail(skillName);
            if (!string.IsNullOrWhiteSpace(lieutenantDetail))
            {
                normalized.Add((0, lieutenantDetail));
            }
        }

        return normalized
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsCommonSpecOpsSkill(string? skillName)
    {
        if (string.IsNullOrWhiteSpace(skillName))
        {
            return false;
        }

        return skillName.Contains("lieutenant", StringComparison.OrdinalIgnoreCase) ||
               skillName.Contains("infinity spec-ops", StringComparison.OrdinalIgnoreCase) ||
               skillName.Contains("infinity spec ops", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtractLieutenantSkillDetail(string skillName)
    {
        if (!skillName.Contains("lieutenant", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var detail = Regex.Replace(skillName, "lieutenant", string.Empty, RegexOptions.IgnoreCase).Trim();
        if (string.IsNullOrWhiteSpace(detail))
        {
            return null;
        }

        detail = detail.Trim('(', ')', '[', ']', '-', ':', ',', ';', ' ');
        return string.IsNullOrWhiteSpace(detail) ? null : detail;
    }

    private static string ReadOptionSwc(JsonElement option)
    {
        if (!option.TryGetProperty("swc", out var swcElement))
        {
            return "-";
        }

        if (swcElement.ValueKind == JsonValueKind.String)
        {
            var swc = swcElement.GetString();
            return string.IsNullOrWhiteSpace(swc) ? "-" : swc;
        }

        if (swcElement.ValueKind == JsonValueKind.Number)
        {
            return swcElement.ToString();
        }

        return "-";
    }

    private static string ReadOptionCost(JsonElement option)
    {
        if (!option.TryGetProperty("points", out var pointsElement))
        {
            return "-";
        }

        if (pointsElement.ValueKind == JsonValueKind.Number)
        {
            if (pointsElement.TryGetInt32(out var intCost))
            {
                return intCost.ToString();
            }

            return pointsElement.ToString();
        }

        if (pointsElement.ValueKind == JsonValueKind.String)
        {
            var points = pointsElement.GetString();
            return string.IsNullOrWhiteSpace(points) ? "-" : points;
        }

        return "-";
    }

    private static string ReadAdjustedOptionCost(JsonElement profileGroupsRoot, JsonElement group, JsonElement option)
    {
        var baseCostText = ReadOptionCost(option);
        if (!int.TryParse(
                baseCostText,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out var baseCost))
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
        return adjustedCost.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

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

    private static IEnumerable<JsonElement> GetDisplayPeripheralEntriesForOption(
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

    private static bool IsControllerGroup(JsonElement profileGroupsRoot, JsonElement group)
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

    private static int ReadOptionMinis(JsonElement option)
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
            int.TryParse(
                minisElement.GetString(),
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out var minisText))
        {
            return Math.Max(0, minisText);
        }

        return 0;
    }

    private static bool IsPositiveSwc(string swc)
    {
        if (string.IsNullOrWhiteSpace(swc) || swc == "-")
        {
            return false;
        }

        return decimal.TryParse(
                   swc,
                   System.Globalization.NumberStyles.Number,
                   System.Globalization.CultureInfo.InvariantCulture,
                   out var value)
               && value > 0m;
    }

}
