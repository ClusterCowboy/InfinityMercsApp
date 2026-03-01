using System.Collections.ObjectModel;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Input;
using InfinityMercsApp.Data.Database;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Controls;
using Microsoft.Extensions.DependencyInjection;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using Svg.Skia;

namespace InfinityMercsApp.Views;

public partial class ArmyFactionSelectionPage : ContentPage
{
    private readonly record struct ExtraDefinition(string Name, string Type);

    private const int MaxIconsPerRow = 3;
    private const float IconSize = 24f;
    private const float IconGap = 20f;
    private const float RightPadding = 24f;
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
    private readonly AppSettingsService? _appSettingsService;
    private ArmyFactionSelectionItem? _selectedFaction;
    private ArmyFactionSelectionItem? _leftSlotFaction;
    private ArmyFactionSelectionItem? _rightSlotFaction;
    private SKPicture? _leftSlotPicture;
    private SKPicture? _rightSlotPicture;
    private SKPicture? _selectedUnitPicture;
    private SKPicture? _regularOrderIconPicture;
    private SKPicture? _irregularOrderIconPicture;
    private SKPicture? _impetuousIconPicture;
    private SKPicture? _tacticalAwarenessIconPicture;
    private SKPicture? _cubeIconPicture;
    private SKPicture? _cube2IconPicture;
    private SKPicture? _hackableIconPicture;
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
    private FormattedString _equipmentSummaryFormatted = new();
    private FormattedString _specialSkillsSummaryFormatted = new();
    private bool _showRegularOrderIcon;
    private bool _showIrregularOrderIcon;
    private bool _showImpetuousIcon;
    private bool _showTacticalAwarenessIcon;
    private bool _showCubeIcon;
    private bool _showCube2Icon;
    private bool _showHackableIcon;
    private bool _showUnitsInInches = true;
    private int? _unitMoveFirstCm;
    private int? _unitMoveSecondCm;

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
        _appSettingsService = services?.GetService<AppSettingsService>();

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
                Console.Error.WriteLine("ArmyFactionSelectionPage SelectUnitCommand invoked with null item.");
                return;
            }

            Console.WriteLine($"ArmyFactionSelectionPage SelectUnitCommand: id={item.Id}, faction={item.SourceFactionId}, name='{item.Name}'.");
            SetSelectedUnit(item);
        });

        BindingContext = this;
        SetActiveSlot(0);
        EquipmentSummaryFormatted = BuildNamedSummaryFormatted("Equipment", [], Color.FromArgb("#06B6D4"));
        SpecialSkillsSummaryFormatted = BuildNamedSummaryFormatted("Special Skills", [], Color.FromArgb("#F59E0B"));
        _ = LoadHeaderIconsAsync();
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
    public FormattedString EquipmentSummaryFormatted { get => _equipmentSummaryFormatted; private set { _equipmentSummaryFormatted = value; OnPropertyChanged(); } }
    public FormattedString SpecialSkillsSummaryFormatted { get => _specialSkillsSummaryFormatted; private set { _specialSkillsSummaryFormatted = value; OnPropertyChanged(); } }
    public bool HasAnyTopHeaderIcons => ShowRegularOrderIcon || ShowIrregularOrderIcon || ShowImpetuousIcon || ShowTacticalAwarenessIcon;
    public bool HasAnyBottomHeaderIcons => ShowCubeIcon || ShowCube2Icon || ShowHackableIcon;

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
            OnPropertyChanged(nameof(HasAnyTopHeaderIcons));
            TopIconRowCanvas.InvalidateSurface();
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
            OnPropertyChanged(nameof(HasAnyTopHeaderIcons));
            TopIconRowCanvas.InvalidateSurface();
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
            TopIconRowCanvas.InvalidateSurface();
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
            TopIconRowCanvas.InvalidateSurface();
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
            BottomIconRowCanvas.InvalidateSurface();
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
            BottomIconRowCanvas.InvalidateSurface();
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
            BottomIconRowCanvas.InvalidateSurface();
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
                    PackagedLogoPath = _factionLogoCacheService?.GetPackagedFactionLogoPath(faction.Id)
                        ?? $"SVGCache/factions/{faction.Id}.svg"
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

        var factionChanged = false;
        if (_activeSlotIndex == 0 || !ShowRightSelectionBox)
        {
            factionChanged = _leftSlotFaction?.Id != item.Id;
            _leftSlotFaction = item;
            LeftSlotText = item.Name;
            _ = LoadSlotIconAsync(0, item.CachedLogoPath, item.PackagedLogoPath);
        }
        else
        {
            factionChanged = _rightSlotFaction?.Id != item.Id;
            _rightSlotFaction = item;
            RightSlotText = item.Name;
            _ = LoadSlotIconAsync(1, item.CachedLogoPath, item.PackagedLogoPath);
        }

        AutoSelectEmptySlot();
        if (factionChanged)
        {
            _ = LoadUnitsForActiveSlotAsync();
        }
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
    }

    private void OnRightSlotTapped(object? sender, TappedEventArgs e)
    {
        if (!ShowRightSelectionBox)
        {
            return;
        }

        SetActiveSlot(1);
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
                        PackagedLogoPath = _factionLogoCacheService?.GetPackagedUnitLogoPath(faction.Id, unit.UnitId)
                            ?? $"SVGCache/units/{faction.Id}-{unit.UnitId}.svg"
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
        Console.WriteLine($"ArmyFactionSelectionPage SetSelectedUnit requested: id={item.Id}, faction={item.SourceFactionId}, name='{item.Name}'.");
        if (_selectedUnit == item)
        {
            Console.WriteLine("ArmyFactionSelectionPage SetSelectedUnit skipped (same item instance).");
            return;
        }

        if (_selectedUnit is not null)
        {
            _selectedUnit.IsSelected = false;
        }

        _selectedUnit = item;
        _selectedUnit.IsSelected = true;
        Console.WriteLine($"ArmyFactionSelectionPage selected unit now: id={_selectedUnit.Id}, faction={_selectedUnit.SourceFactionId}, name='{_selectedUnit.Name}'.");
        _ = LoadSelectedUnitLogoAsync(item);
        _ = LoadSelectedUnitDetailsAsync();
    }

    private async Task LoadSelectedUnitDetailsAsync(CancellationToken cancellationToken = default)
    {
        ResetUnitDetails(clearLogo: false);
        if (_selectedUnit is null || _armyDataAccessor is null)
        {
            Console.Error.WriteLine("ArmyFactionSelectionPage LoadSelectedUnitDetailsAsync aborted: selected unit or accessor missing.");
            return;
        }

        try
        {
            Console.WriteLine($"ArmyFactionSelectionPage LoadSelectedUnitDetailsAsync started: id={_selectedUnit.Id}, faction={_selectedUnit.SourceFactionId}, name='{_selectedUnit.Name}'.");
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
            var charsLookup = BuildIdNameLookup(snapshot?.FiltersJson, "chars");
            var extrasLookup = BuildExtrasLookup(snapshot?.FiltersJson);
            await ApplyGlobalDisplayUnitsPreferenceAsync(cancellationToken);
            if (!string.IsNullOrWhiteSpace(unit.ProfileGroupsJson))
            {
                using var doc = JsonDocument.Parse(unit.ProfileGroupsJson);
                var options = EnumerateOptions(doc.RootElement).ToList();
                PopulateUnitStatsFromFirstProfile(doc.RootElement);
                var orderTraits = ParseUnitOrderTraits(doc.RootElement);
                ShowIrregularOrderIcon = orderTraits.HasIrregular;
                ShowRegularOrderIcon = !orderTraits.HasIrregular && orderTraits.HasRegular;
                ShowImpetuousIcon = orderTraits.HasImpetuous;
                ShowTacticalAwarenessIcon = orderTraits.HasTacticalAwareness;
                var techTraits = ParseUnitTechTraits(doc.RootElement, equipLookup, skillsLookup, charsLookup);
                ShowCubeIcon = techTraits.HasCube;
                ShowCube2Icon = techTraits.HasCube2;
                ShowHackableIcon = techTraits.HasHackable;

                var stableEquip = ComputeCommonDisplayNamesFromProfiles(
                    unit.ProfileGroupsJson,
                    "equip",
                    equipLookup,
                    extrasLookup,
                    _showUnitsInInches);
                if (stableEquip.Count == 0 && options.Count > 0)
                {
                    stableEquip = IntersectDisplayNamesWithIncludes(
                        doc.RootElement,
                        options,
                        "equip",
                        equipLookup,
                        extrasLookup,
                        _showUnitsInInches);
                }

                var stableSkills = ComputeCommonDisplayNamesFromProfiles(
                    unit.ProfileGroupsJson,
                    "skills",
                    skillsLookup,
                    extrasLookup,
                    _showUnitsInInches);
                if (stableSkills.Count == 0 && options.Count > 0)
                {
                    stableSkills = IntersectDisplayNamesWithIncludes(
                        doc.RootElement,
                        options,
                        "skills",
                        skillsLookup,
                        extrasLookup,
                        _showUnitsInInches);
                }

                stableSkills = stableSkills
                    .Where(x => !x.Contains("lieutenant", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                Console.WriteLine(
                    $"ArmyFactionSelectionPage summary extraction: unit='{_selectedUnit.Name}', options={options.Count}, " +
                    $"commonEquip={stableEquip.Count}, commonSkills={stableSkills.Count}.");

                EquipmentSummary = $"Equipment: {(stableEquip.Count == 0 ? "-" : string.Join(", ", stableEquip))}";
                SpecialSkillsSummary = $"Special Skills: {(stableSkills.Count == 0 ? "-" : string.Join(", ", stableSkills))}";
                EquipmentSummaryFormatted = BuildNamedSummaryFormatted("Equipment", stableEquip, Color.FromArgb("#06B6D4"));
                SpecialSkillsSummaryFormatted = BuildNamedSummaryFormatted("Special Skills", stableSkills, Color.FromArgb("#F59E0B"));
                Console.WriteLine($"ArmyFactionSelectionPage LoadSelectedUnitDetailsAsync completed: heading='{UnitNameHeading}', MOV='{UnitMov}', equipment='{EquipmentSummary}'.");
                return;
            }
            else
            {
                Console.Error.WriteLine($"ArmyFactionSelectionPage: profileGroups missing for faction={_selectedUnit.SourceFactionId}, unit={_selectedUnit.Id}.");
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage LoadSelectedUnitDetailsAsync failed: {ex}");
        }
    }

    private async Task LoadSelectedUnitLogoAsync(ArmyUnitSelectionItem item)
    {
        _selectedUnitPicture?.Dispose();
        _selectedUnitPicture = null;

        try
        {
            Stream? stream = await OpenBestUnitLogoStreamAsync(item);

            if (stream is null)
            {
                SelectedUnitCanvas.InvalidateSurface();
                return;
            }

            await using (stream)
            {
                var svg = new SKSvg();
                _selectedUnitPicture = svg.Load(stream);
                if (_selectedUnitPicture is null)
                {
                    Console.Error.WriteLine($"ArmyFactionSelectionPage selected logo parse failed: unit='{item.Name}', id={item.Id}, faction={item.SourceFactionId}.");
                }
                else
                {
                    var bounds = _selectedUnitPicture.CullRect;
                    Console.WriteLine($"ArmyFactionSelectionPage selected logo loaded: unit='{item.Name}', bounds=({bounds.Left},{bounds.Top},{bounds.Right},{bounds.Bottom}).");
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage LoadSelectedUnitLogoAsync failed: {ex.Message}");
            _selectedUnitPicture = null;
        }

        SelectedUnitCanvas.InvalidateSurface();
    }

    private async Task<Stream?> OpenBestUnitLogoStreamAsync(ArmyUnitSelectionItem item)
    {
        Console.WriteLine($"ArmyFactionSelectionPage logo resolve start: unit='{item.Name}', id={item.Id}, faction={item.SourceFactionId}.");
        foreach (var cachedPath in BuildUnitCachedPathCandidates(item))
        {
            if (string.IsNullOrWhiteSpace(cachedPath))
            {
                continue;
            }

            var exists = File.Exists(cachedPath);
            Console.WriteLine($"ArmyFactionSelectionPage logo cached candidate: '{cachedPath}', exists={exists}.");
            if (!string.IsNullOrWhiteSpace(cachedPath) && File.Exists(cachedPath))
            {
                Console.WriteLine($"ArmyFactionSelectionPage logo using cached: '{cachedPath}'.");
                return File.OpenRead(cachedPath);
            }
        }

        foreach (var packagedPath in BuildUnitPackagedPathCandidates(item))
        {
            if (string.IsNullOrWhiteSpace(packagedPath))
            {
                continue;
            }

            try
            {
                var stream = await FileSystem.Current.OpenAppPackageFileAsync(packagedPath);
                Console.WriteLine($"ArmyFactionSelectionPage logo using packaged: '{packagedPath}'.");
                return stream;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"ArmyFactionSelectionPage logo packaged candidate failed: '{packagedPath}': {ex.Message}");
            }
        }

        Console.Error.WriteLine($"ArmyFactionSelectionPage logo resolve failed: unit='{item.Name}', id={item.Id}, faction={item.SourceFactionId}.");
        return null;
    }

    private IEnumerable<string?> BuildUnitCachedPathCandidates(ArmyUnitSelectionItem item)
    {
        yield return item.CachedLogoPath;

        if (_factionLogoCacheService is not null)
        {
            yield return _factionLogoCacheService.GetCachedUnitLogoPath(item.SourceFactionId, item.Id);

            if (_leftSlotFaction is not null)
            {
                yield return _factionLogoCacheService.GetCachedUnitLogoPath(_leftSlotFaction.Id, item.Id);
            }

            if (_rightSlotFaction is not null)
            {
                yield return _factionLogoCacheService.GetCachedUnitLogoPath(_rightSlotFaction.Id, item.Id);
            }

            yield return _factionLogoCacheService.GetCachedLogoPath(item.SourceFactionId);
        }
    }

    private IEnumerable<string?> BuildUnitPackagedPathCandidates(ArmyUnitSelectionItem item)
    {
        yield return item.PackagedLogoPath;

        if (_factionLogoCacheService is not null)
        {
            yield return _factionLogoCacheService.GetPackagedUnitLogoPath(item.SourceFactionId, item.Id);

            if (_leftSlotFaction is not null)
            {
                yield return _factionLogoCacheService.GetPackagedUnitLogoPath(_leftSlotFaction.Id, item.Id);
            }

            if (_rightSlotFaction is not null)
            {
                yield return _factionLogoCacheService.GetPackagedUnitLogoPath(_rightSlotFaction.Id, item.Id);
            }

            yield return _factionLogoCacheService.GetPackagedFactionLogoPath(item.SourceFactionId);
        }
        else
        {
            yield return $"SVGCache/units/{item.SourceFactionId}-{item.Id}.svg";
            if (_leftSlotFaction is not null)
            {
                yield return $"SVGCache/units/{_leftSlotFaction.Id}-{item.Id}.svg";
            }

            if (_rightSlotFaction is not null)
            {
                yield return $"SVGCache/units/{_rightSlotFaction.Id}-{item.Id}.svg";
            }

            yield return $"SVGCache/factions/{item.SourceFactionId}.svg";
        }
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

        if (element.ValueKind == JsonValueKind.Object &&
            element.TryGetProperty("id", out var idElement))
        {
            return TryParseId(idElement, out id);
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

    private void ResetUnitDetails(bool clearLogo = true)
    {
        UnitNameHeading = "Select a unit";
        if (clearLogo)
        {
            Console.WriteLine("ArmyFactionSelectionPage ResetUnitDetails: clearing selected unit logo.");
            _selectedUnitPicture?.Dispose();
            _selectedUnitPicture = null;
            SelectedUnitCanvas.InvalidateSurface();
        }
        ResetUnitStatsOnly();
        EquipmentSummary = "Equipment: -";
        SpecialSkillsSummary = "Special Skills: -";
        EquipmentSummaryFormatted = BuildNamedSummaryFormatted("Equipment", [], Color.FromArgb("#06B6D4"));
        SpecialSkillsSummaryFormatted = BuildNamedSummaryFormatted("Special Skills", [], Color.FromArgb("#F59E0B"));
        ShowRegularOrderIcon = false;
        ShowIrregularOrderIcon = false;
        ShowImpetuousIcon = false;
        ShowTacticalAwarenessIcon = false;
        ShowCubeIcon = false;
        ShowCube2Icon = false;
        ShowHackableIcon = false;
    }

    private void ResetUnitStatsOnly()
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

    private static IEnumerable<JsonElement> EnumerateOptions(JsonElement profileGroupsRoot)
    {
        if (profileGroupsRoot.ValueKind != JsonValueKind.Array)
        {
            yield break;
        }

        foreach (var group in profileGroupsRoot.EnumerateArray())
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

    private static bool TryGetFirstProfileGroup(string? profileGroupsJson, out JsonElement group)
    {
        group = default;
        if (string.IsNullOrWhiteSpace(profileGroupsJson))
        {
            return false;
        }

        try
        {
            using var doc = JsonDocument.Parse(profileGroupsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array || doc.RootElement.GetArrayLength() == 0)
            {
                return false;
            }

            group = doc.RootElement[0].Clone();
            return true;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage TryGetFirstProfileGroup failed: {ex.Message}");
            return false;
        }
    }

    private void PopulateUnitStatsFromFirstProfile(JsonElement profileGroupsArray)
    {
        ResetUnitStatsOnly();

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
            var firstOption = EnumerateOptions(profileGroupsArray).FirstOrDefault();
            if (firstOption.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            PopulateUnitStatsFromElement(firstOption);
            return;
        }

        PopulateUnitStatsFromElement(firstProfile.Value);
    }

    private void PopulateUnitStatsFromElement(JsonElement selectedElement)
    {
        (_unitMoveFirstCm, _unitMoveSecondCm) = ParseMoveValues(selectedElement);
        UpdateUnitMoveDisplay();
        UnitCc = ReadIntAsString(selectedElement, "cc");
        UnitBs = ReadIntAsString(selectedElement, "bs");
        UnitPh = ReadIntAsString(selectedElement, "ph");
        UnitWip = ReadIntAsString(selectedElement, "wip");
        UnitArm = ReadIntAsString(selectedElement, "arm");
        UnitBts = ReadIntAsString(selectedElement, "bts");
        UnitS = ReadIntAsString(selectedElement, "s");
        UnitAva = ReadAvaAsString(selectedElement);
        var (vitalityHeader, vitalityValue) = ReadVitality(selectedElement);
        UnitVitalityHeader = vitalityHeader;
        UnitVitality = vitalityValue;
    }

    private static string ReadMoveFromProfile(JsonElement profile)
    {
        if (TryGetPropertyFlexible(profile, "move", out var moveElement) && moveElement.ValueKind == JsonValueKind.Array)
        {
            var moveParts = moveElement.EnumerateArray()
                .Select(ReadNumericString)
                .Where(x => !string.IsNullOrWhiteSpace(x) && x != "-")
                .ToList();

            if (moveParts.Count > 0)
            {
                return string.Join("-", moveParts);
            }
        }

        return ReadMove(profile);
    }

    private void UpdateUnitMoveDisplay()
    {
        if (!_unitMoveFirstCm.HasValue || !_unitMoveSecondCm.HasValue)
        {
            UnitMov = "-";
            return;
        }

        if (_showUnitsInInches)
        {
            var first = (int)Math.Round(_unitMoveFirstCm.Value / 2.5, MidpointRounding.AwayFromZero);
            var second = (int)Math.Round(_unitMoveSecondCm.Value / 2.5, MidpointRounding.AwayFromZero);
            UnitMov = $"{first}-{second}";
            return;
        }

        UnitMov = $"{_unitMoveFirstCm.Value}-{_unitMoveSecondCm.Value}";
    }

    private static (int? firstCm, int? secondCm) ParseMoveValues(JsonElement element)
    {
        if (!TryGetPropertyFlexible(element, "move", out var moveElement) &&
            !TryGetPropertyFlexible(element, "mov", out moveElement))
        {
            return (null, null);
        }

        if (moveElement.ValueKind == JsonValueKind.String)
        {
            var parts = (moveElement.GetString() ?? string.Empty)
                .Split('-', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
                .Select(x => int.TryParse(x, out var parsed) ? (int?)parsed : null)
                .Where(x => x.HasValue)
                .Select(x => x!.Value)
                .ToList();

            return parts.Count >= 2 ? (parts[0], parts[1]) : (null, null);
        }

        if (moveElement.ValueKind != JsonValueKind.Array)
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

    private static string ReadIntAsString(JsonElement option, string propertyName)
    {
        if (!TryGetPropertyFlexible(option, propertyName, out var element))
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

    private static string ReadNumericString(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out var numericValue))
        {
            return numericValue.ToString();
        }

        if (element.ValueKind == JsonValueKind.String && int.TryParse(element.GetString(), out var parsedValue))
        {
            return parsedValue.ToString();
        }

        return "-";
    }

    private static string ReadMove(JsonElement option)
    {
        if (!TryGetPropertyFlexible(option, "mov", out var movElement))
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

        var w = ReadIntAsString(option, "w");
        if (w != "-")
        {
            return ("VITA", w);
        }

        return ("VITA", ReadIntAsString(option, "vita"));
    }

    private static string ReadAvaAsString(JsonElement option)
    {
        if (!TryGetPropertyFlexible(option, "ava", out var avaElement))
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

    private static bool TryGetPropertyFlexible(JsonElement element, string propertyName, out JsonElement value)
    {
        var variants = new[]
        {
            propertyName,
            propertyName.ToLowerInvariant(),
            propertyName.ToUpperInvariant(),
            char.ToUpperInvariant(propertyName[0]) + propertyName.Substring(1).ToLowerInvariant()
        };

        foreach (var variant in variants.Distinct(StringComparer.Ordinal))
        {
            if (element.TryGetProperty(variant, out value))
            {
                return true;
            }
        }

        foreach (var containerName in new[] { "stats", "Stats", "attributes", "Attributes", "attrs", "Attrs" })
        {
            if (!element.TryGetProperty(containerName, out var container) || container.ValueKind != JsonValueKind.Object)
            {
                continue;
            }

            foreach (var variant in variants.Distinct(StringComparer.Ordinal))
            {
                if (container.TryGetProperty(variant, out value))
                {
                    return true;
                }
            }
        }

        value = default;
        return false;
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
            UpdateUnitMoveDisplay();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage ApplyGlobalDisplayUnitsPreferenceAsync failed: {ex.Message}");
        }
    }

    private static List<string> ComputeCommonNamesFromProfiles(
        string? profileGroupsJson,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup)
    {
        if (string.IsNullOrWhiteSpace(profileGroupsJson))
        {
            return [];
        }

        HashSet<string>? common = null;
        try
        {
            using var doc = JsonDocument.Parse(profileGroupsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            foreach (var group in doc.RootElement.EnumerateArray())
            {
                if (!group.TryGetProperty("profiles", out var profilesElement) || profilesElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var profile in profilesElement.EnumerateArray())
                {
                    var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (profile.TryGetProperty(propertyName, out var arr) && arr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var entry in arr.EnumerateArray())
                        {
                            if (TryParseId(entry, out var id) && lookup.TryGetValue(id, out var name) && !string.IsNullOrWhiteSpace(name))
                            {
                                set.Add(name);
                            }
                        }
                    }

                    if (common is null)
                    {
                        common = set;
                    }
                    else
                    {
                        common.IntersectWith(set);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage ComputeCommonNamesFromProfiles failed for '{propertyName}': {ex.Message}");
            return [];
        }

        if (common is null || common.Count == 0)
        {
            return [];
        }

        return common.OrderBy(x => x, StringComparer.OrdinalIgnoreCase).ToList();
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
                    ApplyTechTraitsFromContainer(profile, "equip", equipLookup, ref hasCube, ref hasCube2, ref hasHackable);
                    ApplyTechTraitsFromContainer(profile, "skills", skillsLookup, ref hasCube, ref hasCube2, ref hasHackable);
                    ApplyTechTraitsFromContainer(profile, "chars", charsLookup, ref hasCube, ref hasCube2, ref hasHackable);
                }
            }

            if (group.TryGetProperty("options", out var optionsElement) && optionsElement.ValueKind == JsonValueKind.Array)
            {
                foreach (var option in optionsElement.EnumerateArray())
                {
                    ApplyTechTraitsFromContainer(option, "equip", equipLookup, ref hasCube, ref hasCube2, ref hasHackable);
                    ApplyTechTraitsFromContainer(option, "skills", skillsLookup, ref hasCube, ref hasCube2, ref hasHackable);
                    ApplyTechTraitsFromContainer(option, "chars", charsLookup, ref hasCube, ref hasCube2, ref hasHackable);
                }
            }
        }

        return (hasCube, hasCube2, hasHackable);
    }

    private static void ApplyTechTraitsFromContainer(
        JsonElement container,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup,
        ref bool hasCube,
        ref bool hasCube2,
        ref bool hasHackable)
    {
        if (!container.TryGetProperty(propertyName, out var arr) || arr.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        foreach (var entry in arr.EnumerateArray())
        {
            if (!TryParseId(entry, out var id) || !lookup.TryGetValue(id, out var name))
            {
                continue;
            }

            ApplyTechTraitName(name, ref hasCube, ref hasCube2, ref hasHackable);
        }
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

    private static FormattedString BuildNamedSummaryFormatted(string label, IEnumerable<string> values, Color accentColor)
    {
        var list = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var formatted = new FormattedString();
        formatted.Spans.Add(new Span { Text = $"{label}: " });
        if (list.Count == 0)
        {
            formatted.Spans.Add(new Span { Text = "-" });
            return formatted;
        }

        for (var i = 0; i < list.Count; i++)
        {
            if (i > 0)
            {
                formatted.Spans.Add(new Span { Text = ", " });
            }

            formatted.Spans.Add(new Span
            {
                Text = list[i],
                TextColor = accentColor
            });
        }

        return formatted;
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

    private static List<string> ComputeCommonDisplayNamesFromProfiles(
        string? profileGroupsJson,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup,
        IReadOnlyDictionary<int, ExtraDefinition> extrasLookup,
        bool showUnitsInInches)
    {
        if (string.IsNullOrWhiteSpace(profileGroupsJson))
        {
            return [];
        }

        HashSet<string>? common = null;
        try
        {
            using var doc = JsonDocument.Parse(profileGroupsJson);
            if (doc.RootElement.ValueKind != JsonValueKind.Array)
            {
                return [];
            }

            foreach (var group in doc.RootElement.EnumerateArray())
            {
                if (!group.TryGetProperty("profiles", out var profilesElement) || profilesElement.ValueKind != JsonValueKind.Array)
                {
                    continue;
                }

                foreach (var profile in profilesElement.EnumerateArray())
                {
                    var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    if (profile.TryGetProperty(propertyName, out var arr) && arr.ValueKind == JsonValueKind.Array)
                    {
                        foreach (var entry in arr.EnumerateArray())
                        {
                            if (!TryParseId(entry, out var id))
                            {
                                continue;
                            }

                            var baseName = lookup.TryGetValue(id, out var resolvedName) ? resolvedName : id.ToString();
                            set.Add(BuildEntryDisplayName(baseName, entry, extrasLookup, showUnitsInInches));
                        }
                    }

                    if (common is null)
                    {
                        common = set;
                    }
                    else
                    {
                        common.IntersectWith(set);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage ComputeCommonDisplayNamesFromProfiles failed for '{propertyName}': {ex.Message}");
            return [];
        }

        if (common is null || common.Count == 0)
        {
            return [];
        }

        return common
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<string> IntersectNamedIdsWithIncludes(
        JsonElement profileGroupsRoot,
        IReadOnlyList<JsonElement> options,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup)
    {
        HashSet<int>? intersection = null;
        foreach (var option in options)
        {
            var ids = new HashSet<int>();
            foreach (var entry in GetOptionEntriesWithIncludes(profileGroupsRoot, option, propertyName))
            {
                if (TryParseId(entry, out var id))
                {
                    ids.Add(id);
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

    private static List<string> IntersectDisplayNamesWithIncludes(
        JsonElement profileGroupsRoot,
        IReadOnlyList<JsonElement> options,
        string propertyName,
        IReadOnlyDictionary<int, string> lookup,
        IReadOnlyDictionary<int, ExtraDefinition> extrasLookup,
        bool showUnitsInInches)
    {
        HashSet<string>? intersection = null;
        foreach (var option in options)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in GetOptionEntriesWithIncludes(profileGroupsRoot, option, propertyName))
            {
                if (!TryParseId(entry, out var id))
                {
                    continue;
                }

                var baseName = lookup.TryGetValue(id, out var resolvedName) ? resolvedName : id.ToString();
                names.Add(BuildEntryDisplayName(baseName, entry, extrasLookup, showUnitsInInches));
            }

            if (intersection is null)
            {
                intersection = names;
            }
            else
            {
                intersection.IntersectWith(names);
            }
        }

        if (intersection is null || intersection.Count == 0)
        {
            return [];
        }

        return intersection
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
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
                    ? (typeElement.GetString() ?? string.Empty)
                    : string.Empty;

                map[id] = new ExtraDefinition(name, type);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage BuildExtrasLookup failed: {ex.Message}");
        }

        return map;
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

            if (extrasLookup.TryGetValue(extraId, out var definition) &&
                !string.IsNullOrWhiteSpace(definition.Name))
            {
                extras.Add(FormatExtraDisplay(definition, showUnitsInInches));
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

    private static string FormatExtraDisplay(ExtraDefinition definition, bool showUnitsInInches)
    {
        if (!showUnitsInInches ||
            !string.Equals(definition.Type, "DISTANCE", StringComparison.OrdinalIgnoreCase))
        {
            return definition.Name;
        }

        return ConvertDistanceTextToInches(definition.Name);
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

        var sign = match.Groups[1].Value;
        var numberText = match.Groups[2].Value;
        if (!double.TryParse(numberText, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var cm))
        {
            return distanceText;
        }

        var inches = (int)Math.Round(cm / 2.5, MidpointRounding.AwayFromZero);
        var replacement = $"{sign}{inches}";
        return distanceText.Remove(match.Index, match.Length).Insert(match.Index, replacement);
    }

    private void OnUnitListItemTapped(object? sender, TappedEventArgs e)
    {
        if (sender is not Element element || element.BindingContext is not ArmyUnitSelectionItem item)
        {
            return;
        }

        SetSelectedUnit(item);
    }

    private void OnUnitItemTappedFromView(object? sender, EventArgs e)
    {
        if (sender is not FactionListItemView view || view.BindingContext is not ArmyUnitSelectionItem item)
        {
            Console.Error.WriteLine("ArmyFactionSelectionPage OnUnitItemTappedFromView: no unit binding context.");
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

    private async Task LoadHeaderIconsAsync()
    {
        _regularOrderIconPicture?.Dispose();
        _regularOrderIconPicture = null;
        _irregularOrderIconPicture?.Dispose();
        _irregularOrderIconPicture = null;
        _impetuousIconPicture?.Dispose();
        _impetuousIconPicture = null;
        _tacticalAwarenessIconPicture?.Dispose();
        _tacticalAwarenessIconPicture = null;
        _cubeIconPicture?.Dispose();
        _cubeIconPicture = null;
        _cube2IconPicture?.Dispose();
        _cube2IconPicture = null;
        _hackableIconPicture?.Dispose();
        _hackableIconPicture = null;

        try
        {
            await using var regularStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-circle-arrow-803872.svg");
            var regularSvg = new SKSvg();
            _regularOrderIconPicture = regularSvg.Load(regularStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage regular order icon load failed: {ex.Message}");
        }

        try
        {
            await using var irregularStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-arrow-963008.svg");
            var irregularSvg = new SKSvg();
            _irregularOrderIconPicture = irregularSvg.Load(irregularStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage irregular order icon load failed: {ex.Message}");
        }

        try
        {
            await using var impetuousStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-fire-131591.svg");
            var impetuousSvg = new SKSvg();
            _impetuousIconPicture = impetuousSvg.Load(impetuousStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage impetuous icon load failed: {ex.Message}");
        }

        try
        {
            await using var tacticalStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-double-arrows-7302616.svg");
            var tacticalSvg = new SKSvg();
            _tacticalAwarenessIconPicture = tacticalSvg.Load(tacticalStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage tactical awareness icon load failed: {ex.Message}");
        }

        try
        {
            await using var cubeStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/cube-alt-2-svgrepo-com.svg");
            var cubeSvg = new SKSvg();
            _cubeIconPicture = cubeSvg.Load(cubeStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage cube icon load failed: {ex.Message}");
        }

        try
        {
            await using var cube2Stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/cubes-svgrepo-com.svg");
            var cube2Svg = new SKSvg();
            _cube2IconPicture = cube2Svg.Load(cube2Stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage cube2 icon load failed: {ex.Message}");
        }

        try
        {
            await using var hackableStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-circuit-8241852.svg");
            var hackableSvg = new SKSvg();
            _hackableIconPicture = hackableSvg.Load(hackableStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage hackable icon load failed: {ex.Message}");
        }

        TopIconRowCanvas.InvalidateSurface();
        BottomIconRowCanvas.InvalidateSurface();
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
        if (_selectedUnitPicture is null)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage selected canvas paint: no picture (canvas={e.Info.Width}x{e.Info.Height}).");
        }
        else
        {
            var b = _selectedUnitPicture.CullRect;
            Console.WriteLine($"ArmyFactionSelectionPage selected canvas paint: canvas={e.Info.Width}x{e.Info.Height}, bounds=({b.Left},{b.Top},{b.Right},{b.Bottom}).");
        }

        DrawSlotPicture(_selectedUnitPicture, e);
    }

    private void OnTopIconRowCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        var pictures = BuildTopIconPictures();
        DrawIconRow(canvas, e.Info, pictures);
    }

    private void OnBottomIconRowCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        var pictures = BuildBottomIconPictures();
        DrawIconRow(canvas, e.Info, pictures);
    }

    private List<SKPicture> BuildTopIconPictures()
    {
        var pictures = new List<SKPicture>(MaxIconsPerRow);
        var orderTypePicture = ShowIrregularOrderIcon ? _irregularOrderIconPicture : _regularOrderIconPicture;
        if ((ShowRegularOrderIcon || ShowIrregularOrderIcon) && orderTypePicture is not null)
        {
            pictures.Add(orderTypePicture);
        }

        if (ShowImpetuousIcon && _impetuousIconPicture is not null)
        {
            pictures.Add(_impetuousIconPicture);
        }

        if (ShowTacticalAwarenessIcon && _tacticalAwarenessIconPicture is not null)
        {
            pictures.Add(_tacticalAwarenessIconPicture);
        }

        return pictures;
    }

    private List<SKPicture> BuildBottomIconPictures()
    {
        var pictures = new List<SKPicture>(MaxIconsPerRow);
        if (ShowCubeIcon && _cubeIconPicture is not null)
        {
            pictures.Add(_cubeIconPicture);
        }

        if (ShowCube2Icon && _cube2IconPicture is not null)
        {
            pictures.Add(_cube2IconPicture);
        }

        if (ShowHackableIcon && _hackableIconPicture is not null)
        {
            pictures.Add(_hackableIconPicture);
        }

        return pictures;
    }

    private static void DrawIconRow(SKCanvas canvas, SKImageInfo info, IReadOnlyList<SKPicture> pictures)
    {
        if (pictures.Count == 0)
        {
            return;
        }

        var drawCount = Math.Min(MaxIconsPerRow, pictures.Count);
        var rowWidth = (MaxIconsPerRow * IconSize) + ((MaxIconsPerRow - 1) * IconGap);
        var startX = info.Width - RightPadding - rowWidth;
        if (startX < 0)
        {
            startX = 0;
        }

        for (var i = 0; i < drawCount; i++)
        {
            var x = startX + (i * (IconSize + IconGap));
            var y = (info.Height - IconSize) / 2f;
            var destination = new SKRect(x, y, x + IconSize, y + IconSize);
            DrawPictureInRect(canvas, pictures[i], destination);
        }
    }

    private static void DrawPictureInRect(SKCanvas canvas, SKPicture picture, SKRect destination)
    {
        var bounds = picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0)
        {
            return;
        }

        var scale = Math.Min(destination.Width / bounds.Width, destination.Height / bounds.Height);
        var drawnWidth = bounds.Width * scale;
        var drawnHeight = bounds.Height * scale;
        var translateX = destination.Left + ((destination.Width - drawnWidth) / 2f) - (bounds.Left * scale);
        var translateY = destination.Top + ((destination.Height - drawnHeight) / 2f) - (bounds.Top * scale);

        using var restore = new SKAutoCanvasRestore(canvas, true);
        canvas.Translate(translateX, translateY);
        canvas.Scale(scale);
        canvas.DrawPicture(picture);
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

        using var restore = new SKAutoCanvasRestore(canvas, true);
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
