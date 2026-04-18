using System.Text.Json;
using System.Text.RegularExpressions;

namespace InfinityMercsApp.Domain.Models.Perks;

/// <summary>
/// Maps a unit profile's known text attributes (skills/equipment/weapons/chars)
/// to perk node ids using deterministic perk-tree node text matching.
/// </summary>
public static class CompanyPerkOwnershipResolver
{
    public sealed class OwnedPerk
    {
        public string Id { get; init; } = string.Empty;
        public string ListId { get; init; } = string.Empty;
        public string ListName { get; init; } = string.Empty;
        public int TrackNumber { get; init; }
        public int Tier { get; init; }
        public string PerkText { get; init; } = string.Empty;
    }

    public static IReadOnlyList<string> ResolveOwnedPerkNodeIds(
        IEnumerable<string>? skills,
        IEnumerable<string>? equipment = null,
        IEnumerable<string>? weapons = null,
        IEnumerable<string>? characteristics = null,
        bool includeMechaTrack = true)
    {
        var profileTerms = BuildProfileTerms(skills, equipment, weapons, characteristics);
        if (profileTerms.Count == 0)
        {
            return [];
        }

        var matchedIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var list in CompanyPerkCatalog.GetPerkNodeLists())
        {
            if (!includeMechaTrack &&
                string.Equals(list.ListId, "mecha", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            foreach (var root in list.Roots)
            {
                CollectMatches(root, profileTerms, matchedIds);
            }
        }

        return matchedIds
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public static IReadOnlyList<OwnedPerk> ResolveOwnedPerksFromProfile(
        JsonElement profile,
        JsonElement? option,
        IReadOnlyDictionary<int, string> skillsLookup,
        IReadOnlyDictionary<int, string>? equipLookup = null,
        IReadOnlyDictionary<int, string>? weaponsLookup = null,
        IReadOnlyDictionary<int, string>? charsLookup = null,
        IReadOnlyDictionary<int, string>? extrasLookup = null)
    {
        var skillNames = ExtractLookupNames(profile, "skills", skillsLookup, extrasLookup);
        var equipNames = ExtractLookupNames(profile, "equip", equipLookup, extrasLookup);
        var weaponNames = ExtractLookupNames(profile, "weapons", weaponsLookup, extrasLookup);
        var charNames = ExtractLookupNames(profile, "chars", charsLookup, extrasLookup);

        if (option.HasValue)
        {
            var optionValue = option.Value;
            skillNames.UnionWith(ExtractLookupNames(optionValue, "skills", skillsLookup, extrasLookup));
            equipNames.UnionWith(ExtractLookupNames(optionValue, "equip", equipLookup, extrasLookup));
            weaponNames.UnionWith(ExtractLookupNames(optionValue, "weapons", weaponsLookup, extrasLookup));
            charNames.UnionWith(ExtractLookupNames(optionValue, "chars", charsLookup, extrasLookup));
        }

        var includeMechaTrack = IsTagProfile(profile);
        var ownedIds = ResolveOwnedPerkNodeIds(skillNames, equipNames, weaponNames, charNames, includeMechaTrack);
        if (ownedIds.Count == 0)
        {
            return [];
        }

        var nodeLookup = BuildOwnedPerkLookup();
        return ownedIds
            .Where(id => nodeLookup.ContainsKey(id))
            .Select(id => nodeLookup[id])
            .OrderBy(x => x.ListName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.TrackNumber)
            .ThenBy(x => x.Tier)
            .ToList();
    }

    public static string BuildOwnedPerkReport(IReadOnlyList<OwnedPerk> ownedPerks)
    {
        if (ownedPerks is null || ownedPerks.Count == 0)
        {
            return "Owned Perks: (none)";
        }

        var lines = new List<string> { "Owned Perks:" };
        foreach (var perk in ownedPerks)
        {
            lines.Add($"- {perk.Id} | {perk.ListName} T{perk.Tier} Track {perk.TrackNumber} | {perk.PerkText}");
        }

        return string.Join(Environment.NewLine, lines);
    }

    private static HashSet<string> BuildProfileTerms(
        IEnumerable<string>? skills,
        IEnumerable<string>? equipment,
        IEnumerable<string>? weapons,
        IEnumerable<string>? characteristics)
    {
        var terms = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        AddTerms(terms, skills);
        AddTerms(terms, equipment);
        AddTerms(terms, weapons);
        AddTerms(terms, characteristics);
        return terms;
    }

    private static void AddTerms(HashSet<string> terms, IEnumerable<string>? source)
    {
        if (source is null)
        {
            return;
        }

        foreach (var item in source)
        {
            if (string.IsNullOrWhiteSpace(item))
            {
                continue;
            }

            // Skill-style PH annotations (for example "Gizmokit (PH=11)") are not
            // the same as owning the actual equipment item.
            if (IsSkillOnlyKitWithPhValue(item))
            {
                continue;
            }

            var normalized = Normalize(item);
            if (normalized.Length >= 3)
            {
                terms.Add(normalized);
            }

            foreach (var alias in ExpandKnownAliases(item))
            {
                var normalizedAlias = Normalize(alias);
                if (normalizedAlias.Length >= 3)
                {
                    terms.Add(normalizedAlias);
                }
            }
        }
    }

    private static IReadOnlyList<IReadOnlyList<string>> ExtractPerkRequirementGroups(string perkText)
    {
        var groups = new List<IReadOnlyList<string>>();
        foreach (var group in SplitTopLevel(perkText, ','))
        {
            var alternatives = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var rawAlternative in SplitTopLevelOr(group))
            {
                var normalized = Normalize(rawAlternative);
                if (normalized.Length >= 3)
                {
                    alternatives.Add(normalized);
                }

                var relaxed = Normalize(RemoveOptionalMetadata(rawAlternative));
                if (relaxed.Length >= 3)
                {
                    alternatives.Add(relaxed);
                }
            }

            if (alternatives.Count > 0)
            {
                groups.Add(alternatives.ToList());
            }
        }

        return groups;
    }

    private static bool IsMatch(string profileTerm, string nodeTerm)
    {
        // Limited-use skills (for example "Camouflage (1 Use)") should not satisfy
        // a perk that requires the unrestricted/base skill name.
        if (ContainsLimitedUseToken(profileTerm) && !ContainsLimitedUseToken(nodeTerm))
        {
            return false;
        }

        // Specialized hacking devices do not satisfy the base "Hacking Device"
        // requirement. A unit must explicitly have base Hacking Device for that.
        if (IsBaseHackingDeviceRequirement(nodeTerm) && IsSpecializedHackingDevice(profileTerm))
        {
            return false;
        }

        if (string.Equals(profileTerm, nodeTerm, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var left = profileTerm.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var right = nodeTerm.Split(' ', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (left.Length == 0 || right.Length == 0)
        {
            return false;
        }

        if (IsSimpleStatModifier(right) && left.Contains("ps", StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        // Weapon-based stat lines (for example "Para CC Weapon (-3)") should not
        // satisfy raw attribute perks like "CC (-3)".
        if (IsSimpleStatModifier(right) && left.Contains("weapon", StringComparer.OrdinalIgnoreCase))
        {
            return false;
        }

        return IsTokenSubset(right, left);
    }

    private static bool IsSimpleStatModifier(IReadOnlyList<string> tokens)
    {
        if (tokens.Count != 2)
        {
            return false;
        }

        var statTokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "cc",
            "bs",
            "ph",
            "wip",
            "arm",
            "bts",
            "vita",
            "mov",
            "s"
        };

        if (!statTokens.Contains(tokens[0]))
        {
            return false;
        }

        return int.TryParse(tokens[1], out _) ||
               Regex.IsMatch(tokens[1], @"^(?:plus|minus)\d+$", RegexOptions.IgnoreCase);
    }

    private static bool IsTokenSubset(IReadOnlyList<string> maybeSubset, IReadOnlyList<string> maybeSuperset)
    {
        if (maybeSubset.Count > maybeSuperset.Count)
        {
            return false;
        }

        var set = new HashSet<string>(maybeSuperset, StringComparer.OrdinalIgnoreCase);
        return maybeSubset.All(set.Contains);
    }

    private static HashSet<string> ExtractLookupNames(
        JsonElement container,
        string propertyName,
        IReadOnlyDictionary<int, string>? lookup,
        IReadOnlyDictionary<int, string>? extrasLookup)
    {
        var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (lookup is null ||
            container.ValueKind != JsonValueKind.Object ||
            !container.TryGetProperty(propertyName, out var values) ||
            values.ValueKind != JsonValueKind.Array)
        {
            return names;
        }

        foreach (var entry in values.EnumerateArray())
        {
            if (entry.ValueKind != JsonValueKind.Object ||
                !entry.TryGetProperty("id", out var idEl) ||
                idEl.ValueKind != JsonValueKind.Number)
            {
                continue;
            }

            var id = idEl.GetInt32();
            if (lookup.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name))
            {
                if (TryBuildQualifiedName(name, entry, extrasLookup, out var qualifiedName))
                {
                    names.Add(qualifiedName);
                }
                else
                {
                    names.Add(name);
                }
            }
        }

        return names;
    }

    private static bool TryBuildQualifiedName(
        string baseName,
        JsonElement entry,
        IReadOnlyDictionary<int, string>? extrasLookup,
        out string qualifiedName)
    {
        qualifiedName = string.Empty;
        if (extrasLookup is null ||
            !entry.TryGetProperty("extra", out var extrasEl) ||
            extrasEl.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var extraNames = new List<string>();
        foreach (var extra in extrasEl.EnumerateArray())
        {
            if (extra.ValueKind != JsonValueKind.Number)
            {
                continue;
            }

            var id = extra.GetInt32();
            if (extrasLookup.TryGetValue(id, out var extraName) && !string.IsNullOrWhiteSpace(extraName))
            {
                extraNames.Add(extraName);
            }
        }

        if (extraNames.Count == 0)
        {
            return false;
        }

        qualifiedName = $"{baseName} ({string.Join(", ", extraNames)})";
        return true;
    }

    private static Dictionary<string, OwnedPerk> BuildOwnedPerkLookup()
    {
        var lookup = new Dictionary<string, OwnedPerk>(StringComparer.OrdinalIgnoreCase);
        var listMetadata = CompanyPerkCatalog
            .GetPerkListCatalogEntries()
            .ToDictionary(x => x.Id, StringComparer.OrdinalIgnoreCase);

        foreach (var list in CompanyPerkCatalog.GetPerkNodeLists())
        {
            listMetadata.TryGetValue(list.ListId, out var metadata);
            var listName = metadata?.Name ?? list.ListName;

            foreach (var node in Flatten(list.Roots))
            {
                if (string.IsNullOrWhiteSpace(node.Id))
                {
                    continue;
                }

                if (!TryParseTrackTier(node.Id, out var trackNumber, out var tier))
                {
                    continue;
                }

                lookup[node.Id] = new OwnedPerk
                {
                    Id = node.Id,
                    ListId = list.ListId,
                    ListName = listName,
                    TrackNumber = trackNumber,
                    Tier = tier,
                    PerkText = node.Name
                };
            }
        }

        return lookup;
    }

    private static string Normalize(string input)
    {
        var canonical = CanonicalizeSemanticShorthand(input
            .Trim()
            .ToLowerInvariant());

        var chars = canonical
            .Select(ch => char.IsLetterOrDigit(ch) ? ch : ' ')
            .ToArray();
        return string.Join(
            ' ',
            new string(chars).Split(' ', StringSplitOptions.RemoveEmptyEntries));
    }

    private static string CanonicalizeSemanticShorthand(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        // Keep Burst ("+1B") distinct from Ballistic Skill ("+1 BS").
        var value = Regex.Replace(input, @"\+?\s*(\d+)\s*bs\b", " ballistic_skill_plus_$1 ");
        value = Regex.Replace(value, @"\+?\s*(\d+)\s*b\b", " burst_plus_$1 ");

        // Preserve signed numeric modifiers (for example "+3", "-3") so
        // "Infiltration" does not match "Infiltration (+3)".
        value = Regex.Replace(value, @"\+\s*(\d+)\b", " plus$1 ");
        value = Regex.Replace(value, @"-\s*(\d+)\b", " minus$1 ");

        // Preserve limited-use tags (for example "1 Use").
        value = Regex.Replace(value, @"\b(\d+)\s*use\b", " limiteduse$1 ");
        return value;
    }

    private static bool ContainsLimitedUseToken(string normalizedTerm)
    {
        return Regex.IsMatch(normalizedTerm, @"\blimiteduse\d+\b", RegexOptions.IgnoreCase);
    }

    private static bool IsBaseHackingDeviceRequirement(string normalizedTerm)
    {
        return Regex.IsMatch(normalizedTerm, @"\bhacking device\b", RegexOptions.IgnoreCase) &&
               !Regex.IsMatch(normalizedTerm, @"\b(?:killer|evo)\b", RegexOptions.IgnoreCase);
    }

    private static bool IsSpecializedHackingDevice(string normalizedTerm)
    {
        return Regex.IsMatch(normalizedTerm, @"\bhacking device\b", RegexOptions.IgnoreCase) &&
               Regex.IsMatch(normalizedTerm, @"\b(?:killer|evo)\b", RegexOptions.IgnoreCase);
    }

    private static bool IsSkillOnlyKitWithPhValue(string rawTerm)
    {
        if (string.IsNullOrWhiteSpace(rawTerm))
        {
            return false;
        }

        return Regex.IsMatch(
            rawTerm,
            @"^\s*(?:gizmokit|medikit)\s*\(\s*ph\s*=\s*\d+\s*\)\s*$",
            RegexOptions.IgnoreCase);
    }

    private static void CollectMatches(
        PerkNode node,
        IReadOnlyCollection<string> profileTerms,
        HashSet<string> matchedIds)
    {
        if (!string.IsNullOrWhiteSpace(node.Id) &&
            !string.IsNullOrWhiteSpace(node.Name))
        {
            var requirementGroups = ExtractPerkRequirementGroups(node.Name);
            if (requirementGroups.Count > 0 &&
                requirementGroups.All(group =>
                    group.Any(optionTerm => profileTerms.Any(profileTerm => IsMatch(profileTerm, optionTerm)))))
            {
                matchedIds.Add(node.Id);
            }
        }

        foreach (var child in node.Children)
        {
            CollectMatches(child, profileTerms, matchedIds);
        }
    }

    private static IEnumerable<string> SplitTopLevel(string text, char separator)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        var start = 0;
        var depth = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch == '(')
            {
                depth++;
            }
            else if (ch == ')')
            {
                depth = Math.Max(0, depth - 1);
            }
            else if (ch == separator && depth == 0)
            {
                var part = text[start..i].Trim();
                if (part.Length > 0)
                {
                    yield return part;
                }

                start = i + 1;
            }
        }

        var last = text[start..].Trim();
        if (last.Length > 0)
        {
            yield return last;
        }
    }

    private static IEnumerable<string> SplitTopLevelOr(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            yield break;
        }

        var start = 0;
        var depth = 0;
        for (var i = 0; i < text.Length; i++)
        {
            var ch = text[i];
            if (ch == '(')
            {
                depth++;
                continue;
            }

            if (ch == ')')
            {
                depth = Math.Max(0, depth - 1);
                continue;
            }

            if (depth == 0 &&
                i + 3 < text.Length &&
                text[i] == ' ' &&
                text[i + 1] == 'O' &&
                text[i + 2] == 'R' &&
                text[i + 3] == ' ')
            {
                var part = text[start..i].Trim();
                if (part.Length > 0)
                {
                    yield return part;
                }

                start = i + 4;
                i += 3;
            }
        }

        var last = text[start..].Trim();
        if (last.Length > 0)
        {
            yield return last;
        }
    }

    private static string RemoveOptionalMetadata(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var result = text
            .Replace("Must be Captain", string.Empty, StringComparison.OrdinalIgnoreCase)
            .Trim();

        var optionalParentheticalPhrases = new[]
        {
            "no slot required",
            "role",
            "accessory",
            "reworked",
            "in active"
        };

        var builder = new System.Text.StringBuilder(result.Length);
        for (var i = 0; i < result.Length; i++)
        {
            var ch = result[i];
            if (ch != '(')
            {
                builder.Append(ch);
                continue;
            }

            var close = result.IndexOf(')', i + 1);
            if (close < 0)
            {
                builder.Append(ch);
                continue;
            }

            var inner = result.Substring(i + 1, close - i - 1).Trim().ToLowerInvariant();
            if (optionalParentheticalPhrases.Contains(inner))
            {
                i = close;
                continue;
            }

            builder.Append(result, i, close - i + 1);
            i = close;
        }

        return builder.ToString();
    }

    private static IEnumerable<string> ExpandKnownAliases(string rawText)
    {
        if (string.IsNullOrWhiteSpace(rawText))
        {
            yield break;
        }

        // Army data often stores Forward Deployment as centimeters (for example +20)
        // while perk text is written in inches (for example +8").
        if (rawText.Contains("Forward Deployment", StringComparison.OrdinalIgnoreCase))
        {
            var numberText = new string(rawText.Where(char.IsDigit).ToArray());
            if (int.TryParse(numberText, out var centimeters) && centimeters > 0)
            {
                var inches = (int)Math.Round(centimeters / 2.54d, MidpointRounding.AwayFromZero);
                yield return $"Forward Deployment (+{inches}\")";
            }
        }

        var lower = rawText.ToLowerInvariant();
        var martialMatch = Regex.Match(lower, @"martial\s+arts\s+l\s*(\d+)");
        if (martialMatch.Success && int.TryParse(martialMatch.Groups[1].Value, out var level))
        {
            if (level >= 1)
            {
                yield return "Martial Arts L1";
            }

            if (level >= 3)
            {
                yield return "Martial Arts L3";
            }

            if (level >= 5)
            {
                yield return "Martial Arts L5";
            }
        }
    }

    private static bool IsTagProfile(JsonElement profile)
    {
        if (profile.ValueKind != JsonValueKind.Object ||
            !profile.TryGetProperty("type", out var typeEl))
        {
            return false;
        }

        if (typeEl.ValueKind == JsonValueKind.Number && typeEl.TryGetInt32(out var typeId))
        {
            return typeId == 4;
        }

        if (typeEl.ValueKind == JsonValueKind.String &&
            int.TryParse(typeEl.GetString(), out var parsedTypeId))
        {
            return parsedTypeId == 4;
        }

        return false;
    }

    private static bool TryParseTrackTier(string nodeId, out int track, out int tier)
    {
        track = 0;
        tier = 0;
        var match = Regex.Match(nodeId, @"-track-(?<track>\d+)-tier-(?<tier>\d+)", RegexOptions.IgnoreCase);
        if (!match.Success)
        {
            return false;
        }

        return int.TryParse(match.Groups["track"].Value, out track) &&
               int.TryParse(match.Groups["tier"].Value, out tier);
    }

    private static IEnumerable<PerkNode> Flatten(IEnumerable<PerkNode> roots)
    {
        var stack = new Stack<PerkNode>(roots.Reverse());
        while (stack.Count > 0)
        {
            var node = stack.Pop();
            yield return node;
            for (var i = node.Children.Count - 1; i >= 0; i--)
            {
                stack.Push(node.Children[i]);
            }
        }
    }
}
