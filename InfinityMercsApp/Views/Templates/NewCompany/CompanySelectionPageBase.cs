using InfinityMercsApp.Domain.CompanyCreation;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Infrastructure.Services;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
using SkiaSharp.Views.Maui;

namespace InfinityMercsApp.Views.Templates.NewCompany;

/// <summary>
/// Shared abstract base class for company selection pages.
/// This centralizes service resolution and shared UI wiring while allowing
/// each concrete page to keep its own state models and mode-specific behavior.
/// </summary>
public abstract class CompanySelectionPageBase : ContentPage
{
    protected CompanySelectionPageBase(
        IArmySourceSelectionModeService armySourceSelectionModeService,
        IMetadataProvider metadataProvider,
        IFactionProvider factionProvider,
        ISpecOpsProvider specOpsProvider,
        FactionLogoCacheService factionLogoCacheService,
        IAppSettingsProvider appSettingsProvider)
    {
        MetadataProvider = metadataProvider;
        FactionProvider = factionProvider;
        SpecOpsProvider = specOpsProvider;
        FactionLogoCacheService = factionLogoCacheService;
        AppSettingsProvider = appSettingsProvider;
        Mode = armySourceSelectionModeService.Get();
    }

    // Make this settable for now to support cohesive companies before this is redone using proper navigation
    protected ArmySourceSelectionMode Mode { get; set; }
    protected IMetadataProvider MetadataProvider { get; }
    protected IFactionProvider FactionProvider { get; }
    protected ISpecOpsProvider SpecOpsProvider { get; }
    protected FactionLogoCacheService FactionLogoCacheService { get; }
    protected IAppSettingsProvider AppSettingsProvider { get; }

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
