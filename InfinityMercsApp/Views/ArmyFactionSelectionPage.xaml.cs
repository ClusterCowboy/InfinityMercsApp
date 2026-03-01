using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using InfinityMercsApp.Data.Database;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using Svg.Skia;

namespace InfinityMercsApp.Views;

public partial class ArmyFactionSelectionPage : ContentPage
{
    private static readonly Color ActiveBorder = Color.FromArgb("#2563EB");
    private static readonly Color InactiveBorder = Color.FromArgb("#9CA3AF");
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

    private readonly ArmySourceSelectionMode _mode;
    private readonly IMetadataAccessor? _metadataAccessor;
    private readonly IArmyDataAccessor? _armyDataAccessor;
    private readonly FactionLogoCacheService? _factionLogoCacheService;
    private ArmyFactionSelectionItem? _selectedFaction;
    private ArmyFactionSelectionItem? _leftSlotFaction;
    private ArmyFactionSelectionItem? _rightSlotFaction;
    private SKPicture? _leftSlotPicture;
    private SKPicture? _rightSlotPicture;
    private SKPicture? _selectedUnitPicture;
    private int _activeSlotIndex;
    private bool _loaded;
    private bool _lieutenantOnlyUnits;
    private ArmyUnitSelectionItem? _selectedUnit;
    private string _leftSlotText = string.Empty;
    private string _rightSlotText = string.Empty;
    private Color _leftSlotBorderColor = ActiveBorder;
    private Color _rightSlotBorderColor = InactiveBorder;
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

    public ArmyFactionSelectionPage(ArmySourceSelectionMode mode)
    {
        InitializeComponent();
        _mode = mode;
        Title = _mode == ArmySourceSelectionMode.VanillaFactions
            ? "Choose your faction:"
            : "Choose your sectorials";

        var services = Application.Current?.Handler?.MauiContext?.Services;
        _metadataAccessor = services?.GetService<IMetadataAccessor>();
        _armyDataAccessor = services?.GetService<IArmyDataAccessor>();
        _factionLogoCacheService = services?.GetService<FactionLogoCacheService>();

        SelectFactionCommand = new Command<ArmyFactionSelectionItem>(item =>
        {
            if (item is null)
            {
                return;
            }

            SetSelectedFaction(item);
        });
        SelectUnitCommand = new Command<ArmyUnitSelectionItem>(item =>
        {
            if (item is null)
            {
                return;
            }

            SetSelectedUnit(item);
        });

        BindingContext = this;
        SetActiveSlot(0);
    }

    public ObservableCollection<ArmyFactionSelectionItem> Factions { get; } = [];
    public ObservableCollection<ArmyUnitSelectionItem> Units { get; } = [];

    public ICommand SelectFactionCommand { get; }
    public ICommand SelectUnitCommand { get; }

    public bool ShowRightSelectionBox => _mode == ArmySourceSelectionMode.Sectorials;

