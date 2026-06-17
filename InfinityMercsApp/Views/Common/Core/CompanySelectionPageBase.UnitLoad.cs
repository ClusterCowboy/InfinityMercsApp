using System.Collections.ObjectModel;
using ArmyFactionRecord = InfinityMercsApp.Domain.Models.Army.Faction;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;
using ArmySpecopsUnitRecord = InfinityMercsApp.Domain.Models.Army.SpecopsUnit;

namespace InfinityMercsApp.Views.Common;

public abstract partial class CompanySelectionPageBase
{
    /// <summary>
    /// Holds the combined results of a unit load operation: a dictionary of units keyed by
    /// their lookup key, and a dictionary of fireteam aggregates keyed by team name.
    /// </summary>
    protected sealed class CompanyMergedUnitsAndTeams
    {
        /// <summary>Units indexed by their unique lookup key for fast roster operations.</summary>
        public required Dictionary<string, ArmyUnitSelectionItem> UnitsByKey { get; init; }

        /// <summary>Fireteam aggregates indexed by team name, built from faction chart JSON.</summary>
        public required Dictionary<string, CompanyTeamAggregate> TeamsByName { get; init; }
    }

    /// <summary>
    /// Loads and merges resume and spec-ops units from all provided factions into a single
    /// <see cref="CompanyMergedUnitsAndTeams{TUnit}"/> result.
    /// Also caches unit logos and merges fireteam chart data as side effects.
    /// </summary>
    protected static async Task<CompanyMergedUnitsAndTeams> BuildMergedUnitsAndTeamsAsync<TFaction>(
        IReadOnlyCollection<TFaction> factions,
        Func<TFaction, int> readFactionId,
        Func<int, CancellationToken, IReadOnlyList<ArmyResumeRecord>> getResumeByFaction,
        Func<int, CancellationToken, Task<IReadOnlyList<ArmySpecopsUnitRecord>>> getSpecopsByFactionAsync,
        Func<int, CancellationToken, ArmyFactionRecord?> getFactionSnapshot,
        Func<int, IReadOnlyList<ArmyResumeRecord>, CancellationToken, Task> cacheUnitLogosAsync,
        Action<string?, Dictionary<string, CompanyTeamAggregate>> mergeFireteamEntries,
        Func<ArmyResumeRecord, IReadOnlyDictionary<int, string>, bool> isCharacterCategory,
        Func<ArmyResumeRecord, IReadOnlyDictionary<int, string>, IReadOnlyDictionary<int, string>, string?> buildUnitSubtitle,
        Func<int, ArmyResumeRecord, IReadOnlyDictionary<int, string>, IReadOnlyDictionary<int, string>, ArmyUnitSelectionItem> createResumeUnit,
        Func<int, ArmySpecopsUnitRecord, IReadOnlyDictionary<int, ArmyResumeRecord>, IReadOnlyList<ArmyResumeRecord>, IReadOnlyDictionary<int, string>, IReadOnlyDictionary<int, string>, ArmyUnitSelectionItem> createSpecopsUnit,
        CancellationToken cancellationToken)
    {
        var merged = await CompanySelectionUnitLoadWorkflow.BuildMergedUnitsAndTeamsAsync(
            factions,
            readFactionId,
            getResumeByFaction,
            getSpecopsByFactionAsync,
            getFactionSnapshot,
            cacheUnitLogosAsync,
            mergeFireteamEntries,
            createResumeUnit,
            createSpecopsUnit,
            cancellationToken);

        return new CompanyMergedUnitsAndTeams
        {
            UnitsByKey = merged.UnitsByKey,
            TeamsByName = merged.TeamsByName
        };
    }

    /// <summary>
    /// Clears and repopulates the observable units collection from the merged unit set.
    /// Triggers binding updates for the unit list view.
    /// </summary>
    protected static void PopulateUnitsCollection(
        ICollection<ArmyUnitSelectionItem> unitsTarget,
        IEnumerable<ArmyUnitSelectionItem> mergedUnits)
    {
        CompanySelectionUnitLoadWorkflow.PopulateUnitsCollection(unitsTarget, mergedUnits);
    }

    /// <summary>
    /// Builds a dictionary of wildcard (non-team-specific) unit limits derived from the merged
    /// team aggregates, keyed by unit key. Used to populate the wildcard catch-all team entry.
    /// </summary>
    protected static Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> BuildWildcardUnitLimits(
        IEnumerable<CompanyTeamAggregate> mergedTeams,
        IEnumerable<ArmyUnitSelectionItem> mergedUnits)
    {
        return CompanySelectionUnitLoadWorkflow.BuildWildcardUnitLimits(mergedTeams, mergedUnits);
    }

    /// <summary>
    /// Appends the wildcard team entry to the team list, representing units that can fill
    /// any fireteam slot. Only adds the entry when at least one wildcard limit exists.
    /// </summary>
    protected static void AppendWildcardTeamEntry(
        Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> wildcardUnitLimits,
        IEnumerable<ArmyUnitSelectionItem> mergedUnits,
        ICollection<ArmyTeamListItem> teamEntries,
        Func<string, string, string, string?, IEnumerable<ArmyUnitSelectionItem>, ArmyTeamUnitLimitItem> buildTeamUnitLimitItem,
        Func<string, string, bool, bool, ObservableCollection<ArmyTeamUnitLimitItem>, ArmyTeamListItem> createTeam)
    {
        CompanySelectionUnitLoadWorkflow.AppendWildcardTeamEntry(
            wildcardUnitLimits,
            mergedUnits,
            teamEntries,
            buildTeamUnitLimitItem,
            createTeam);
    }

    /// <summary>
    /// Converts the merged team aggregates into concrete team list items and adds them to
    /// <paramref name="teamEntries"/>. Teams are filtered and sorted according to the
    /// <paramref name="includeTeam"/> predicate and count accessors.
    /// </summary>
    protected static void BuildTeamEntriesFromMerged(
        CompanyMergedUnitsAndTeams merged,
        ICollection<ArmyTeamListItem> teamEntries,
        Func<CompanyTeamAggregate, bool> includeTeam,
        Func<CompanyTeamAggregate, int> readTeamCount,
        Func<CompanyTeamAggregate, string> buildTeamCountText,
        Func<string, string, string, string?, IEnumerable<ArmyUnitSelectionItem>, ArmyTeamUnitLimitItem> buildTeamUnitLimitItem,
        Func<string, string, bool, bool, ObservableCollection<ArmyTeamUnitLimitItem>, ArmyTeamListItem> createTeam)
    {
        CompanySelectionUnitLoadWorkflow.BuildTeamEntriesFromMerged(
            merged.UnitsByKey,
            merged.TeamsByName.Values,
            teamEntries,
            includeTeam,
            readTeamCount,
            buildTeamCountText,
            buildTeamUnitLimitItem,
            createTeam);
    }
}
