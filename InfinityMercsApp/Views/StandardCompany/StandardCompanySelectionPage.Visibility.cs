using InfinityMercsApp.Views.Common;

namespace InfinityMercsApp.Views.StandardCompany;

public partial class StandardCompanySelectionPage
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
                _filterState.ActiveUnitFilter,
                LieutenantOnlyUnits,
                pointsRemaining,
                GetUnitFromProvider,
                cancellationToken);

            var visibleCount = Units.Count(x => x.IsVisible);
            Console.WriteLine(
                $"[StandardCompanySelectionPage] Visibility result: {visibleCount}/{Units.Count} visible " +
                $"(pointsRemaining={pointsRemaining}, filterActive={_filterState.ActiveUnitFilter.IsActive}, lieutenantOnly={LieutenantOnlyUnits}).");

            // Safety fallback: never leave the list blank when units are loaded.
            if (visibleCount == 0 && Units.Count > 0)
            {
                Console.WriteLine("[StandardCompanySelectionPage] No visible units after filtering. Forcing loaded units visible.");
                foreach (var unit in Units)
                {
                    unit.IsVisible = true;
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
            Console.Error.WriteLine($"CompanySelectionPage ApplyUnitVisibilityFiltersAsync failed: {ex.Message}");
        }
    }
}
