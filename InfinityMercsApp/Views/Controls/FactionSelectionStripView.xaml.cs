using System.Collections;
using System.Windows.Input;

namespace InfinityMercsApp.Views.Controls;

/// <summary>
/// Horizontal faction picker strip. Backed by a virtualized <see cref="CollectionView"/> so only the
/// on-screen faction tiles are realised; tapping a tile runs <see cref="SelectFactionCommand"/>.
/// </summary>
public partial class FactionSelectionStripView : ContentView
{
    public static readonly BindableProperty ItemsSourceProperty =
        BindableProperty.Create(nameof(ItemsSource), typeof(IEnumerable), typeof(FactionSelectionStripView));

    public static readonly BindableProperty SelectFactionCommandProperty =
        BindableProperty.Create(nameof(SelectFactionCommand), typeof(ICommand), typeof(FactionSelectionStripView));

    public FactionSelectionStripView()
    {
        InitializeComponent();
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

    /// <summary>
    /// The item source actually bound to the inner list. Faction tiles are SVG-heavy, so a hidden
    /// strip binds nothing and pays no build cost until it is shown.
    /// </summary>
    public IEnumerable? EffectiveItemsSource => IsVisible ? ItemsSource : null;

    protected override void OnPropertyChanged(string? propertyName = null)
    {
        base.OnPropertyChanged(propertyName);

        if (propertyName == IsVisibleProperty.PropertyName ||
            propertyName == ItemsSourceProperty.PropertyName)
        {
            OnPropertyChanged(nameof(EffectiveItemsSource));
        }
    }
}
