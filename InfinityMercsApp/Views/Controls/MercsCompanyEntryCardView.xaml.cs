using SkiaSharp.Views.Maui;
using System.Windows.Input;

namespace InfinityMercsApp.Views.Controls;

public partial class MercsCompanyEntryCardView : ContentView
{
    public static readonly BindableProperty SelectEntryCommandProperty =
        BindableProperty.Create(
            nameof(SelectEntryCommand),
            typeof(ICommand),
            typeof(MercsCompanyEntryCardView));

    public static readonly BindableProperty RemoveEntryCommandProperty =
        BindableProperty.Create(
            nameof(RemoveEntryCommand),
            typeof(ICommand),
            typeof(MercsCompanyEntryCardView));

    public static readonly BindableProperty ShowOrderModifierBadgesProperty =
        BindableProperty.Create(
            nameof(ShowOrderModifierBadges),
            typeof(bool),
            typeof(MercsCompanyEntryCardView),
            false);

    public MercsCompanyEntryCardView()
    {
        InitializeComponent();
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

    public event EventHandler<SKPaintSurfaceEventArgs>? PeripheralIconCanvasPaintSurface;
    public event EventHandler<SKPaintSurfaceEventArgs>? IrregularIconCanvasPaintSurface;
    public event EventHandler<SKPaintSurfaceEventArgs>? RegularModifierIconCanvasPaintSurface;

    private void OnCardTapped(object? sender, TappedEventArgs e)
    {
        ExecuteCommand(SelectEntryCommand);
    }

    private void OnRemoveButtonClicked(object? sender, EventArgs e)
    {
        ExecuteCommand(RemoveEntryCommand);
    }

    private void ExecuteCommand(ICommand? command)
    {
        var parameter = BindingContext;
        if (command?.CanExecute(parameter) == true)
        {
            command.Execute(parameter);
        }
    }

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
