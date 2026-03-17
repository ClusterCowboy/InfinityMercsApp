using InfinityMercsApp.Views.Controls;
using ArmyFactionRecord = InfinityMercsApp.Domain.Models.Army.Faction;
using ArmySpecopsUnitRecord = InfinityMercsApp.Domain.Models.Army.SpecopsUnit;
using ArmyUnitRecord = InfinityMercsApp.Domain.Models.Army.Unit;

namespace InfinityMercsApp.Views.Common;

public abstract partial class CompanySelectionPageBase
{
    protected static int ComputePointsRemaining(string selectedStartSeasonPoints, string seasonPointsCapText)
    {
        return CompanySelectionVisibilityWorkflow.ComputePointsRemaining(selectedStartSeasonPoints, seasonPointsCapText);
    }

    protected static int ComputePointsRemaining(ICompanySelectionVisibilityState state)
    {
        return CompanySelectionVisibilityWorkflow.ComputePointsRemaining(state);
    }

    protected static async Task<CompanyUnitVisibilityLookupContext> BuildUnitVisibilityLookupContextAsync<TFaction>(
        IEnumerable<TFaction> factions,
        Func<TFaction, int> readFactionId,
        Func<int, CancellationToken, ArmyFactionRecord?> getFactionSnapshot,
        Func<int, CancellationToken, Task<IReadOnlyList<ArmySpecopsUnitRecord>>> getSpecopsUnitsByFactionAsync,
        CancellationToken cancellationToken)
    {
        return await CompanySelectionVisibilityWorkflow.BuildUnitVisibilityLookupContextAsync(
            factions,
            readFactionId,
            getFactionSnapshot,
            getSpecopsUnitsByFactionAsync,
            cancellationToken);
    }

    protected static void ApplyUnitVisibilityCore<TUnit>(
        IEnumerable<TUnit> units,
        CompanyUnitVisibilityLookupContext lookupContext,
        UnitFilterCriteria activeUnitFilter,
        bool lieutenantOnlyUnits,
        int pointsRemaining,
        Func<int, int, CancellationToken, ArmyUnitRecord?> getUnitByFactionAndId,
        CancellationToken cancellationToken)
        where TUnit : CompanyUnitSelectionItemBase
    {
        CompanySelectionVisibilityWorkflow.ApplyUnitVisibility(
            units,
            lookupContext,
            activeUnitFilter,
            lieutenantOnlyUnits,
            pointsRemaining,
            getUnitByFactionAndId,
            cancellationToken);
    }

    protected static TUnit? RefreshSelectedUnitVisibilityCore<TUnit>(
        TUnit? selectedUnit,
        Action resetUnitDetails,
        Action applyLieutenantVisualStates)
        where TUnit : CompanyUnitSelectionItemBase
    {
        return CompanySelectionVisibilityWorkflow.RefreshSelectedUnitVisibility(
            selectedUnit,
            resetUnitDetails,
            applyLieutenantVisualStates);
    }

    protected static void RefreshTeamEntryVisibilityCore<TTeam, TAllowed, TUnit>(
        IEnumerable<TTeam> teamEntries,
        IEnumerable<TUnit> units)
        where TTeam : CompanyTeamListItemBase<TAllowed>
        where TAllowed : CompanyTeamUnitLimitItemBase
        where TUnit : CompanyUnitSelectionItemBase
    {
        CompanySelectionVisibilityWorkflow.RefreshTeamEntryVisibility<TTeam, TAllowed, TUnit>(
            teamEntries,
            units);
    }
}
