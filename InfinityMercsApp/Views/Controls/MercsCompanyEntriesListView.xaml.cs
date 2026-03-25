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

    public MercsCompanyEntriesListView()
    {
        InitializeComponent();
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
}
