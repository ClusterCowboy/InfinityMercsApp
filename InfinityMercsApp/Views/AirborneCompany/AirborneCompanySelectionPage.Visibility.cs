using InfinityMercsApp.Views.Common;
using InfinityMercsApp.Views.Controls;

namespace InfinityMercsApp.Views.AirborneCompany;

public partial class AirborneCompanySelectionPage
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

            // Fallback: if lieutenant-only filtering would blank the list, relax it automatically.
            if (!Units.Any(x => x.IsVisible) && LieutenantOnlyUnits)
            {
                LieutenantOnlyUnits = false;
                _filterState.ActiveUnitFilter = new UnitFilterCriteria
                {
                    Terms = _filterState.ActiveUnitFilter.Terms,
                    MinPoints = _filterState.ActiveUnitFilter.MinPoints,
                    MaxPoints = _filterState.ActiveUnitFilter.MaxPoints,
                    LieutenantOnlyUnits = false,
                    TeamsView = false
                };
                SetIsUnitFilterActive(_filterState.ActiveUnitFilter.IsActive);

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
            }

            var visibleCount = Units.Count(x => x.IsVisible);

            if (visibleCount == 0 && Units.Count > 0)
            {
                Console.WriteLine("[AirborneCompanySelectionPage] No visible units after filtering. Forcing available units visible.");
                var hasLeftSlotEntry = HasLeftSlotEntry();
                foreach (var unit in Units)
                {
                    unit.IsVisible = !hasLeftSlotEntry || !IsLeftSlotUnit(unit);
                }

                visibleCount = Units.Count(x => x.IsVisible);
            }

            Console.WriteLine(
                $"[AirborneCompanySelectionPage] Visibility result: {visibleCount}/{Units.Count} visible " +
                $"(pointsRemaining={pointsRemaining}, activeSlot={_activeSlotIndex}, filterActive={_filterState.ActiveUnitFilter.IsActive}, lieutenantOnly={LieutenantOnlyUnits}).");

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
            Console.Error.WriteLine($"AirborneCompanySelectionPage ApplyUnitVisibilityFiltersAsync failed: {ex.Message}");
        }
    }
}
