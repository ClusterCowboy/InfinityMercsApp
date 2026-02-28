using System.Collections.ObjectModel;
using System.Globalization;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Text;
using System.Windows.Input;
using Microsoft.Maui.Controls;
using InfinityMercsApp.Data.Database;
using InfinityMercsApp.Services;

namespace InfinityMercsApp.ViewModels;

public class ViewerViewModel : BaseViewModel
{
    private readonly record struct ExtraDefinition(string Name, string Type, string? Url);

    private enum FactionFilterMode
    {
        All,
        Factions,
        Sectorials
    }

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
    private FormattedString _equipmentSummaryFormatted = new();
    private FormattedString _specialSkillsSummaryFormatted = new();
    private bool _showRegularOrderIcon;
    private bool _showIrregularOrderIcon;
    private bool _showImpetuousIcon;
    private bool _showTacticalAwarenessIcon;
    private bool _showCubeIcon;
    private bool _showCube2Icon;
    private bool _showHackableIcon;
    private string? _impetuousIconUrl;
    private string? _tacticalAwarenessIconUrl;
    private string? _cubeIconUrl;
    private string? _cube2IconUrl;
    private string? _hackableIconUrl;
    private bool _showUnitsInInches = true;
    private int? _unitMoveFirstCm;
    private int? _unitMoveSecondCm;
    private ViewerFactionItem? _selectedFaction;
    private ViewerUnitItem? _selectedUnit;
    private bool _mercsOnlyUnits;
    private bool _lieutenantOnlyUnits;
    private FactionFilterMode _factionFilterMode = FactionFilterMode.All;
    private List<ViewerFactionItem> _allFactions = [];
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

    public FormattedString EquipmentSummaryFormatted
    {
        get => _equipmentSummaryFormatted;
        private set
        {
            _equipmentSummaryFormatted = value;
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

    public FormattedString SpecialSkillsSummaryFormatted
    {
        get => _specialSkillsSummaryFormatted;
        private set
        {
            _specialSkillsSummaryFormatted = value;
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

    public bool ShowRegularOrderIcon
    {
        get => _showRegularOrderIcon;
        private set
        {
            if (_showRegularOrderIcon == value)
            {
                return;
            }

            _showRegularOrderIcon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasOrderTypeIcon));
            OnPropertyChanged(nameof(HasAnyTopHeaderIcons));
        }
    }

    public bool ShowIrregularOrderIcon
    {
        get => _showIrregularOrderIcon;
        private set
        {
            if (_showIrregularOrderIcon == value)
            {
                return;
            }

            _showIrregularOrderIcon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasOrderTypeIcon));
            OnPropertyChanged(nameof(HasAnyTopHeaderIcons));
        }
    }

