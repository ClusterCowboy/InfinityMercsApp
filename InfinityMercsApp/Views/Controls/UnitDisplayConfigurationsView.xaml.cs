using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;

namespace InfinityMercsApp.Views.Controls;

/// <summary>
/// Shared selected-unit panel that forwards canvas/size events to hosting pages.
/// </summary>
public partial class UnitDisplayConfigurationsView : ContentView
{
    public event EventHandler<SKPaintSurfaceEventArgs>? HeaderIconsCanvasPaintSurface;
    public event EventHandler<SKPaintSurfaceEventArgs>? SelectedUnitCanvasPaintSurface;
    public event EventHandler<SKPaintSurfaceEventArgs>? PeripheralIconCanvasPaintSurface;
    public event EventHandler<SKPaintSurfaceEventArgs>? ProfileTacticalIconCanvasPaintSurface;
    public event EventHandler<EventArgs>? UnitNameHeadingSizeChanged;

    public UnitDisplayConfigurationsView()
    {
        InitializeComponent();
    }

    public Label UnitNameHeadingElement => UnitNameHeadingLabel;

    public void InvalidateHeaderIconsCanvas()
    {
        HeaderIconsCanvas.InvalidateSurface();
    }

    public void InvalidateSelectedUnitCanvas()
    {
        SelectedUnitCanvas.InvalidateSurface();
    }

    public void InvalidatePeripheralHeaderIconCanvas()
    {
        PeripheralHeaderIconCanvas.InvalidateSurface();
    }

    private void OnHeaderIconsCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        HeaderIconsCanvasPaintSurface?.Invoke(sender, e);
    }

    private void OnSelectedUnitCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        SelectedUnitCanvasPaintSurface?.Invoke(sender, e);
    }

    private void OnPeripheralIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        PeripheralIconCanvasPaintSurface?.Invoke(sender, e);
    }

    private void OnProfileTacticalIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        ProfileTacticalIconCanvasPaintSurface?.Invoke(sender, e);
    }

    private void OnUnitNameHeadingLabelSizeChanged(object? sender, EventArgs e)
    {
        UnitNameHeadingSizeChanged?.Invoke(sender, e);
    }
}