    public string UnitNameHeading { get => _unitNameHeading; private set { if (_unitNameHeading != value) { _unitNameHeading = value; OnPropertyChanged(); } } }
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
    public string EquipmentSummary { get => _equipmentSummary; private set { if (_equipmentSummary != value) { _equipmentSummary = value; OnPropertyChanged(); } } }
    public string SpecialSkillsSummary { get => _specialSkillsSummary; private set { if (_specialSkillsSummary != value) { _specialSkillsSummary = value; OnPropertyChanged(); } } }

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
            _ = LoadUnitsForActiveSlotAsync();
        }
    }

    public string LeftSlotText
    {
        get => _leftSlotText;
        set
        {
            if (_leftSlotText == value)
            {
                return;
            }

            _leftSlotText = value;
            OnPropertyChanged();
        }
    }

    public string RightSlotText
    {
        get => _rightSlotText;
        set
        {
            if (_rightSlotText == value)
            {
                return;
            }

            _rightSlotText = value;
            OnPropertyChanged();
        }
    }

    public Color LeftSlotBorderColor
    {
        get => _leftSlotBorderColor;
        set
        {
            if (_leftSlotBorderColor == value)
            {
                return;
            }

            _leftSlotBorderColor = value;
            OnPropertyChanged();
        }
    }

    public Color RightSlotBorderColor
    {
        get => _rightSlotBorderColor;
        set
        {
            if (_rightSlotBorderColor == value)
            {
                return;
            }

            _rightSlotBorderColor = value;
            OnPropertyChanged();
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();

        if (_loaded)
        {
            return;
        }

        _loaded = true;
        await LoadFactionsAsync();
    }

    private async Task LoadFactionsAsync(CancellationToken cancellationToken = default)
    {
        if (_metadataAccessor is null)
        {
            Console.Error.WriteLine("ArmyFactionSelectionPage metadata service unavailable.");
            return;
        }

        try
        {
            var factions = await _metadataAccessor.GetFactionsAsync(includeDiscontinued: true, cancellationToken);

            if (_factionLogoCacheService is not null)
            {
                await _factionLogoCacheService.CacheFactionLogosFromRecordsAsync(factions, cancellationToken);
            }

            IEnumerable<FactionRecord> filtered = factions;
            if (_mode == ArmySourceSelectionMode.VanillaFactions)
            {
                filtered = filtered.Where(x => x.Id == x.ParentId);
            }
            else
            {
                filtered = filtered.Where(x => x.Id != x.ParentId);
            }

            var items = filtered
                .OrderBy(x => x.Name)
                .Select(faction => new ArmyFactionSelectionItem
                {
                    Id = faction.Id,
                    ParentId = faction.ParentId,
                    Name = faction.Name,
                    CachedLogoPath = _factionLogoCacheService?.TryGetCachedLogoPath(faction.Id),
                    PackagedLogoPath = $"SVGCache/factions/{faction.Id}.svg"
                })
                .ToList();

            Factions.Clear();
            foreach (var faction in items)
            {
                Factions.Add(faction);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage LoadFactionsAsync failed: {ex.Message}");
        }
    }

    private void SetSelectedFaction(ArmyFactionSelectionItem item)
    {
        if (_selectedFaction == item)
        {
            AssignSelectedFactionToActiveSlot(item);
            return;
        }

        if (_selectedFaction is not null)
        {
            _selectedFaction.IsSelected = false;
        }

        _selectedFaction = item;
        _selectedFaction.IsSelected = true;
        AssignSelectedFactionToActiveSlot(item);
    }

    private void AssignSelectedFactionToActiveSlot(ArmyFactionSelectionItem item)
    {
        if (ShowRightSelectionBox && IsDuplicateSelectionForActiveSlot(item))
        {
            Console.WriteLine($"[ArmyFactionSelectionPage] Duplicate selection blocked for faction {item.Id} ({item.Name}).");
            return;
        }

        if (_activeSlotIndex == 0 || !ShowRightSelectionBox)
        {
            _leftSlotFaction = item;
            LeftSlotText = item.Name;
            _ = LoadSlotIconAsync(0, item.CachedLogoPath, item.PackagedLogoPath);
        }
        else
        {
            _rightSlotFaction = item;
            RightSlotText = item.Name;
            _ = LoadSlotIconAsync(1, item.CachedLogoPath, item.PackagedLogoPath);
        }

        AutoSelectEmptySlot();
        _ = LoadUnitsForActiveSlotAsync();
    }

    private bool IsDuplicateSelectionForActiveSlot(ArmyFactionSelectionItem item)
    {
        if (!ShowRightSelectionBox)
        {
            return false;
        }

        if (_activeSlotIndex == 0)
        {
            return _rightSlotFaction is not null
                && _rightSlotFaction.Id == item.Id
                && (_leftSlotFaction is null || _leftSlotFaction.Id != item.Id);
        }

        return _leftSlotFaction is not null
            && _leftSlotFaction.Id == item.Id
            && (_rightSlotFaction is null || _rightSlotFaction.Id != item.Id);
    }

    private void AutoSelectEmptySlot()
    {
        if (!ShowRightSelectionBox)
        {
            SetActiveSlot(0);
            return;
        }

        var leftEmpty = _leftSlotFaction is null;
        var rightEmpty = _rightSlotFaction is null;

        if (leftEmpty && !rightEmpty)
        {
            SetActiveSlot(0);
            return;
        }

        if (rightEmpty && !leftEmpty)
        {
            SetActiveSlot(1);
        }
    }

    private void SetActiveSlot(int index)
    {
        _activeSlotIndex = index == 1 && ShowRightSelectionBox ? 1 : 0;
        LeftSlotBorderColor = _activeSlotIndex == 0 ? ActiveBorder : InactiveBorder;
        RightSlotBorderColor = _activeSlotIndex == 1 ? ActiveBorder : InactiveBorder;
    }

    private void OnLeftSlotTapped(object? sender, TappedEventArgs e)
    {
        SetActiveSlot(0);
        _ = LoadUnitsForActiveSlotAsync();
    }

    private void OnRightSlotTapped(object? sender, TappedEventArgs e)
    {
        if (!ShowRightSelectionBox)
        {
            return;
        }

        SetActiveSlot(1);
        _ = LoadUnitsForActiveSlotAsync();
    }

    private async Task LoadUnitsForActiveSlotAsync(CancellationToken cancellationToken = default)
    {
        Units.Clear();
        _selectedUnit = null;
        ResetUnitDetails();
        if (_armyDataAccessor is null)
        {
            return;
        }

        var factions = GetUnitSourceFactions();
        if (factions.Count == 0)
        {
            return;
        }

        try
        {
            var mergedUnits = new Dictionary<string, ArmyUnitSelectionItem>(StringComparer.OrdinalIgnoreCase);

            foreach (var faction in factions)
            {
                var units = await _armyDataAccessor.GetResumeByFactionMercsOnlyAsync(faction.Id, cancellationToken);
                var snapshot = await _armyDataAccessor.GetFactionSnapshotAsync(faction.Id, cancellationToken);
                var typeLookup = BuildIdNameLookup(snapshot?.FiltersJson, "type");
                var categoryLookup = BuildIdNameLookup(snapshot?.FiltersJson, "category");

                if (LieutenantOnlyUnits)
                {
                    var skillsLookup = BuildIdNameLookup(snapshot?.FiltersJson, "skills");
                    var filteredIds = new HashSet<int>();
                    foreach (var unit in units)
                    {
                        var unitRecord = await _armyDataAccessor.GetUnitAsync(faction.Id, unit.UnitId, cancellationToken);
                        if (UnitHasLieutenantOption(unitRecord?.ProfileGroupsJson, skillsLookup))
                        {
                            filteredIds.Add(unit.UnitId);
                        }
                    }

                    units = units.Where(x => filteredIds.Contains(x.UnitId)).ToList();
                }

                if (_factionLogoCacheService is not null)
                {
                    await _factionLogoCacheService.CacheUnitLogosFromRecordsAsync(faction.Id, units, cancellationToken);
                }

                foreach (var unit in units)
                {
                    var key = unit.Name.Trim();
                    if (string.IsNullOrWhiteSpace(key) || mergedUnits.ContainsKey(key))
                    {
                        continue;
                    }

                    mergedUnits[key] = new ArmyUnitSelectionItem
                    {
                        Id = unit.UnitId,
                        SourceFactionId = faction.Id,
                        Name = unit.Name,
                        Type = unit.Type,
                        Subtitle = BuildUnitSubtitle(unit, typeLookup, categoryLookup),
                        CachedLogoPath = _factionLogoCacheService?.TryGetCachedUnitLogoPath(faction.Id, unit.UnitId),
                        PackagedLogoPath = $"SVGCache/units/{faction.Id}-{unit.UnitId}.svg"
                    };
                }
            }

            foreach (var unit in mergedUnits.Values
                .OrderBy(x => GetUnitTypeSortIndex(x.Type))
                .ThenBy(x => x.Name, StringComparer.OrdinalIgnoreCase))
            {
                Units.Add(unit);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage LoadUnitsForActiveSlotAsync failed: {ex.Message}");
        }
    }

    private void SetSelectedUnit(ArmyUnitSelectionItem item)
    {
        if (_selectedUnit == item)
        {
            return;
        }

        if (_selectedUnit is not null)
        {
            _selectedUnit.IsSelected = false;
        }

        _selectedUnit = item;
        _selectedUnit.IsSelected = true;
        _ = LoadSelectedUnitLogoAsync(item);
        _ = LoadSelectedUnitDetailsAsync();
    }

    private async Task LoadSelectedUnitDetailsAsync(CancellationToken cancellationToken = default)
    {
        ResetUnitDetails();
        if (_selectedUnit is null || _armyDataAccessor is null)
        {
            return;
        }

        try
        {
            UnitNameHeading = _selectedUnit.Name;
            var unit = await _armyDataAccessor.GetUnitAsync(_selectedUnit.SourceFactionId, _selectedUnit.Id, cancellationToken);
            var snapshot = await _armyDataAccessor.GetFactionSnapshotAsync(_selectedUnit.SourceFactionId, cancellationToken);
            if (unit is null)
            {
                Console.Error.WriteLine($"ArmyFactionSelectionPage: unit record not found for faction={_selectedUnit.SourceFactionId}, unit={_selectedUnit.Id}.");
                return;
            }

            var equipLookup = BuildIdNameLookup(snapshot?.FiltersJson, "equip");
            var skillsLookup = BuildIdNameLookup(snapshot?.FiltersJson, "skills");
            var options = EnumerateOptions(unit.ProfileGroupsJson).ToList();
            if (options.Count == 0)
            {
                Console.Error.WriteLine($"ArmyFactionSelectionPage: no options found in profileGroups for faction={_selectedUnit.SourceFactionId}, unit={_selectedUnit.Id}.");
                return;
            }

            var first = options[0];
            UnitMov = ReadMove(first);
            UnitCc = ReadIntAsString(first, "cc");
            UnitBs = ReadIntAsString(first, "bs");
            UnitPh = ReadIntAsString(first, "ph");
            UnitWip = ReadIntAsString(first, "wip");
            UnitArm = ReadIntAsString(first, "arm");
            UnitBts = ReadIntAsString(first, "bts");
            var (vitalityHeader, vitality) = ReadVitality(first);
            UnitVitalityHeader = vitalityHeader;
            UnitVitality = vitality;
            UnitS = ReadIntAsString(first, "s");
            UnitAva = ReadAvaAsString(first);

            var stableEquip = IntersectNamedIds(options, "equip", equipLookup);
            var stableSkills = IntersectNamedIds(options, "skills", skillsLookup)
                .Where(x => !x.Contains("lieutenant", StringComparison.OrdinalIgnoreCase))
                .ToList();

            EquipmentSummary = $"Equipment: {(stableEquip.Count == 0 ? "-" : string.Join(", ", stableEquip))}";
            SpecialSkillsSummary = $"Special Skills: {(stableSkills.Count == 0 ? "-" : string.Join(", ", stableSkills))}";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage LoadSelectedUnitDetailsAsync failed: {ex.Message}");
        }
    }

    private async Task LoadSelectedUnitLogoAsync(ArmyUnitSelectionItem item)
    {
        _selectedUnitPicture?.Dispose();
        _selectedUnitPicture = null;

        try
        {
            Stream? stream = null;
            if (!string.IsNullOrWhiteSpace(item.CachedLogoPath) && File.Exists(item.CachedLogoPath))
            {
                stream = File.OpenRead(item.CachedLogoPath);
            }
            else if (!string.IsNullOrWhiteSpace(item.PackagedLogoPath))
            {
                stream = await FileSystem.Current.OpenAppPackageFileAsync(item.PackagedLogoPath);
            }

            if (stream is null)
            {
                SelectedUnitCanvas.InvalidateSurface();
                return;
            }

            await using (stream)
            {
                var svg = new SKSvg();
                _selectedUnitPicture = svg.Load(stream);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage LoadSelectedUnitLogoAsync failed: {ex.Message}");
            _selectedUnitPicture = null;
        }

        SelectedUnitCanvas.InvalidateSurface();
    }

    private List<ArmyFactionSelectionItem> GetUnitSourceFactions()
    {
        if (!ShowRightSelectionBox)
        {
            return _leftSlotFaction is null ? [] : [_leftSlotFaction];
        }

        var list = new List<ArmyFactionSelectionItem>(2);
        if (_leftSlotFaction is not null)
        {
            list.Add(_leftSlotFaction);
        }

        if (_rightSlotFaction is not null && (_leftSlotFaction is null || _rightSlotFaction.Id != _leftSlotFaction.Id))
        {
            list.Add(_rightSlotFaction);
        }

        return list;
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
            Console.Error.WriteLine($"ArmyFactionSelectionPage BuildIdNameLookup failed for '{sectionName}': {ex.Message}");
        }

        return map;
    }

    private static bool UnitHasLieutenantOption(string? profileGroupsJson, IReadOnlyDictionary<int, string> skillsLookup)
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
                    if (IsLieutenantOption(option, skillsLookup))
                    {
                        return true;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage UnitHasLieutenantOption failed: {ex.Message}");
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

        if (!option.TryGetProperty("skills", out var skillsElement) || skillsElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var entry in skillsElement.EnumerateArray())
        {
            if (!TryParseId(entry, out var id))
            {
                continue;
            }

            if (skillsLookup.TryGetValue(id, out var name) &&
                name.Contains("lieutenant", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
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

            if (string.Equals(typeElement.GetString(), "LIEUTENANT", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    private static bool TryParseId(JsonElement element, out int id)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var numberId))
        {
            id = numberId;
            return true;
        }

        if (element.ValueKind == JsonValueKind.String &&
            int.TryParse(element.GetString(), out var stringId))
        {
            id = stringId;
            return true;
        }

        id = 0;
        return false;
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

    private void ResetUnitDetails()
    {
        UnitNameHeading = "Select a unit";
        _selectedUnitPicture?.Dispose();
        _selectedUnitPicture = null;
        SelectedUnitCanvas.InvalidateSurface();
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
        EquipmentSummary = "Equipment: -";
        SpecialSkillsSummary = "Special Skills: -";
    }

    private static IEnumerable<JsonElement> EnumerateOptions(string? profileGroupsJson)
    {
        if (string.IsNullOrWhiteSpace(profileGroupsJson))
        {
            yield break;
        }

        using var doc = JsonDocument.Parse(profileGroupsJson);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var group in doc.RootElement.EnumerateArray())
        {
            if (!group.TryGetProperty("options", out var options) || options.ValueKind != JsonValueKind.Array)
            {
                continue;
            }

            foreach (var option in options.EnumerateArray())
            {
                yield return option.Clone();
            }
        }
    }

    private static string ReadIntAsString(JsonElement option, string propertyName)
    {
        if (!option.TryGetProperty(propertyName, out var element))
        {
            return "-";
        }

        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var value))
        {
            return value.ToString();
        }

        if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out var parsed))
        {
            return parsed.ToString();
        }

        return "-";
    }

    private static string ReadMove(JsonElement option)
    {
        if (!option.TryGetProperty("mov", out var movElement))
        {
            return "-";
        }

        if (movElement.ValueKind == JsonValueKind.Array)
        {
            var parts = movElement.EnumerateArray()
                .Select(x => x.ValueKind == JsonValueKind.Number && x.TryGetInt32(out var n) ? n.ToString() : x.GetString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .ToList();
            return parts.Count == 0 ? "-" : string.Join("-", parts);
        }

        if (movElement.ValueKind == JsonValueKind.String)
        {
            return movElement.GetString() ?? "-";
        }

        return "-";
    }

    private static (string Header, string Value) ReadVitality(JsonElement option)
    {
        var str = ReadIntAsString(option, "str");
        if (str != "-")
        {
            return ("STR", str);
        }

        return ("VITA", ReadIntAsString(option, "vita"));
    }

    private static string ReadAvaAsString(JsonElement option)
    {
        if (!option.TryGetProperty("ava", out var avaElement))
        {
            return "-";
        }

        int value;
        if (avaElement.ValueKind == JsonValueKind.Number && avaElement.TryGetInt32(out value))
        {
            return value == 255 ? "T" : value.ToString();
        }

        if (avaElement.ValueKind == JsonValueKind.String && int.TryParse(avaElement.GetString(), out value))
        {
            return value == 255 ? "T" : value.ToString();
        }

        return "-";
    }

    private static List<string> IntersectNamedIds(
        IReadOnlyList<JsonElement> options,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup)
    {
        HashSet<int>? intersection = null;
        foreach (var option in options)
        {
            var ids = new HashSet<int>();
            if (option.TryGetProperty(propertyName, out var arr) && arr.ValueKind == JsonValueKind.Array)
            {
                foreach (var entry in arr.EnumerateArray())
                {
                    if (TryParseId(entry, out var id))
                    {
                        ids.Add(id);
                    }
                }
            }

            if (intersection is null)
            {
                intersection = ids;
            }
            else
            {
                intersection.IntersectWith(ids);
            }
        }

        if (intersection is null || intersection.Count == 0)
        {
            return [];
        }

        return intersection
            .Where(lookup.ContainsKey)
            .Select(id => lookup[id])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private void OnUnitListItemTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Element element || element.BindingContext is not ArmyUnitSelectionItem item)
        {
            return;
        }

        SetSelectedUnit(item);
    }

    private async Task LoadSlotIconAsync(int slotIndex, string? cachedPath, string? packagedPath)
    {
        try
        {
            Stream? stream = null;
            if (!string.IsNullOrWhiteSpace(cachedPath) && File.Exists(cachedPath))
            {
                stream = File.OpenRead(cachedPath);
            }
            else if (!string.IsNullOrWhiteSpace(packagedPath))
            {
                stream = await FileSystem.Current.OpenAppPackageFileAsync(packagedPath);
            }

            if (slotIndex == 0)
            {
                _leftSlotPicture?.Dispose();
                _leftSlotPicture = null;
            }
            else
            {
                _rightSlotPicture?.Dispose();
                _rightSlotPicture = null;
            }

            if (stream is null)
            {
                InvalidateSlotCanvas(slotIndex);
                return;
            }

            await using (stream)
            {
                var svg = new SKSvg();
                var picture = svg.Load(stream);
                if (slotIndex == 0)
                {
                    _leftSlotPicture = picture;
                }
                else
                {
                    _rightSlotPicture = picture;
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage slot icon load failed: {ex.Message}");
            if (slotIndex == 0)
            {
                _leftSlotPicture = null;
            }
            else
            {
                _rightSlotPicture = null;
            }
        }

        InvalidateSlotCanvas(slotIndex);
    }

    private void InvalidateSlotCanvas(int slotIndex)
    {
        if (slotIndex == 0)
        {
            LeftSlotCanvas.InvalidateSurface();
            return;
        }

        RightSlotCanvas.InvalidateSurface();
    }

    private void OnLeftSlotCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawSlotPicture(_leftSlotPicture, e);
    }

    private void OnRightSlotCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawSlotPicture(_rightSlotPicture, e);
    }

    private void OnSelectedUnitCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        DrawSlotPicture(_selectedUnitPicture, e);
    }

    private static void DrawSlotPicture(SKPicture? picture, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);

        if (picture is null)
        {
            return;
        }

        var bounds = picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var width = e.Info.Width;
        var height = e.Info.Height;
        var scale = Math.Min(width / bounds.Width, height / bounds.Height);
        var x = (width - (bounds.Width * scale)) / 2f;
        var y = (height - (bounds.Height * scale)) / 2f;

        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(picture);
    }
}

public class ArmyFactionSelectionItem : BaseViewModel, IViewerListItem
{
    public int Id { get; init; }

    public int ParentId { get; init; }

    public string Name { get; init; } = string.Empty;

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

public class ArmyUnitSelectionItem : BaseViewModel, IViewerListItem
{
    public int Id { get; init; }
    public int SourceFactionId { get; init; }

    public int? Type { get; init; }

    public string Name { get; init; } = string.Empty;

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
