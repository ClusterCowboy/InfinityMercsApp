using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Views.Adaptive;
using System.Collections.ObjectModel;

namespace InfinityMercsApp.Views;

public partial class ArmoryPage : AdaptiveContentPage
{
    private readonly IMetadataProvider? _metadataProvider;
    private readonly ObservableCollection<WeaponGroup> _weaponGroups = [];
    private WeaponGroup? _selectedWeaponGroup;
    private bool _showDetail;

    public ObservableCollection<WeaponGroup> WeaponGroups => _weaponGroups;

    public WeaponGroup? SelectedWeaponGroup
    {
        get => _selectedWeaponGroup;
        set
        {
            if (_selectedWeaponGroup == value)
            {
                return;
            }

            _selectedWeaponGroup = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedWeaponName));
            OnPropertyChanged(nameof(HasSelectedWeapon));
            OnPropertyChanged(nameof(NoWeaponSelected));

            // In compact mode, choosing a weapon reveals the detail pane over the list.
            _showDetail = _selectedWeaponGroup is not null;
            ApplyLayout();
        }
    }

    public string SelectedWeaponName => _selectedWeaponGroup?.Name ?? "Select a weapon";
    public bool HasSelectedWeapon => _selectedWeaponGroup is not null;
    public bool NoWeaponSelected => _selectedWeaponGroup is null;

    public ArmoryPage(IMetadataProvider? metadataProvider = null)
    {
        _metadataProvider = metadataProvider;
        InitializeComponent();
        BindingContext = this;
        LoadWeapons(null);
        ApplyLayout();
    }

    protected override void OnLayoutModeChanged(AdaptiveLayoutMode mode) => ApplyLayout();

    private void ApplyLayout()
    {
        if (IsCompact)
        {
            RootGrid.ColumnDefinitions = [new ColumnDefinition(GridLength.Star)];
            RootGrid.ColumnSpacing = 0;
            Grid.SetColumn(ListPane, 0);
            Grid.SetColumn(DetailPane, 0);
            ListPane.IsVisible = !_showDetail;
            DetailPane.IsVisible = _showDetail;
            DetailBackButton.IsVisible = true;
        }
        else
        {
            var listWidth = LayoutMode switch
            {
                AdaptiveLayoutMode.Medium => 320d,
                AdaptiveLayoutMode.Expanded => 340d,
                _ => 360d
            };

            RootGrid.ColumnDefinitions =
            [
                new ColumnDefinition(new GridLength(listWidth)),
                new ColumnDefinition(GridLength.Star)
            ];
            RootGrid.ColumnSpacing = 16;
            Grid.SetColumn(ListPane, 0);
            Grid.SetColumn(DetailPane, 1);
            ListPane.IsVisible = true;
            DetailPane.IsVisible = true;
            DetailBackButton.IsVisible = false;
        }

        // Keep weapon cards at a comfortable reading width on the largest screens. Stay Fill and cap
        // via MaximumWidthRequest; centering can collapse a stack whose children have no fixed width.
        DetailWeaponList.HorizontalOptions = LayoutOptions.Fill;
        DetailWeaponList.MaximumWidthRequest = IsWide ? 820d : double.PositiveInfinity;
    }

    private void OnDetailBackClicked(object? sender, EventArgs e)
    {
        _showDetail = false;
        ApplyLayout();
    }

    private void OnWeaponSearchTextChanged(object? sender, TextChangedEventArgs e)
    {
        LoadWeapons(e.NewTextValue);
    }

    private void LoadWeapons(string? searchTerm)
    {
        if (_metadataProvider is null)
        {
            return;
        }

        var weapons = string.IsNullOrWhiteSpace(searchTerm)
            ? _metadataProvider.GetAllWeapons()
            : _metadataProvider.SearchWeaponsByName(searchTerm);

        var groups = weapons
            .GroupBy(w => w.Name.Trim(), StringComparer.OrdinalIgnoreCase)
            .Select(g => new WeaponGroup
            {
                Name = g.Key,
                Modes = g.OrderBy(w => w.Mode ?? string.Empty).ToList()
            })
            .OrderBy(g => g.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _weaponGroups.Clear();
        foreach (var group in groups)
        {
            _weaponGroups.Add(group);
        }
    }

    public sealed class WeaponGroup
    {
        public required string Name { get; init; }
        public required IReadOnlyList<Weapon> Modes { get; init; }
    }
}
