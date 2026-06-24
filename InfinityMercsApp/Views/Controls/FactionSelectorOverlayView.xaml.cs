using System.Collections;
using System.Windows.Input;

namespace InfinityMercsApp.Views.Controls;

/// <summary>
/// Full-screen overlay faction picker for small (Compact) screens. Presents the faction list as a
/// vertically scrolling list of "Company Style Icon" cards (large SVG + label), collapsing from a
/// two-column grid to single-column rows as the overlay narrows. Selecting a faction runs the
/// page's <see cref="SelectFactionCommand"/>; the hosting page decides when to close the overlay
/// (via <see cref="IsOpen"/>) so multi-slot companies can keep picking.
/// </summary>
public partial class FactionSelectorOverlayView : ContentView
{
    // Below this overlay width the cards stop being a 2-up grid and become full-width rows.
    private const double SingleColumnMaxWidth = 360d;

    public static readonly BindableProperty IsOpenProperty =
        BindableProperty.Create(nameof(IsOpen), typeof(bool), typeof(FactionSelectorOverlayView), false);

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(FactionSelectorOverlayView), "Choose your faction");

    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(FactionSelectorOverlayView));

    public static readonly BindableProperty SelectFactionCommandProperty =
        BindableProperty.Create(nameof(SelectFactionCommand), typeof(ICommand), typeof(FactionSelectorOverlayView));

    public static readonly BindableProperty UseVerticalTileLayoutProperty =
        BindableProperty.Create(nameof(UseVerticalTileLayout), typeof(bool), typeof(FactionSelectorOverlayView), true);

    /// <summary>Raised when the user dismisses the overlay via the close button.</summary>
    public event EventHandler? CloseRequested;

    public FactionSelectorOverlayView()
    {
        InitializeComponent();

        // Run the page's selection command for the tapped faction; the page owns close behaviour.
        ItemTappedCommand = new Command<object>(parameter =>
        {
            if (SelectFactionCommand?.CanExecute(parameter) == true)
            {
                SelectFactionCommand.Execute(parameter);
            }
        });
    }

    public bool IsOpen
    {
        get => (bool)GetValue(IsOpenProperty);
        set => SetValue(IsOpenProperty, value);
    }

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public IEnumerable? ItemsSource
    {
        get => (IEnumerable?)GetValue(ItemsSourceProperty);
        set => SetValue(ItemsSourceProperty, value);
    }

    public ICommand? SelectFactionCommand
    {
        get => (ICommand?)GetValue(SelectFactionCommandProperty);
        set => SetValue(SelectFactionCommandProperty, value);
    }

    /// <summary>True when cards should use the large vertical-tile (grid) layout; false for rows.</summary>
    public bool UseVerticalTileLayout
    {
        get => (bool)GetValue(UseVerticalTileLayoutProperty);
        private set => SetValue(UseVerticalTileLayoutProperty, value);
    }

    /// <summary>Internal wrapper command bound by each card's template.</summary>
    public ICommand ItemTappedCommand { get; }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);

        if (width <= 0)
        {
            return;
        }

        var span = width < SingleColumnMaxWidth ? 1 : 2;
        UseVerticalTileLayout = span == 2;

        if (FactionCollectionView.ItemsLayout is GridItemsLayout grid && grid.Span == span)
        {
            return;
        }

        FactionCollectionView.ItemsLayout = new GridItemsLayout(span, ItemsLayoutOrientation.Vertical)
        {
            HorizontalItemSpacing = 8,
            VerticalItemSpacing = 8
        };
    }

    private void OnCloseTapped(object? sender, TappedEventArgs e)
    {
        CloseRequested?.Invoke(this, EventArgs.Empty);
    }
}
