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
    protected sealed class CompanyMergedUnitsAndTeams<TUnit>
    {
        /// <summary>Units indexed by their unique lookup key for fast roster operations.</summary>
        public required Dictionary<string, TUnit> UnitsByKey { get; init; }

        /// <summary>Fireteam aggregates indexed by team name, built from faction chart JSON.</summary>
        public required Dictionary<string, CompanyTeamAggregate> TeamsByName { get; init; }
    }

    /// <summary>
    /// Loads and merges resume and spec-ops units from all provided factions into a single
    /// <see cref="CompanyMergedUnitsAndTeams{TUnit}"/> result.
    /// Also caches unit logos and merges fireteam chart data as side effects.
    /// </summary>
    protected static async Task<CompanyMergedUnitsAndTeams<TUnit>> BuildMergedUnitsAndTeamsAsync<TFaction, TUnit>(
        IReadOnlyCollection<TFaction> factions,
        Func<TFaction, int> readFactionId,
        Func<int, CancellationToken, IReadOnlyList<ArmyResumeRecord>> getResumeByFaction,
        Func<int, CancellationToken, Task<IReadOnlyList<ArmySpecopsUnitRecord>>> getSpecopsByFactionAsync,
        Func<int, CancellationToken, ArmyFactionRecord?> getFactionSnapshot,
        Func<int, IReadOnlyList<ArmyResumeRecord>, CancellationToken, Task> cacheUnitLogosAsync,
        Action<string?, Dictionary<string, CompanyTeamAggregate>> mergeFireteamEntries,
        Func<ArmyResumeRecord, IReadOnlyDictionary<int, string>, bool> isCharacterCategory,
        Func<ArmyResumeRecord, IReadOnlyDictionary<int, string>, IReadOnlyDictionary<int, string>, string?> buildUnitSubtitle,
        Func<int, ArmyResumeRecord, IReadOnlyDictionary<int, string>, IReadOnlyDictionary<int, string>, TUnit> createResumeUnit,
        Func<int, ArmySpecopsUnitRecord, IReadOnlyDictionary<int, ArmyResumeRecord>, IReadOnlyList<ArmyResumeRecord>, IReadOnlyDictionary<int, string>, IReadOnlyDictionary<int, string>, TUnit> createSpecopsUnit,
        CancellationToken cancellationToken)
        where TUnit : CompanyUnitSelectionItemBase
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

        return new CompanyMergedUnitsAndTeams<TUnit>
        {
            UnitsByKey = merged.UnitsByKey,
            TeamsByName = merged.TeamsByName
        };
    }

    /// <summary>
    /// Clears and repopulates the observable units collection from the merged unit set.
    /// Triggers binding updates for the unit list view.
    /// </summary>
    protected static void PopulateUnitsCollection<TUnit>(
        ICollection<TUnit> unitsTarget,
        IEnumerable<TUnit> mergedUnits)
        where TUnit : CompanyUnitSelectionItemBase
    {
        CompanySelectionUnitLoadWorkflow.PopulateUnitsCollection(unitsTarget, mergedUnits);
    }

    /// <summary>
    /// Builds a dictionary of wildcard (non-team-specific) unit limits derived from the merged
    /// team aggregates, keyed by unit key. Used to populate the wildcard catch-all team entry.
    /// </summary>
    protected static Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> BuildWildcardUnitLimits<TUnit>(
        IEnumerable<CompanyTeamAggregate> mergedTeams,
        IEnumerable<TUnit> mergedUnits)
        where TUnit : CompanyUnitSelectionItemBase
    {
        return CompanySelectionUnitLoadWorkflow.BuildWildcardUnitLimits(mergedTeams, mergedUnits);
    }

    /// <summary>
    /// Appends the wildcard team entry to the team list, representing units that can fill
    /// any fireteam slot. Only adds the entry when at least one wildcard limit exists.
    /// </summary>
    protected static void AppendWildcardTeamEntry<TUnit, TAllowed, TTeam>(
        Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> wildcardUnitLimits,
        IEnumerable<TUnit> mergedUnits,
        ICollection<TTeam> teamEntries,
        Func<string, string, string, string?, IEnumerable<TUnit>, TAllowed> buildTeamUnitLimitItem,
        Func<string, string, bool, bool, ObservableCollection<TAllowed>, TTeam> createTeam)
        where TUnit : CompanyUnitSelectionItemBase
        where TAllowed : CompanyTeamUnitLimitItemBase
        where TTeam : CompanyTeamListItemBase<TAllowed>
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
    protected static void BuildTeamEntriesFromMerged<TUnit, TAllowed, TTeam>(
        CompanyMergedUnitsAndTeams<TUnit> merged,
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
