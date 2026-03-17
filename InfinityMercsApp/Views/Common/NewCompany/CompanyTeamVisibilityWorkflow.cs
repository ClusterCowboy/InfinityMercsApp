namespace InfinityMercsApp.Views.Common.NewCompany;

internal static class CompanyTeamVisibilityWorkflow
{
    internal static void RefreshTeamEntryVisibility<TTeam, TAllowedProfile, TUnit>(
        IReadOnlyList<TTeam> teamEntries,
        IReadOnlyList<TUnit> units,
        Func<TTeam, IEnumerable<TAllowedProfile>> readAllowedProfiles,
        Func<TTeam, bool> readIsWildcardBucket,
        Action<TTeam, bool> setTeamIsVisible,
        Action<TTeam, bool> setTeamIsExpanded,
        Func<TAllowedProfile, bool> readAllowedIsCharacter,
        Action<TAllowedProfile, bool> setAllowedIsVisible,
        Func<TAllowedProfile, string?> readAllowedName,
        Func<TAllowedProfile, string?> readAllowedSlug,
        Func<TAllowedProfile, int?> readAllowedResolvedUnitId,
        Func<TAllowedProfile, int?> readAllowedResolvedSourceFactionId,
        Func<TUnit, bool> readUnitIsVisible,
        Func<TUnit, int> readUnitId,
        Func<TUnit, int> readUnitSourceFactionId,
        Func<TUnit, string> readUnitName,
        Func<TUnit, string?> readUnitSlug)
        where TTeam : class
        where TAllowedProfile : class
        where TUnit : class
    {
        if (teamEntries.Count == 0)
        {
            return;
        }

        var visibleUnits = units.Where(readUnitIsVisible).ToList();
        var visibleUnitNames = new HashSet<string>(
            visibleUnits.Select(readUnitName),
            StringComparer.OrdinalIgnoreCase);
        var visibleUnitSlugs = new HashSet<string>(
            visibleUnits
                .Select(x => readUnitSlug(x)?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))!
                .Select(x => x!),
            StringComparer.OrdinalIgnoreCase);
        var visibleNormalizedNames = visibleUnits
            .Select(x => CompanyTeamMatchingWorkflow.NormalizeTeamUnitName(readUnitName(x)))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.Ordinal)
            .ToList();

        foreach (var team in teamEntries)
        {
            var visibleAllowedCount = 0;
            foreach (var allowed in readAllowedProfiles(team))
            {
                if (readAllowedIsCharacter(allowed))
                {
                    setAllowedIsVisible(allowed, false);
                    continue;
                }

                var isVisible = CompanyTeamMatchingWorkflow.IsVisibleTeamAllowedProfile(
                    readAllowedName(allowed),
                    readAllowedSlug(allowed),
                    visibleUnitNames,
                    visibleUnitSlugs,
                    visibleNormalizedNames,
                    treatWildcardAsAlwaysVisible: !readIsWildcardBucket(team),
                    visibleUnits: visibleUnits,
                    resolvedUnitId: readAllowedResolvedUnitId(allowed),
                    resolvedSourceFactionId: readAllowedResolvedSourceFactionId(allowed),
                    readUnitId: readUnitId,
                    readSourceFactionId: readUnitSourceFactionId);
                setAllowedIsVisible(allowed, isVisible);
                if (isVisible)
                {
                    visibleAllowedCount++;
                }
            }

            setTeamIsVisible(team, visibleAllowedCount > 0);
            setTeamIsExpanded(team, true);
        }
    }
}

