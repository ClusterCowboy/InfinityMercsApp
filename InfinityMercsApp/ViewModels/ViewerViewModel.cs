using CommunityToolkit.Mvvm.ComponentModel;
using InfinityMercsApp.Domain.Sorting;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Input;

namespace InfinityMercsApp.ViewModels;

public partial class ViewerViewModel : ObservableObject
{
    private readonly record struct ExtraDefinition(string Name, string Type, string? Url);

    private enum FactionFilterMode
    {
        All,
        Factions,
        Sectorials
    }

    private readonly IMetadataProvider _metadataProvider;
    private readonly IArmyImportProvider _armyImportProvider;
    private readonly IFactionProvider _factionProvider;
    private readonly FactionLogoCacheService? _factionLogoCacheService;
    private readonly IAppSettingsProvider _appSettingsProvider;
    private bool _showRegularOrderIcon;
    private bool _showIrregularOrderIcon;
    private bool _showImpetuousIcon;
    private bool _showTacticalAwarenessIcon;
    private bool _showCubeIcon;
    private bool _showCube2Icon;
    private bool _showHackableIcon;
    private bool _showUnitsInInches = true;
    private IReadOnlyDictionary<int, string> _currentEquipmentLookup = new Dictionary<int, string>();
    private IReadOnlyDictionary<int, string> _currentEquipmentLinks = new Dictionary<int, string>();
    private IReadOnlyDictionary<int, string> _currentSkillsLookup = new Dictionary<int, string>();
    private IReadOnlyDictionary<int, string> _currentSkillsLinks = new Dictionary<int, string>();
    private int? _unitMoveFirstCm;
    private int? _unitMoveSecondCm;
    private ViewerFactionItem? _selectedFaction;
    private ViewerUnitItem? _selectedUnit;
    private bool _mercsOnlyUnits;
    private bool _lieutenantOnlyUnits;
    private FactionFilterMode _factionFilterMode = FactionFilterMode.All;
    private List<ViewerFactionItem> _allFactions = [];
    public ViewerViewModel(
        IMetadataProvider metadataProvider,
        IArmyImportProvider armyImportProvider,
        IFactionProvider factionProvider,
        IAppSettingsProvider appSettingsProvider,
        FactionLogoCacheService? factionLogoCacheService = null)
    {
        _metadataProvider = metadataProvider;
        _armyImportProvider = armyImportProvider;
        _factionProvider = factionProvider;
        _factionLogoCacheService = factionLogoCacheService;
        _appSettingsProvider = appSettingsProvider;

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

        ShowUnitsTabCommand = new Command(() => ShowUnitsTab = true);
        ShowFireteamsTabCommand = new Command(() => ShowUnitsTab = false);
    }

    public ObservableCollection<ViewerFactionItem> Factions { get; } = [];

    public ObservableCollection<ViewerUnitItem> Units { get; } = [];
    public ObservableCollection<ViewerProfileItem> Profiles { get; } = [];
    public ObservableCollection<FireteamTeamItem> Fireteams { get; } = [];

    [ObservableProperty]
    private bool showUnitsTab = true;

    [ObservableProperty]
    // TODO - set to opposite of ShowUnitsTab
    private bool showFireteamsTab;

    [ObservableProperty]
    private bool isLoading;

    [ObservableProperty]
    private string status;

    [ObservableProperty]
    private string unitsStatus;

    [ObservableProperty]
    private string profilesStatus;

    [ObservableProperty]
    private string equipmentSummary;

    [ObservableProperty]
    private FormattedString equipmentSummaryFormatted;

    [ObservableProperty]
    private string specialSkillsSummary;

    [ObservableProperty]
    private FormattedString specialSkillsSummaryFormatted;

    [ObservableProperty]
    private string unitNameHeading;

    [ObservableProperty]
    private string unitMov;

    [ObservableProperty]
    private string unitCc;

    [ObservableProperty]
    private string unitBs;

    [ObservableProperty]
    private string unitPh;

    [ObservableProperty]
    private string unitWip;

    [ObservableProperty]
    private string unitArm;

    [ObservableProperty]
    private string unitBts;

    [ObservableProperty]
    private string unitVitalityHeader;

    [ObservableProperty]
    private string unitVitality;

    [ObservableProperty]
    private string unitS;

    [ObservableProperty]
    private string unitAva;

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

    [ObservableProperty]
    private string? impetuousIconUrl;

    [ObservableProperty]
    private string? tacticalAwarenessIconUrl;

    [ObservableProperty]
    private string? cubeIconUrl;

    [ObservableProperty]
    private string? cube2IconUrl;

    [ObservableProperty]
    private string? hackableIconUrl;

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

