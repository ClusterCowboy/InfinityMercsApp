using System.Collections.ObjectModel;
using System.Windows.Input;
using InfinityMercsApp.Data.Database;
using InfinityMercsApp.Services;

namespace InfinityMercsApp.ViewModels;

public class ViewerViewModel : BaseViewModel
{
    private readonly IMetadataAccessor? _metadataAccessor;
    private readonly IArmyDataAccessor? _armyDataAccessor;
    private readonly FactionLogoCacheService? _factionLogoCacheService;
    private bool _isLoading;
    private string _status = "Loading factions...";
    private string _unitsStatus = "Select a faction.";
    private ViewerFactionItem? _selectedFaction;
    private ViewerUnitItem? _selectedUnit;

    public ViewerViewModel(
        IMetadataAccessor? metadataAccessor = null,
        IArmyDataAccessor? armyDataAccessor = null,
        FactionLogoCacheService? factionLogoCacheService = null)
    {
        _metadataAccessor = metadataAccessor;
        _armyDataAccessor = armyDataAccessor;
        _factionLogoCacheService = factionLogoCacheService;

        SelectFactionCommand = new Command<ViewerFactionItem>(async item =>
        {
            if (item is null)
            {
                return;
            }

            SelectedFaction = item;
            await LoadUnitsForSelectedFactionAsync();
        });

        SelectUnitCommand = new Command<ViewerUnitItem>(item =>
        {
            if (item is null)
            {
                return;
            }

            SelectedUnit = item;
        });
    }

    public ObservableCollection<ViewerFactionItem> Factions { get; } = [];

    public ObservableCollection<ViewerUnitItem> Units { get; } = [];

    public bool IsLoading
    {
        get => _isLoading;
        private set
        {
            if (_isLoading == value)
            {
                return;
            }

            _isLoading = value;
            OnPropertyChanged();
        }
    }

    public string Status
    {
        get => _status;
        private set
        {
            if (_status == value)
            {
                return;
            }

            _status = value;
            OnPropertyChanged();
        }
    }

    public string UnitsStatus
    {
        get => _unitsStatus;
        private set
        {
            if (_unitsStatus == value)
            {
                return;
            }

            _unitsStatus = value;
            OnPropertyChanged();
        }
    }

    public ViewerFactionItem? SelectedFaction
    {
        get => _selectedFaction;
        set
        {
            if (_selectedFaction == value)
            {
                return;
            }

            if (_selectedFaction is not null)
            {
                _selectedFaction.IsSelected = false;
            }

            _selectedFaction = value;

            if (_selectedFaction is not null)
            {
                _selectedFaction.IsSelected = true;
            }

            SelectedUnit = null;
            OnPropertyChanged();
            OnPropertyChanged(nameof(SelectedFactionLogoUrl));
        }
    }

    public ViewerUnitItem? SelectedUnit
    {
        get => _selectedUnit;
        set
        {
            if (_selectedUnit == value)
            {
                return;
            }

            if (_selectedUnit is not null)
            {
                _selectedUnit.IsSelected = false;
            }

            _selectedUnit = value;

            if (_selectedUnit is not null)
            {
                _selectedUnit.IsSelected = true;
            }

            OnPropertyChanged();
        }
    }

    public string SelectedFactionLogoUrl => SelectedFaction?.Logo ?? string.Empty;

    public ICommand SelectFactionCommand { get; }

    public ICommand SelectUnitCommand { get; }

    public async Task LoadFactionsAsync(CancellationToken cancellationToken = default)
    {
        if (_metadataAccessor is null)
        {
            Status = "Metadata service unavailable.";
            return;
        }

        try
        {
            IsLoading = true;
            Status = "Loading factions...";
            var factions = await _metadataAccessor.GetFactionsAsync(false, cancellationToken);
            if (_factionLogoCacheService is not null)
            {
                await _factionLogoCacheService.CacheFactionLogosFromRecordsAsync(factions, cancellationToken);
            }

            Factions.Clear();
            foreach (var faction in factions)
            {
                Factions.Add(new ViewerFactionItem
                {
                    Id = faction.Id,
                    Name = faction.Name,
                    Logo = faction.Logo,
                    CachedLogoPath = _factionLogoCacheService?.TryGetCachedLogoPath(faction.Id),
                    PackagedLogoPath = $"SVGCache/{faction.Id}.svg"
                });
            }

            Status = factions.Count == 0 ? "No factions available." : $"{factions.Count} factions loaded.";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LoadFactionsAsync failed: {ex.Message}");
            Status = $"Failed to load factions: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    public async Task LoadUnitsForSelectedFactionAsync(CancellationToken cancellationToken = default)
    {
        Units.Clear();
        SelectedUnit = null;

        if (SelectedFaction is null)
        {
            UnitsStatus = "Select a faction.";
            return;
        }

        if (_armyDataAccessor is null)
        {
            UnitsStatus = "Army data service unavailable.";
            return;
        }

        try
        {
            UnitsStatus = "Loading units...";
            var units = await _armyDataAccessor.GetResumeByFactionAsync(SelectedFaction.Id, cancellationToken);
            if (_factionLogoCacheService is not null)
            {
                UnitsStatus = "Preparing unit SVG cache...";
                var cacheResult = await _factionLogoCacheService.CacheUnitLogosFromRecordsAsync(SelectedFaction.Id, units, cancellationToken);
                Console.Error.WriteLine($"Unit cache for faction {SelectedFaction.Id}: downloaded={cacheResult.Downloaded}, reused={cacheResult.CachedReuse}, failed={cacheResult.Failed}");
            }

            foreach (var unit in units.OrderBy(x => x.Name))
            {
                Units.Add(new ViewerUnitItem
                {
                    Id = unit.UnitId,
                    Name = unit.Name,
                    Logo = unit.Logo,
                    CachedLogoPath = _factionLogoCacheService?.TryGetCachedUnitLogoPath(SelectedFaction.Id, unit.UnitId),
                    PackagedLogoPath = $"SVGCache/units/{SelectedFaction.Id}-{unit.UnitId}.svg"
                });
            }

            UnitsStatus = Units.Count == 0 ? "No units available for this faction." : $"{Units.Count} units loaded.";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LoadUnitsForSelectedFactionAsync failed: {ex.Message}");
            UnitsStatus = $"Failed to load units: {ex.Message}";
        }
    }
}

public interface IViewerListItem
{
    string Name { get; }

    string? CachedLogoPath { get; }

    string? PackagedLogoPath { get; }

    bool IsSelected { get; set; }
}

public class ViewerFactionItem : BaseViewModel, IViewerListItem
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Logo { get; init; }

    public string? CachedLogoPath { get; init; }

    public string? PackagedLogoPath { get; init; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
        }
    }
}

public class ViewerUnitItem : BaseViewModel, IViewerListItem
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Logo { get; init; }

    public string? CachedLogoPath { get; init; }

    public string? PackagedLogoPath { get; init; }

    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value)
            {
                return;
            }

            _isSelected = value;
            OnPropertyChanged();
        }
    }
}
