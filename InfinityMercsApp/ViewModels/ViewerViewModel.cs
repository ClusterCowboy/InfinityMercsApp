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
    private readonly AppSettingsService? _appSettingsService;
    private bool _isLoading;
    private string _status = "Loading factions...";
    private string _unitsStatus = "Select a faction.";
    private string _profilesStatus = "Select a unit.";
    private string _unitNameHeading = "Select a unit";
    private string _unitMov = "-";
    private string _unitCc = "-";
    private string _unitBs = "-";
    private string _unitPh = "-";
    private string _unitWip = "-";
    private string _unitArm = "-";
    private string _unitBts = "-";
    private string _unitVitalityHeader = "VITA";
    private string _unitVitality = "-";
    private string _unitS = "-";
    private string _unitAva = "-";
    private string _equipmentSummary = "Equipment: -";
    private string _specialSkillsSummary = "Special Skills: -";
    private bool _showUnitsInInches = true;
    private int? _unitMoveFirstCm;
    private int? _unitMoveSecondCm;
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
        FactionLogoCacheService? factionLogoCacheService = null,
        AppSettingsService? appSettingsService = null)
    {
        _metadataAccessor = metadataAccessor;
        _armyDataAccessor = armyDataAccessor;
        _factionLogoCacheService = factionLogoCacheService;
        _appSettingsService = appSettingsService;

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

    public string EquipmentSummary
    {
        get => _equipmentSummary;
        private set
        {
            if (_equipmentSummary == value)
            {
                return;
            }

            _equipmentSummary = value;
            OnPropertyChanged();
        }
    }

    public string SpecialSkillsSummary
    {
        get => _specialSkillsSummary;
        private set
        {
            if (_specialSkillsSummary == value)
            {
                return;
            }

            _specialSkillsSummary = value;
            OnPropertyChanged();
        }
    }

    public string UnitNameHeading
    {
        get => _unitNameHeading;
        private set
        {
            if (_unitNameHeading == value)
            {
                return;
            }

            _unitNameHeading = value;
            OnPropertyChanged();
        }
    }

    public string UnitMov { get => _unitMov; private set { if (_unitMov != value) { _unitMov = value; OnPropertyChanged(); } } }
    public string UnitCc { get => _unitCc; private set { if (_unitCc != value) { _unitCc = value; OnPropertyChanged(); } } }
    public string UnitBs { get => _unitBs; private set { if (_unitBs != value) { _unitBs = value; OnPropertyChanged(); } } }
    public string UnitPh { get => _unitPh; private set { if (_unitPh != value) { _unitPh = value; OnPropertyChanged(); } } }
    public string UnitWip { get => _unitWip; private set { if (_unitWip != value) { _unitWip = value; OnPropertyChanged(); } } }
    public string UnitArm { get => _unitArm; private set { if (_unitArm != value) { _unitArm = value; OnPropertyChanged(); } } }
    public string UnitBts { get => _unitBts; private set { if (_unitBts != value) { _unitBts = value; OnPropertyChanged(); } } }
    public string UnitVitalityHeader { get => _unitVitalityHeader; private set { if (_unitVitalityHeader != value) { _unitVitalityHeader = value; OnPropertyChanged(); } } }
    public string UnitVitality { get => _unitVitality; private set { if (_unitVitality != value) { _unitVitality = value; OnPropertyChanged(); } } }
    public string UnitS { get => _unitS; private set { if (_unitS != value) { _unitS = value; OnPropertyChanged(); } } }
    public string UnitAva { get => _unitAva; private set { if (_unitAva != value) { _unitAva = value; OnPropertyChanged(); } } }

    public bool ShowUnitsInInches
    {
        get => _showUnitsInInches;
        set
        {
            if (_showUnitsInInches == value)
            {
                return;
            }

            _showUnitsInInches = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowUnitsInCentimeters));
            UpdateUnitMoveDisplay();
        }
    }

    public bool ShowUnitsInCentimeters
    {
        get => !_showUnitsInInches;
        set
        {
            var targetInches = !value;
            if (_showUnitsInInches == targetInches)
            {
                return;
            }

            _showUnitsInInches = targetInches;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowUnitsInInches));
            UpdateUnitMoveDisplay();
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
            ResetUnitDetails();
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
        await ApplyGlobalDisplayUnitsPreferenceAsync(cancellationToken);

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
        ResetUnitDetails();

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

    private void ResetUnitDetails()
    {
        Profiles.Clear();
        ProfilesStatus = "Select a unit.";
        UnitNameHeading = "Select a unit";
        ResetUnitStats();
        EquipmentSummary = "Equipment: -";
        SpecialSkillsSummary = "Special Skills: -";
    }

    private void ResetUnitStats()
    {
        _unitMoveFirstCm = null;
        _unitMoveSecondCm = null;
        UnitMov = "-";
        UnitCc = "-";
        UnitBs = "-";
        UnitPh = "-";
        UnitWip = "-";
        UnitArm = "-";
        UnitBts = "-";
        UnitVitalityHeader = "VITA";
        UnitVitality = "-";
        UnitS = "-";
        UnitAva = "-";
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

    private static void CollectIdsFromArrayProperty(JsonElement container, string propertyName, HashSet<int> target)
    {
        if (!container.TryGetProperty(propertyName, out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var entry in arr.EnumerateArray())
        {
            if (TryParseId(entry, out var id))
            {
                target.Add(id);
            }
        }
    }

    private static HashSet<int> CollectIdsFromArrayProperty(JsonElement container, string propertyName)
    {
        var ids = new HashSet<int>();
        CollectIdsFromArrayProperty(container, propertyName, ids);
        return ids;
    }

    private static bool TryParseId(JsonElement element, out int id)
    {
        id = 0;
        if (element.ValueKind == JsonValueKind.Number)
        {
            return element.TryGetInt32(out id);
        }

        if (element.ValueKind == JsonValueKind.String)
        {
            return int.TryParse(element.GetString(), out id);
        }

        if (element.ValueKind == JsonValueKind.Object && element.TryGetProperty("id", out var idElement))
        {
            return TryParseId(idElement, out id);
        }

        return false;
    }

    private static string BuildNamedSummary(string label, IEnumerable<int> ids, IReadOnlyDictionary<int, string> lookup)
    {
        var values = ids
            .Select(id => lookup.TryGetValue(id, out var name) ? name : id.ToString())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return values.Count == 0
            ? $"{label}: -"
            : $"{label}: {string.Join(", ", values)}";
    }

    private void PopulateUnitStatsFromFirstProfile(JsonElement profileGroupsArray)
    {
        ResetUnitStats();

        if (profileGroupsArray.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        JsonElement? firstProfile = null;
        foreach (var group in profileGroupsArray.EnumerateArray())
        {
            if (!group.TryGetProperty("profiles", out var profilesElement) || profilesElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var profile in profilesElement.EnumerateArray())
            {
                firstProfile = profile;
                break;
            }

            if (firstProfile.HasValue)
            {
                break;
            }
        }

        if (!firstProfile.HasValue)
        {
            return;
        }

        var profileElement = firstProfile.Value;
        (_unitMoveFirstCm, _unitMoveSecondCm) = ParseMoveValues(profileElement);
        UpdateUnitMoveDisplay();
        UnitCc = ReadIntAsString(profileElement, "cc");
        UnitBs = ReadIntAsString(profileElement, "bs");
        UnitPh = ReadIntAsString(profileElement, "ph");
        UnitWip = ReadIntAsString(profileElement, "wip");
        UnitArm = ReadIntAsString(profileElement, "arm");
        UnitBts = ReadIntAsString(profileElement, "bts");
        UnitS = ReadIntAsString(profileElement, "s");
        UnitAva = ReadAvaAsString(profileElement);

        var isStr = profileElement.TryGetProperty("str", out var strElement) &&
                    strElement.ValueKind == JsonValueKind.True;
        UnitVitalityHeader = isStr ? "STR" : "VITA";
        UnitVitality = ReadIntAsString(profileElement, "w");
    }

    private static string ReadIntAsString(JsonElement element, string propertyName)
    {
        if (!element.TryGetProperty(propertyName, out var value))
        {
            return "-";
        }

        return value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out var i)
            ? i.ToString()
            : "-";
    }

    private static string ReadAvaAsString(JsonElement element)
    {
        if (!element.TryGetProperty("ava", out var value))
        {
            return "-";
        }

        if (value.ValueKind != JsonValueKind.Number || !value.TryGetInt32(out var ava))
        {
            return "-";
        }

        return ava == 255 ? "T" : ava.ToString();
    }

    private static (int? firstCm, int? secondCm) ParseMoveValues(JsonElement element)
    {
        if (!element.TryGetProperty("move", out var moveElement) || moveElement.ValueKind != JsonValueKind.Array)
        {
            return (null, null);
        }

        var values = moveElement.EnumerateArray()
            .Where(x => x.ValueKind == JsonValueKind.Number && x.TryGetInt32(out _))
            .Select(x => x.GetInt32())
            .ToList();

        return values.Count >= 2 ? (values[0], values[1]) : (null, null);
    }

    private void UpdateUnitMoveDisplay()
    {
        if (!_unitMoveFirstCm.HasValue || !_unitMoveSecondCm.HasValue)
        {
            UnitMov = "-";
            return;
        }

        if (ShowUnitsInInches)
        {
            var first = (int)Math.Round(_unitMoveFirstCm.Value / 2.5, MidpointRounding.AwayFromZero);
            var second = (int)Math.Round(_unitMoveSecondCm.Value / 2.5, MidpointRounding.AwayFromZero);
            UnitMov = $"{first}-{second}";
            return;
        }

        UnitMov = $"{_unitMoveFirstCm.Value}-{_unitMoveSecondCm.Value}";
    }

    public async Task LoadProfilesForSelectedUnitAsync(CancellationToken cancellationToken = default)
    {
        await ApplyGlobalDisplayUnitsPreferenceAsync(cancellationToken);

        Profiles.Clear();
        UnitNameHeading = SelectedUnit?.Name ?? "Select a unit";
        EquipmentSummary = "Equipment: -";
        SpecialSkillsSummary = "Special Skills: -";

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
            var snapshot = await _armyDataAccessor.GetFactionSnapshotAsync(SelectedFaction.Id, cancellationToken);
            var equipLookup = BuildIdNameLookup(snapshot?.FiltersJson, "equip");
            var skillsLookup = BuildIdNameLookup(snapshot?.FiltersJson, "skills");

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

            PopulateUnitStatsFromFirstProfile(doc.RootElement);

            HashSet<int>? commonEquipIds = null;
            HashSet<int>? commonSkillIds = null;
            var profileCount = 0;

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
                    var profileEquip = CollectIdsFromArrayProperty(profile, "equip");
                    var profileSkills = CollectIdsFromArrayProperty(profile, "skills");

                    if (commonEquipIds is null)
                    {
                        commonEquipIds = new HashSet<int>(profileEquip);
                    }
                    else
                    {
                        commonEquipIds.IntersectWith(profileEquip);
                    }

                    if (commonSkillIds is null)
                    {
                        commonSkillIds = new HashSet<int>(profileSkills);
                    }
                    else
                    {
                        commonSkillIds.IntersectWith(profileSkills);
                    }

                    profileCount++;

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

            var stableEquip = profileCount > 0 ? (IEnumerable<int>)(commonEquipIds ?? []) : [];
            var stableSkills = profileCount > 0 ? (IEnumerable<int>)(commonSkillIds ?? []) : [];
            EquipmentSummary = BuildNamedSummary("Equipment", stableEquip, equipLookup);
            SpecialSkillsSummary = BuildNamedSummary("Special Skills", stableSkills, skillsLookup);
            ProfilesStatus = Profiles.Count == 0 ? "No profiles found for this unit." : $"{Profiles.Count} profiles loaded.";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"LoadProfilesForSelectedUnitAsync failed: {ex.Message}");
            ProfilesStatus = $"Failed to load profiles: {ex.Message}";
        }
    }

    private async Task ApplyGlobalDisplayUnitsPreferenceAsync(CancellationToken cancellationToken = default)
    {
        if (_appSettingsService is null)
        {
            return;
        }

        try
        {
            var showInches = await _appSettingsService.GetShowUnitsInInchesAsync(cancellationToken);
            if (_showUnitsInInches == showInches)
            {
                return;
            }

            _showUnitsInInches = showInches;
            OnPropertyChanged(nameof(ShowUnitsInInches));
            OnPropertyChanged(nameof(ShowUnitsInCentimeters));
            UpdateUnitMoveDisplay();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ApplyGlobalDisplayUnitsPreferenceAsync failed: {ex.Message}");
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
