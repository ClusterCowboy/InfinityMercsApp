using System.Globalization;
using System.Text;

namespace InfinityMercsApp.Views.StandardCompany;

public static class StandardCompanyTeamService
{
    public static void RefreshTeamEntryVisibility(
        IReadOnlyList<ArmyTeamListItem> teamEntries,
        IReadOnlyList<ArmyUnitSelectionItem> units)
    {
        if (teamEntries.Count == 0)
        {
            return;
        }

        var visibleUnits = units.Where(x => x.IsVisible).ToList();
        var visibleUnitNames = new HashSet<string>(
            visibleUnits.Select(x => x.Name),
            StringComparer.OrdinalIgnoreCase);
        var visibleUnitSlugs = new HashSet<string>(
            visibleUnits
                .Select(x => x.Slug?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))!
                .Select(x => x!),
            StringComparer.OrdinalIgnoreCase);
        var visibleNormalizedNames = visibleUnits
            .Select(x => NormalizeTeamUnitName(x.Name))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        foreach (var team in teamEntries)
        {
            var visibleAllowedCount = 0;
            foreach (var allowed in team.AllowedProfiles)
            {
                if (allowed.IsCharacter)
                {
                    allowed.IsVisible = false;
                    continue;
                }

                var isVisible = IsVisibleTeamAllowedProfile(
                    allowed.Name,
                    allowed.Slug,
                    visibleUnitNames,
                    visibleUnitSlugs,
                    visibleNormalizedNames,
                    treatWildcardAsAlwaysVisible: !team.IsWildcardBucket,
                    visibleUnits: visibleUnits,
                    resolvedUnitId: allowed.ResolvedUnitId,
                    resolvedSourceFactionId: allowed.ResolvedSourceFactionId);
                allowed.IsVisible = isVisible;
                if (isVisible)
                {
                    visibleAllowedCount++;
                }
            }

            team.IsVisible = visibleAllowedCount > 0;
            team.IsExpanded = true;
        }
    }

    public static Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> FilterCharacterUnitLimits(
        Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> unitLimits,
        IEnumerable<ArmyUnitSelectionItem> sourceUnits)
    {
        var filtered = new Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)>(StringComparer.OrdinalIgnoreCase);

        foreach (var unitLimit in unitLimits)
        {
            var matchedUnit = ResolveUnitForTeamEntry(unitLimit.Key, unitLimit.Value.Slug, sourceUnits);
            if (matchedUnit?.IsCharacter == true)
            {
                continue;
            }

            filtered[unitLimit.Key] = unitLimit.Value;
        }

        return filtered;
    }

    public static Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> FilterWildcardUnitLimits(
        Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> unitLimits)
    {
        var filtered = new Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)>(StringComparer.OrdinalIgnoreCase);

        foreach (var unitLimit in unitLimits)
        {
            if (IsWildcardEntry(unitLimit.Key, unitLimit.Value.Slug))
            {
                continue;
            }

            filtered[unitLimit.Key] = unitLimit.Value;
        }

        return filtered;
    }

    public static List<ArmyTeamUnitLimitItem> BuildAllowedTeamProfiles(
        Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> unitLimits,
        IEnumerable<ArmyUnitSelectionItem> sourceUnits)
    {
        var merged = new Dictionary<string, (string Name, int Min, int Max, string? Slug, bool MinAsterisk, bool IsWildcard)>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in unitLimits)
        {
            var name = entry.Key;
            var value = entry.Value;
            var isWildcard = IsWildcardEntry(name, value.Slug);
            var key = isWildcard ? "__WILDCARD__" : name.Trim();
            var normalizedName = isWildcard ? "Wildcards" : name;

            if (merged.TryGetValue(key, out var existing))
            {
                merged[key] = (
                    existing.Name,
                    Math.Min(existing.Min, value.Min),
                    Math.Max(existing.Max, value.Max),
                    string.IsNullOrWhiteSpace(existing.Slug) ? value.Slug : existing.Slug,
                    existing.MinAsterisk || value.MinAsterisk,
                    existing.IsWildcard || isWildcard);
                continue;
            }

            merged[key] = (
                normalizedName,
                value.Min,
                value.Max,
                value.Slug,
                value.MinAsterisk,
                isWildcard);
        }

        return merged.Values
            .OrderBy(x => x.IsWildcard ? 1 : 0)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => BuildTeamUnitLimitItem(
                x.Name,
                x.MinAsterisk ? "*" : x.Min.ToString(),
                x.Max.ToString(),
                x.Slug,
                sourceUnits))
            .ToList();
    }

    public static ArmyTeamUnitLimitItem BuildTeamUnitLimitItem(
        string displayName,
        string min,
        string max,
        string? slug,
        IEnumerable<ArmyUnitSelectionItem> sourceUnits)
    {
        var matched = ResolveUnitForTeamEntry(displayName, slug, sourceUnits);
        return new ArmyTeamUnitLimitItem
        {
            Name = displayName,
            Min = min,
            Max = max,
            Slug = slug,
            IsCharacter = matched?.IsCharacter ?? false,
            CachedLogoPath = matched?.CachedLogoPath,
            PackagedLogoPath = matched?.PackagedLogoPath,
            Subtitle = matched?.Subtitle,
            ResolvedUnitId = matched?.Id,
            ResolvedSourceFactionId = matched?.SourceFactionId
        };
    }

    public static bool IsWildcardEntry(string? name, string? slug)
    {
        return ContainsWildcardToken(name) || ContainsWildcardToken(slug);
    }

    public static bool IsWildcardTeamName(string? teamName)
    {
        return ContainsWildcardToken(teamName);
    }

    private static bool IsVisibleTeamAllowedProfile(
        string? allowedProfileName,
        string? allowedProfileSlug,
        HashSet<string> visibleUnitNames,
        HashSet<string> visibleUnitSlugs,
        IReadOnlyList<string> visibleNormalizedNames,
        bool treatWildcardAsAlwaysVisible = true,
        IReadOnlyList<ArmyUnitSelectionItem>? visibleUnits = null,
        int? resolvedUnitId = null,
        int? resolvedSourceFactionId = null)
    {
        if (treatWildcardAsAlwaysVisible && IsWildcardEntry(allowedProfileName, allowedProfileSlug))
        {
            return true;
        }

        // For wildcard bucket rows, only show entries that resolve to an actual visible unit.
        // This prevents false positives from comment text like "(Orc Troops, Bolts)".
        if (!treatWildcardAsAlwaysVisible)
        {
            if (!resolvedUnitId.HasValue || !resolvedSourceFactionId.HasValue || visibleUnits is null)
            {
                return false;
            }

            return visibleUnits.Any(x =>
                x.Id == resolvedUnitId.Value &&
                x.SourceFactionId == resolvedSourceFactionId.Value);
        }

        if (resolvedUnitId.HasValue && resolvedSourceFactionId.HasValue && visibleUnits is not null)
        {
            return visibleUnits.Any(x =>
                x.Id == resolvedUnitId.Value &&
                x.SourceFactionId == resolvedSourceFactionId.Value);
        }

        if (!string.IsNullOrWhiteSpace(allowedProfileSlug) &&
            (visibleUnitSlugs.Contains(allowedProfileSlug.Trim()) ||
             ContainsEquivalentSlug(visibleUnitSlugs, allowedProfileSlug)))
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(allowedProfileName))
        {
            return false;
        }

        if (visibleUnitNames.Contains(allowedProfileName))
        {
            return true;
        }

        var normalizedAllowed = NormalizeTeamUnitName(allowedProfileName);
        if (string.IsNullOrWhiteSpace(normalizedAllowed))
        {
            return false;
        }

        foreach (var normalizedVisible in visibleNormalizedNames)
        {
            if (string.Equals(normalizedAllowed, normalizedVisible, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static ArmyUnitSelectionItem? ResolveUnitForTeamEntry(
        string? allowedProfileName,
        string? allowedProfileSlug,
        IEnumerable<ArmyUnitSelectionItem> sourceUnits)
    {
        var reinforcementOnly = IsReinforcementTeamEntry(allowedProfileName);
        var preferredSourceUnits = reinforcementOnly
            ? sourceUnits.Where(IsReinforcementUnit).ToList()
            : sourceUnits.ToList();
        var fallbackSourceUnits = reinforcementOnly
            ? sourceUnits.Where(x => !IsReinforcementUnit(x)).ToList()
            : [];

        if (!string.IsNullOrWhiteSpace(allowedProfileSlug))
        {
            var slugMatch = preferredSourceUnits.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(x.Slug) &&
                string.Equals(x.Slug.Trim(), allowedProfileSlug.Trim(), StringComparison.Ordinal));
            if (slugMatch is not null)
            {
                return slugMatch;
            }

            // Rule 3 fallback: when first slug lookup misses, retry with case-insensitive compare.
            slugMatch = preferredSourceUnits.FirstOrDefault(x =>
                !string.IsNullOrWhiteSpace(x.Slug) &&
                string.Equals(x.Slug.Trim(), allowedProfileSlug.Trim(), StringComparison.OrdinalIgnoreCase));
            if (slugMatch is not null)
            {
                return slugMatch;
            }

            // Final fallback for inconsistent reinforcement slug prefixes in data (e.g. arjuna-unit vs reinf-arjuna-unit).
            var normalizedAllowedSlug = NormalizeSlugForLookup(allowedProfileSlug);
            if (!string.IsNullOrWhiteSpace(normalizedAllowedSlug))
            {
                slugMatch = preferredSourceUnits.FirstOrDefault(x =>
                    !string.IsNullOrWhiteSpace(x.Slug) &&
                    string.Equals(NormalizeSlugForLookup(x.Slug), normalizedAllowedSlug, StringComparison.Ordinal));
                if (slugMatch is not null)
                {
                    return slugMatch;
                }
            }

            if (!string.IsNullOrWhiteSpace(allowedProfileName))
            {
                var slugFallbackNameMatch = preferredSourceUnits.FirstOrDefault(x =>
                    string.Equals(x.Name, allowedProfileName, StringComparison.OrdinalIgnoreCase));
                if (slugFallbackNameMatch is not null)
                {
                    return slugFallbackNameMatch;
                }
            }

            // Final fallback when no reinforcement record exists for the entry.
            if (reinforcementOnly)
            {
                slugMatch = fallbackSourceUnits.FirstOrDefault(x =>
                    !string.IsNullOrWhiteSpace(x.Slug) &&
                    string.Equals(x.Slug.Trim(), allowedProfileSlug.Trim(), StringComparison.OrdinalIgnoreCase));
                if (slugMatch is not null)
                {
                    return slugMatch;
                }
            }

            return null;
        }

        if (string.IsNullOrWhiteSpace(allowedProfileName))
        {
            return null;
        }

        var exactNameMatch = preferredSourceUnits.FirstOrDefault(x =>
            string.Equals(x.Name, allowedProfileName, StringComparison.OrdinalIgnoreCase));
        if (exactNameMatch is not null)
        {
            return exactNameMatch;
        }

        var normalizedAllowed = NormalizeTeamUnitName(allowedProfileName);
        if (string.IsNullOrWhiteSpace(normalizedAllowed))
        {
            return null;
        }

        foreach (var unit in preferredSourceUnits)
        {
            var normalizedUnit = NormalizeTeamUnitName(unit.Name);
            if (string.IsNullOrWhiteSpace(normalizedUnit))
            {
                continue;
            }

            if (string.Equals(normalizedAllowed, normalizedUnit, StringComparison.Ordinal))
            {
                return unit;
            }
        }

        if (reinforcementOnly)
        {
            var fallbackNameMatch = fallbackSourceUnits.FirstOrDefault(x =>
                string.Equals(x.Name, allowedProfileName, StringComparison.OrdinalIgnoreCase));
            if (fallbackNameMatch is not null)
            {
                return fallbackNameMatch;
            }
        }

        return null;
    }

    private static bool ContainsWildcardToken(string? value)
    {
        return !string.IsNullOrWhiteSpace(value) &&
               (value.IndexOf("wildcard", StringComparison.OrdinalIgnoreCase) >= 0 ||
                value.IndexOf("wild", StringComparison.OrdinalIgnoreCase) >= 0);
    }

    private static bool ContainsEquivalentSlug(HashSet<string> visibleUnitSlugs, string allowedProfileSlug)
    {
        var normalizedAllowed = NormalizeSlugForLookup(allowedProfileSlug);
        if (string.IsNullOrWhiteSpace(normalizedAllowed))
        {
            return false;
        }

        foreach (var visibleSlug in visibleUnitSlugs)
        {
            if (string.Equals(NormalizeSlugForLookup(visibleSlug), normalizedAllowed, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string NormalizeTeamUnitName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return string.Empty;
        }

        var formD = name.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(formD.Length);
        foreach (var c in formD)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark)
            {
                continue;
            }

            if (char.IsLetterOrDigit(c))
            {
                builder.Append(char.ToUpperInvariant(c));
            }
        }

        return builder.ToString();
    }

    private static string NormalizeSlugForLookup(string? slug)
    {
        if (string.IsNullOrWhiteSpace(slug))
        {
            return string.Empty;
        }

        var lowered = slug.Trim().ToLowerInvariant().Replace('_', '-');
        var builder = new StringBuilder(lowered.Length);
        var previousWasDash = false;
        foreach (var c in lowered)
        {
            if (char.IsLetterOrDigit(c))
            {
                builder.Append(c);
                previousWasDash = false;
            }
            else if (!previousWasDash)
            {
                builder.Append('-');
                previousWasDash = true;
            }
        }

        var normalized = builder.ToString().Trim('-');
        if (normalized.StartsWith("reinf-", StringComparison.Ordinal))
        {
            normalized = normalized["reinf-".Length..];
        }

        if (normalized.EndsWith("-reinf", StringComparison.Ordinal))
        {
            normalized = normalized[..^"-reinf".Length];
        }

        return normalized;
    }

    private static bool IsReinforcementTeamEntry(string? allowedProfileName)
    {
        return !string.IsNullOrWhiteSpace(allowedProfileName) &&
               allowedProfileName.IndexOf("REINF", StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static bool IsReinforcementUnit(ArmyUnitSelectionItem unit)
    {
        return (!string.IsNullOrWhiteSpace(unit.Name) &&
                unit.Name.IndexOf("REINF", StringComparison.OrdinalIgnoreCase) >= 0) ||
               (!string.IsNullOrWhiteSpace(unit.Slug) &&
                unit.Slug.IndexOf("reinf", StringComparison.OrdinalIgnoreCase) >= 0);
    }
}
