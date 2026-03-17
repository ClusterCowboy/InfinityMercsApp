using InfinityMercsApp.Views.Common;

namespace InfinityMercsApp.Views.CohesiveCompany;

public partial class CohesiveCompanySelectionPage
{
    private async Task ApplyUnitVisibilityFiltersAsync(CancellationToken cancellationToken = default)
    {
        AreTeamEntriesReady = false;
        if (Units.Count == 0)
        {
            return;
        }

        try
        {
            var pointsRemaining = ComputePointsRemaining(this);

            var factions = CompanyUnitDetailsShared.BuildUnitSourceFactions(
                ShowRightSelectionBox,
                _factionSelectionState.LeftSlotFaction,
                _factionSelectionState.RightSlotFaction,
                faction => faction.Id);
            var lookupContext = await BuildUnitVisibilityLookupContextAsync(
                factions,
                faction => faction.Id,
                GetFactionSnapshotFromProvider,
                _specOpsProvider.GetSpecopsUnitsByFactionAsync,
                cancellationToken);

            ApplyUnitVisibilityCore(
                Units,
                lookupContext,
                _activeUnitFilter,
                LieutenantOnlyUnits,
                pointsRemaining,
                GetUnitFromProvider,
                cancellationToken);

            _selectedUnit = RefreshSelectedUnitVisibilityCore(
                _selectedUnit,
                () => ResetUnitDetails(),
                ApplyLieutenantVisualStates);

            RefreshTeamEntryVisibilityCore<ArmyTeamListItem, ArmyTeamUnitLimitItem, ArmyUnitSelectionItem>(
                TeamEntries,
                Units);
            AreTeamEntriesReady = true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage ApplyUnitVisibilityFiltersAsync failed: {ex.Message}");
            AreTeamEntriesReady = false;
        }
    }
}
