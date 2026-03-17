namespace InfinityMercsApp.Views.Common;

internal static class CompanyTeamProfilesWorkflow
{
    internal static Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> FilterCharacterUnitLimits<TUnit>(
        Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> unitLimits,
        IEnumerable<TUnit> sourceUnits,
        Func<string?, string?, IEnumerable<TUnit>, TUnit?> resolveUnitForTeamEntry,
        Func<TUnit, bool> readIsCharacter)
        where TUnit : class
    {
        var filtered = new Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)>(StringComparer.OrdinalIgnoreCase);

        foreach (var unitLimit in unitLimits)
        {
            var matchedUnit = resolveUnitForTeamEntry(unitLimit.Key, unitLimit.Value.Slug, sourceUnits);
            if (matchedUnit is not null && readIsCharacter(matchedUnit))
            {
                continue;
            }

            filtered[unitLimit.Key] = unitLimit.Value;
        }

        return filtered;
    }

    internal static Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> FilterCharacterUnitLimits<TUnit>(
        Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> unitLimits,
        IEnumerable<TUnit> sourceUnits,
        Func<TUnit, bool> readIsCharacter)
        where TUnit : CompanyUnitSelectionItemBase
    {
        return FilterCharacterUnitLimits(
            unitLimits,
            sourceUnits,
            (allowedProfileName, allowedProfileSlug, units) => CompanyTeamMatchingWorkflow.ResolveUnitForTeamEntry(
                allowedProfileName,
                allowedProfileSlug,
                units),
            readIsCharacter);
    }

    internal static Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> FilterWildcardUnitLimits(
        Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> unitLimits)
    {
        var filtered = new Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)>(StringComparer.OrdinalIgnoreCase);

        foreach (var unitLimit in unitLimits)
        {
            if (CompanyTeamMatchingWorkflow.IsWildcardEntry(unitLimit.Key, unitLimit.Value.Slug))
            {
                continue;
            }

            filtered[unitLimit.Key] = unitLimit.Value;
        }

        return filtered;
    }

    internal static List<TTeamUnitLimitItem> BuildAllowedTeamProfiles<TUnit, TTeamUnitLimitItem>(
        Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> unitLimits,
        IEnumerable<TUnit> sourceUnits,
        Func<string, string, string, string?, IEnumerable<TUnit>, TTeamUnitLimitItem> buildTeamUnitLimitItem)
        where TUnit : class
        where TTeamUnitLimitItem : class
    {
        var merged = new Dictionary<string, (string Name, int Min, int Max, string? Slug, bool MinAsterisk, bool IsWildcard)>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in unitLimits)
        {
            var name = entry.Key;
            var value = entry.Value;
            var isWildcard = CompanyTeamMatchingWorkflow.IsWildcardEntry(name, value.Slug);
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
            .Select(x => buildTeamUnitLimitItem(
                x.Name,
                x.MinAsterisk ? "*" : x.Min.ToString(),
                x.Max.ToString(),
                x.Slug,
                sourceUnits))
            .ToList();
    }

    internal static TTeamUnitLimitItem BuildTeamUnitLimitItem<TUnit, TTeamUnitLimitItem>(
        string displayName,
        string min,
        string max,
        string? slug,
        IEnumerable<TUnit> sourceUnits,
        Func<string?, string?, IEnumerable<TUnit>, TUnit?> resolveUnitForTeamEntry,
        Func<string, string, string, string?, TUnit?, TTeamUnitLimitItem> createItem)
        where TUnit : class
        where TTeamUnitLimitItem : class
    {
        var matched = resolveUnitForTeamEntry(displayName, slug, sourceUnits);
        return createItem(displayName, min, max, slug, matched);
    }

    internal static TTeamUnitLimitItem BuildTeamUnitLimitItem<TUnit, TTeamUnitLimitItem>(
        string displayName,
        string min,
        string max,
        string? slug,
        IEnumerable<TUnit> sourceUnits,
        Func<string?, string?, IEnumerable<TUnit>, TUnit?> resolveUnitForTeamEntry,
        Func<TUnit, bool> readIsCharacter,
        Func<TUnit, string?> readCachedLogoPath,
        Func<TUnit, string?> readPackagedLogoPath,
        Func<TUnit, string?> readSubtitle,
        Func<TUnit, int> readUnitId,
        Func<TUnit, int> readSourceFactionId)
        where TUnit : class
        where TTeamUnitLimitItem : CompanyTeamUnitLimitItemBase, new()
    {
        var matched = resolveUnitForTeamEntry(displayName, slug, sourceUnits);
        return new TTeamUnitLimitItem
        {
            Name = displayName,
            Min = min,
            Max = max,
            Slug = slug,
            IsCharacter = matched is not null && readIsCharacter(matched),
            CachedLogoPath = matched is null ? null : readCachedLogoPath(matched),
            PackagedLogoPath = matched is null ? null : readPackagedLogoPath(matched),
            Subtitle = matched is null ? null : readSubtitle(matched),
            ResolvedUnitId = matched is null ? null : readUnitId(matched),
            ResolvedSourceFactionId = matched is null ? null : readSourceFactionId(matched)
        };
    }

    internal static TTeamUnitLimitItem BuildTeamUnitLimitItem<TUnit, TTeamUnitLimitItem>(
        string displayName,
        string min,
        string max,
        string? slug,
        IEnumerable<TUnit> sourceUnits)
        where TUnit : CompanyUnitSelectionItemBase
        where TTeamUnitLimitItem : CompanyTeamUnitLimitItemBase, new()
    {
        return BuildTeamUnitLimitItem<TUnit, TTeamUnitLimitItem>(
            displayName,
            min,
            max,
            slug,
            sourceUnits,
            (allowedProfileName, allowedProfileSlug, units) => CompanyTeamMatchingWorkflow.ResolveUnitForTeamEntry(
                allowedProfileName,
                allowedProfileSlug,
                units),
            x => x.IsCharacter,
            x => x.CachedLogoPath,
            x => x.PackagedLogoPath,
            x => x.Subtitle,
            x => x.Id,
            x => x.SourceFactionId);
    }
}


