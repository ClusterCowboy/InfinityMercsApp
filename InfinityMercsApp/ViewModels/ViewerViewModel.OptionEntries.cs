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
    private static List<string> GetOrderedNames(
        JsonElement container,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup)
    {
        return GetOrderedIdNames(container, propertyName, lookup)
            .Select(x => x.Name)
            .ToList();
    }

    private static List<(int Id, string Name)> GetOrderedIdDisplayNames(
        JsonElement container,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup,
        IReadOnlyDictionary<int, ExtraDefinition> extrasLookup,
        bool showUnitsInInches)
    {
        if (!container.TryGetProperty(propertyName, out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var entries = new List<(int Order, int Id, string Name)>();
        foreach (var entry in arr.EnumerateArray())
        {
            if (!TryParseId(entry, out var id))
            {
                continue;
            }

            var order = int.MaxValue;
            if (entry.ValueKind == JsonValueKind.Object &&
                entry.TryGetProperty("order", out var orderElement) &&
                orderElement.ValueKind == JsonValueKind.Number &&
                orderElement.TryGetInt32(out var parsedOrder))
            {
                order = parsedOrder;
            }

            var baseName = lookup.TryGetValue(id, out var resolvedName) ? resolvedName : id.ToString();
            var displayName = BuildEntryDisplayName(baseName, entry, extrasLookup, showUnitsInInches);
            entries.Add((order, id, displayName));
        }

        return entries
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => (x.Id, x.Name))
            .ToList();
    }

    private static List<(int Id, string Name)> GetOrderedIdDisplayNamesFromEntries(
        IEnumerable<JsonElement> entriesSource,
        IReadOnlyDictionary<int, string> lookup,
        IReadOnlyDictionary<int, ExtraDefinition> extrasLookup,
        bool showUnitsInInches)
    {
        var entries = new List<(int Order, int Id, string Name)>();
        foreach (var entry in entriesSource)
        {
            if (!TryParseId(entry, out var id))
            {
                continue;
            }

            var order = int.MaxValue;
            if (entry.ValueKind == JsonValueKind.Object &&
                entry.TryGetProperty("order", out var orderElement) &&
                orderElement.ValueKind == JsonValueKind.Number &&
                orderElement.TryGetInt32(out var parsedOrder))
            {
                order = parsedOrder;
            }

            var baseName = lookup.TryGetValue(id, out var resolvedName) ? resolvedName : id.ToString();
            var displayName = BuildEntryDisplayName(baseName, entry, extrasLookup, showUnitsInInches);
            entries.Add((order, id, displayName));
        }

        return entries
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => (x.Id, x.Name))
            .ToList();
    }

    private static List<(int Id, string Name)> GetCountedIdDisplayNamesFromEntries(
        IEnumerable<JsonElement> entriesSource,
        IReadOnlyDictionary<int, string> lookup,
        IReadOnlyDictionary<int, ExtraDefinition> extrasLookup,
        bool showUnitsInInches)
    {
        var counts = new Dictionary<string, (int Id, int Count)>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entriesSource)
        {
            if (!TryParseId(entry, out var id))
            {
                continue;
            }

            var baseName = lookup.TryGetValue(id, out var resolvedName) ? resolvedName : id.ToString();
            var displayName = BuildEntryDisplayName(baseName, entry, extrasLookup, showUnitsInInches);
            if (string.IsNullOrWhiteSpace(displayName))
            {
                continue;
            }

            var quantity = ReadEntryQuantity(entry);
            if (counts.TryGetValue(displayName, out var existing))
            {
                counts[displayName] = (existing.Id, existing.Count + quantity);
            }
            else
            {
                counts[displayName] = (id, quantity);
            }
        }

        return counts
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => (x.Value.Id, $"{x.Key} ({x.Value.Count})"))
            .ToList();
    }

    private static int ReadEntryQuantity(JsonElement entry)
    {
        if (entry.ValueKind != JsonValueKind.Object)
        {
            return 1;
        }

        if (!entry.TryGetProperty("q", out var quantityElement))
        {
            return 1;
        }

        if (quantityElement.ValueKind == JsonValueKind.Number && quantityElement.TryGetInt32(out var quantityNumber))
        {
            return Math.Max(1, quantityNumber);
        }

        if (quantityElement.ValueKind == JsonValueKind.String &&
            int.TryParse(
                quantityElement.GetString(),
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out var quantityText))
        {
            return Math.Max(1, quantityText);
        }

        return 1;
    }

    private static IEnumerable<JsonElement> GetOptionEntriesWithIncludes(
        JsonElement profileGroupsRoot,
        JsonElement option,
        string propertyName)
    {
        var collected = new List<JsonElement>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectOptionEntriesWithIncludes(profileGroupsRoot, option, propertyName, collected, visited, null);
        return collected;
    }

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

            foreach (var option in optionsElement.EnumerateArray())
            {
                if (!option.TryGetProperty("id", out var optionIdElement) ||
                    !TryParseId(optionIdElement, out var parsedOptionId) ||
                    parsedOptionId != optionId)
                {
                    continue;
                }

                return option;
            }
        }

        return null;
    }

    private static string BuildEntryDisplayName(
        string baseName,
        JsonElement entry,
        IReadOnlyDictionary<int, ExtraDefinition> extrasLookup,
        bool showUnitsInInches)
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
            if (!TryParseId(extraEntry, out var extraId))
            {
                continue;
            }

            if (extrasLookup.TryGetValue(extraId, out var extraDefinition) &&
                !string.IsNullOrWhiteSpace(extraDefinition.Name))
            {
                extras.Add(FormatExtraDisplay(extraDefinition, showUnitsInInches));
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

    private static string FormatExtraDisplay(ExtraDefinition extraDefinition, bool showUnitsInInches)
    {
        if (!string.Equals(extraDefinition.Type, "DISTANCE", StringComparison.OrdinalIgnoreCase))
        {
            return extraDefinition.Name;
        }

        var valueText = showUnitsInInches
            ? ConvertDistanceTextToInches(extraDefinition.Name)
            : extraDefinition.Name;

        return AppendDistanceUnitSuffix(valueText, showUnitsInInches);
    }

    private static string AppendDistanceUnitSuffix(string distanceText, bool showUnitsInInches)
    {
        if (string.IsNullOrWhiteSpace(distanceText))
        {
            return distanceText;
        }

        if (Regex.IsMatch(distanceText, "(\"|cm)\\s*$", RegexOptions.IgnoreCase))
        {
            return distanceText;
        }

        var match = Regex.Match(distanceText, @"([+-]?\d+(?:\.\d+)?)");
        if (!match.Success)
        {
            return distanceText;
        }

        var suffix = showUnitsInInches ? "\"" : "cm";
        return string.Concat(
            distanceText.AsSpan(0, match.Index + match.Length),
            suffix,
            distanceText.AsSpan(match.Index + match.Length));
    }

    private static string ConvertDistanceTextToInches(string distanceText)
    {
        if (string.IsNullOrWhiteSpace(distanceText))
        {
            return distanceText;
        }

        var match = Regex.Match(distanceText, @"([+-]?)(\d+(?:\.\d+)?)");
        if (!match.Success)
        {
            return distanceText;
        }

        var signToken = match.Groups[1].Value;
        var valueToken = match.Groups[2].Value;
        if (!decimal.TryParse(valueToken, NumberStyles.Float, CultureInfo.InvariantCulture, out var valueCm))
        {
            return distanceText;
        }

        if (signToken == "-")
        {
            valueCm = -valueCm;
        }

        var valueInches = (int)Math.Round((double)(valueCm / 2.5m), MidpointRounding.AwayFromZero);
        var replacement = valueInches < 0
            ? valueInches.ToString(CultureInfo.InvariantCulture)
            : signToken == "+"
                ? $"+{valueInches}"
                : valueInches.ToString(CultureInfo.InvariantCulture);

        return string.Concat(
            distanceText.AsSpan(0, match.Index),
            replacement,
            distanceText.AsSpan(match.Index + match.Length));
    }

    private static Dictionary<int, ExtraDefinition> BuildExtrasLookup(string? filtersJson)
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
                if (!entry.TryGetProperty("id", out var idElement) || !TryParseId(idElement, out var id))
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
                    ? typeElement.GetString() ?? string.Empty
                    : string.Empty;

                map[id] = new ExtraDefinition(name, type, TryReadLink(entry));
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"BuildExtrasLookup failed: {ex.Message}");
        }

        return map;
    }

    private static void AddFilterOptionsFromProfilesAndOptions(
        string profileGroupsJson,
        IReadOnlyDictionary<int, string> charsLookup,
        IReadOnlyDictionary<int, string> skillsLookup,
        IReadOnlyDictionary<int, string> equipLookup,
        IReadOnlyDictionary<int, string> weaponsLookup,
        IReadOnlyDictionary<int, string> ammoLookup,
        HashSet<string> characteristics,
        HashSet<string> skills,
        HashSet<string> equipment,
        HashSet<string> weapons,
        HashSet<string> ammo,
        ref int maxPoints)
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
                if (group.TryGetProperty("options", out var optionsElement) && optionsElement.ValueKind == JsonValueKind.Array)
                {
                    foreach (var option in optionsElement.EnumerateArray())
                    {
                        var optionCost = ParseCostValue(ReadAdjustedOptionCost(doc.RootElement, group, option));
                        maxPoints = Math.Max(maxPoints, optionCost);

                        foreach (var character in GetOrderedIdNames(option, "chars", charsLookup))
                        {
                            if (!string.IsNullOrWhiteSpace(character.Name))
                            {
                                characteristics.Add(character.Name.Trim());
                            }
                        }

                        foreach (var skill in GetOrderedIdNames(option, "skills", skillsLookup))
                        {
                            if (!string.IsNullOrWhiteSpace(skill.Name))
                            {
                                skills.Add(skill.Name.Trim());
                            }
                        }

                        foreach (var equip in GetOrderedIdNames(option, "equip", equipLookup))
                        {
                            if (!string.IsNullOrWhiteSpace(equip.Name))
                            {
                                equipment.Add(equip.Name.Trim());
                            }
                        }

                        foreach (var weapon in GetOrderedIdNames(option, "weapons", weaponsLookup))
                        {
                            if (!string.IsNullOrWhiteSpace(weapon.Name))
                            {
                                weapons.Add(weapon.Name.Trim());
                            }
                        }

                        foreach (var ammoEntry in GetOrderedIdNames(option, "ammunition", ammoLookup))
                        {
                            if (!string.IsNullOrWhiteSpace(ammoEntry.Name))
                            {
                                ammo.Add(ammoEntry.Name.Trim());
                            }
                        }

                        foreach (var ammoEntry in GetOrderedIdNames(option, "ammo", ammoLookup))
                        {
                            if (!string.IsNullOrWhiteSpace(ammoEntry.Name))
                            {
                                ammo.Add(ammoEntry.Name.Trim());
                            }
                        }
                    }
                }

                if (!group.TryGetProperty("profiles", out var profilesElement) || profilesElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var profile in profilesElement.EnumerateArray())
                {
                    foreach (var character in GetOrderedIdNames(profile, "chars", charsLookup))
                    {
                        if (!string.IsNullOrWhiteSpace(character.Name))
                        {
                            characteristics.Add(character.Name.Trim());
                        }
                    }

                    foreach (var skill in GetOrderedIdNames(profile, "skills", skillsLookup))
                    {
                        if (!string.IsNullOrWhiteSpace(skill.Name))
                        {
                            skills.Add(skill.Name.Trim());
                        }
                    }

                    foreach (var equip in GetOrderedIdNames(profile, "equip", equipLookup))
                    {
                        if (!string.IsNullOrWhiteSpace(equip.Name))
                        {
                            equipment.Add(equip.Name.Trim());
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"AddFilterOptionsFromProfilesAndOptions failed: {ex.Message}");
        }
    }

    private static int ParseCostValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return 0;
        }

        var match = Regex.Match(value, @"-?\d+");
        if (!match.Success)
        {
            return 0;
        }

        return int.TryParse(match.Value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? parsed
            : 0;
    }

    private static string? TryReadLink(JsonElement entry)
    {
        foreach (var key in new[] { "url", "href", "link", "wiki", "web" })
        {
            if (!entry.TryGetProperty(key, out var valueElement) || valueElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var value = valueElement.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return value;
            }
        }

        return null;
    }

    private static List<(string Text, string? Url)> BuildLinkedLines(
        IEnumerable<(int Id, string Name)> entries,
        IReadOnlyDictionary<int, string> links)
    {
        var result = new List<(string Text, string? Url)>();
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                continue;
            }

            var url = links.TryGetValue(entry.Id, out var resolvedUrl) ? resolvedUrl : null;
            var existingIndex = result.FindIndex(x => string.Equals(x.Text, entry.Name, StringComparison.OrdinalIgnoreCase));
            if (existingIndex >= 0)
            {
                if (string.IsNullOrWhiteSpace(result[existingIndex].Url) && !string.IsNullOrWhiteSpace(url))
                {
                    result[existingIndex] = (result[existingIndex].Text, url);
                }

                continue;
            }

            result.Add((entry.Name, url));
        }

        return result;
    }

    private static FormattedString BuildLinkedFormattedString(IEnumerable<(string Text, string? Url)> lines, Color textColor)
    {
        var formatted = new FormattedString();
        var list = lines.ToList();
        if (list.Count == 0)
        {
            formatted.Spans.Add(new Span { Text = "-", TextColor = textColor });
            return formatted;
        }

        for (var i = 0; i < list.Count; i++)
        {
            var line = list[i];
            AppendWithLieutenantHighlight(formatted, line.Text, textColor, line.Url);
            if (i < list.Count - 1)
            {
                formatted.Spans.Add(new Span { Text = Environment.NewLine, TextColor = textColor });
            }
        }

        return formatted;
    }

    private static void AppendWithLieutenantHighlight(FormattedString formatted, string text, Color defaultColor, string? link = null)
    {
        if (string.IsNullOrEmpty(text))
        {
            return;
        }

        var matches = Regex.Matches(text, "(lieutenant)", RegexOptions.IgnoreCase);
        if (matches.Count == 0)
        {
            var span = new Span
            {
                Text = text,
                TextColor = defaultColor
            };
            AttachLinkGesture(span, link);
            formatted.Spans.Add(span);
            return;
        }

        var currentIndex = 0;
        foreach (Match match in matches)
        {
            if (match.Index > currentIndex)
            {
                var prefixSpan = new Span
                {
                    Text = text.Substring(currentIndex, match.Index - currentIndex),
                    TextColor = defaultColor
                };
                AttachLinkGesture(prefixSpan, link);
                formatted.Spans.Add(prefixSpan);
            }

            var highlightedSpan = new Span
            {
                Text = text.Substring(match.Index, match.Length),
                TextColor = Color.FromArgb("#C084FC")
            };
            AttachLinkGesture(highlightedSpan, link);
            formatted.Spans.Add(highlightedSpan);

            currentIndex = match.Index + match.Length;
        }

        if (currentIndex < text.Length)
        {
            var suffixSpan = new Span
            {
                Text = text[currentIndex..],
                TextColor = defaultColor
            };
            AttachLinkGesture(suffixSpan, link);
            formatted.Spans.Add(suffixSpan);
        }
    }

    private static void AttachLinkGesture(Span span, string? link)
    {
        if (string.IsNullOrWhiteSpace(link))
        {
            return;
        }

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) => await OpenLinkAsync(link);
        span.GestureRecognizers.Add(tap);
    }

    private static async Task OpenLinkAsync(string? link)
    {
        if (string.IsNullOrWhiteSpace(link))
        {
            return;
        }

        try
        {
            await Launcher.Default.OpenAsync(link);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to open link '{link}': {ex.Message}");
        }
    }

    private static Dictionary<int, string> BuildIdLinkLookup(string? filtersJson, string sectionName)
    {
        var map = new Dictionary<int, string>();
        if (string.IsNullOrWhiteSpace(filtersJson))
        {
            return map;
        }

        try
        {
            using var doc = JsonDocument.Parse(filtersJson);
            if (!doc.RootElement.TryGetProperty(sectionName, out var section) || section.ValueKind != JsonValueKind.Array)
            {
                return map;
            }

            foreach (var entry in section.EnumerateArray())
            {
                if (!entry.TryGetProperty("id", out var idElement) || !TryParseId(idElement, out var id))
                {
                    continue;
                }

                var link = TryReadLink(entry);
                if (!string.IsNullOrWhiteSpace(link))
                {
                    map[id] = link;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"BuildIdLinkLookup failed for section '{sectionName}': {ex.Message}");
        }

        return map;
    }

    private static List<(int Id, string Name)> GetOrderedIdNames(
        JsonElement container,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup)
    {
        if (!container.TryGetProperty(propertyName, out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var entries = new List<(int Order, int Id, string Name)>();
        foreach (var entry in arr.EnumerateArray())
        {
            if (!TryParseId(entry, out var id))
            {
                continue;
            }

            var order = int.MaxValue;
            if (entry.ValueKind == JsonValueKind.Object &&
                entry.TryGetProperty("order", out var orderElement) &&
                orderElement.ValueKind == JsonValueKind.Number &&
                orderElement.TryGetInt32(out var parsedOrder))
            {
                order = parsedOrder;
            }

            var name = lookup.TryGetValue(id, out var resolvedName) ? resolvedName : id.ToString();
            entries.Add((order, id, name));
        }

        return entries
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => (x.Id, x.Name))
            .ToList();
    }

}
