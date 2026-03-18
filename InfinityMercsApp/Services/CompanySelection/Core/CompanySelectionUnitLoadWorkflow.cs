using System.Collections.ObjectModel;
using InfinityMercsApp.Domain.Utilities;
using InfinityMercsApp.Services;
using ArmyFactionRecord = InfinityMercsApp.Domain.Models.Army.Faction;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;
using ArmySpecopsUnitRecord = InfinityMercsApp.Domain.Models.Army.SpecopsUnit;

namespace InfinityMercsApp.Views.Common;

internal static class CompanySelectionUnitLoadWorkflow
{
    internal static async Task<(Dictionary<string, TUnit> UnitsByKey, Dictionary<string, CompanyTeamAggregate> TeamsByName)> BuildMergedUnitsAndTeamsAsync<TFaction, TUnit>(
        IReadOnlyCollection<TFaction> factions,
        Func<TFaction, int> readFactionId,
        Func<int, CancellationToken, IReadOnlyList<ArmyResumeRecord>> getResumeByFaction,
        Func<int, CancellationToken, Task<IReadOnlyList<ArmySpecopsUnitRecord>>> getSpecopsByFactionAsync,
        Func<int, CancellationToken, ArmyFactionRecord?> getFactionSnapshot,
        Func<int, IReadOnlyList<ArmyResumeRecord>, CancellationToken, Task> cacheUnitLogosAsync,
        Action<string?, Dictionary<string, CompanyTeamAggregate>> mergeFireteamEntries,
        Func<int, ArmyResumeRecord, IReadOnlyDictionary<int, string>, IReadOnlyDictionary<int, string>, TUnit> createResumeUnit,
        Func<int, ArmySpecopsUnitRecord, IReadOnlyDictionary<int, ArmyResumeRecord>, IReadOnlyList<ArmyResumeRecord>, IReadOnlyDictionary<int, string>, IReadOnlyDictionary<int, string>, TUnit> createSpecopsUnit,
        CancellationToken cancellationToken)
        where TUnit : CompanyUnitSelectionItemBase
    {
        var mergedUnits = new Dictionary<string, TUnit>(StringComparer.OrdinalIgnoreCase);
        var mergedTeams = new Dictionary<string, CompanyTeamAggregate>(StringComparer.OrdinalIgnoreCase);

        foreach (var faction in factions)
        {
            var factionId = readFactionId(faction);
            var units = getResumeByFaction(factionId, cancellationToken);
            var resumeByUnitId = units
                .GroupBy(x => x.UnitId)
                .ToDictionary(x => x.Key, x => x.First());
            var specopsUnits = await getSpecopsByFactionAsync(factionId, cancellationToken);
            var snapshot = getFactionSnapshot(factionId, cancellationToken);
            var typeLookup = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot?.FiltersJson, "type");
            var categoryLookup = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot?.FiltersJson, "category");
            mergeFireteamEntries(snapshot?.FireteamChartJson, mergedTeams);

            await cacheUnitLogosAsync(factionId, units, cancellationToken);

            foreach (var unit in units)
            {
                var key = unit.Name.Trim();
                if (string.IsNullOrWhiteSpace(key) || mergedUnits.ContainsKey(key))
                {
                    continue;
                }

                mergedUnits[key] = createResumeUnit(factionId, unit, typeLookup, categoryLookup);
            }