    [ObservableProperty]
    private string fireteamDuoCount;

    [ObservableProperty]
    private string fireteamHarisCount;

    [ObservableProperty]
    public string fireteamCoreCount;

    [ObservableProperty]
    public string fireteamsStatus;

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
    public ICommand ShowUnitsTabCommand { get; }
    public ICommand ShowFireteamsTabCommand { get; }

    public async Task LoadSpecificUnitAsync(
        int sourceFactionId,
        int sourceUnitId,
        string unitName,
        string? cachedLogoPath = null,
        string? packagedLogoPath = null,
        CancellationToken cancellationToken = default)
    {
        SelectedFaction = new ViewerFactionItem
        {
            Id = sourceFactionId,
            Name = string.Empty
        };

        SelectedUnit = new ViewerUnitItem
        {
            Id = sourceUnitId,
            Name = unitName,
            CachedLogoPath = cachedLogoPath,
            PackagedLogoPath = packagedLogoPath
        };

        await LoadProfilesForSelectedUnitAsync(cancellationToken);
    }

    public async Task LoadSpecificConfigurationAsync(
        int sourceFactionId,
        int sourceUnitId,
        string unitName,
        string profileKey,
        bool isLieutenant,
        string? cachedLogoPath = null,
        string? packagedLogoPath = null,
        CancellationToken cancellationToken = default)
    {
        await LoadSpecificUnitAsync(
            sourceFactionId,
            sourceUnitId,
            unitName,
            cachedLogoPath,
            packagedLogoPath,
            cancellationToken);

        if (string.IsNullOrWhiteSpace(profileKey))
        {
            return;
        }

        var matchedProfile = Profiles
            .FirstOrDefault(x =>
                ProfileKeysMatch(x.ProfileKey, profileKey) &&
                x.IsLieutenant == isLieutenant);

        matchedProfile ??= Profiles
            .FirstOrDefault(x => ProfileKeysMatch(x.ProfileKey, profileKey));

        if (matchedProfile is null)
        {
            Profiles.Clear();
            ProfilesStatus = "Saved configuration not found for this unit.";
            return;
        }

        Profiles.Clear();
        Profiles.Add(matchedProfile);
        ApplySelectedProfileTopSummaries(matchedProfile);

        ProfilesStatus = "1 configuration loaded.";
    }

    public void ApplySelectedProfileTopSummaries(ViewerProfileItem matchedProfile)
    {
        var mergedEquipment = MergeSummaryAndUnique(EquipmentSummary, matchedProfile.UniqueEquipment);
        var mergedSkills = MergeSummaryAndUnique(SpecialSkillsSummary, matchedProfile.UniqueSkills);
        var mergedEquipmentValues = ExtractSummaryValues(mergedEquipment).ToList();
        var mergedSkillValues = ExtractSummaryValues(mergedSkills).ToList();

        EquipmentSummary = $"Equipment: {mergedEquipment}";
        SpecialSkillsSummary = $"Special Skills: {mergedSkills}";

        EquipmentSummaryFormatted = BuildNamedSummaryFormatted(
            "Equipment",
            mergedEquipmentValues,
            _currentEquipmentLookup,
            _currentEquipmentLinks,
            Color.FromArgb("#06B6D4"));
        SpecialSkillsSummaryFormatted = BuildNamedSummaryFormatted(
            "Special Skills",
            mergedSkillValues,
            _currentSkillsLookup,
            _currentSkillsLinks,
            Color.FromArgb("#F59E0B"));
    }

    public void ApplyCaptainStatBonuses(
        int ccBonus,
        int bsBonus,
        int phBonus,
        int wipBonus,
        int armBonus,
        int btsBonus,
        int vitalityBonus)
    {
        UnitCc = ApplyNumericBonus(UnitCc, ccBonus);
        UnitBs = ApplyNumericBonus(UnitBs, bsBonus);
        UnitPh = ApplyNumericBonus(UnitPh, phBonus);
        UnitWip = ApplyNumericBonus(UnitWip, wipBonus);
        UnitArm = ApplyNumericBonus(UnitArm, armBonus);
        UnitBts = ApplyNumericBonus(UnitBts, btsBonus);
        UnitVitality = ApplyNumericBonus(UnitVitality, vitalityBonus);
    }

    private static string MergeSummaryAndUnique(string summaryLine, string uniqueValues)
    {
        var merged = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var part in ExtractSummaryValues(summaryLine))
        {
            merged.Add(part);
        }

        foreach (var part in ExtractSummaryValues(uniqueValues))
        {
            merged.Add(part);
        }

        if (merged.Count == 0)
        {
            return "-";
        }

