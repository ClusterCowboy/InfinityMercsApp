using System.Collections.ObjectModel;
using ArmyFactionRecord = InfinityMercsApp.Domain.Models.Army.Faction;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;
using ArmySpecopsUnitRecord = InfinityMercsApp.Domain.Models.Army.SpecopsUnit;

namespace InfinityMercsApp.Views.Common;

public abstract partial class CompanySelectionPageBase
{
    protected sealed class CompanyMergedUnitsAndTeams<TUnit>
    {
        public required Dictionary<string, TUnit> UnitsByKey { get; init; }
        public required Dictionary<string, CompanyTeamAggregate> TeamsByName { get; init; }
    }

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

    protected static void PopulateUnitsCollection<TUnit>(
        ICollection<TUnit> unitsTarget,
        IEnumerable<TUnit> mergedUnits)
        where TUnit : CompanyUnitSelectionItemBase
    {
        CompanySelectionUnitLoadWorkflow.PopulateUnitsCollection(unitsTarget, mergedUnits);
    }

    protected static Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> BuildWildcardUnitLimits<TUnit>(
        IEnumerable<CompanyTeamAggregate> mergedTeams,
        IEnumerable<TUnit> mergedUnits)
        where TUnit : CompanyUnitSelectionItemBase
    {
        return CompanySelectionUnitLoadWorkflow.BuildWildcardUnitLimits(mergedTeams, mergedUnits);
    }

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