            foreach (var specopsUnit in specopsUnits.OrderBy(x => x.EntryOrder))
            {
                var baseName = string.IsNullOrWhiteSpace(specopsUnit.Name)
                    ? units.FirstOrDefault(x => x.UnitId == specopsUnit.UnitId)?.Name ?? $"Unit {specopsUnit.UnitId}"
                    : specopsUnit.Name.Trim();
                var key = $"{baseName} - Spec Ops";
                if (string.IsNullOrWhiteSpace(key) || mergedUnits.ContainsKey(key))
                {
                    continue;
                }

                mergedUnits[key] = createSpecopsUnit(
                    factionId,
                    specopsUnit,
                    resumeByUnitId,
                    units,
                    typeLookup,
                    categoryLookup);
            }
        }

        return (mergedUnits, mergedTeams);
    }

    internal static void PopulateUnitsCollection<TUnit>(
        ICollection<TUnit> unitsTarget,
        IEnumerable<TUnit> mergedUnits)
        where TUnit : CompanyUnitSelectionItemBase
    {
        foreach (var unit in ArmyUnitSort.OrderByUnitTypeAndName(mergedUnits, x => x.Type, x => x.Name))
        {
            unitsTarget.Add(unit);
        }
    }

    internal static Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> BuildWildcardUnitLimits<TUnit>(
        IEnumerable<CompanyTeamAggregate> mergedTeams,
        IEnumerable<TUnit> mergedUnits)
        where TUnit : CompanyUnitSelectionItemBase
    {
        var wildcardUnitLimits = new Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)>(StringComparer.OrdinalIgnoreCase);
        foreach (var team in mergedTeams)
        {
            var isWildcardTeam = CompanyTeamMatchingWorkflow.IsWildcardTeamName(team.Name);
            var nonCharacterUnitLimits = CompanyTeamProfilesWorkflow.FilterCharacterUnitLimits(
                team.UnitLimits,
                mergedUnits,
                x => x.IsCharacter);
            foreach (var entry in nonCharacterUnitLimits)
            {
                var unitName = entry.Key;
                var value = entry.Value;
                if (!isWildcardTeam && !CompanyTeamMatchingWorkflow.IsWildcardEntry(unitName, value.Slug))
                {
                    continue;
                }

                if (wildcardUnitLimits.TryGetValue(unitName, out var existing))
                {
                    wildcardUnitLimits[unitName] = (
                        Math.Min(existing.Min, value.Min),
                        Math.Max(existing.Max, value.Max),
                        string.IsNullOrWhiteSpace(existing.Slug) ? value.Slug : existing.Slug,
                        existing.MinAsterisk || value.MinAsterisk);
                }
                else
                {
                    wildcardUnitLimits[unitName] = value;
                }
            }
        }

        return wildcardUnitLimits;
    }

    internal static void AppendWildcardTeamEntry<TUnit, TAllowed, TTeam>(
        Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> wildcardUnitLimits,
        IEnumerable<TUnit> mergedUnits,
        ICollection<TTeam> teamEntries,
        Func<string, string, string, string?, IEnumerable<TUnit>, TAllowed> buildTeamUnitLimitItem,
        Func<string, string, bool, bool, ObservableCollection<TAllowed>, TTeam> createTeam)
        where TUnit : CompanyUnitSelectionItemBase
        where TAllowed : CompanyTeamUnitLimitItemBase
        where TTeam : CompanyTeamListItemBase<TAllowed>
    {
        if (wildcardUnitLimits.Count == 0)
        {
            return;
        }

        var wildcardAllowedProfiles = wildcardUnitLimits
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => buildTeamUnitLimitItem(
                x.Key,
                x.Value.MinAsterisk ? "*" : x.Value.Min.ToString(),
                x.Value.Max.ToString(),
                x.Value.Slug,
                mergedUnits))
            .Where(x => !x.IsCharacter)
            .ToList();

        if (wildcardAllowedProfiles.Count == 0)
        {
            return;
        }

        teamEntries.Add(createTeam(
            "Wildcards",
            string.Empty,
            true,
            true,
            new ObservableCollection<TAllowed>(wildcardAllowedProfiles)));
    }

    internal static void BuildTeamEntriesFromMerged<TUnit, TAllowed, TTeam>(
        IReadOnlyDictionary<string, TUnit> unitsByKey,
        IEnumerable<CompanyTeamAggregate> mergedTeams,
        ICollection<TTeam> teamEntries,
        Func<CompanyTeamAggregate, bool> includeTeam,
        Func<CompanyTeamAggregate, int> readTeamCount,
        Func<CompanyTeamAggregate, string> buildTeamCountText,
        Func<string, string, string, string?, IEnumerable<TUnit>, TAllowed> buildTeamUnitLimitItem,
        Func<string, string, bool, bool, ObservableCollection<TAllowed>, TTeam> createTeam)
        where TUnit : CompanyUnitSelectionItemBase
        where TAllowed : CompanyTeamUnitLimitItemBase
        where TTeam : CompanyTeamListItemBase<TAllowed>
    {
        foreach (var team in mergedTeams
                     .Where(includeTeam)
                     .Where(x => readTeamCount(x) > 0)
                     .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
        {
            var nonCharacterUnitLimits = CompanyTeamProfilesWorkflow.FilterCharacterUnitLimits(
                team.UnitLimits,
                unitsByKey.Values,
                x => x.IsCharacter);
            var nonCharacterNonWildcardUnitLimits = CompanyTeamProfilesWorkflow.FilterWildcardUnitLimits(nonCharacterUnitLimits);
            var allowedProfiles = CompanyTeamProfilesWorkflow.BuildAllowedTeamProfiles(
                nonCharacterNonWildcardUnitLimits,
                unitsByKey.Values,
                buildTeamUnitLimitItem);
            if (allowedProfiles.Count == 0)
            {
                continue;
            }

            teamEntries.Add(createTeam(
                team.Name,
                buildTeamCountText(team),
                false,
                true,
                new ObservableCollection<TAllowed>(allowedProfiles)));
        }

        var wildcardUnitLimits = BuildWildcardUnitLimits(
            mergedTeams,
            unitsByKey.Values);
        AppendWildcardTeamEntry(
            wildcardUnitLimits,
            unitsByKey.Values,
            teamEntries,
            buildTeamUnitLimitItem,
            createTeam);
    }
}
