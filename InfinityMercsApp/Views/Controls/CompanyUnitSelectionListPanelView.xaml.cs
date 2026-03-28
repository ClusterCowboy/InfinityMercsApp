using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using System.Collections;
using System.Windows.Input;

namespace InfinityMercsApp.Views.Controls;

public partial class CompanyUnitSelectionListPanelView : ContentView
{
    private double _unitSelectionPanLastTotalY;

    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(CompanyUnitSelectionListPanelView));

    public static readonly BindableProperty ItemTappedCommandProperty =
        BindableProperty.Create(
            nameof(ItemTappedCommand),
            typeof(ICommand),
            typeof(CompanyUnitSelectionListPanelView));

    public static readonly BindableProperty ShowUnitsListProperty =
        BindableProperty.Create(
            nameof(ShowUnitsList),
            typeof(bool),
            typeof(CompanyUnitSelectionListPanelView),
            true);

    public CompanyUnitSelectionListPanelView()
    {
        InitializeComponent();
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public ICommand? ItemTappedCommand
    {
        get => (ICommand?)GetValue(ItemTappedCommandProperty);
        set => SetValue(ItemTappedCommandProperty, value);
    }

    public bool ShowUnitsList
    {
        get => (bool)GetValue(ShowUnitsListProperty);
        set => SetValue(ShowUnitsListProperty, value);
    }

    public Border FilterButton => UnitSelectionFilterButton;
    public SKCanvasView FilterCanvas => UnitSelectionFilterCanvas;

    public event EventHandler<EventArgs>? HeaderBorderSizeChanged;
    public event EventHandler<TappedEventArgs>? FilterButtonTapped;
    public event EventHandler<SKPaintSurfaceEventArgs>? FilterCanvasPaintSurface;

    private void OnHeaderBorderSizeChanged(object? sender, EventArgs e)
    {
        HeaderBorderSizeChanged?.Invoke(sender, e);
    }

    private void OnFilterButtonTapped(object? sender, TappedEventArgs e)
    {
        FilterButtonTapped?.Invoke(sender, e);
    }

    private void OnFilterCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        FilterCanvasPaintSurface?.Invoke(sender, e);
    }

    private async void OnUnitSelectionScrollPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        if (sender is not ScrollView scrollView)
        {
            return;
        }

        switch (e.StatusType)
        {
            case GestureStatus.Started:
                _unitSelectionPanLastTotalY = 0d;
                break;
            case GestureStatus.Running:
                var deltaY = e.TotalY - _unitSelectionPanLastTotalY;
                _unitSelectionPanLastTotalY = e.TotalY;
                var targetY = Math.Max(0d, scrollView.ScrollY - deltaY);
                await scrollView.ScrollToAsync(0d, targetY, false);
                break;
            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                _unitSelectionPanLastTotalY = 0d;
                break;
        }
    }
}