        return string.Join(", ", merged.OrderBy(x => x, StringComparer.OrdinalIgnoreCase));
    }

    private static IEnumerable<string> ExtractSummaryValues(string? summaryText)
    {
        if (string.IsNullOrWhiteSpace(summaryText))
        {
            return [];
        }

        var payload = summaryText;
        var colonIndex = summaryText.IndexOf(':');
        if (colonIndex >= 0 && colonIndex < summaryText.Length - 1)
        {
            payload = summaryText[(colonIndex + 1)..];
        }

        return payload
            .Split([',', '\r', '\n'], StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Where(x => !string.Equals(x, "-", StringComparison.Ordinal));
    }

    private static string ApplyNumericBonus(string value, int bonus)
    {
        if (bonus == 0)
        {
            return value;
        }

        return int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
            ? (parsed + bonus).ToString(CultureInfo.InvariantCulture)
            : value;
    }

    private static bool ProfileKeysMatch(string candidateKey, string requestedKey)
    {
        if (string.Equals(candidateKey, requestedKey, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return string.Equals(
            BuildLegacyProfileKey(candidateKey),
            BuildLegacyProfileKey(requestedKey),
            StringComparison.OrdinalIgnoreCase);
    }

    private static string BuildLegacyProfileKey(string profileKey)
    {
        if (string.IsNullOrWhiteSpace(profileKey))
        {
            return string.Empty;
        }

        var parts = profileKey.Split('|');
        if (parts.Length < 3)
        {
            return profileKey;
        }

        return $"{parts[0]}|{parts[1]}|{parts[2]}";
    }

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

        if (_metadataProvider is null)
        {
            Status = "Metadata service unavailable.";
            return;
        }

        try
        {
            IsLoading = true;
            Status = "Loading factions...";
            var factions = _metadataProvider.GetFactions(true);
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
                PackagedLogoPath = _factionLogoCacheService?.GetPackagedFactionLogoPath(faction.Id)
                    ?? $"SVGCache/factions/{faction.Id}.svg"
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
        ResetFireteamCounts();

        if (SelectedFaction is null)
        {
            UnitsStatus = "Select a faction.";
            return;
        }

        if (_armyImportProvider is null)
        {
            UnitsStatus = "Army data service unavailable.";
            return;
        }

        try
        {
            UnitsStatus = "Loading units...";
            var units = MercsOnlyUnits
                ? _factionProvider.GetResumeByFactionMercsOnly(SelectedFaction.Id)
                : _factionProvider.GetResumeByFaction(SelectedFaction.Id);

            var snapshot = _factionProvider.GetFactionSnapshot(SelectedFaction.Id);
            UpdateFireteamCounts(snapshot?.FireteamChartJson);
            var allowedFireteamSlugs = units
                .Select(x => x.Slug?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            var allowedFireteamNames = units
                .Select(x => x.Name?.Trim())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => NormalizeFireteamUnitName(x!))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
            UpdateFireteamTeams(
                snapshot?.FireteamChartJson,
                MercsOnlyUnits,
                allowedFireteamSlugs,
                allowedFireteamNames);
            var typeLookup = BuildIdNameLookup(snapshot?.FiltersJson, "type");
            var categoryLookup = BuildIdNameLookup(snapshot?.FiltersJson, "category");
            var skillsLookup = BuildIdNameLookup(snapshot?.FiltersJson, "skills");

            var filteredUnitIds = new HashSet<int>();
            foreach (var unit in units)
            {
                var unitRecord = _factionProvider.GetUnit(SelectedFaction.Id, unit.UnitId);
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
                .OrderBy(unit => ArmyUnitSort.GetUnitTypeSortIndex(unit.Type))
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
                    PackagedLogoPath = _factionLogoCacheService?.GetPackagedUnitLogoPath(SelectedFaction.Id, unit.UnitId)
                        ?? $"SVGCache/units/{SelectedFaction.Id}-{unit.UnitId}.svg"
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

    private void ResetFireteamCounts()
    {
        FireteamDuoCount = "-";
        FireteamHarisCount = "-";
        FireteamCoreCount = "-";
        Fireteams.Clear();
        FireteamsStatus = "No fireteams available.";
    }

    private void UpdateFireteamCounts(string? fireteamChartJson)
    {
        FireteamDuoCount = "-";
        FireteamHarisCount = "-";
        FireteamCoreCount = "-";
        if (string.IsNullOrWhiteSpace(fireteamChartJson))
        {
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(fireteamChartJson);
            if (!doc.RootElement.TryGetProperty("spec", out var specElement) ||
                specElement.ValueKind != JsonValueKind.Object)
            {
                return;
            }

            FireteamDuoCount = ReadFireteamCount(specElement, "DUO");
            FireteamHarisCount = ReadFireteamCount(specElement, "HARIS");
            FireteamCoreCount = ReadFireteamCount(specElement, "CORE");
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"UpdateFireteamCounts failed: {ex.Message}");
        }
    }

    private void UpdateFireteamTeams(
        string? fireteamChartJson,
        bool mercsOnlyFilterEnabled,
        IReadOnlySet<string>? allowedUnitSlugs = null,
        IReadOnlySet<string>? allowedUnitNames = null)
    {
        Fireteams.Clear();
        FireteamsStatus = "No fireteams available.";

        if (string.IsNullOrWhiteSpace(fireteamChartJson))
        {
            return;
        }

        try
        {
            using var doc = JsonDocument.Parse(fireteamChartJson);
            if (!doc.RootElement.TryGetProperty("teams", out var teamsElement) ||
                teamsElement.ValueKind != JsonValueKind.Array)
            {
                return;
            }

            foreach (var teamElement in teamsElement.EnumerateArray())
            {
                var teamName = ReadString(teamElement, "name", "Unnamed Team");
                var teamTypes = ReadTeamTypes(teamElement);
                var unitLimits = ReadUnitLimits(teamElement);
                if (mercsOnlyFilterEnabled)
                {
                    unitLimits = unitLimits
                        .Where(x => IsAllowedFireteamUnit(x, allowedUnitSlugs, allowedUnitNames))
                        .ToList();
                    if (unitLimits.Count == 0)
                    {
                        continue;
                    }
                }

                Fireteams.Add(new FireteamTeamItem
                {
                    Name = teamName,
                    TeamTypes = string.IsNullOrWhiteSpace(teamTypes) ? "-" : teamTypes,
                    UnitLimits = unitLimits
                });
            }

            FireteamsStatus = Fireteams.Count == 0
                ? "No fireteams available."
                : $"{Fireteams.Count} fireteams loaded.";
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"UpdateFireteamTeams failed: {ex.Message}");
            FireteamsStatus = $"Failed to parse fireteams: {ex.Message}";
        }
    }

    private static List<FireteamUnitLimitItem> ReadUnitLimits(JsonElement teamElement)
    {
        var results = new List<FireteamUnitLimitItem>();
        if (!teamElement.TryGetProperty("units", out var unitsElement) || unitsElement.ValueKind != JsonValueKind.Array)
        {
            return results;
        }

        foreach (var unitElement in unitsElement.EnumerateArray())
        {
            var unitName = ReadString(unitElement, "name", string.Empty);
            if (string.IsNullOrWhiteSpace(unitName))
            {
                unitName = ReadString(unitElement, "slug", "Unknown");
            }
            var unitSlug = ReadString(unitElement, "slug", string.Empty);

            var min = TryReadIntProperty(unitElement, "min", out var minValue) ? minValue : 0;
            var max = TryReadIntProperty(unitElement, "max", out var maxValue) ? maxValue : 0;

            results.Add(new FireteamUnitLimitItem
            {
                Name = unitName,
                Slug = unitSlug,
                Min = min.ToString(CultureInfo.InvariantCulture),
                Max = max.ToString(CultureInfo.InvariantCulture)
            });
        }

        return results;
    }

    private static string ReadTeamTypes(JsonElement teamElement)
    {
        if (!teamElement.TryGetProperty("type", out var typeElement))
        {
            return string.Empty;
        }

        if (typeElement.ValueKind == JsonValueKind.Array)
        {
            var types = typeElement.EnumerateArray()
                .Where(x => x.ValueKind == JsonValueKind.String)
                .Select(x => x.GetString())
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x!.Trim());

            return string.Join(", ", types);
        }

        if (typeElement.ValueKind == JsonValueKind.String)
        {
            return typeElement.GetString() ?? string.Empty;
        }

        return string.Empty;
    }

    private static bool TryReadIntProperty(JsonElement element, string propertyName, out int value)
    {
        if (!element.TryGetProperty(propertyName, out var propertyValue))
        {
            value = 0;
            return false;
        }

        return TryReadInt(propertyValue, out value);
    }

    private static string ReadString(JsonElement element, string propertyName, string fallback)
    {
        if (!element.TryGetProperty(propertyName, out var property) || property.ValueKind != JsonValueKind.String)
        {
            return fallback;
        }

        return property.GetString() ?? fallback;
    }

    private static bool IsAllowedFireteamUnit(
        FireteamUnitLimitItem unit,
        IReadOnlySet<string>? allowedUnitSlugs,
        IReadOnlySet<string>? allowedUnitNames)
    {
        if (!string.IsNullOrWhiteSpace(unit.Slug) &&
            allowedUnitSlugs is not null &&
            allowedUnitSlugs.Contains(unit.Slug))
        {
            return true;
        }

        if (allowedUnitNames is not null &&
            allowedUnitNames.Contains(NormalizeFireteamUnitName(unit.Name)))
        {
            return true;
        }

        return false;
    }

    private static string NormalizeFireteamUnitName(string value)
    {
        return Regex.Replace(value, @"\s+", " ").Trim().ToUpperInvariant();
    }

    private static string ReadFireteamCount(JsonElement specElement, string key)
    {
        foreach (var property in specElement.EnumerateObject())
        {
            if (!string.Equals(property.Name, key, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (TryReadInt(property.Value, out var rawValue))
            {
                return rawValue > 5 ? "T" : rawValue.ToString(CultureInfo.InvariantCulture);
            }

            return "-";
        }

        return "-";
    }

    private static bool TryReadInt(JsonElement element, out int value)
    {
        if (element.ValueKind == JsonValueKind.Number && element.TryGetInt32(out value))
        {
            return true;
        }

        if (element.ValueKind == JsonValueKind.String &&
            int.TryParse(element.GetString(), NumberStyles.Integer, CultureInfo.InvariantCulture, out value))
        {
            return true;
        }

        value = 0;
        return false;
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
        _currentEquipmentLookup = new Dictionary<int, string>();
        _currentEquipmentLinks = new Dictionary<int, string>();
        _currentSkillsLookup = new Dictionary<int, string>();
        _currentSkillsLinks = new Dictionary<int, string>();
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
        Infrastructure.Models.Database.Army.Resume unit,
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
        Color? textColor)
    {
        var list = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var formatted = new FormattedString();
        var labelSpan = new Span { Text = $"{label}: " };
        if (textColor is not null)
        {
            labelSpan.TextColor = textColor;
        }

        formatted.Spans.Add(labelSpan);

        if (list.Count == 0)
        {
            var emptySpan = new Span { Text = "-" };
            if (textColor is not null)
            {
                emptySpan.TextColor = textColor;
            }

            formatted.Spans.Add(emptySpan);
            return formatted;
        }

        for (var i = 0; i < list.Count; i++)
        {
            var value = list[i];
            var span = new Span { Text = value };
            if (textColor is not null)
            {
                span.TextColor = textColor;
            }

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
                var separatorSpan = new Span { Text = ", " };
                if (textColor is not null)
                {
                    separatorSpan.TextColor = textColor;
                }

                formatted.Spans.Add(separatorSpan);
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
        Regex.IsMatch(
            weaponName,
            @"\bccw\b|\bda ccw\b|\bap ccw\b|\bknife\b|\bsword\b|\bmonofilament\b|\bviral ccw\b|\bpistols?\b|\bclose combat weapon\b|\bcc\s*weapon\b|\bc\.?\s*c\.?\s*weapon\b|\bpara\s*cc\s*weapon\b",
            RegexOptions.IgnoreCase);

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

    private static List<(int Id, string Name)> BuildConfigurationSkillEntries(IEnumerable<(int Id, string Name)> rawEntries)
    {
        var normalized = new List<(int Id, string Name)>();
        foreach (var entry in rawEntries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                continue;
            }

            var skillName = entry.Name.Trim();
            if (!IsCommonSpecOpsSkill(skillName))
            {
                normalized.Add((entry.Id, skillName));
                continue;
            }

            var lieutenantDetail = ExtractLieutenantSkillDetail(skillName);
            if (!string.IsNullOrWhiteSpace(lieutenantDetail))
            {
                normalized.Add((0, lieutenantDetail));
            }
        }

        return normalized
            .GroupBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool IsCommonSpecOpsSkill(string? skillName)
    {
        if (string.IsNullOrWhiteSpace(skillName))
        {
            return false;
        }

        return skillName.Contains("lieutenant", StringComparison.OrdinalIgnoreCase) ||
               skillName.Contains("infinity spec-ops", StringComparison.OrdinalIgnoreCase) ||
               skillName.Contains("infinity spec ops", StringComparison.OrdinalIgnoreCase);
    }

    private static string? ExtractLieutenantSkillDetail(string skillName)
    {
        if (!skillName.Contains("lieutenant", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var detail = Regex.Replace(skillName, "lieutenant", string.Empty, RegexOptions.IgnoreCase).Trim();
        if (string.IsNullOrWhiteSpace(detail))
        {
            return null;
        }

        detail = detail.Trim('(', ')', '[', ']', '-', ':', ',', ';', ' ');
        return string.IsNullOrWhiteSpace(detail) ? null : detail;
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

    private static string ReadAdjustedOptionCost(JsonElement profileGroupsRoot, JsonElement group, JsonElement option)
    {
        var baseCostText = ReadOptionCost(option);
        if (!int.TryParse(
                baseCostText,
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out var baseCost))
        {
            return baseCostText;
        }

        var totalPeripheralCount = GetDisplayPeripheralEntriesForOption(profileGroupsRoot, group, option)
            .Sum(ReadEntryQuantity);
        if (totalPeripheralCount <= 1)
        {
            return baseCostText;
        }

        var minis = ReadOptionMinis(option);
        if (minis <= 1 || minis <= totalPeripheralCount)
        {
            return baseCostText;
        }

        if (baseCost <= 0 || baseCost % minis != 0)
        {
            return baseCostText;
        }

        var removedPeripheralCount = totalPeripheralCount - 1;
        var perModelCost = baseCost / minis;
        var deduction = removedPeripheralCount * perModelCost;
        var adjustedCost = Math.Max(0, baseCost - deduction);
        return adjustedCost.ToString(System.Globalization.CultureInfo.InvariantCulture);
    }

    private static IEnumerable<JsonElement> GetControllerPeripheralEntries(JsonElement group)
    {
        if (!group.TryGetProperty("profiles", out var profilesElement) || profilesElement.ValueKind != JsonValueKind.Array)
        {
            return [];
        }

        var collected = new List<JsonElement>();
        foreach (var profile in profilesElement.EnumerateArray())
        {
            if (profile.TryGetProperty("peripheral", out var peripheralElement) &&
                peripheralElement.ValueKind == JsonValueKind.Array &&
                peripheralElement.GetArrayLength() > 0)
            {
                collected.AddRange(peripheralElement.EnumerateArray().ToList());
            }
        }

        return collected;
    }

    private static HashSet<int> GetControllerPeripheralIds(JsonElement group)
    {
        var ids = new HashSet<int>();
        foreach (var entry in GetControllerPeripheralEntries(group))
        {
            if (TryParseId(entry, out var id))
            {
                ids.Add(id);
            }
        }

        return ids;
    }

    private static IEnumerable<JsonElement> GetFilteredOptionPeripheralEntries(
        JsonElement profileGroupsRoot,
        JsonElement group,
        JsonElement option)
    {
        var allowedIds = GetControllerPeripheralIds(group);
        var optionEntries = GetOptionEntriesWithIncludes(profileGroupsRoot, option, "peripheral").ToList();

        if (allowedIds.Count == 0)
        {
            return optionEntries;
        }

        return optionEntries
            .Where(entry => TryParseId(entry, out var id) && allowedIds.Contains(id))
            .ToList();
    }

    private static IEnumerable<JsonElement> GetDisplayPeripheralEntriesForOption(
        JsonElement profileGroupsRoot,
        JsonElement group,
        JsonElement option)
    {
        var optionEntries = GetFilteredOptionPeripheralEntries(profileGroupsRoot, group, option).ToList();
        if (optionEntries.Count > 0)
        {
            return optionEntries;
        }

        return GetControllerPeripheralEntries(group).ToList();
    }

    private static bool IsControllerGroup(JsonElement profileGroupsRoot, JsonElement group)
    {
        if (GetControllerPeripheralIds(group).Count > 0)
        {
            return true;
        }

        if (!group.TryGetProperty("options", out var optionsElement) || optionsElement.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        foreach (var option in optionsElement.EnumerateArray())
        {
            if (GetOptionEntriesWithIncludes(profileGroupsRoot, option, "peripheral").Any())
            {
                return true;
            }
        }

        return false;
    }

    private static int ReadOptionMinis(JsonElement option)
    {
        if (!option.TryGetProperty("minis", out var minisElement))
        {
            return 0;
        }

        if (minisElement.ValueKind == JsonValueKind.Number && minisElement.TryGetInt32(out var minisNumber))
        {
            return Math.Max(0, minisNumber);
        }

        if (minisElement.ValueKind == JsonValueKind.String &&
            int.TryParse(
                minisElement.GetString(),
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out var minisText))
        {
            return Math.Max(0, minisText);
        }

        return 0;
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

    private static List<(int Id, string Name)> GetCountedIdDisplayNamesFromEntries(
        IEnumerable<JsonElement> entriesSource,
        IReadOnlyDictionary<int, string> lookup,
        IReadOnlyDictionary<int, ExtraDefinition> extrasLookup,
        bool showUnitsInInches)
    {
        var counts = new Dictionary<string, (int Id, int Count)>(StringComparer.OrdinalIgnoreCase);
        foreach (var entry in entriesSource)
        {
            if (!TryParseId(entry, out var id))
            {
                continue;
            }

            var baseName = lookup.TryGetValue(id, out var resolvedName) ? resolvedName : id.ToString();
            var displayName = BuildEntryDisplayName(baseName, entry, extrasLookup, showUnitsInInches);
            if (string.IsNullOrWhiteSpace(displayName))
            {
                continue;
            }

            var quantity = ReadEntryQuantity(entry);
            if (counts.TryGetValue(displayName, out var existing))
            {
                counts[displayName] = (existing.Id, existing.Count + quantity);
            }
            else
            {
                counts[displayName] = (id, quantity);
            }
        }

        return counts
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .Select(x => (x.Value.Id, $"{x.Key} ({x.Value.Count})"))
            .ToList();
    }

    private static int ReadEntryQuantity(JsonElement entry)
    {
        if (entry.ValueKind != JsonValueKind.Object)
        {
            return 1;
        }

        if (!entry.TryGetProperty("q", out var quantityElement))
        {
            return 1;
        }

        if (quantityElement.ValueKind == JsonValueKind.Number && quantityElement.TryGetInt32(out var quantityNumber))
        {
            return Math.Max(1, quantityNumber);
        }

        if (quantityElement.ValueKind == JsonValueKind.String &&
            int.TryParse(
                quantityElement.GetString(),
                System.Globalization.NumberStyles.Integer,
                System.Globalization.CultureInfo.InvariantCulture,
                out var quantityText))
        {
            return Math.Max(1, quantityText);
        }

        return 1;
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
        if (!string.Equals(extraDefinition.Type, "DISTANCE", StringComparison.OrdinalIgnoreCase))
        {
            return extraDefinition.Name;
        }

        var valueText = showUnitsInInches
            ? ConvertDistanceTextToInches(extraDefinition.Name)
            : extraDefinition.Name;

        return AppendDistanceUnitSuffix(valueText, showUnitsInInches);
    }

    private static string AppendDistanceUnitSuffix(string distanceText, bool showUnitsInInches)
    {
        if (string.IsNullOrWhiteSpace(distanceText))
        {
            return distanceText;
        }

        if (Regex.IsMatch(distanceText, "(\"|cm)\\s*$", RegexOptions.IgnoreCase))
        {
            return distanceText;
        }

        var match = Regex.Match(distanceText, @"([+-]?\d+(?:\.\d+)?)");
        if (!match.Success)
        {
            return distanceText;
        }

        var suffix = showUnitsInInches ? "\"" : "cm";
        return string.Concat(
            distanceText.AsSpan(0, match.Index + match.Length),
            suffix,
            distanceText.AsSpan(match.Index + match.Length));
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

        if (_armyImportProvider is null)
        {
            ProfilesStatus = "Army data service unavailable.";
            return;
        }

        try
        {
            ProfilesStatus = "Loading profiles...";
            var unit = _factionProvider.GetUnit(SelectedFaction.Id, SelectedUnit.Id);
            var snapshot = _factionProvider.GetFactionSnapshot(SelectedFaction.Id);
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
            _currentEquipmentLookup = equipLookup;
            _currentEquipmentLinks = equipLinks;
            _currentSkillsLookup = skillsLookup;
            _currentSkillsLinks = skillsLinks;

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
            var hasControllerGroups = doc.RootElement.EnumerateArray().Any(usageGroup => IsControllerGroup(doc.RootElement, usageGroup));

            foreach (var usageGroup in doc.RootElement.EnumerateArray())
            {
                if (hasControllerGroups && !IsControllerGroup(doc.RootElement, usageGroup))
                {
                    continue;
                }

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

                    var usageSkillEntries = BuildConfigurationSkillEntries(
                        GetOrderedIdDisplayNamesFromEntries(
                            GetOptionEntriesWithIncludes(doc.RootElement, usageOption, "skills"),
                            skillsLookup,
                            extrasLookup,
                            ShowUnitsInInches));
                    foreach (var skillName in usageSkillEntries.Select(x => x.Name))
                    {
                        skillUsageCounts[skillName] = skillUsageCounts.TryGetValue(skillName, out var count) ? count + 1 : 1;
                    }
                }
            }

            var seenConfigurations = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var group in doc.RootElement.EnumerateArray())
            {
                if (hasControllerGroups && !IsControllerGroup(doc.RootElement, group))
                {
                    continue;
                }

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

                    var optionSkillsEntries = BuildConfigurationSkillEntries(
                            GetOrderedIdDisplayNamesFromEntries(
                                GetOptionEntriesWithIncludes(doc.RootElement, option, "skills"),
                                skillsLookup,
                                extrasLookup,
                                ShowUnitsInInches))
                        .ToList();
                    var uniqueSkillsEntries = optionSkillsEntries
                        .Where(x => skillUsageCounts.TryGetValue(x.Name, out var c) && c == 1)
                        .ToList();
                    var uniqueSkills = JoinOrDash(uniqueSkillsEntries.Select(x => x.Name));

                    var peripheralEntries = GetCountedIdDisplayNamesFromEntries(
                                GetDisplayPeripheralEntriesForOption(doc.RootElement, group, option),
                                peripheralLookup,
                                extrasLookup,
                                ShowUnitsInInches)
                            .ToList();
                    var peripherals = JoinOrDash(peripheralEntries.Select(x => x.Name));
                    var swc = ReadOptionSwc(option);
                    var cost = ReadAdjustedOptionCost(doc.RootElement, group, option);
                    var isLieutenant = IsLieutenantOption(option, skillsLookup);
                    var profileKey = $"{groupName}|{optionName}|{cost}|{swc}|lt:{(isLieutenant ? 1 : 0)}";

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
                    var peripheralLines = BuildLinkedLines(peripheralEntries.Select(x => (x.Id, x.Name)).ToList(), peripheralLinks);

                    Profiles.Add(new ViewerProfileItem
                    {
                        GroupName = groupName,
                        Name = displayName,
                        ProfileKey = profileKey,
                        IsLieutenant = isLieutenant,
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
                        Cost = cost,
                        ShowProfileTacticalAwarenessIcon = !ShowTacticalAwarenessIcon &&
                                                           optionSkillsEntries.Any(x => x.Name.Contains("tactical awareness", StringComparison.OrdinalIgnoreCase))
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
        if (_appSettingsProvider is null)
        {
            return;
        }

        try
        {
            var showInches = _appSettingsProvider.GetShowUnitsInInches();
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

public partial class ViewerFactionItem : ObservableObject, IViewerListItem
{
    public int Id { get; init; }

    public int ParentId { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Logo { get; init; }

    public string? CachedLogoPath { get; init; }

    public string? PackagedLogoPath { get; init; }

    public string? Subtitle => null;

    public bool HasSubtitle => false;

    [ObservableProperty]
    private bool isSelected;
}

public partial class ViewerUnitItem : ObservableObject, IViewerListItem
{
    public int Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? Logo { get; init; }

    public string? CachedLogoPath { get; init; }

    public string? PackagedLogoPath { get; init; }

    public string? Subtitle { get; init; }

    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

    private bool _isSelected;

    [ObservableProperty]
    private bool isSelected;
}

public class FireteamTeamItem
{
    public string Name { get; init; } = string.Empty;
    public string TeamTypes { get; init; } = "-";
    public IReadOnlyList<FireteamUnitLimitItem> UnitLimits { get; init; } = [];
}

public class FireteamUnitLimitItem
{
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Min { get; init; } = "0";
    public string Max { get; init; } = "0";
}

public partial class ViewerProfileItem : ObservableObject
{
    public string GroupName { get; init; } = string.Empty;

    public string Name { get; init; } = string.Empty;

    public string ProfileKey { get; init; } = string.Empty;

    public bool IsLieutenant { get; init; }

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
    public bool HasPeripheralStatBlock { get; init; }
    public string PeripheralNameHeading { get; init; } = string.Empty;
    public string PeripheralMov { get; init; } = "-";
    public string PeripheralCc { get; init; } = "-";
    public string PeripheralBs { get; init; } = "-";
    public string PeripheralPh { get; init; } = "-";
    public string PeripheralWip { get; init; } = "-";
    public string PeripheralArm { get; init; } = "-";
    public string PeripheralBts { get; init; } = "-";
    public string PeripheralVitalityHeader { get; init; } = "VITA";
    public string PeripheralVitality { get; init; } = "-";
    public string PeripheralS { get; init; } = "-";
    public string PeripheralAva { get; init; } = "-";
    public string PeripheralSubtitle { get; init; } = "-";
    public FormattedString PeripheralEquipmentLineFormatted { get; init; } = new();
    public bool HasPeripheralEquipmentLine { get; init; }
    public FormattedString PeripheralSkillsLineFormatted { get; init; } = new();
    public bool HasPeripheralSkillsLine { get; init; }

    public string Swc { get; init; } = "-";

    public string SwcDisplay { get; init; } = string.Empty;

    public string Cost { get; init; } = "-";
    public bool ShowProfileTacticalAwarenessIcon { get; init; }

    [ObservableProperty]
    private bool isVisible;

    [ObservableProperty]
    private bool isLieutenantBlocked;
}
