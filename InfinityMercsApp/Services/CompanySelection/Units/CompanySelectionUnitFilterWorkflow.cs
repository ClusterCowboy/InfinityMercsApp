using InfinityMercsApp.Views.Controls;
using MercsArmyListEntry = InfinityMercsApp.Domain.Models.Army.MercsArmyListEntry;

namespace InfinityMercsApp.Views.Common;

internal static class CompanySelectionUnitFilterWorkflow
{
    internal static UnitFilterPopupView? TryOpenUnitFilterPopup(
        UnitFilterPopupOptions options,
        UnitFilterCriteria activeUnitFilter,
        bool lieutenantOnlyUnits,
        bool teamsView,
        double popupHeight,
        EventHandler<UnitFilterCriteria> onFilterArmyApplied,
        EventHandler onCloseRequested,
        ContentView popupHost,
        VisualElement popupOverlay,
        Action<string>? logError = null,
        bool teamsViewEnabled = true,
        bool lieutenantOnlyUnitsEnabled = true)
    {
        try
        {
            var popup = new UnitFilterPopupView(
                options,
                activeUnitFilter,
                lieutenantOnlyUnits: lieutenantOnlyUnits,
                teamsView: teamsView,
                teamsViewEnabled: teamsViewEnabled,
                lieutenantOnlyUnitsEnabled: lieutenantOnlyUnitsEnabled);
            popup.HeightRequest = popupHeight;
            popup.FilterArmyApplied += onFilterArmyApplied;
            popup.CloseRequested += onCloseRequested;
            popupHost.HeightRequest = popupHeight;
            popupHost.Content = popup;
            popupOverlay.IsVisible = true;
            return popup;
        }
        catch (Exception ex)
        {
            logError?.Invoke($"CompanySelectionPage filter popup open failed: {ex.Message}");
            return null;
        }
    }

    internal static UnitFilterPopupView? CloseUnitFilterPopup(
        UnitFilterPopupView? requestedPopup,
        UnitFilterPopupView? activePopup,
        EventHandler<UnitFilterCriteria> onFilterArmyApplied,
        EventHandler onCloseRequested,
        ContentView popupHost,
        VisualElement popupOverlay)
    {
        var target = requestedPopup ?? activePopup;
        if (target is not null)
        {
            target.FilterArmyApplied -= onFilterArmyApplied;
            target.CloseRequested -= onCloseRequested;
        }

        popupHost.Content = null;
        popupHost.HeightRequest = -1;
        popupOverlay.IsVisible = false;
        return null;
    }

    internal static UnitFilterCriteria ApplyCriteriaFromPopup(
        UnitFilterCriteria? criteria,
        Action<bool> setLieutenantOnlyUnits,
        Action<bool> setTeamsView)
    {
        return CompanySelectionUnitFilterOptionsService.ApplyCriteriaFromPopup(
            criteria,
            setLieutenantOnlyUnits,
            setTeamsView);
    }

    internal static double ResolveUnitFilterPopupHeight(Page page)
    {
        var pageHeight = page.Height > 0
            ? page.Height
            : page.Window?.Height ?? Application.Current?.Windows.FirstOrDefault()?.Page?.Height ?? 0;
        if (pageHeight <= 0)
        {
            return 800;
        }

        return pageHeight * 0.9;
    }

    internal static int ResolveFilterPopupMaxPoints(string selectedStartSeasonPoints)
    {
        return CompanySelectionUnitFilterOptionsService.ResolveFilterPopupMaxPoints(selectedStartSeasonPoints);
    }

    internal static UnitFilterPopupOptions ClonePopupOptionsForCurrentPoints(UnitFilterPopupOptions source, int maxPoints)
    {
        return CompanySelectionUnitFilterOptionsService.ClonePopupOptionsForCurrentPoints(source, maxPoints);
    }

    internal static UnitFilterPopupOptions GetPreparedPopupOptionsForCurrentPoints(
        UnitFilterPopupOptions? preparedOptions,
        int maxPoints)
    {
        return CompanySelectionUnitFilterOptionsService.GetPreparedPopupOptionsForCurrentPoints(
            preparedOptions,
            maxPoints);
    }

    internal static async Task<UnitFilterPopupOptions> BuildUnitFilterPopupOptionsAsync<TFaction>(
        bool showRightSelectionBox,
        TFaction? leftSlotFaction,
        TFaction? rightSlotFaction,
        Func<TFaction, int> readFactionId,
        Func<int, CancellationToken, string?> getFiltersJsonByFactionId,
        Func<IReadOnlyCollection<int>, CancellationToken, Task<IReadOnlyList<MercsArmyListEntry>>> getMergedMercsArmyListAsync,
        int maxPoints,
        Action<UnitFilterPopupOptions> setPreparedOptions,
        Action<string>? log = null,
        CancellationToken cancellationToken = default)
        where TFaction : class
    {
        return await CompanySelectionUnitFilterOptionsService.BuildUnitFilterPopupOptionsAsync(
            showRightSelectionBox,
            leftSlotFaction,
            rightSlotFaction,
            readFactionId,
            getFiltersJsonByFactionId,
            getMergedMercsArmyListAsync,
            maxPoints,
            setPreparedOptions,
            log,
            cancellationToken);
    }
}
