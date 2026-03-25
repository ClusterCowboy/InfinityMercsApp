using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using System.Collections;
using System.Windows.Input;

namespace InfinityMercsApp.Views.Controls;

public partial class CompanyUnitSelectionListPanelView : ContentView
{
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

    public static readonly BindableProperty IsUnitSelectionActiveProperty =
        BindableProperty.Create(
            nameof(IsUnitSelectionActive),
            typeof(bool),
            typeof(CompanyUnitSelectionListPanelView),
            true);

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

    public bool IsUnitSelectionActive
    {
        get => (bool)GetValue(IsUnitSelectionActiveProperty);
        set => SetValue(IsUnitSelectionActiveProperty, value);
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
}
