using SkiaSharp.Views.Maui;
using System.Collections;
using System.Windows.Input;

namespace InfinityMercsApp.Views.Controls;

public partial class MercsCompanyEntriesListView : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(
            nameof(ItemsSource),
            typeof(IEnumerable),
            typeof(MercsCompanyEntriesListView));

    public static readonly BindableProperty SelectEntryCommandProperty =
        BindableProperty.Create(
            nameof(SelectEntryCommand),
            typeof(ICommand),
            typeof(MercsCompanyEntriesListView));

    public static readonly BindableProperty RemoveEntryCommandProperty =
        BindableProperty.Create(
            nameof(RemoveEntryCommand),
            typeof(ICommand),
            typeof(MercsCompanyEntriesListView));

    public static readonly BindableProperty ShowOrderModifierBadgesProperty =
        BindableProperty.Create(
            nameof(ShowOrderModifierBadges),
            typeof(bool),
            typeof(MercsCompanyEntriesListView),
            false);

    public static readonly BindableProperty EmptyTextProperty =
        BindableProperty.Create(
            nameof(EmptyText),
            typeof(string),
            typeof(MercsCompanyEntriesListView),
            "No units added yet.");

    public static readonly BindableProperty ViewportHeightRatioProperty =
        BindableProperty.Create(
            nameof(ViewportHeightRatio),
            typeof(double),
            typeof(MercsCompanyEntriesListView),
            0.4d,
            propertyChanged: OnViewportHeightRatioChanged);

    public MercsCompanyEntriesListView()
    {
        InitializeComponent();
        SizeChanged += OnListViewSizeChanged;
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public ICommand? SelectEntryCommand
    {
        get => (ICommand?)GetValue(SelectEntryCommandProperty);
        set => SetValue(SelectEntryCommandProperty, value);
    }

    public ICommand? RemoveEntryCommand
    {
        get => (ICommand?)GetValue(RemoveEntryCommandProperty);
        set => SetValue(RemoveEntryCommandProperty, value);
    }

    public bool ShowOrderModifierBadges
    {
        get => (bool)GetValue(ShowOrderModifierBadgesProperty);
        set => SetValue(ShowOrderModifierBadgesProperty, value);
    }

    public string EmptyText
    {
        get => (string)GetValue(EmptyTextProperty);
        set => SetValue(EmptyTextProperty, value);
    }

    public double ViewportHeightRatio
    {
        get => (double)GetValue(ViewportHeightRatioProperty);
        set => SetValue(ViewportHeightRatioProperty, value);
    }

    public event EventHandler<SKPaintSurfaceEventArgs>? PeripheralIconCanvasPaintSurface;
    public event EventHandler<SKPaintSurfaceEventArgs>? IrregularIconCanvasPaintSurface;
    public event EventHandler<SKPaintSurfaceEventArgs>? RegularModifierIconCanvasPaintSurface;

    private void OnPeripheralIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        PeripheralIconCanvasPaintSurface?.Invoke(sender, e);
    }

    private void OnIrregularIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        IrregularIconCanvasPaintSurface?.Invoke(sender, e);
    }

    private void OnRegularModifierIconCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        RegularModifierIconCanvasPaintSurface?.Invoke(sender, e);
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        UpdateHeightFromViewport();
    }

    private void OnListViewSizeChanged(object? sender, EventArgs e)
    {
        UpdateHeightFromViewport();
    }

    private static void OnViewportHeightRatioChanged(BindableObject bindable, object oldValue, object newValue)
    {
        if (bindable is MercsCompanyEntriesListView view)
        {
            view.UpdateHeightFromViewport();
        }
    }

    private void UpdateHeightFromViewport()
    {
        var ratio = Math.Clamp(ViewportHeightRatio, 0.1d, 1d);
        var viewportHeight = ResolveViewportHeight();
        if (viewportHeight <= 0)
        {
            return;
        }

        var targetHeight = Math.Max(120d, Math.Round(viewportHeight * ratio));
        if (Math.Abs(EntriesCollection.HeightRequest - targetHeight) > 0.5d)
        {
            EntriesCollection.HeightRequest = targetHeight;
        }

        if (Math.Abs(HeightRequest - targetHeight) > 0.5d)
        {
            HeightRequest = targetHeight;
        }
    }

    private double ResolveViewportHeight()
    {
        var windowHeight = Window?.Height
            ?? Application.Current?.Windows.FirstOrDefault()?.Height
            ?? 0d;

        if (windowHeight > 0)
        {
            return windowHeight;
        }

        if (Parent is VisualElement visualParent && visualParent.Height > 0)
        {
            return visualParent.Height;
        }

        return Height;
    }
}
