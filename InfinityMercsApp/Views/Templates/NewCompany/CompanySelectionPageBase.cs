using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using ArmyUnitRecord = InfinityMercsApp.Domain.Models.Army.Unit;

namespace InfinityMercsApp.Views.Templates.NewCompany;

/// <summary>
/// Shared abstract base class for company selection pages.
/// This centralizes service resolution and shared UI wiring while allowing
/// each concrete page to keep its own state models and mode-specific behavior.
/// </summary>
public abstract class CompanySelectionPageBase : ContentPage
{
    private SKPicture? _filterIconPicture;

    protected CompanySelectionPageBase(
        ArmySourceSelectionMode mode,
        IMetadataProvider? metadataProvider,
        IFactionProvider? factionProvider,
        ISpecOpsProvider specOpsProvider,
        ICohesiveCompanyFactionQueryProvider cohesiveCompanyFactionQueryProvider,
        FactionLogoCacheService? factionLogoCacheService,
        IAppSettingsProvider? appSettingsProvider)
    {
        Mode = mode;
        MetadataProvider = metadataProvider;
        FactionProvider = factionProvider;
        SpecOpsProvider = specOpsProvider;
        CohesiveCompanyFactionQueryProvider = cohesiveCompanyFactionQueryProvider;
        FactionLogoCacheService = factionLogoCacheService;
        AppSettingsProvider = appSettingsProvider;
    }

    protected ArmySourceSelectionMode Mode { get; }
    protected IMetadataProvider? MetadataProvider { get; }
    protected IFactionProvider? FactionProvider { get; }
    protected ISpecOpsProvider SpecOpsProvider { get; }
    protected ICohesiveCompanyFactionQueryProvider CohesiveCompanyFactionQueryProvider { get; }
    protected FactionLogoCacheService? FactionLogoCacheService { get; }
    protected IAppSettingsProvider? AppSettingsProvider { get; }

    protected abstract IArmyDataService ArmyDataService { get; }
    protected abstract FactionSlotSelectorView FactionSlotSelectorViewForVisuals { get; }
    protected abstract UnitDisplayConfigurationsView UnitDisplayConfigurationsViewForVisuals { get; }
    protected abstract SKCanvasView UnitSelectionFilterCanvasInactiveForVisuals { get; }
    protected abstract SKCanvasView UnitSelectionFilterCanvasActiveForVisuals { get; }
    protected abstract bool SummaryHighlightLieutenantForVisuals { get; }
    protected abstract Color UnitHeaderSecondaryColorForVisuals { get; }
    protected abstract void SetUnitHeaderPrimaryColorForVisuals(Color value);
    protected abstract void SetUnitHeaderSecondaryColorForVisuals(Color value);
    protected abstract void SetUnitHeaderPrimaryTextColorForVisuals(Color value);
    protected abstract void SetUnitHeaderSecondaryTextColorForVisuals(Color value);
    protected abstract void SetEquipmentSummaryFormattedForVisuals(FormattedString value);
    protected abstract void SetSpecialSkillsSummaryFormattedForVisuals(FormattedString value);

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

    protected bool GetShowUnitsInInchesFromProvider(CancellationToken cancellationToken = default)
    {
        return CompanySelectionVisualUiWorkflow.GetShowUnitsInInchesFromProvider(AppSettingsProvider, cancellationToken);
    }

    /// <summary>
    /// Wires shared UnitDisplayConfigurationsView events to page handlers.
    /// Derived pages should call this once after InitializeComponent.
    /// </summary>
    protected void WireUnitDisplayEvents(
        UnitDisplayConfigurationsView unitDisplayView,
        EventHandler<SKPaintSurfaceEventArgs> onHeaderIconsPaintSurface,
        EventHandler<SKPaintSurfaceEventArgs> onSelectedUnitPaintSurface,
        EventHandler<SKPaintSurfaceEventArgs> onPeripheralIconPaintSurface,
        EventHandler<SKPaintSurfaceEventArgs> onProfileTacticalPaintSurface,
        EventHandler<EventArgs> onUnitNameHeadingSizeChanged)
    {
        unitDisplayView.HeaderIconsCanvasPaintSurface += onHeaderIconsPaintSurface;
        unitDisplayView.SelectedUnitCanvasPaintSurface += onSelectedUnitPaintSurface;
        unitDisplayView.PeripheralIconCanvasPaintSurface += onPeripheralIconPaintSurface;
        unitDisplayView.ProfileTacticalIconCanvasPaintSurface += onProfileTacticalPaintSurface;
        unitDisplayView.UnitNameHeadingSizeChanged += onUnitNameHeadingSizeChanged;
    }

    protected abstract Task LoadFactionsAsync();
    protected abstract Task LoadUnitsForActiveSlotAsync();
    protected abstract Task LoadSelectedUnitDetailsAsync();
    protected abstract Task StartCompanyAsync();
}