    public bool ShowImpetuousIcon
    {
        get => _showImpetuousIcon;
        private set
        {
            if (_showImpetuousIcon == value)
            {
                return;
            }

            _showImpetuousIcon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAnyTopHeaderIcons));
        }
    }

    public bool ShowTacticalAwarenessIcon
    {
        get => _showTacticalAwarenessIcon;
        private set
        {
            if (_showTacticalAwarenessIcon == value)
            {
                return;
            }

            _showTacticalAwarenessIcon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAnyTopHeaderIcons));
        }
    }

    public bool ShowCubeIcon
    {
        get => _showCubeIcon;
        private set
        {
            if (_showCubeIcon == value)
            {
                return;
            }

            _showCubeIcon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAnyBottomHeaderIcons));
        }
    }

    public bool ShowCube2Icon
    {
        get => _showCube2Icon;
        private set
        {
            if (_showCube2Icon == value)
            {
                return;
            }

            _showCube2Icon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAnyBottomHeaderIcons));
        }
    }

    public bool ShowHackableIcon
    {
        get => _showHackableIcon;
        private set
        {
            if (_showHackableIcon == value)
            {
                return;
            }

            _showHackableIcon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAnyBottomHeaderIcons));
        }
    }

    public bool HasOrderTypeIcon => ShowRegularOrderIcon || ShowIrregularOrderIcon;
    public bool HasAnyTopHeaderIcons => HasOrderTypeIcon || ShowImpetuousIcon || ShowTacticalAwarenessIcon;
    public bool HasAnyBottomHeaderIcons => ShowCubeIcon || ShowCube2Icon || ShowHackableIcon;

    public string? ImpetuousIconUrl
    {
        get => _impetuousIconUrl;
        private set
        {
            if (_impetuousIconUrl == value)
            {
                return;
            }

            _impetuousIconUrl = value;
            OnPropertyChanged();
        }
    }

    public string? TacticalAwarenessIconUrl
    {
        get => _tacticalAwarenessIconUrl;
        private set
        {
            if (_tacticalAwarenessIconUrl == value)
            {
                return;
            }

            _tacticalAwarenessIconUrl = value;
            OnPropertyChanged();
        }
    }

    public string? CubeIconUrl
    {
        get => _cubeIconUrl;
        private set
        {
            if (_cubeIconUrl == value)
            {
                return;
            }

            _cubeIconUrl = value;
            OnPropertyChanged();
        }
    }

    public string? Cube2IconUrl
    {
        get => _cube2IconUrl;
        private set
        {
            if (_cube2IconUrl == value)
            {
                return;
            }

            _cube2IconUrl = value;
            OnPropertyChanged();
        }
    }

    public string? HackableIconUrl
    {
        get => _hackableIconUrl;
        private set
        {
            if (_hackableIconUrl == value)
            {
                return;
            }

            _hackableIconUrl = value;
            OnPropertyChanged();
        }
    }

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

    public bool LieutenantOnlyUnits
    {
        get => _lieutenantOnlyUnits;
        set
        {
            if (_lieutenantOnlyUnits == value)
            {
                return;
            }

            _lieutenantOnlyUnits = value;
            OnPropertyChanged();

            if (SelectedFaction is not null)
            {
                _ = LoadUnitsForSelectedFactionAsync();
            }
        }
    }

    public bool ShowAllFactionEntries
    {
        get => _factionFilterMode == FactionFilterMode.All;
        set
        {
            if (!value || _factionFilterMode == FactionFilterMode.All)
            {
                return;
            }

            _factionFilterMode = FactionFilterMode.All;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowFactionEntriesOnly));
            OnPropertyChanged(nameof(ShowSectorialEntriesOnly));
            ApplyFactionFilter();
        }
    }

    public bool ShowFactionEntriesOnly
    {
        get => _factionFilterMode == FactionFilterMode.Factions;
        set
        {
            if (!value || _factionFilterMode == FactionFilterMode.Factions)
            {
                return;
            }

            _factionFilterMode = FactionFilterMode.Factions;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowAllFactionEntries));
            OnPropertyChanged(nameof(ShowSectorialEntriesOnly));
            ApplyFactionFilter();
        }
    }

    public bool ShowSectorialEntriesOnly
    {
        get => _factionFilterMode == FactionFilterMode.Sectorials;
        set
        {
            if (!value || _factionFilterMode == FactionFilterMode.Sectorials)
            {
                return;
            }

            _factionFilterMode = FactionFilterMode.Sectorials;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowAllFactionEntries));
            OnPropertyChanged(nameof(ShowFactionEntriesOnly));
            ApplyFactionFilter();
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
            var factions = await _metadataAccessor.GetFactionsAsync(true, cancellationToken);
            if (_factionLogoCacheService is not null)
            {
                await _factionLogoCacheService.CacheFactionLogosFromRecordsAsync(factions, cancellationToken);
            }

            _allFactions = factions.Select(faction => new ViewerFactionItem
            {
                Id = faction.Id,
                ParentId = faction.ParentId,
                Name = faction.Name,
                Logo = faction.Logo,
                CachedLogoPath = _factionLogoCacheService?.TryGetCachedLogoPath(faction.Id),
                PackagedLogoPath = $"SVGCache/factions/{faction.Id}.svg"
            }).ToList();

            ApplyFactionFilter();

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
            var skillsLookup = BuildIdNameLookup(snapshot?.FiltersJson, "skills");

            var filteredUnitIds = new HashSet<int>();
            foreach (var unit in units)
            {
                var unitRecord = await _armyDataAccessor.GetUnitAsync(SelectedFaction.Id, unit.UnitId, cancellationToken);
                if (UnitHasVisibleOption(
                        unitRecord?.ProfileGroupsJson,
                        skillsLookup,
                        LieutenantOnlyUnits,
                        MercsOnlyUnits))
                {
                    filteredUnitIds.Add(unit.UnitId);
                }
            }

            units = units.Where(x => filteredUnitIds.Contains(x.UnitId)).ToList();

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

    private void ApplyFactionFilter()
    {
        IEnumerable<ViewerFactionItem> filtered = _allFactions;
        if (_factionFilterMode == FactionFilterMode.Factions)
        {
            filtered = filtered.Where(x => x.Id == x.ParentId);
        }
        else if (_factionFilterMode == FactionFilterMode.Sectorials)
        {
            filtered = filtered.Where(x => x.Id != x.ParentId);
        }

        var filteredList = filtered.ToList();

        Factions.Clear();
        foreach (var faction in filteredList)
        {
            Factions.Add(faction);
        }

        if (SelectedFaction is not null && !filteredList.Contains(SelectedFaction))
        {
            SelectedFaction = null;
            Units.Clear();
            UnitsStatus = "Select a faction.";
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
        ShowRegularOrderIcon = false;
        ShowIrregularOrderIcon = false;
        ShowImpetuousIcon = false;
        ShowTacticalAwarenessIcon = false;
        ShowCubeIcon = false;
        ShowCube2Icon = false;
        ShowHackableIcon = false;
        ImpetuousIconUrl = null;
        TacticalAwarenessIconUrl = null;
        CubeIconUrl = null;
        Cube2IconUrl = null;
        HackableIconUrl = null;
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

    private static string BuildNamedSummary(string label, IEnumerable<string> values)
    {
        var list = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return list.Count == 0
            ? $"{label}: -"
            : $"{label}: {string.Join(", ", list)}";
    }

    private static FormattedString BuildNamedSummaryFormatted(
        string label,
        IEnumerable<string> values,
        IReadOnlyDictionary<int, string>? equipLookup,
        IReadOnlyDictionary<int, string>? links,
        Color textColor)
    {
        var list = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var formatted = new FormattedString();
        formatted.Spans.Add(new Span { Text = $"{label}: ", TextColor = textColor });

        if (list.Count == 0)
        {
            formatted.Spans.Add(new Span { Text = "-", TextColor = textColor });
            return formatted;
        }

        for (var i = 0; i < list.Count; i++)
        {
            var value = list[i];
            var span = new Span { Text = value, TextColor = textColor };
            var link = TryResolveLinkForDisplayName(value, equipLookup, links);
            if (!string.IsNullOrWhiteSpace(link))
            {
                var tap = new TapGestureRecognizer();
                tap.Tapped += async (_, _) => await OpenLinkAsync(link);
                span.GestureRecognizers.Add(tap);
            }

            formatted.Spans.Add(span);
            if (i < list.Count - 1)
            {
                formatted.Spans.Add(new Span { Text = ", ", TextColor = textColor });
            }
        }

        return formatted;
    }

    private static string? TryResolveLinkForDisplayName(
        string displayName,
        IReadOnlyDictionary<int, string>? nameLookup,
        IReadOnlyDictionary<int, string>? links)
    {
        if (string.IsNullOrWhiteSpace(displayName) || nameLookup is null || links is null)
        {
            return null;
        }

        foreach (var pair in nameLookup)
        {
            if (!links.ContainsKey(pair.Key))
            {
                continue;
            }

            if (string.Equals(pair.Value, displayName, StringComparison.OrdinalIgnoreCase))
            {
                return links[pair.Key];
            }
        }

        var trimmed = displayName;
        var parenIndex = displayName.IndexOf(" (", StringComparison.Ordinal);
        if (parenIndex > 0)
        {
            trimmed = displayName[..parenIndex];
        }

        foreach (var pair in nameLookup)
        {
            if (!links.ContainsKey(pair.Key))
            {
                continue;
            }

            if (string.Equals(pair.Value, trimmed, StringComparison.OrdinalIgnoreCase))
            {
                return links[pair.Key];
            }
        }

        return null;
    }

    private static string? TryResolveFirstLinkByPredicate(
        IReadOnlyDictionary<int, string> nameLookup,
        IReadOnlyDictionary<int, string> links,
        Func<string, bool> predicate)
    {
        foreach (var pair in nameLookup)
        {
            if (!links.TryGetValue(pair.Key, out var url))
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(url))
            {
                continue;
            }

            if (predicate(pair.Value))
            {
                return url;
            }
        }

        return null;
    }

    private static string BuildOptionConfigurationSummary(
        JsonElement option,
        IReadOnlyDictionary<int, string> weaponsLookup,
        IReadOnlyDictionary<int, string> equipLookup,
        IReadOnlyDictionary<int, string> skillsLookup)
    {
        var weapons = GetOrderedNames(option, "weapons", weaponsLookup);
        var equip = GetOrderedNames(option, "equip", equipLookup);
        var skills = GetOrderedNames(option, "skills", skillsLookup);

        var primary = weapons.Count > 0 ? string.Join(", ", weapons) : string.Empty;
        var extras = equip.Concat(skills).ToList();

        if (string.IsNullOrWhiteSpace(primary) && extras.Count == 0)
        {
            return "-";
        }

        if (string.IsNullOrWhiteSpace(primary))
        {
            return string.Join(", ", extras);
        }

        if (extras.Count == 0)
        {
            return primary;
        }

        return $"{primary} + {string.Join(", ", extras)}";
    }

    private static bool IsMeleeWeaponName(string weaponName) =>
        weaponName.Contains("cc", StringComparison.OrdinalIgnoreCase);

    private static string JoinOrDash(IEnumerable<string> values)
    {
        var list = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return list.Count == 0 ? "-" : string.Join(Environment.NewLine, list);
    }

    private static FormattedString BuildNameFormatted(string name)
    {
        var formatted = new FormattedString();
        if (string.IsNullOrWhiteSpace(name))
        {
            formatted.Spans.Add(new Span { Text = string.Empty });
            return formatted;
        }

        const string token = "Lieutenant";
        var start = 0;
        while (true)
        {
            var index = name.IndexOf(token, start, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                if (start < name.Length)
                {
                    formatted.Spans.Add(new Span { Text = name[start..] });
                }

                break;
            }

            if (index > start)
            {
                formatted.Spans.Add(new Span { Text = name[start..index] });
            }

            formatted.Spans.Add(new Span
            {
                Text = name.Substring(index, token.Length),
                TextColor = Color.FromArgb("#C084FC")
            });
            start = index + token.Length;
        }

        return formatted;
    }

    private static bool HasLieutenantOrder(JsonElement option)
    {
        if (!option.TryGetProperty("orders", out var ordersElement) || ordersElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var order in ordersElement.EnumerateArray())
        {
            if (!order.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var type = typeElement.GetString();
            if (string.Equals(type, "LIEUTENANT", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool IsLieutenantOption(JsonElement option, IReadOnlyDictionary<int, string> skillsLookup)
    {
        if (HasLieutenantOrder(option))
        {
            return true;
        }

        if (option.TryGetProperty("name", out var nameElement) &&
            nameElement.ValueKind == JsonValueKind.String &&
            nameElement.GetString()?.Contains("lieutenant", StringComparison.OrdinalIgnoreCase) == true)
        {
            return true;
        }

        foreach (var skill in GetOrderedIdNames(option, "skills", skillsLookup))
        {
            if (skill.Name.Contains("lieutenant", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool UnitHasVisibleOption(
        string? profileGroupsJson,
        IReadOnlyDictionary<int, string> skillsLookup,
        bool requireLieutenant,
        bool requireMercsZeroSwc)
    {
        if (string.IsNullOrWhiteSpace(profileGroupsJson))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(profileGroupsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return false;
            }

            foreach (var group in doc.RootElement.EnumerateArray())
            {
                if (!group.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var option in optionsElement.EnumerateArray())
                {
                    if (requireLieutenant && !IsLieutenantOption(option, skillsLookup))
                    {
                        continue;
                    }

                    if (requireMercsZeroSwc && IsPositiveSwc(ReadOptionSwc(option)))
                    {
                        continue;
                    }

                    return true;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"UnitHasVisibleOption failed: {ex.Message}");
        }

        return false;
    }

    private static string BuildOptionDisplayName(
        JsonElement option,
        string baseName,
        IReadOnlyDictionary<int, string> equipLookup,
        IReadOnlyDictionary<int, string> skillsLookup)
    {
        var details = new List<string>();
        var normalizedBase = baseName.ToLowerInvariant();

        foreach (var skillName in GetOrderedNames(option, "skills", skillsLookup))
        {
            if (IsNameDetailTag(skillName) && !normalizedBase.Contains(skillName.ToLowerInvariant()))
            {
                details.Add(skillName);
            }
        }

        foreach (var equipName in GetOrderedNames(option, "equip", equipLookup))
        {
            if (IsNameDetailTag(equipName) && !normalizedBase.Contains(equipName.ToLowerInvariant()))
            {
                details.Add(equipName);
            }
        }

        if (option.TryGetProperty("orders", out var ordersElement) && ordersElement.ValueKind == JsonValueKind.Array)
        {
            foreach (var order in ordersElement.EnumerateArray())
            {
                if (!order.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var type = typeElement.GetString();
                if (string.Equals(type, "LIEUTENANT", StringComparison.OrdinalIgnoreCase) &&
                    !normalizedBase.Contains("lieutenant"))
                {
                    details.Add("Lieutenant");
                }
            }
        }

        var distinctDetails = details
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (distinctDetails.Count == 0)
        {
            return baseName;
        }

        return $"{baseName} ({string.Join(", ", distinctDetails)})";
    }

    private static bool IsNameDetailTag(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        return value.Contains("forward observer", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("hacker", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("hacking device", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("specialist", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("paramedic", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("doctor", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("engineer", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("lieutenant", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("nco", StringComparison.OrdinalIgnoreCase) ||
               value.Contains("chain of command", StringComparison.OrdinalIgnoreCase);
    }

    private static string ReadOptionSwc(JsonElement option)
    {
        if (!option.TryGetProperty("swc", out var swcElement))
        {
            return "-";
        }

        if (swcElement.ValueKind == JsonValueKind.String)
        {
            var swc = swcElement.GetString();
            return string.IsNullOrWhiteSpace(swc) ? "-" : swc;
        }

        if (swcElement.ValueKind == JsonValueKind.Number)
        {
            return swcElement.ToString();
        }

        return "-";
    }

    private static string ReadOptionCost(JsonElement option)
    {
        if (!option.TryGetProperty("points", out var pointsElement))
        {
            return "-";
        }

        if (pointsElement.ValueKind == JsonValueKind.Number)
        {
            if (pointsElement.TryGetInt32(out var intCost))
            {
                return intCost.ToString();
            }

            return pointsElement.ToString();
        }

        if (pointsElement.ValueKind == JsonValueKind.String)
        {
            var points = pointsElement.GetString();
            return string.IsNullOrWhiteSpace(points) ? "-" : points;
        }

        return "-";
    }

    private static bool IsPositiveSwc(string swc)
    {
        if (string.IsNullOrWhiteSpace(swc) || swc == "-")
        {
            return false;
        }

        return decimal.TryParse(
                   swc,
                   System.Globalization.NumberStyles.Number,
                   System.Globalization.CultureInfo.InvariantCulture,
                   out var value)
               && value > 0m;
    }

    private static List<string> GetOrderedNames(
        JsonElement container,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup)
    {
        return GetOrderedIdNames(container, propertyName, lookup)
            .Select(x => x.Name)
            .ToList();
    }

    private static List<(int Id, string Name)> GetOrderedIdDisplayNames(
        JsonElement container,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup,
        IReadOnlyDictionary<int, ExtraDefinition> extrasLookup,
        bool showUnitsInInches)
    {
        if (!container.TryGetProperty(propertyName, out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var entries = new List<(int Order, int Id, string Name)>();
        foreach (var entry in arr.EnumerateArray())
        {
            if (!TryParseId(entry, out var id))
            {
                continue;
            }

            var order = int.MaxValue;
            if (entry.ValueKind == JsonValueKind.Object &&
                entry.TryGetProperty("order", out var orderElement) &&
                orderElement.ValueKind == JsonValueKind.Number &&
                orderElement.TryGetInt32(out var parsedOrder))
            {
                order = parsedOrder;
            }

            var baseName = lookup.TryGetValue(id, out var resolvedName) ? resolvedName : id.ToString();
            var displayName = BuildEntryDisplayName(baseName, entry, extrasLookup, showUnitsInInches);
            entries.Add((order, id, displayName));
        }

        return entries
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => (x.Id, x.Name))
            .ToList();
    }

    private static List<(int Id, string Name)> GetOrderedIdDisplayNamesFromEntries(
        IEnumerable<JsonElement> entriesSource,
        IReadOnlyDictionary<int, string> lookup,
        IReadOnlyDictionary<int, ExtraDefinition> extrasLookup,
        bool showUnitsInInches)
    {
        var entries = new List<(int Order, int Id, string Name)>();
        foreach (var entry in entriesSource)
        {
            if (!TryParseId(entry, out var id))
            {
                continue;
            }

            var order = int.MaxValue;
            if (entry.ValueKind == JsonValueKind.Object &&
                entry.TryGetProperty("order", out var orderElement) &&
                orderElement.ValueKind == JsonValueKind.Number &&
                orderElement.TryGetInt32(out var parsedOrder))
            {
                order = parsedOrder;
            }

            var baseName = lookup.TryGetValue(id, out var resolvedName) ? resolvedName : id.ToString();
            var displayName = BuildEntryDisplayName(baseName, entry, extrasLookup, showUnitsInInches);
            entries.Add((order, id, displayName));
        }

        return entries
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => (x.Id, x.Name))
            .ToList();
    }

    private static IEnumerable<JsonElement> GetOptionEntriesWithIncludes(
        JsonElement profileGroupsRoot,
        JsonElement option,
        string propertyName)
    {
        var collected = new List<JsonElement>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        CollectOptionEntriesWithIncludes(profileGroupsRoot, option, propertyName, collected, visited, null);
        return collected;
    }

    private static void CollectOptionEntriesWithIncludes(
        JsonElement profileGroupsRoot,
        JsonElement option,
        string propertyName,
        List<JsonElement> target,
        HashSet<string> visited,
        (int GroupId, int OptionId)? includeRef)
    {
        var key = includeRef.HasValue
            ? $"{includeRef.Value.GroupId}:{includeRef.Value.OptionId}"
            : option.GetRawText().GetHashCode().ToString();
        if (!visited.Add(key))
        {
            return;
        }

        if (option.TryGetProperty(propertyName, out var arr) && arr.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in arr.EnumerateArray())
            {
                target.Add(entry);
            }
        }

        if (!option.TryGetProperty("includes", out var includesElement) || includesElement.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var include in includesElement.EnumerateArray())
        {
            if (include.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            if (!TryParseIncludeReference(include, out var includeGroupId, out var includeOptionId))
            {
                continue;
            }

            var includedOption = FindIncludedOption(profileGroupsRoot, includeGroupId, includeOptionId);
            if (includedOption.HasValue)
            {
                CollectOptionEntriesWithIncludes(
                    profileGroupsRoot,
                    includedOption.Value,
                    propertyName,
                    target,
                    visited,
                    (includeGroupId, includeOptionId));
            }
        }
    }

    private static bool TryParseIncludeReference(JsonElement include, out int groupId, out int optionId)
    {
        groupId = 0;
        optionId = 0;

        if (!include.TryGetProperty("group", out var groupElement) || !TryParseId(groupElement, out groupId))
        {
            return false;
        }

        if (!include.TryGetProperty("option", out var optionElement) || !TryParseId(optionElement, out optionId))
        {
            return false;
        }

        return true;
    }

    private static JsonElement? FindIncludedOption(JsonElement profileGroupsRoot, int groupId, int optionId)
    {
        if (profileGroupsRoot.ValueKind != JsonValueKind.Array)
        {
            return null;
        }

        foreach (var group in profileGroupsRoot.EnumerateArray())
        {
            if (!group.TryGetProperty("id", out var groupIdElement) ||
                !TryParseId(groupIdElement, out var parsedGroupId) ||
                parsedGroupId != groupId)
            {
                continue;
            }

            if (!group.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var option in optionsElement.EnumerateArray())
            {
                if (!option.TryGetProperty("id", out var optionIdElement) ||
                    !TryParseId(optionIdElement, out var parsedOptionId) ||
                    parsedOptionId != optionId)
                {
                    continue;
                }

                return option;
            }
        }

        return null;
    }

    private static string BuildEntryDisplayName(
        string baseName,
        JsonElement entry,
        IReadOnlyDictionary<int, ExtraDefinition> extrasLookup,
        bool showUnitsInInches)
    {
        if (entry.ValueKind != JsonValueKind.Object)
        {
            return baseName;
        }

        if (!entry.TryGetProperty("extra", out var extraElement) || extraElement.ValueKind != JsonValueKind.Array)
        {
            return baseName;
        }

        var extras = new List<string>();
        foreach (var extraEntry in extraElement.EnumerateArray())
        {
            if (!TryParseId(extraEntry, out var extraId))
            {
                continue;
            }

            if (extrasLookup.TryGetValue(extraId, out var extraDefinition) &&
                !string.IsNullOrWhiteSpace(extraDefinition.Name))
            {
                extras.Add(FormatExtraDisplay(extraDefinition, showUnitsInInches));
            }
            else
            {
                extras.Add(extraId.ToString());
            }
        }

        var distinctExtras = extras
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return distinctExtras.Count == 0
            ? baseName
            : $"{baseName} ({string.Join(", ", distinctExtras)})";
    }

    private static string FormatExtraDisplay(ExtraDefinition extraDefinition, bool showUnitsInInches)
    {
        if (!showUnitsInInches ||
            !string.Equals(extraDefinition.Type, "DISTANCE", StringComparison.OrdinalIgnoreCase))
        {
            return extraDefinition.Name;
        }

        return ConvertDistanceTextToInches(extraDefinition.Name);
    }

    private static string ConvertDistanceTextToInches(string distanceText)
    {
        if (string.IsNullOrWhiteSpace(distanceText))
        {
            return distanceText;
        }

        var match = Regex.Match(distanceText, @"([+-]?)(\d+(?:\.\d+)?)");
        if (!match.Success)
        {
            return distanceText;
        }

        var signToken = match.Groups[1].Value;
        var valueToken = match.Groups[2].Value;
        if (!decimal.TryParse(valueToken, NumberStyles.Float, CultureInfo.InvariantCulture, out var valueCm))
        {
            return distanceText;
        }

        if (signToken == "-")
        {
            valueCm = -valueCm;
        }

        var valueInches = (int)Math.Round((double)(valueCm / 2.5m), MidpointRounding.AwayFromZero);
        var replacement = valueInches < 0
            ? valueInches.ToString(CultureInfo.InvariantCulture)
            : signToken == "+"
                ? $"+{valueInches}"
                : valueInches.ToString(CultureInfo.InvariantCulture);

        return string.Concat(
            distanceText.AsSpan(0, match.Index),
            replacement,
            distanceText.AsSpan(match.Index + match.Length));
    }

    private static Dictionary<int, ExtraDefinition> BuildExtrasLookup(string? filtersJson)
    {
        var map = new Dictionary<int, ExtraDefinition>();
        if (string.IsNullOrWhiteSpace(filtersJson))
        {
            return map;
        }

        try
        {
            using var doc = JsonDocument.Parse(filtersJson);
            if (!doc.RootElement.TryGetProperty("extras", out var section) || section.ValueKind != JsonValueKind.Array)
            {
                return map;
            }

            foreach (var entry in section.EnumerateArray())
            {
                if (!entry.TryGetProperty("id", out var idElement) || !TryParseId(idElement, out var id))
                {
                    continue;
                }

                if (!entry.TryGetProperty("name", out var nameElement) || nameElement.ValueKind != JsonValueKind.String)
                {
                    continue;
                }

                var name = nameElement.GetString();
                if (string.IsNullOrWhiteSpace(name))
                {
                    continue;
                }

                var type = entry.TryGetProperty("type", out var typeElement) && typeElement.ValueKind == JsonValueKind.String
                    ? typeElement.GetString() ?? string.Empty
                    : string.Empty;

                map[id] = new ExtraDefinition(name, type, TryReadLink(entry));
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"BuildExtrasLookup failed: {ex.Message}");
        }

        return map;
    }

    private static string? TryReadLink(JsonElement entry)
    {
        foreach (var key in new[] { "url", "href", "link", "wiki", "web" })
        {
            if (!entry.TryGetProperty(key, out var valueElement) || valueElement.ValueKind != JsonValueKind.String)
            {
                continue;
            }

            var value = valueElement.GetString();
            if (string.IsNullOrWhiteSpace(value))
            {
                continue;
            }

            if (Uri.TryCreate(value, UriKind.Absolute, out var uri) &&
                (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            {
                return value;
            }
        }

        return null;
    }

    private static List<(string Text, string? Url)> BuildLinkedLines(
        IEnumerable<(int Id, string Name)> entries,
        IReadOnlyDictionary<int, string> links)
    {
        var result = new List<(string Text, string? Url)>();
        foreach (var entry in entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                continue;
            }

            var url = links.TryGetValue(entry.Id, out var resolvedUrl) ? resolvedUrl : null;
            var existingIndex = result.FindIndex(x => string.Equals(x.Text, entry.Name, StringComparison.OrdinalIgnoreCase));
            if (existingIndex >= 0)
            {
                if (string.IsNullOrWhiteSpace(result[existingIndex].Url) && !string.IsNullOrWhiteSpace(url))
                {
                    result[existingIndex] = (result[existingIndex].Text, url);
                }

                continue;
            }

            result.Add((entry.Name, url));
        }

        return result;
    }

    private static FormattedString BuildLinkedFormattedString(IEnumerable<(string Text, string? Url)> lines, Color textColor)
    {
        var formatted = new FormattedString();
        var list = lines.ToList();
        if (list.Count == 0)
        {
            formatted.Spans.Add(new Span { Text = "-", TextColor = textColor });
            return formatted;
        }

        for (var i = 0; i < list.Count; i++)
        {
            var line = list[i];
            var span = new Span { Text = line.Text, TextColor = textColor };
            if (!string.IsNullOrWhiteSpace(line.Url))
            {
                var link = line.Url;
                var tap = new TapGestureRecognizer();
                tap.Tapped += async (_, _) => await OpenLinkAsync(link);
                span.GestureRecognizers.Add(tap);
            }

            formatted.Spans.Add(span);
            if (i < list.Count - 1)
            {
                formatted.Spans.Add(new Span { Text = Environment.NewLine, TextColor = textColor });
            }
        }

        return formatted;
    }

    private static async Task OpenLinkAsync(string? link)
    {
        if (string.IsNullOrWhiteSpace(link))
        {
            return;
        }

        try
        {
            await Launcher.Default.OpenAsync(link);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to open link '{link}': {ex.Message}");
        }
    }

    private static Dictionary<int, string> BuildIdLinkLookup(string? filtersJson, string sectionName)
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
                if (!entry.TryGetProperty("id", out var idElement) || !TryParseId(idElement, out var id))
                {
                    continue;
                }

                var link = TryReadLink(entry);
                if (!string.IsNullOrWhiteSpace(link))
                {
                    map[id] = link;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"BuildIdLinkLookup failed for section '{sectionName}': {ex.Message}");
        }

        return map;
    }

    private static List<(int Id, string Name)> GetOrderedIdNames(
        JsonElement container,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup)
    {
        if (!container.TryGetProperty(propertyName, out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var entries = new List<(int Order, int Id, string Name)>();
        foreach (var entry in arr.EnumerateArray())
        {
            if (!TryParseId(entry, out var id))
            {
                continue;
            }

            var order = int.MaxValue;
            if (entry.ValueKind == JsonValueKind.Object &&
                entry.TryGetProperty("order", out var orderElement) &&
                orderElement.ValueKind == JsonValueKind.Number &&
                orderElement.TryGetInt32(out var parsedOrder))
            {
                order = parsedOrder;
            }

            var name = lookup.TryGetValue(id, out var resolvedName) ? resolvedName : id.ToString();
            entries.Add((order, id, name));
        }

        return entries
            .OrderBy(x => x.Order)
            .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(x => (x.Id, x.Name))
            .ToList();
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

    private static (bool HasRegular, bool HasIrregular, bool HasImpetuous, bool HasTacticalAwareness) ParseUnitOrderTraits(JsonElement profileGroupsArray)
    {
        var hasRegular = false;
        var hasIrregular = false;
        var hasImpetuous = false;
        var hasTacticalAwareness = false;

        if (profileGroupsArray.ValueKind != JsonValueKind.Array)
        {
            return (false, false, false, false);
        }

        foreach (var group in profileGroupsArray.EnumerateArray())
        {
            if (!group.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var option in optionsElement.EnumerateArray())
            {
                if (!option.TryGetProperty("orders", out var ordersElement) || ordersElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var order in ordersElement.EnumerateArray())
                {
                    if (!order.TryGetProperty("type", out var typeElement) || typeElement.ValueKind != JsonValueKind.String)
                    {
                        continue;
                    }

                    var type = typeElement.GetString();
                    if (string.IsNullOrWhiteSpace(type))
                    {
                        continue;
                    }

                    if (string.Equals(type, "REGULAR", StringComparison.OrdinalIgnoreCase))
                    {
                        hasRegular = true;
                    }
                    else if (string.Equals(type, "IRREGULAR", StringComparison.OrdinalIgnoreCase))
                    {
                        hasIrregular = true;
                    }
                    else if (string.Equals(type, "IMPETUOUS", StringComparison.OrdinalIgnoreCase))
                    {
                        hasImpetuous = true;
                    }
                    else if (string.Equals(type, "TACTICAL", StringComparison.OrdinalIgnoreCase))
                    {
                        hasTacticalAwareness = true;
                    }
                }
            }
        }

        return (hasRegular, hasIrregular, hasImpetuous, hasTacticalAwareness);
    }

    private static (bool HasCube, bool HasCube2, bool HasHackable) ParseUnitTechTraits(
        JsonElement profileGroupsArray,
        IReadOnlyDictionary<int, string> equipLookup,
        IReadOnlyDictionary<int, string> skillsLookup,
        IReadOnlyDictionary<int, string> charsLookup)
    {
        var hasCube = false;
        var hasCube2 = false;
        var hasHackable = false;

        if (profileGroupsArray.ValueKind != JsonValueKind.Array)
        {
            return (false, false, false);
        }

        foreach (var group in profileGroupsArray.EnumerateArray())
        {
            if (group.TryGetProperty("profiles", out var profilesElement) && profilesElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var profile in profilesElement.EnumerateArray())
                {
                    foreach (var equip in GetOrderedIdNames(profile, "equip", equipLookup))
                    {
                        ApplyTechTraitName(equip.Name, ref hasCube, ref hasCube2, ref hasHackable);
                    }

                    foreach (var skill in GetOrderedIdNames(profile, "skills", skillsLookup))
                    {
                        ApplyTechTraitName(skill.Name, ref hasCube, ref hasCube2, ref hasHackable);
                    }

                    foreach (var character in GetOrderedIdNames(profile, "chars", charsLookup))
                    {
                        ApplyTechTraitName(character.Name, ref hasCube, ref hasCube2, ref hasHackable);
                    }
                }
            }

            if (group.TryGetProperty("options", out var optionsElement) && optionsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var option in optionsElement.EnumerateArray())
                {
                    foreach (var equip in GetOrderedIdNames(option, "equip", equipLookup))
                    {
                        ApplyTechTraitName(equip.Name, ref hasCube, ref hasCube2, ref hasHackable);
                    }

                    foreach (var skill in GetOrderedIdNames(option, "skills", skillsLookup))
                    {
                        ApplyTechTraitName(skill.Name, ref hasCube, ref hasCube2, ref hasHackable);
                    }

                    foreach (var character in GetOrderedIdNames(option, "chars", charsLookup))
                    {
                        ApplyTechTraitName(character.Name, ref hasCube, ref hasCube2, ref hasHackable);
                    }
                }
            }
        }

        return (hasCube, hasCube2, hasHackable);
    }

    private static void ApplyTechTraitName(string name, ref bool hasCube, ref bool hasCube2, ref bool hasHackable)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return;
        }

        var normalized = NormalizeTokenText(name);

        if (Regex.IsMatch(normalized, @"\bhackable\b", RegexOptions.IgnoreCase) &&
            !Regex.IsMatch(normalized, @"\b(non[\s-]*hackable|not[\s-]*hackable)\b", RegexOptions.IgnoreCase))
        {
            hasHackable = true;
        }

        var hasNegativeCube = Regex.IsMatch(
            normalized,
            @"\b(no[\s-]*cube|without[\s-]*cube|cube[\s-]*none)\b",
            RegexOptions.IgnoreCase);

        if (hasNegativeCube)
        {
            return;
        }

        var isCube2 = Regex.IsMatch(
            normalized,
            @"\bcube\s*2(?:\.0)?\b|\bcube2(?:\.0)?\b",
            RegexOptions.IgnoreCase);

        if (isCube2)
        {
            hasCube2 = true;
            return;
        }

        if (Regex.IsMatch(normalized, @"\bcube\b", RegexOptions.IgnoreCase))
        {
            hasCube = true;
        }
    }

    private static string NormalizeTokenText(string value)
    {
        var lowered = value.ToLowerInvariant();
        var sb = new StringBuilder(lowered.Length);
        foreach (var c in lowered)
        {
            if (char.IsLetterOrDigit(c) || c == '.')
            {
                sb.Append(c);
            }
            else
            {
                sb.Append(' ');
            }
        }

        return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
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
        EquipmentSummaryFormatted = BuildNamedSummaryFormatted(
            "Equipment",
            [],
            equipLookup: null,
            links: null,
            Color.FromArgb("#06B6D4"));
        SpecialSkillsSummaryFormatted = BuildNamedSummaryFormatted(
            "Special Skills",
            [],
            equipLookup: null,
            links: null,
            Color.FromArgb("#F59E0B"));

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
            var equipLinks = BuildIdLinkLookup(snapshot?.FiltersJson, "equip");
            var skillsLookup = BuildIdNameLookup(snapshot?.FiltersJson, "skills");
            var skillsLinks = BuildIdLinkLookup(snapshot?.FiltersJson, "skills");
            var charsLookup = BuildIdNameLookup(snapshot?.FiltersJson, "chars");
            var charsLinks = BuildIdLinkLookup(snapshot?.FiltersJson, "chars");
            var weaponsLookup = BuildIdNameLookup(snapshot?.FiltersJson, "weapons");
            var weaponsLinks = BuildIdLinkLookup(snapshot?.FiltersJson, "weapons");
            var extrasLookup = BuildExtrasLookup(snapshot?.FiltersJson);
            var peripheralLookup = BuildIdNameLookup(snapshot?.FiltersJson, "peripheral");
            var peripheralLinks = BuildIdLinkLookup(snapshot?.FiltersJson, "peripheral");

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
            var orderTraits = ParseUnitOrderTraits(doc.RootElement);
            ShowIrregularOrderIcon = orderTraits.HasIrregular;
            ShowRegularOrderIcon = !orderTraits.HasIrregular && orderTraits.HasRegular;
            ShowImpetuousIcon = orderTraits.HasImpetuous;
            ShowTacticalAwarenessIcon = orderTraits.HasTacticalAwareness;
            ImpetuousIconUrl = orderTraits.HasImpetuous
                ? TryResolveFirstLinkByPredicate(
                    skillsLookup,
                    skillsLinks,
                    name => name.Contains("impetuous", StringComparison.OrdinalIgnoreCase))
                : null;
            TacticalAwarenessIconUrl = orderTraits.HasTacticalAwareness
                ? TryResolveFirstLinkByPredicate(
                    skillsLookup,
                    skillsLinks,
                    name => name.Contains("tactical", StringComparison.OrdinalIgnoreCase))
                : null;
            var techTraits = ParseUnitTechTraits(doc.RootElement, equipLookup, skillsLookup, charsLookup);
            ShowCubeIcon = techTraits.HasCube;
            ShowCube2Icon = techTraits.HasCube2;
            ShowHackableIcon = techTraits.HasHackable;
            CubeIconUrl = techTraits.HasCube
                ? TryResolveFirstLinkByPredicate(
                    charsLookup,
                    charsLinks,
                    name =>
                    {
                        var normalized = NormalizeTokenText(name);
                        return Regex.IsMatch(normalized, @"\bcube\b", RegexOptions.IgnoreCase) &&
                               !Regex.IsMatch(normalized, @"\bcube\s*2(?:\.0)?\b|\bcube2(?:\.0)?\b", RegexOptions.IgnoreCase);
                    })
                  ?? TryResolveFirstLinkByPredicate(
                    equipLookup,
                    equipLinks,
                    name =>
                    {
                        var normalized = NormalizeTokenText(name);
                        return Regex.IsMatch(normalized, @"\bcube\b", RegexOptions.IgnoreCase) &&
                               !Regex.IsMatch(normalized, @"\bcube\s*2(?:\.0)?\b|\bcube2(?:\.0)?\b", RegexOptions.IgnoreCase);
                    })
                  ?? TryResolveFirstLinkByPredicate(
                    skillsLookup,
                    skillsLinks,
                    name =>
                    {
                        var normalized = NormalizeTokenText(name);
                        return Regex.IsMatch(normalized, @"\bcube\b", RegexOptions.IgnoreCase) &&
                               !Regex.IsMatch(normalized, @"\bcube\s*2(?:\.0)?\b|\bcube2(?:\.0)?\b", RegexOptions.IgnoreCase);
                    })
                : null;
            Cube2IconUrl = techTraits.HasCube2
                ? TryResolveFirstLinkByPredicate(
                    charsLookup,
                    charsLinks,
                    name => Regex.IsMatch(NormalizeTokenText(name), @"\bcube\s*2(?:\.0)?\b|\bcube2(?:\.0)?\b", RegexOptions.IgnoreCase))
                  ?? TryResolveFirstLinkByPredicate(
                    equipLookup,
                    equipLinks,
                    name => Regex.IsMatch(NormalizeTokenText(name), @"\bcube\s*2(?:\.0)?\b|\bcube2(?:\.0)?\b", RegexOptions.IgnoreCase))
                  ?? TryResolveFirstLinkByPredicate(
                    skillsLookup,
                    skillsLinks,
                    name => Regex.IsMatch(NormalizeTokenText(name), @"\bcube\s*2(?:\.0)?\b|\bcube2(?:\.0)?\b", RegexOptions.IgnoreCase))
                : null;
            HackableIconUrl = techTraits.HasHackable
                ? TryResolveFirstLinkByPredicate(
                    charsLookup,
                    charsLinks,
                    name => name.Contains("hackable", StringComparison.OrdinalIgnoreCase))
                  ?? TryResolveFirstLinkByPredicate(
                    equipLookup,
                    equipLinks,
                    name => name.Contains("hackable", StringComparison.OrdinalIgnoreCase))
                  ?? TryResolveFirstLinkByPredicate(
                    skillsLookup,
                    skillsLinks,
                    name => name.Contains("hackable", StringComparison.OrdinalIgnoreCase))
                : null;

            HashSet<string>? commonEquipNames = null;
            HashSet<string>? commonSkillNames = null;
            var profileCount = 0;
            var equipUsageCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            var skillUsageCounts = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

            foreach (var usageGroup in doc.RootElement.EnumerateArray())
            {
                if (!usageGroup.TryGetProperty("options", out var usageOptionsElement) || usageOptionsElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var usageOption in usageOptionsElement.EnumerateArray())
                {
                    if (LieutenantOnlyUnits && !IsLieutenantOption(usageOption, skillsLookup))
                    {
                        continue;
                    }

                    var usageSwc = ReadOptionSwc(usageOption);
                    if (MercsOnlyUnits && IsPositiveSwc(usageSwc))
                    {
                        continue;
                    }

                    foreach (var equipName in GetOrderedIdDisplayNamesFromEntries(
                                 GetOptionEntriesWithIncludes(doc.RootElement, usageOption, "equip"),
                                 equipLookup,
                                 extrasLookup,
                                 ShowUnitsInInches).Select(x => x.Name))
                    {
                        equipUsageCounts[equipName] = equipUsageCounts.TryGetValue(equipName, out var count) ? count + 1 : 1;
                    }

                    foreach (var skillName in GetOrderedIdDisplayNamesFromEntries(
                                 GetOptionEntriesWithIncludes(doc.RootElement, usageOption, "skills"),
                                 skillsLookup,
                                 extrasLookup,
                                 ShowUnitsInInches).Select(x => x.Name))
                    {
                        skillUsageCounts[skillName] = skillUsageCounts.TryGetValue(skillName, out var count) ? count + 1 : 1;
                    }
                }
            }

            var seenConfigurations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

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
                    var profileEquip = GetOrderedIdDisplayNames(profile, "equip", equipLookup, extrasLookup, ShowUnitsInInches)
                        .Select(x => x.Name)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);
                    var profileSkills = GetOrderedIdDisplayNames(profile, "skills", skillsLookup, extrasLookup, ShowUnitsInInches)
                        .Select(x => x.Name)
                        .ToHashSet(StringComparer.OrdinalIgnoreCase);

                    if (commonEquipNames is null)
                    {
                        commonEquipNames = new HashSet<string>(profileEquip, StringComparer.OrdinalIgnoreCase);
                    }
                    else
                    {
                        commonEquipNames.IntersectWith(profileEquip);
                    }

                    if (commonSkillNames is null)
                    {
                        commonSkillNames = new HashSet<string>(profileSkills, StringComparer.OrdinalIgnoreCase);
                    }
                    else
                    {
                        commonSkillNames.IntersectWith(profileSkills);
                    }

                    profileCount++;
                }

                if (!group.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var option in optionsElement.EnumerateArray())
                {
                    if (LieutenantOnlyUnits && !IsLieutenantOption(option, skillsLookup))
                    {
                        continue;
                    }

                    var optionName = option.TryGetProperty("name", out var nameElement) && nameElement.ValueKind == JsonValueKind.String
                        ? nameElement.GetString() ?? string.Empty
                        : string.Empty;

                    if (string.IsNullOrWhiteSpace(optionName))
                    {
                        optionName = groupName;
                    }

                    var displayName = BuildOptionDisplayName(option, optionName, equipLookup, skillsLookup);
                    var optionWeapons = GetOrderedIdDisplayNamesFromEntries(
                            GetOptionEntriesWithIncludes(doc.RootElement, option, "weapons"),
                            weaponsLookup,
                            extrasLookup,
                            ShowUnitsInInches);
                    var rangedWeaponEntries = optionWeapons.Where(x => !IsMeleeWeaponName(x.Name)).ToList();
                    var meleeWeaponEntries = optionWeapons.Where(x => IsMeleeWeaponName(x.Name)).ToList();
                    var rangedWeapons = JoinOrDash(rangedWeaponEntries.Select(x => x.Name));
                    var meleeWeapons = JoinOrDash(meleeWeaponEntries.Select(x => x.Name));

                    var uniqueEquipmentEntries = GetOrderedIdDisplayNamesFromEntries(
                                GetOptionEntriesWithIncludes(doc.RootElement, option, "equip"),
                                equipLookup,
                                extrasLookup,
                                ShowUnitsInInches)
                            .Where(x => equipUsageCounts.TryGetValue(x.Name, out var c) && c == 1)
                            .ToList();
                    var uniqueEquipment = JoinOrDash(uniqueEquipmentEntries.Select(x => x.Name));

                    var uniqueSkillsEntries = GetOrderedIdDisplayNamesFromEntries(
                                GetOptionEntriesWithIncludes(doc.RootElement, option, "skills"),
                                skillsLookup,
                                extrasLookup,
                                ShowUnitsInInches)
                            .Where(x => skillUsageCounts.TryGetValue(x.Name, out var c) && c == 1)
                            .Where(x => !x.Name.Contains("lieutenant", StringComparison.OrdinalIgnoreCase))
                            .ToList();
                    var uniqueSkills = JoinOrDash(uniqueSkillsEntries.Select(x => x.Name));

                    var peripheralEntries = GetOrderedIdDisplayNamesFromEntries(
                                GetOptionEntriesWithIncludes(doc.RootElement, option, "peripheral"),
                                peripheralLookup,
                                extrasLookup,
                                ShowUnitsInInches)
                            .ToList();
                    var peripherals = JoinOrDash(peripheralEntries.Select(x => x.Name));
                    var swc = ReadOptionSwc(option);
                    var cost = ReadOptionCost(option);

                    if (MercsOnlyUnits && IsPositiveSwc(swc))
                    {
                        continue;
                    }

                    var dedupeKey = $"{groupName}|{displayName}|{rangedWeapons}|{meleeWeapons}|{uniqueEquipment}|{uniqueSkills}|{peripherals}|{swc}|{cost}";
                    if (!seenConfigurations.Add(dedupeKey))
                    {
                        continue;
                    }

                    var rangedLines = BuildLinkedLines(rangedWeaponEntries, weaponsLinks);
                    var meleeLines = BuildLinkedLines(meleeWeaponEntries, weaponsLinks);
                    var uniqueEquipmentLines = BuildLinkedLines(uniqueEquipmentEntries, equipLinks);
                    var uniqueSkillsLines = BuildLinkedLines(uniqueSkillsEntries, skillsLinks);
                    var peripheralLines = BuildLinkedLines(peripheralEntries, peripheralLinks);

                    Profiles.Add(new ViewerProfileItem
                    {
                        GroupName = groupName,
                        Name = displayName,
                        NameFormatted = BuildNameFormatted(displayName),
                        RangedWeapons = rangedWeapons,
                        RangedWeaponsFormatted = BuildLinkedFormattedString(rangedLines, Color.FromArgb("#EF4444")),
                        MeleeWeapons = meleeWeapons,
                        MeleeWeaponsFormatted = BuildLinkedFormattedString(meleeLines, Color.FromArgb("#22C55E")),
                        UniqueEquipment = uniqueEquipment,
                        UniqueEquipmentFormatted = BuildLinkedFormattedString(uniqueEquipmentLines, Color.FromArgb("#06B6D4")),
                        UniqueSkills = uniqueSkills,
                        UniqueSkillsFormatted = BuildLinkedFormattedString(uniqueSkillsLines, Color.FromArgb("#F59E0B")),
                        Peripherals = peripherals,
                        PeripheralsFormatted = BuildLinkedFormattedString(peripheralLines, Color.FromArgb("#FACC15")),
                        Swc = swc,
                        SwcDisplay = MercsOnlyUnits ? string.Empty : $"SWC {swc}",
                        Cost = cost
                    });
                }
            }

            var stableEquip = profileCount > 0 ? (IEnumerable<string>)(commonEquipNames ?? []) : [];
            var stableSkills = profileCount > 0 ? (IEnumerable<string>)(commonSkillNames ?? []) : [];
            EquipmentSummary = BuildNamedSummary("Equipment", stableEquip);
            SpecialSkillsSummary = BuildNamedSummary("Special Skills", stableSkills);
            EquipmentSummaryFormatted = BuildNamedSummaryFormatted(
                "Equipment",
                stableEquip,
                equipLookup,
                equipLinks,
                Color.FromArgb("#06B6D4"));
            SpecialSkillsSummaryFormatted = BuildNamedSummaryFormatted(
                "Special Skills",
                stableSkills,
                skillsLookup,
                skillsLinks,
                Color.FromArgb("#F59E0B"));
            ProfilesStatus = Profiles.Count == 0 ? "No configurations found for this unit." : $"{Profiles.Count} configurations loaded.";
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

    public int ParentId { get; init; }

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

    public FormattedString? NameFormatted { get; init; }

    public string RangedWeapons { get; init; } = "-";
    public FormattedString? RangedWeaponsFormatted { get; init; }

    public string MeleeWeapons { get; init; } = "-";
    public FormattedString? MeleeWeaponsFormatted { get; init; }

    public string UniqueEquipment { get; init; } = "-";
    public FormattedString? UniqueEquipmentFormatted { get; init; }

    public string UniqueSkills { get; init; } = "-";
    public FormattedString? UniqueSkillsFormatted { get; init; }

    public string Peripherals { get; init; } = "-";
    public FormattedString? PeripheralsFormatted { get; init; }

    public bool HasPeripherals => !string.IsNullOrWhiteSpace(Peripherals) && Peripherals != "-";

    public string Swc { get; init; } = "-";

    public string SwcDisplay { get; init; } = string.Empty;

    public string Cost { get; init; } = "-";
}
