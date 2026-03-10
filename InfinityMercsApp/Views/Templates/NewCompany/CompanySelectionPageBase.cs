using InfinityMercsApp.Data.Database;
using InfinityMercsApp.Services;
using InfinityMercsApp.Views.Controls;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp.Views.Maui;

namespace InfinityMercsApp.Views.Templates.NewCompany;

/// <summary>
/// Shared abstract base class for company selection pages.
/// This centralizes service resolution and shared UI wiring while allowing
/// each concrete page to keep its own state models and mode-specific behavior.
/// </summary>
public abstract class CompanySelectionPageBase : ContentPage
{
    protected CompanySelectionPageBase(ArmySourceSelectionMode mode)
    {
        Mode = mode;

        var services = Application.Current?.Handler?.MauiContext?.Services;
        MetadataAccessor = services?.GetService<IMetadataAccessor>();
        ArmyDataAccessor = services?.GetService<IArmyDataAccessor>();
        SpecOpsDataAccessor = services?.GetService<ISpecOpsDataAccessor>()
            ?? throw new InvalidOperationException("SpecOpsDataAccessor service is not registered.");
        FactionLogoCacheService = services?.GetService<FactionLogoCacheService>();
        AppSettingsService = services?.GetService<AppSettingsService>();
    }

    protected ArmySourceSelectionMode Mode { get; }
    protected IMetadataAccessor? MetadataAccessor { get; }
    protected IArmyDataAccessor? ArmyDataAccessor { get; }
    protected ISpecOpsDataAccessor SpecOpsDataAccessor { get; }
    protected FactionLogoCacheService? FactionLogoCacheService { get; }
    protected AppSettingsService? AppSettingsService { get; }

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
