using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
using InfinityMercsApp.Views.Templates.NewCompany;

namespace InfinityMercsApp.Views.CohesiveCompany;

public partial class CCArmyFactionSelectionPage
{
    private void OnFactionSelectionHeaderTapped(object? sender, TappedEventArgs e)
    {
        CompanySelectionUnitSelectionUiWorkflow.ActivateFactionSelection(
            value => IsFactionSelectionActive = value,
            () => AreTeamEntriesReady = false);
    }

    private void OnUnitSelectionHeaderTapped(object? sender, TappedEventArgs e)
    {
        CompanySelectionUnitSelectionUiWorkflow.ActivateUnitSelection(
            value => IsFactionSelectionActive = value,
            () =>
            {
                if (_factionSelectionState.SelectedFaction is not null ||
                    _factionSelectionState.LeftSlotFaction is not null ||
                    _factionSelectionState.RightSlotFaction is not null)
                {
                    _ = LoadUnitsForActiveSlotAsync();
                }
            });
    }

    private void OnUnitSelectionFilterButtonTapped(object? sender, TappedEventArgs e)
    {
        _activeUnitFilterPopup = CompanySelectionUnitFilterWorkflow.TryOpenUnitFilterPopup(
            GetPreparedPopupOptionsForCurrentPoints(),
            _activeUnitFilter,
            LieutenantOnlyUnits,
            TeamsView,
            ResolveUnitFilterPopupHeight(),
            OnFilterArmyApplied,
            OnUnitFilterPopupCloseRequested,
            UnitFilterPopupHost,
            UnitFilterOverlay,
            message => Console.Error.WriteLine(message));
    }

    private void OnFilterArmyApplied(object? sender, UnitFilterCriteria criteria)
    {
        _activeUnitFilter = CompanySelectionUnitFilterWorkflow.ApplyCriteriaFromPopup(
            criteria,
            value => LieutenantOnlyUnits = value,
            value => TeamsView = value);
        CloseUnitFilterPopup(sender as UnitFilterPopupView);
        _ = ApplyUnitVisibilityFiltersAsync();
    }

    private void OnUnitFilterPopupCloseRequested(object? sender, EventArgs e)
    {
        CloseUnitFilterPopup(sender as UnitFilterPopupView);
    }

    private void CloseUnitFilterPopup(UnitFilterPopupView? popup)
    {
        _activeUnitFilterPopup = CompanySelectionUnitFilterWorkflow.CloseUnitFilterPopup(
            popup,
            _activeUnitFilterPopup,
            OnFilterArmyApplied,
            OnUnitFilterPopupCloseRequested,
            UnitFilterPopupHost,
            UnitFilterOverlay);
    }

    private double ResolveUnitFilterPopupHeight()
    {
        return CompanySelectionUnitFilterWorkflow.ResolveUnitFilterPopupHeight(this);
    }

    private void OnUnitSelectionHeaderBorderSizeChanged(object? sender, EventArgs e)
    {
        CompanySelectionUnitSelectionUiWorkflow.ApplyHeaderFilterButtonSizes(
            sender,
            UnitSelectionFilterButtonInactive,
            UnitSelectionFilterCanvasInactive,
            UnitSelectionFilterButtonActive,
            UnitSelectionFilterCanvasActive,
            ApplyFilterButtonSize);
    }

    private async Task<UnitFilterPopupOptions> BuildUnitFilterPopupOptionsAsync(CancellationToken cancellationToken = default)
    {
        return await CompanySelectionUnitFilterWorkflow.BuildUnitFilterPopupOptionsAsync(
            ShowRightSelectionBox,
            _factionSelectionState.LeftSlotFaction,
            _factionSelectionState.RightSlotFaction,
            faction => faction.Id,
            (factionId, ct) => GetFactionSnapshotFromProvider(factionId, ct)?.FiltersJson,
            (factionIds, ct) => GetMergedMercsArmyListFromQueryAccessorAsync(factionIds, ct),
            ResolveFilterPopupMaxPoints(),
            value => _preparedUnitFilterPopupOptions = value,
            message => Console.WriteLine(message),
            cancellationToken);
    }

    private int ResolveFilterPopupMaxPoints()
    {
        return CompanySelectionUnitFilterWorkflow.ResolveFilterPopupMaxPoints(SelectedStartSeasonPoints);
    }

    private UnitFilterPopupOptions ClonePopupOptionsForCurrentPoints(UnitFilterPopupOptions source)
    {
        return CompanySelectionUnitFilterWorkflow.ClonePopupOptionsForCurrentPoints(source, ResolveFilterPopupMaxPoints());
    }

    private UnitFilterPopupOptions GetPreparedPopupOptionsForCurrentPoints()
    {
        return CompanySelectionUnitFilterWorkflow.GetPreparedPopupOptionsForCurrentPoints(
            _preparedUnitFilterPopupOptions,
            ResolveFilterPopupMaxPoints());
    }
}
