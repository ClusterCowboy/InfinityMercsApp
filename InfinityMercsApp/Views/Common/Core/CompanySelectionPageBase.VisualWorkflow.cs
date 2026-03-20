using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using ArmyUnitRecord = InfinityMercsApp.Domain.Models.Army.Unit;

namespace InfinityMercsApp.Views.Common;

public abstract partial class CompanySelectionPageBase
{
    /// <summary>
    /// Loads the faction logo for the given slot index, preferring the cached on-disk path
    /// over the bundled app-package path. Errors are logged to stderr.
    /// </summary>
    protected async Task LoadSlotIconAsync(int slotIndex, string? cachedPath, string? packagedPath)
    {
        await CompanySelectionVisualIconWorkflow.LoadSlotIconAsync(
            slotIndex,
            cachedPath,
            packagedPath,
            FactionSlotSelectorViewForVisuals,
            message => Console.Error.WriteLine(message));
    }

    /// <summary>
    /// Loads the shared header icons (filter icon and any unit-display icons) and stores the
    /// filter icon picture for reuse by the filter canvas paint handlers.
    /// Invalidates both filter canvases once loading completes.
    /// </summary>
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

    /// <summary>
    /// Renders the filter icon in either active or inactive state depending on <c>_isUnitFilterActive</c>.
    /// Shared by both the inactive and active filter canvas controls.
    /// </summary>
    protected void OnUnitSelectionFilterCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        CompanySelectionVisualUiWorkflow.DrawFilterIcon(_filterIconPicture, _isUnitFilterActive, e);
    }

    /// <summary>
    /// Renders the peripheral device icon (e.g. remote body icon) onto its dedicated canvas.
    /// </summary>
    protected void OnPeripheralIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        CompanySelectionVisualUiWorkflow.DrawPeripheralIcon(UnitDisplayConfigurationsViewForVisuals, e);
    }

    /// <summary>
    /// Applies a uniform icon button size to the filter button border and canvas,
    /// keeping the hit target and drawn icon in sync.
    /// </summary>
    protected static void ApplyFilterButtonSize(Border? buttonBorder, SKCanvasView? iconCanvas, double iconButtonSize)
    {
        CompanySelectionVisualUiWorkflow.ApplyFilterButtonSize(buttonBorder, iconCanvas, iconButtonSize);
    }

    /// <summary>
    /// Recalculates and applies the optimal font size for the unit name heading
    /// based on string length so it fits without truncation.
    /// </summary>
    protected void UpdateUnitNameHeadingFontSize()
    {
        CompanySelectionVisualUiWorkflow.UpdateUnitNameHeadingFontSize(UnitDisplayConfigurationsViewForVisuals);
    }

    /// <summary>
    /// Resolves the canonical (non-sectorial) faction name from the unit's faction data
    /// and applies the corresponding header colour scheme.
    /// </summary>
    protected async Task ApplyUnitHeaderColorsAsync(int sourceFactionId, ArmyUnitRecord? unit, CancellationToken cancellationToken)
    {
        // Resolve via the theme workflow so sectorial IDs are mapped back to their vanilla faction colour.
        var factionName = await CompanySelectionVisualThemeWorkflow.ResolveThemeFactionNameAsync(
            Mode,
            ArmyDataService,
            sourceFactionId,
            unit?.FactionsJson,
            cancellationToken);
        ApplyUnitHeaderColorsByVanillaFactionName(factionName);
    }

    /// <summary>
    /// Applies the primary/secondary header colours and text colours derived from the vanilla faction name,
    /// then refreshes the summary formatted strings so they pick up the new accent colour.
    /// </summary>
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

    /// <summary>
    /// Rebuilds the equipment and special-skills FormattedString values,
    /// applying the current faction accent colour and lieutenant highlight setting.
    /// Called after any change that affects text formatting or colour.
    /// </summary>
    protected void RefreshSummaryFormatted()
    {
        CompanySelectionVisualUiWorkflow.ApplySummaryFormatted(
            UnitDisplayConfigurationsViewForVisuals,
            UnitHeaderSecondaryColorForVisuals,
            SummaryHighlightLieutenantForVisuals,
            SetEquipmentSummaryFormattedForVisuals,
            SetSpecialSkillsSummaryFormattedForVisuals);
    }

    /// <summary>
    /// Sets a boolean icon-visibility flag only when it differs from its current value,
    /// then fires property-change notifications for the aggregate HasAny* properties
    /// and invalidates the header icons canvas. Returns <c>true</c> if the value changed.
    /// </summary>
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

        // Cascade notifications to the aggregate icon-row visibility bindings.
        OnPropertyChanged("HasAnyTopHeaderIcons");
        OnPropertyChanged("HasAnyBottomHeaderIcons");
        OnPropertyChanged("HasAnyHeaderIcons");
        UnitDisplayConfigurationsViewForVisuals.InvalidateHeaderIconsCanvas();
        return true;
    }

    /// <summary>
    /// Reads the "show units in inches" preference from the app settings provider.
    /// Returns <c>false</c> when the provider is unavailable.
    /// </summary>
    protected bool GetShowUnitsInInchesFromProvider(CancellationToken cancellationToken = default)
    {
        return CompanySelectionVisualUiWorkflow.GetShowUnitsInInchesFromProvider(AppSettingsProvider, cancellationToken);
    }
}
