using InfinityMercsApp.Views.Common;

namespace InfinityMercsApp.Views.InspiringCompany;

public partial class InspiringCompanySelectionPage
{
    private async Task ApplyUnitVisibilityFiltersAsync(CancellationToken cancellationToken = default)
    {
        if (Units.Count == 0)
        {
            return;
        }

        try
        {
            var pointsRemaining = ComputePointsRemaining(this);

            var factions = CompanyUnitDetailsShared.BuildUnitSourceFactionsForActiveSlot(
                _activeSlotIndex,
                _factionSelectionState.LeftSlotFaction,
                _factionSelectionState.RightSlotFaction);
            var lookupContext = await BuildUnitVisibilityLookupContextAsync(
                factions,
                faction => faction.Id,
                GetFactionSnapshotFromProvider,
                _specOpsProvider.GetSpecopsUnitsByFactionAsync,
                cancellationToken);

            ApplyUnitVisibilityCore(
                Units,
                lookupContext,
                _filterState.ActiveUnitFilter,
                LieutenantOnlyUnits,
                pointsRemaining,
                GetUnitFromProvider,
                cancellationToken);

            if (HasLeftSlotEntry())
            {
                foreach (var unit in Units)
                {
                    if (IsLeftSlotUnit(unit))
                    {
                        unit.IsVisible = false;
                    }
                }
            }

            _selectedUnit = RefreshSelectedUnitVisibilityCore(
                _selectedUnit,
                () => ResetUnitDetails(),
                ApplyLieutenantVisualStates);

            RefreshTeamEntryVisibilityCore<ArmyTeamListItem, ArmyTeamUnitLimitItem, ArmyUnitSelectionItem>(
                TeamEntries,
                Units);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"InspiringCompanySelectionPage ApplyUnitVisibilityFiltersAsync failed: {ex.Message}");
        }
    }
}
