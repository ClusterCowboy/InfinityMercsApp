using System.Collections.ObjectModel;
using System.Text.Json;
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
    private string _profilesStatus = "Select a unit.";
    private ViewerFactionItem? _selectedFaction;
    private ViewerUnitItem? _selectedUnit;
    private bool _mercsOnlyUnits;
    private static readonly Dictionary<int, int> UnitTypeSortOrder = new()
    {
        [1] = 0, // LI
        [2] = 1, // MI
        [3] = 2, // HI
        [4] = 3, // TAG
        [5] = 4, // REM
        [6] = 5, // SK
        [7] = 6  // WB
    };

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

        SelectUnitCommand = new Command<ViewerUnitItem>(async item =>
        {
            if (item is null)
            {
                return;
            }

            SelectedUnit = item;
            await LoadProfilesForSelectedUnitAsync();
        });
    }

    public ObservableCollection<ViewerFactionItem> Factions { get; } = [];

    public ObservableCollection<ViewerUnitItem> Units { get; } = [];
    public ObservableCollection<ViewerProfileItem> Profiles { get; } = [];

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

    public string ProfilesStatus
    {
        get => _profilesStatus;
        private set
        {
            if (_profilesStatus == value)
            {
                return;
            }

            _profilesStatus = value;
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
            Profiles.Clear();
            ProfilesStatus = "Select a unit.";
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

    public bool MercsOnlyUnits
    {
        get => _mercsOnlyUnits;
        set
        {
            if (_mercsOnlyUnits == value)
            {
                return;
            }

            _mercsOnlyUnits = value;
            OnPropertyChanged();

            if (SelectedFaction is not null)
            {
                _ = LoadUnitsForSelectedFactionAsync();
            }
        }
    }

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
        Profiles.Clear();
        ProfilesStatus = "Select a unit.";

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
            var units = MercsOnlyUnits
                ? await _armyDataAccessor.GetResumeByFactionMercsOnlyAsync(SelectedFaction.Id, cancellationToken)
                : await _armyDataAccessor.GetResumeByFactionAsync(SelectedFaction.Id, cancellationToken);

            var snapshot = await _armyDataAccessor.GetFactionSnapshotAsync(SelectedFaction.Id, cancellationToken);
            var typeLookup = BuildIdNameLookup(snapshot?.FiltersJson, "type");
            var categoryLookup = BuildIdNameLookup(snapshot?.FiltersJson, "category");

            if (_factionLogoCacheService is not null)
            {
                UnitsStatus = "Preparing unit SVG cache...";
                var cacheResult = await _factionLogoCacheService.CacheUnitLogosFromRecordsAsync(SelectedFaction.Id, units, cancellationToken);
                Console.Error.WriteLine($"Unit cache for faction {SelectedFaction.Id}: downloaded={cacheResult.Downloaded}, reused={cacheResult.CachedReuse}, failed={cacheResult.Failed}");
            }

            var orderedUnits = units
                .OrderBy(unit => GetUnitTypeSortIndex(unit.Type))
                .ThenBy(unit => unit.Name, StringComparer.OrdinalIgnoreCase);

            foreach (var unit in orderedUnits)
            {
                Units.Add(new ViewerUnitItem
                {
                    Id = unit.UnitId,
                    Name = unit.Name,
                    Logo = unit.Logo,
                    Subtitle = BuildUnitSubtitle(unit, typeLookup, categoryLookup),
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

    private static string BuildUnitSubtitle(
        ArmyResumeRecord unit,
        IReadOnlyDictionary<int, string> typeLookup,
        IReadOnlyDictionary<int, string> categoryLookup)
    {
        var typeName = unit.Type.HasValue && typeLookup.TryGetValue(unit.Type.Value, out var t)
            ? t
            : (unit.Type?.ToString() ?? "?");

        var categoryName = unit.Category.HasValue && categoryLookup.TryGetValue(unit.Category.Value, out var c)
            ? c
            : (unit.Category?.ToString() ?? "?");

        return $"{typeName} - {categoryName}";
    }

    private static int GetUnitTypeSortIndex(int? unitType)
    {
        if (!unitType.HasValue)
        {
            return int.MaxValue - 1;
        }

        return UnitTypeSortOrder.TryGetValue(unitType.Value, out var sortIndex)
            ? sortIndex
            : int.MaxValue;
    }

    private static Dictionary<int, string> BuildIdNameLookup(string? filtersJson, string sectionName)
    {
        var map = new Dictionary<int, string>();
        if (string.IsNullOrWhiteSpace(filtersJson))
        {
            return map;
        }

        try
        {
            using var doc = JsonDocument.Parse(filtersJson);
            if (!doc.RootElement.TryGetProperty(sectionName, out var section) || section.ValueKind != JsonValueKind.Array)
            {
                return map;
            }

            foreach (var entry in section.EnumerateArray())
            {
                if (!entry.TryGetProperty("id", out var idElement))
                {
                    continue;
                }

                int id;
                if (idElement.ValueKind == JsonValueKind.Number && idElement.TryGetInt32(out var intId))
                {
                    id = intId;
                }
                else if (idElement.ValueKind == JsonValueKind.String && int.TryParse(idElement.GetString(), out var stringId))
                {
                    id = stringId;
                }
                else
                {
                    continue;
                }

                if (!entry.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var name = nameElement.GetString();
                if (!string.IsNullOrWhiteSpace(name))
                {
                    map[id] = name;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"BuildIdNameLookup failed for section '{sectionName}': {ex.Message}");
        }

        return map;
    }

    public async Task LoadProfilesForSelectedUnitAsync(CancellationToken cancellationToken = default)
    {
        Profiles.Clear();

        if (SelectedFaction is null || SelectedUnit is null)
        {
            ProfilesStatus = "Select a unit.";
            return;
        }

        if (_armyDataAccessor is null)
        {
            ProfilesStatus = "Army data service unavailable.";
            return;
        }

        try
        {
            ProfilesStatus = "Loading profiles...";
            var unit = await _armyDataAccessor.GetUnitAsync(SelectedFaction.Id, SelectedUnit.Id, cancellationToken);
            if (unit is null || string.IsNullOrWhiteSpace(unit.ProfileGroupsJson))
            {
                ProfilesStatus = "No profiles found for this unit.";
                return;
            }

            using var doc = JsonDocument.Parse(unit.ProfileGroupsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                ProfilesStatus = "No profiles found for this unit.";
                return;
            }

            foreach (var group in doc.RootElement.EnumerateArray())
            {
                var groupName = group.TryGetProperty("isc", out var iscElement) && iscElement.ValueKind == JsonValueKind.String
                    ? iscElement.GetString() ?? string.Empty
                    : string.Empty;

                if (!group.TryGetProperty("profiles", out var profilesElement) || profilesElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var profile in profilesElement.EnumerateArray())
                {
                    var profileName = profile.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
                        ? nameElement.GetString() ?? string.Empty
                        : string.Empty;

                    if (string.IsNullOrWhiteSpace(profileName))
                    {
                        continue;
                    }

                    Profiles.Add(new ViewerProfileItem
                    {
                        GroupName = groupName,
                        Name = profileName
                    });
                }
            }

            ProfilesStatus = Profiles.Count == 0 ? "No profiles found for this unit." : $"{Profiles.Count} profiles loaded.";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LoadProfilesForSelectedUnitAsync failed: {ex.Message}");
            ProfilesStatus = $"Failed to load profiles: {ex.Message}";
        }
    }
}

public interface IViewerListItem
{
    string Name { get; }

    string? CachedLogoPath { get; }

    string? PackagedLogoPath { get; }

    string? Subtitle { get; }

    bool HasSubtitle { get; }

    bool IsSelected { get; set; }
}

public class ViewerFactionItem : BaseViewModel, IViewerListItem
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Logo { get; init; }

    public string? CachedLogoPath { get; init; }

    public string? PackagedLogoPath { get; init; }

    public string? Subtitle => null;

    public bool HasSubtitle => false;

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

    public string? Subtitle { get; init; }

    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

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

public class ViewerProfileItem
{
    public string GroupName { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;
}
