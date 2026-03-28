using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
using InfinityMercsApp.Views.Common;

namespace InfinityMercsApp.Views.CohesiveCompany;

public partial class CohesiveCompanySelectionPage
{
    private void OnToggleFactionStripTapped(object? sender, TappedEventArgs e)
    {
        ToggleFactionStrip(sender);
    }

    private void OnUnitSelectionFilterButtonTapped(object? sender, TappedEventArgs e)
    {
        _filterState.ActiveUnitFilterPopup = CompanySelectionUnitFilterWorkflow.TryOpenUnitFilterPopup(
            GetPreparedPopupOptionsForCurrentPoints(),
            _filterState.ActiveUnitFilter,
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
        _filterState.ActiveUnitFilter = CompanySelectionUnitFilterWorkflow.ApplyCriteriaFromPopup(
            criteria,
            value => LieutenantOnlyUnits = value,
            value => TeamsView = value);
        SetIsUnitFilterActive(_filterState.ActiveUnitFilter.IsActive);
        CloseUnitFilterPopup(sender as UnitFilterPopupView);
        _ = ApplyUnitVisibilityFiltersAsync();
    }

    private void OnUnitFilterPopupCloseRequested(object? sender, EventArgs e)
    {
        CloseUnitFilterPopup(sender as UnitFilterPopupView);
    }

    private void CloseUnitFilterPopup(UnitFilterPopupView? popup)
    {
        _filterState.ActiveUnitFilterPopup = CompanySelectionUnitFilterWorkflow.CloseUnitFilterPopup(
            popup,
            _filterState.ActiveUnitFilterPopup,
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
            UnitSelectionPanel.FilterButton,
            UnitSelectionPanel.FilterCanvas,
            UnitSelectionPanel.FilterButton,
            UnitSelectionPanel.FilterCanvas,
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
            value => _filterState.PreparedUnitFilterPopupOptions = value,
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
            _filterState.PreparedUnitFilterPopupOptions,
            ResolveFilterPopupMaxPoints());
    }
}



