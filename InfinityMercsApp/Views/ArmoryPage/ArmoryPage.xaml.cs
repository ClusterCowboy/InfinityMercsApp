using InfinityMercsApp.Domain.Models.Metadata;
using InfinityMercsApp.Infrastructure.Providers;
using System.Collections.ObjectModel;

namespace InfinityMercsApp.Views;

public partial class ArmoryPage : ContentPage
{
    private readonly IMetadataProvider? _metadataProvider;
    private readonly ObservableCollection<WeaponGroup> _weaponGroups = [];
    private WeaponGroup? _selectedWeaponGroup;

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
