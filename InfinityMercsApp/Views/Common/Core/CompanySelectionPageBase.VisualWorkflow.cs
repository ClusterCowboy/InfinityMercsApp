using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using ArmyUnitRecord = InfinityMercsApp.Domain.Models.Army.Unit;

namespace InfinityMercsApp.Views.Common;

public abstract partial class CompanySelectionPageBase
{
    protected async Task LoadSlotIconAsync(int slotIndex, string? cachedPath, string? packagedPath)
    {
        await CompanySelectionVisualIconWorkflow.LoadSlotIconAsync(
            slotIndex,
            cachedPath,
            packagedPath,
            FactionSlotSelectorViewForVisuals,
            message => Console.Error.WriteLine(message));
    }

    protected async Task LoadHeaderIconsAsync()
    {
        _filterIconPicture = await CompanySelectionVisualIconWorkflow.LoadHeaderIconsAsync(
            UnitDisplayConfigurationsViewForVisuals,
            _filterIconPicture,
            () =>
            {
                UnitSelectionFilterCanvasInactiveForVisuals.InvalidateSurface();
                UnitSelectionFilterCanvasActiveForVisuals.InvalidateSurface();
            },
            message => Console.Error.WriteLine(message));
    }

    protected void OnUnitSelectionFilterCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        CompanySelectionVisualUiWorkflow.DrawFilterIcon(_filterIconPicture, e);
    }

    protected void OnPeripheralIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        CompanySelectionVisualUiWorkflow.DrawPeripheralIcon(UnitDisplayConfigurationsViewForVisuals, e);
    }

    protected static void ApplyFilterButtonSize(Border? buttonBorder, SKCanvasView? iconCanvas, double iconButtonSize)
    {
        CompanySelectionVisualUiWorkflow.ApplyFilterButtonSize(buttonBorder, iconCanvas, iconButtonSize);
    }

    protected void UpdateUnitNameHeadingFontSize()
    {
        CompanySelectionVisualUiWorkflow.UpdateUnitNameHeadingFontSize(UnitDisplayConfigurationsViewForVisuals);
    }

    protected async Task ApplyUnitHeaderColorsAsync(int sourceFactionId, ArmyUnitRecord? unit, CancellationToken cancellationToken)
    {
        var factionName = await CompanySelectionVisualThemeWorkflow.ResolveThemeFactionNameAsync(
            Mode,
            ArmyDataService,
            sourceFactionId,
            unit?.FactionsJson,
            cancellationToken);
        ApplyUnitHeaderColorsByVanillaFactionName(factionName);
    }

    protected void ApplyUnitHeaderColorsByVanillaFactionName(string? vanillaFactionName)
    {
        CompanySelectionVisualUiWorkflow.ApplyHeaderColors(
            vanillaFactionName,
            UnitDisplayConfigurationsViewForVisuals,
            SetUnitHeaderPrimaryColorForVisuals,
            SetUnitHeaderSecondaryColorForVisuals,
            SetUnitHeaderPrimaryTextColorForVisuals,
            SetUnitHeaderSecondaryTextColorForVisuals,
            RefreshSummaryFormatted);
    }

    protected void RefreshSummaryFormatted()
    {
        CompanySelectionVisualUiWorkflow.ApplySummaryFormatted(
            UnitDisplayConfigurationsViewForVisuals,
            UnitHeaderSecondaryColorForVisuals,
            SummaryHighlightLieutenantForVisuals,
            SetEquipmentSummaryFormattedForVisuals,
            SetSpecialSkillsSummaryFormattedForVisuals);
    }

    protected bool SetAndNotifyUnitHeaderIconFlag(
        Func<bool> readCurrent,
        Action<bool> writeCurrent,
        bool value)
    {
        if (readCurrent() == value)
        {
            return false;
        }

        writeCurrent(value);
        OnPropertyChanged();
        OnPropertyChanged("HasAnyTopHeaderIcons");
        OnPropertyChanged("HasAnyBottomHeaderIcons");
        OnPropertyChanged("HasAnyHeaderIcons");
        UnitDisplayConfigurationsViewForVisuals.InvalidateHeaderIconsCanvas();
        return true;
    }

    protected bool GetShowUnitsInInchesFromProvider(CancellationToken cancellationToken = default)
    {
        return CompanySelectionVisualUiWorkflow.GetShowUnitsInInchesFromProvider(AppSettingsProvider, cancellationToken);
    }
}
