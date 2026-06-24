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

    public static readonly BindableProperty IsDenseProperty =
        BindableProperty.Create(
            nameof(IsDense),
            typeof(bool),
            typeof(CompanyUnitSelectionListPanelView),
            false,
            propertyChanged: OnIsDenseChanged);

    // Compact-screen scale for the UNIT SELECTION header. The filter button is sized externally as
    // headerHeight * 0.8, so shrinking the header by this factor shrinks the filter button to match.
    private const double DenseScale = 0.6d;
    private static readonly Thickness HeaderPaddingDefault = new(12, 8, 0, 8);

    public CompanyUnitSelectionListPanelView()
    {
        InitializeComponent();
        ApplyDensity();
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

    /// <summary>When true (small screens), the UNIT SELECTION header and its filter button render at
    /// 60% size to reclaim vertical space.</summary>
    public bool IsDense
    {
        get => (bool)GetValue(IsDenseProperty);
        set => SetValue(IsDenseProperty, value);
    }

    private static void OnIsDenseChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is CompanyUnitSelectionListPanelView panel)
        {
            panel.ApplyDensity();
        }
    }

    private void ApplyDensity()
    {
        if (IsDense)
        {
            UnitSelectionHeaderBorder.Padding = new Thickness(
                HeaderPaddingDefault.Left * DenseScale,
                HeaderPaddingDefault.Top * DenseScale,
                HeaderPaddingDefault.Right,
                HeaderPaddingDefault.Bottom * DenseScale);

            var baseFontSize = ResolveFontSize("FontSizeSectionHeader", 18d);
            UnitSelectionHeaderLabel.FontSize = baseFontSize * DenseScale;
        }
        else
        {
            UnitSelectionHeaderBorder.Padding = HeaderPaddingDefault;
            UnitSelectionHeaderLabel.SetDynamicResource(Label.FontSizeProperty, "FontSizeSectionHeader");
        }
    }

    private static double ResolveFontSize(string key, double fallback)
    {
        if (Application.Current?.Resources.TryGetValue(key, out var raw) == true && raw is double size)
        {
            return size;
        }

        return fallback;
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
