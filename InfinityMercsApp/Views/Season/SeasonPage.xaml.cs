using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Input;
using InfinityMercsApp.Domain.Models.Season;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Domain.Utilities;
using MauiShapes = Microsoft.Maui.Controls.Shapes;
using InfinityMercsApp.Services;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Common;
using InfinityMercsApp.Views.Controls;
using InfinityMercsApp.Infrastructure.Providers;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Svg.Skia;

namespace InfinityMercsApp.Views.Season;

public partial class SeasonPage : ContentPage, IQueryAttributable
{
    private const int TagCompanyFactionId = 2003;
    private const string TagCompanyFallbackIconPath = "SVGCache/MercsIcons/noun-battle-mech-1731140.svg";

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ViewerViewModel _viewerViewModel;
    private readonly FactionLogoCacheService? _factionLogoCacheService;
    private readonly IArmyDataService? _armyDataService;
    private readonly IStoreProvider? _storeProvider;
    private readonly IMetadataProvider? _metadataProvider;
    private readonly IAppSettingsProvider? _appSettingsProvider;
    private static readonly HashSet<string> InventoryExcludedStores = new(StringComparer.OrdinalIgnoreCase)
    {
        "Medical Services",
        "Additional Recruitment"
    };
    private IReadOnlyList<(string Name, string? AssociatedType, string Alignment, IReadOnlyList<string> AssociatedFactions)> _availableStores = [];
    private readonly Dictionary<string, int> _inventoryCounts = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, (int CostCr, double CostSwc)> _inventoryItemCosts = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<Action<int, double>> _marketplaceRecolorActions = [];
    private Button? _buyButton;
    private string? _temporaryStoreName;
    private int _temporaryStoreRoundIndex;
    private string? _pendingTemporaryStoreSelection;

    private static readonly Dictionary<string, string?> FactionAbbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        ["PanOceania"]                   = "PanO",
        ["White Company"]                = "White Co.",
        ["Yu Jing"]                      = "YJ",
        ["Nomads"]                       = "Nomads",
        ["Haqqislam"]                    = "Haqqislam",
        ["Druze"]                        = "Druze",
        ["Ariadna"]                      = "Ariadna",
        ["Dashat"]                       = "Dashat",
        ["Aleph"]                        = "ALEPH",
        ["O-12"]                         = "O12",
        ["Tohaa"]                        = "Tohaa",
        ["Combined Army"]                = "CA",
        ["Japanese Secessionist Army"]   = "JSA",
        // Sub-factions — omitted from label (null = skip)
        ["Shindenbutai"]                 = null,
        ["Oban"]                         = null,
        ["Hayabusa Jōrikusentai"]   = null,
        ["Ikari"]                        = null,
    };

    private static string BuildStorePickerLabel(string name, string? associatedType, string alignment, IReadOnlyList<string> associatedFactions)
    {
        if (string.Equals(name, "Medical Services", StringComparison.OrdinalIgnoreCase))
            return "🩺 Medical Services";

        if (string.Equals(name, "Additional Recruitment", StringComparison.OrdinalIgnoreCase))
            return "Additional Recruitment";

        if (associatedFactions.Count == 0 && string.Equals(alignment, "Neutral", StringComparison.OrdinalIgnoreCase))
            return $"{name} [Neutral]";

        var parts = new List<string>();

        var abbrevs = associatedFactions
            .Select(f => FactionAbbreviations.TryGetValue(f, out var a) ? a : f)
            .Where(a => a is not null)
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (abbrevs.Count > 0)
            parts.Add(string.Join("/", abbrevs));

        if (!string.IsNullOrWhiteSpace(associatedType))
            parts.Add(associatedType.Replace(", ", " | "));

        if (!string.IsNullOrWhiteSpace(alignment) &&
            !string.Equals(alignment, "Always Available", StringComparison.OrdinalIgnoreCase))
            parts.Add(alignment);

        return parts.Count > 0 ? $"{name} [{string.Join(", ", parts)}]" : name;
    }

    private static string StripTemporaryStoreSuffix(string storeName)
    {
        var m = Regex.Match(storeName, @"\s*-\s*Round\s+\d+\s+Store\s*$", RegexOptions.IgnoreCase);
        return m.Success ? storeName[..m.Index].Trim() : storeName;
    }

    // Strips picker decoration to get the canonical store name used for JSON lookup.
    // Handles: "Name [factions, type, alignment]", "+ Medical Services", "Name - Round X Store"
    private static string StripPickerDecoration(string label)
    {
        if (string.IsNullOrWhiteSpace(label)) return label;

        // "+ Medical Services" prefix
        var trimmed = label.TrimStart('+').Trim();

        // " - Round X Store" suffix
        trimmed = StripTemporaryStoreSuffix(trimmed);

        // " [faction, type, alignment]" suffix
        var bracketIdx = trimmed.LastIndexOf('[');
        if (bracketIdx > 0 && trimmed.EndsWith(']'))
            trimmed = trimmed[..bracketIdx].Trim();

        return trimmed;
    }

    private SKPicture? _pickerHeaderLogoPicture;
    private SKPicture? _playRoundFlagsPicture;
    private bool _unitPickerIsOpen;

    // Responsive font scaling
    private static readonly string[] ScaledFontKeys =
        ["FontSizeCaption", "FontSizeBody", "FontSizeSectionHeader", "FontSizeSubHeadline", "FontSizeTitleSmall"];
    private static readonly Dictionary<string, double> DesktopBaseFontSizes = new()
    {
        ["FontSizeCaption"]      = 12.0,
        ["FontSizeBody"]         = 14.0,
        ["FontSizeSectionHeader"]= 18.0,
        ["FontSizeSubHeadline"]  = 24.0,
        ["FontSizeTitleSmall"]   = 22.0,
    };
    private static readonly Dictionary<string, object> OriginalFontResources = new();
    private static double _lastFontScaleWidth = -1;
    private SKPicture? _regularOrderIconPicture;
    private SKPicture? _irregularOrderIconPicture;
    private SKPicture? _lieutenantOrderIconPicture;
    private SKPicture? _peripheralIconPicture;
    private SKPicture? _impetuousIconPicture;
    private SKPicture? _tacticalAwarenessIconPicture;
    private SKPicture? _cubeIconPicture;
    private SKPicture? _cube2IconPicture;
    private SKPicture? _hackableIconPicture;
    private SKPicture? _selectedUnitPicture;
    private int _selectedUnitLogoLoadVersion;
    private string? _companyFilePath;
    private string? _seasonFilePath;
    private bool _loadAttempted;
    private CompanyViewerUnitListItem? _selectedCompanyUnit;
    private SavedImprovedCaptainStats _loadedCaptainStats = new();

    private FormattedString _currentRangedWeaponsFormatted = BuildSimpleFormatted("-", Color.FromArgb("#F87171"));
    private FormattedString _currentMeleeWeaponsFormatted = BuildSimpleFormatted("-", Color.FromArgb("#34D399"));
    private FormattedString _currentPeripheralsFormatted = BuildSimpleFormatted("-", Color.FromArgb("#F59E0B"));
    private bool _hasCurrentPeripherals;
    private string _selectedCaptainNameHeading = string.Empty;
    private string _selectedProfileBaseNameHeading = string.Empty;
    private bool _hasSelectedProfileBaseNameHeading;
    private string _selectedUnitTypeHeading = string.Empty;
    private bool _hasSelectedUnitTypeHeading;
    private bool _showUnitsInInches = true;
    private string _seasonTitleBarText = "Season";
    private string _seasonResourcesTitleBarText = "0 CR - 0 SWC";

    public ObservableCollection<CompanyViewerUnitListItem> CompanyUnits { get; } = [];
    public ICommand SelectCompanyUnitCommand { get; }

    public string SeasonTitleBarText
    {
        get => _seasonTitleBarText;
        private set
        {
            if (_seasonTitleBarText == value) return;
            _seasonTitleBarText = value;
            OnPropertyChanged();
        }
    }

    public string SeasonResourcesTitleBarText
    {
        get => _seasonResourcesTitleBarText;
        private set
        {
            if (_seasonResourcesTitleBarText == value) return;
            _seasonResourcesTitleBarText = value;
            OnPropertyChanged();
        }
    }

    public string SelectedCaptainNameHeading
    {
        get => _selectedCaptainNameHeading;
        private set
        {
            if (_selectedCaptainNameHeading == value) return;
            _selectedCaptainNameHeading = value;
            OnPropertyChanged();
        }
    }

    public string SelectedProfileBaseNameHeading
    {
        get => _selectedProfileBaseNameHeading;
        private set
        {
            if (_selectedProfileBaseNameHeading == value) return;
            _selectedProfileBaseNameHeading = value;
            OnPropertyChanged();
        }
    }

    public string SelectedUnitTypeHeading
    {
        get => _selectedUnitTypeHeading;
        private set
        {
            if (_selectedUnitTypeHeading == value) return;
            _selectedUnitTypeHeading = value;
            OnPropertyChanged();
        }
    }

    public bool HasSelectedProfileBaseNameHeading
    {
        get => _hasSelectedProfileBaseNameHeading;
        private set
        {
            if (_hasSelectedProfileBaseNameHeading == value) return;
            _hasSelectedProfileBaseNameHeading = value;
            OnPropertyChanged();
        }
    }

    public bool HasSelectedUnitTypeHeading
    {
        get => _hasSelectedUnitTypeHeading;
        private set
        {
            if (_hasSelectedUnitTypeHeading == value) return;
            _hasSelectedUnitTypeHeading = value;
            OnPropertyChanged();
        }
    }

    public FormattedString CurrentRangedWeaponsFormatted
    {
        get => _currentRangedWeaponsFormatted;
        private set { _currentRangedWeaponsFormatted = value; OnPropertyChanged(); }
    }

    public FormattedString CurrentMeleeWeaponsFormatted
    {
        get => _currentMeleeWeaponsFormatted;
        private set { _currentMeleeWeaponsFormatted = value; OnPropertyChanged(); }
    }

    public FormattedString CurrentPeripheralsFormatted
    {
        get => _currentPeripheralsFormatted;
        private set { _currentPeripheralsFormatted = value; OnPropertyChanged(); }
    }

    public bool HasCurrentPeripherals
    {
        get => _hasCurrentPeripherals;
        private set
        {
            if (_hasCurrentPeripherals == value) return;
            _hasCurrentPeripherals = value;
            OnPropertyChanged();
        }
    }

    public SeasonPage(
        ViewerViewModel viewerViewModel,
        FactionLogoCacheService? factionLogoCacheService = null,
        IArmyDataService? armyDataService = null,
        IStoreProvider? storeProvider = null,
        IMetadataProvider? metadataProvider = null,
        IAppSettingsProvider? appSettingsProvider = null)
    {
        InitializeComponent();
        _viewerViewModel = viewerViewModel;
        _factionLogoCacheService = factionLogoCacheService;
        _armyDataService = armyDataService;
        _storeProvider = storeProvider;
        _metadataProvider = metadataProvider;
        _appSettingsProvider = appSettingsProvider;
        ApplyGlobalDisplayUnitsPreference();
        BindingContext = _viewerViewModel;
        SelectCompanyUnitCommand = new Command<CompanyViewerUnitListItem>(item => _ = SelectCompanyUnitAsync(item));
        _viewerViewModel.PropertyChanged += OnViewerViewModelPropertyChanged;
        _ = LoadHeaderIconsAsync();
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("companyFilePath", out var companyFileValue))
        {
            return;
        }

        _companyFilePath = Uri.UnescapeDataString(companyFileValue?.ToString() ?? string.Empty);
        _loadAttempted = false;

        // When loading an existing season the caller provides the season file path,
        // preventing a new season file from being created in LoadCompanyFromFileAsync.
        _seasonFilePath = query.TryGetValue("seasonFilePath", out var seasonFileValue)
            ? Uri.UnescapeDataString(seasonFileValue?.ToString() ?? string.Empty)
            : null;

        await UpdateSeasonTitleBarLabelAsync();
        await LoadCompanyFromFileAsync(_companyFilePath);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        CaptureOriginalFontSizes();
        ApplyResponsiveFontSizes(Width);
        var unitsPreferenceChanged = ApplyGlobalDisplayUnitsPreference();
        await UpdateSeasonTitleBarLabelAsync();
        await LoadPlayRoundFlagsIconAsync();
        if (!_loadAttempted && !string.IsNullOrWhiteSpace(_companyFilePath))
        {
            await LoadCompanyFromFileAsync(_companyFilePath);
        }
        else if (unitsPreferenceChanged && _selectedCompanyUnit is not null)
        {
            await SelectCompanyUnitAsync(_selectedCompanyUnit);
        }
        await RefreshMarketplaceResourcesAsync();
        if (InventoryTabContent.IsVisible)
            await RefreshInventoryAsync();
        await MaybeShowStoreSearchPopupAsync();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        RestoreOriginalFontSizes();
        _lastFontScaleWidth = -1;
    }

    protected override void OnSizeAllocated(double width, double height)
    {
        base.OnSizeAllocated(width, height);
        if (width > 0)
            ApplyResponsiveFontSizes(width);
    }

    private static void CaptureOriginalFontSizes()
    {
        if (OriginalFontResources.Count > 0) return;
        var resources = Application.Current?.Resources;
        if (resources is null) return;
        foreach (var key in ScaledFontKeys)
        {
            if (resources.TryGetValue(key, out var value))
                OriginalFontResources[key] = value;
        }
    }

    private static void RestoreOriginalFontSizes()
    {
        var resources = Application.Current?.Resources;
        if (resources is null) return;
        foreach (var (key, value) in OriginalFontResources)
            resources[key] = value;
    }

    private static void ApplyResponsiveFontSizes(double width)
    {
        if (width <= 0 || OriginalFontResources.Count == 0) return;
        if (Math.Abs(width - _lastFontScaleWidth) < 20) return;
        _lastFontScaleWidth = width;

        var scale = width >= 1000 ? 1.35 : width >= 700 ? 1.2 : width >= 500 ? 1.1 : 1.0;
        var resources = Application.Current?.Resources;
        if (resources is null) return;
        foreach (var (key, baseSize) in DesktopBaseFontSizes)
            resources[key] = Math.Round(baseSize * scale, 1);
    }

    private async Task LoadCompanyFromFileAsync(string? filePath)
    {
        ApplyGlobalDisplayUnitsPreference();
        _loadAttempted = true;
        CompanyUnits.Clear();
        _loadedCaptainStats = new SavedImprovedCaptainStats();

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            CompanyNameLabel.Text = "Season";
            CompanyTypeLabel.Text = string.Empty;
            SelectedCaptainNameHeading = string.Empty;
            SelectedProfileBaseNameHeading = string.Empty;
            HasSelectedProfileBaseNameHeading = false;
            SelectedUnitTypeHeading = string.Empty;
            HasSelectedUnitTypeHeading = false;
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var payload = JsonSerializer.Deserialize<SavedCompanyFile>(json, JsonOptions);
            if (payload is null)
            {
                CompanyNameLabel.Text = "Season";
                CompanyTypeLabel.Text = string.Empty;
                return;
            }

            var companyName = string.IsNullOrWhiteSpace(payload.CompanyName)
                ? Path.GetFileNameWithoutExtension(filePath)
                : payload.CompanyName;

            if (_seasonFilePath is null)
            {
                _seasonFilePath = await SeasonFileService.CreateSeasonFileAsync(
                    companyName,
                    payload.CompanyIdentifier ?? string.Empty,
                    filePath ?? string.Empty);
                await UpdateSeasonTitleBarLabelAsync();
            }

            CompanyNameLabel.Text = companyName;
            CompanyTypeLabel.Text = payload.CompanyType ?? string.Empty;

            var captainStats = payload.ImprovedCaptainStats ?? new SavedImprovedCaptainStats();
            _loadedCaptainStats = captainStats;
            var captainDisplayName = string.IsNullOrWhiteSpace(captainStats.CaptainName)
                ? "Captain"
                : captainStats.CaptainName.Trim();
            var captainWeaponChoices = captainStats.IsEnabled
                ? CollectCaptainChoices(captainStats.WeaponChoice1, captainStats.WeaponChoice2, captainStats.WeaponChoice3)
                : [];
            var captainSkillChoices = captainStats.IsEnabled
                ? CollectCaptainChoices(captainStats.SkillChoice1, captainStats.SkillChoice2, captainStats.SkillChoice3)
                : [];
            var captainEquipmentChoices = captainStats.IsEnabled
                ? CollectCaptainChoices(captainStats.EquipmentChoice1, captainStats.EquipmentChoice2, captainStats.EquipmentChoice3)
                : [];

            var orderedEntries = payload.Entries
                .OrderByDescending(x => x.IsLieutenant)
                .ThenBy(x => x.EntryIndex)
                .ToList();

            for (var i = 0; i < orderedEntries.Count; i++)
            {
                var entry = orderedEntries[i];
                var effectiveSourceFactionId = ResolveEffectiveSourceFactionId(entry);
                var baseUnitName = string.IsNullOrWhiteSpace(entry.BaseUnitName)
                    ? (string.IsNullOrWhiteSpace(entry.Name) ? $"Unit {i + 1}" : entry.Name)
                    : entry.BaseUnitName;
                var defaultDisplayName = entry.IsLieutenant ? captainDisplayName : baseUnitName;
                var displayName = string.IsNullOrWhiteSpace(entry.CustomName)
                    ? defaultDisplayName
                    : entry.CustomName.Trim();
                var subtitle = entry.IsPeripheralUnit
                    ? "Peripheral"
                    : (entry.IsLieutenant ? "Lieutenant" : string.Empty);
                var (savedRangedWeapons, savedCcWeapons) = ResolveSavedWeapons(effectiveSourceFactionId, entry);
                var savedSkills = ResolveSavedSkills(effectiveSourceFactionId, entry);
                var savedEquipment = ResolveSavedEquipment(effectiveSourceFactionId, entry);
                var savedCharacteristics = ResolveSavedCharacteristics(effectiveSourceFactionId, entry);
                if (entry.IsLieutenant && captainStats.IsEnabled)
                {
                    savedRangedWeapons = AppendChoices(savedRangedWeapons, captainWeaponChoices);
                    savedSkills = AppendChoices(savedSkills, captainSkillChoices);
                    savedEquipment = AppendChoices(savedEquipment, captainEquipmentChoices);
                }

                var logoSourceFactionId = ResolveLogoSourceFactionId(entry);
                var logoSourceUnitId = ResolveLogoSourceUnitId(entry);

                CompanyUnits.Add(new CompanyViewerUnitListItem
                {
                    Name = displayName,
                    EntryIndex = entry.EntryIndex,
                    BaseUnitName = baseUnitName,
                    BaseUnitDisplayName = BuildUnitBaseDisplayName(baseUnitName),
                    UnitTypeCode = NormalizeUnitTypeCode(entry.UnitTypeCode),
                    Subtitle = subtitle,
                    SourceFactionId = effectiveSourceFactionId,
                    VisualFactionId = logoSourceFactionId,
                    SourceUnitId = entry.SourceUnitId,
                    ProfileKey = entry.ProfileKey,
                    IsPeripheralUnit = entry.IsPeripheralUnit,
                    IsLieutenant = entry.IsLieutenant,
                    Cost = entry.Cost,
                    SavedEquipment = savedEquipment,
                    SavedSkills = savedSkills,
                    SavedCharacteristics = savedCharacteristics,
                    SavedRangedWeapons = savedRangedWeapons,
                    SavedCcWeapons = savedCcWeapons,
                    SavedPeripheralNameHeading = entry.PeripheralNameHeading,
                    UnitMov = SeasonDisplayUnitFormatter.FormatMoveValue(
                        entry.CurrentMov,
                        entry.CurrentMoveFirstCm,
                        entry.CurrentMoveSecondCm,
                        _showUnitsInInches),
                    UnitCc = entry.CurrentCc,
                    UnitBs = entry.CurrentBs,
                    UnitPh = entry.CurrentPh,
                    UnitWip = entry.CurrentWip,
                    UnitArm = entry.CurrentArm,
                    UnitBts = entry.CurrentBts,
                    UnitVitality = entry.CurrentVitaOrStr,
                    UnitS = entry.CurrentS,
                    ExperiencePoints = Math.Max(0, entry.ExperiencePoints),
                    CaptainIconPackagedPath = entry.IsLieutenant ? "SVGCache/NonCBIcons/noun-captain-8115950.svg" : string.Empty,
                    ExperienceIconPackagedPath = GetExperienceIconPackagedPath(entry.ExperiencePoints),
                    SelectionAccentColor = ResolveSelectionAccentColor(logoSourceFactionId),
                    CachedLogoPath = _factionLogoCacheService?.TryGetCachedUnitLogoPath(logoSourceFactionId, logoSourceUnitId),
                    PackagedLogoPath = _factionLogoCacheService?.GetPackagedUnitLogoPath(logoSourceFactionId, logoSourceUnitId)
                        ?? $"SVGCache/units/{logoSourceFactionId}-{logoSourceUnitId}.svg"
                });
            }

            BuildUnitPickerDropdown();
            await LoadGearAssignmentsAsync();
            if (CompanyUnits.Count > 0)
            {
                await SelectCompanyUnitAsync(CompanyUnits[0]);
            }

            await PopulateMarketplaceStoresAsync(payload.SourceFactions);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"SeasonPage failed to load company: {ex.Message}");
            CompanyNameLabel.Text = "Season";
            CompanyTypeLabel.Text = string.Empty;
            SelectedCaptainNameHeading = string.Empty;
            SelectedProfileBaseNameHeading = string.Empty;
            HasSelectedProfileBaseNameHeading = false;
            SelectedUnitTypeHeading = string.Empty;
            HasSelectedUnitTypeHeading = false;
        }
    }

    private Task SelectCompanyUnitAsync(CompanyViewerUnitListItem? item)
    {
        if (item is null)
        {
            return Task.CompletedTask;
        }

        ApplyGlobalDisplayUnitsPreference();

        foreach (var unit in CompanyUnits)
        {
            unit.IsSelected = ReferenceEquals(unit, item);
        }
        _selectedCompanyUnit = item;

        _viewerViewModel.ApplySavedUnitSnapshot(
            item.Name,
            item.UnitMov,
            item.UnitCc,
            item.UnitBs,
            item.UnitPh,
            item.UnitWip,
            item.UnitArm,
            item.UnitBts,
            InferVitalityHeader(item.UnitTypeCode),
            item.UnitVitality,
            item.UnitS,
            item.IsLieutenant);

        ApplySavedOrderIconOverrides(item);

        if (item.IsLieutenant && _loadedCaptainStats.IsEnabled)
        {
            _viewerViewModel.ApplyCaptainStatBonuses(
                _loadedCaptainStats.CcBonus,
                _loadedCaptainStats.BsBonus,
                _loadedCaptainStats.PhBonus,
                _loadedCaptainStats.WipBonus,
                _loadedCaptainStats.ArmBonus,
                _loadedCaptainStats.BtsBonus,
                _loadedCaptainStats.VitalityBonus);
        }

        _viewerViewModel.Profiles.Clear();
        var mergedProfile = BuildMergedProfileItem(item, null);
        _currentMergedProfile = mergedProfile;
        _viewerViewModel.Profiles.Add(mergedProfile);
        _viewerViewModel.ApplySelectedProfileTopSummaries(mergedProfile);
        _viewerViewModel.ApplyHackableOverrideFromCurrentConfiguration(
            mergedProfile.UniqueEquipment,
            mergedProfile.UniqueSkills);

        UpdatePickerHeader(item);
        SelectedCaptainNameHeading = item.Name;
        var baseHeading = string.IsNullOrWhiteSpace(item.BaseUnitName)
            ? (string.IsNullOrWhiteSpace(mergedProfile.Name) ? item.Name : mergedProfile.Name)
            : item.BaseUnitName;
        if (string.Equals(baseHeading?.Trim(), item.Name?.Trim(), StringComparison.OrdinalIgnoreCase))
        {
            SelectedProfileBaseNameHeading = string.Empty;
            HasSelectedProfileBaseNameHeading = false;
        }
        else
        {
            SelectedProfileBaseNameHeading = baseHeading ?? string.Empty;
            HasSelectedProfileBaseNameHeading = !string.IsNullOrWhiteSpace(baseHeading);
        }
        SelectedUnitTypeHeading = item.UnitTypeCode;
        HasSelectedUnitTypeHeading = !string.IsNullOrWhiteSpace(item.UnitTypeCode);
        ApplyUnitHeaderThemeForFaction(item.VisualFactionId > 0 ? item.VisualFactionId : item.SourceFactionId);
        ApplyProfileTraitIconOverrides(item, mergedProfile);
        var lieutenantIconCount = (mergedProfile.IsLieutenant ? 1 : 0) + CountBonusLieutenantOrders(mergedProfile.UniqueSkills);
        TeamUnitDisplayView.ShowLieutenantIcon = mergedProfile.IsLieutenant;
        TeamUnitDisplayView.LieutenantIconCount = lieutenantIconCount;

        UpdateCurrentWeaponsDisplay();
        _ = UpdateTeamTabSectionsAsync(mergedProfile, item);
        _ = LoadSelectedCompanyUnitLogoAsync(item);
        return Task.CompletedTask;
    }

    private void OnViewerViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ViewerViewModel.ShowRegularOrderIcon)
            or nameof(ViewerViewModel.ShowIrregularOrderIcon)
            or nameof(ViewerViewModel.ShowImpetuousIcon)
            or nameof(ViewerViewModel.ShowTacticalAwarenessIcon)
            or nameof(ViewerViewModel.ShowCubeIcon)
            or nameof(ViewerViewModel.ShowCube2Icon)
            or nameof(ViewerViewModel.ShowHackableIcon))
        {
            TeamUnitDisplayView.InvalidateHeaderIconsCanvas();
        }

        if (e.PropertyName == nameof(ViewerViewModel.SelectedUnit))
        {
            _ = LoadSelectedUnitLogoAsync(_viewerViewModel.SelectedUnit);
        }
    }

    private async Task LoadHeaderIconsAsync()
    {
        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/regular.svg");
            var svg = new SKSvg();
            _regularOrderIconPicture = svg.Load(stream);
        }
        catch (Exception ex) { Console.Error.WriteLine($"SeasonPage regular order icon load failed: {ex.Message}"); }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/irregular.svg");
            var svg = new SKSvg();
            _irregularOrderIconPicture = svg.Load(stream);
        }
        catch (Exception ex) { Console.Error.WriteLine($"SeasonPage irregular order icon load failed: {ex.Message}"); }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/lieutenant.svg");
            var svg = new SKSvg();
            _lieutenantOrderIconPicture = svg.Load(stream);
        }
        catch (Exception ex) { Console.Error.WriteLine($"SeasonPage lieutenant order icon load failed: {ex.Message}"); }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/peripheral.svg");
            var svg = new SKSvg();
            _peripheralIconPicture = svg.Load(stream);
        }
        catch (Exception ex) { Console.Error.WriteLine($"SeasonPage peripheral icon load failed: {ex.Message}"); }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/impetuous.svg");
            var svg = new SKSvg();
            _impetuousIconPicture = svg.Load(stream);
        }
        catch (Exception ex) { Console.Error.WriteLine($"SeasonPage impetuous icon load failed: {ex.Message}"); }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/tactical.svg");
            var svg = new SKSvg();
            _tacticalAwarenessIconPicture = svg.Load(stream);
        }
        catch (Exception ex) { Console.Error.WriteLine($"SeasonPage tactical awareness icon load failed: {ex.Message}"); }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/cube.svg");
            var svg = new SKSvg();
            _cubeIconPicture = svg.Load(stream);
        }
        catch (Exception ex) { Console.Error.WriteLine($"SeasonPage cube icon load failed: {ex.Message}"); }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/cube2.svg");
            var svg = new SKSvg();
            _cube2IconPicture = svg.Load(stream);
        }
        catch (Exception ex) { Console.Error.WriteLine($"SeasonPage cube2 icon load failed: {ex.Message}"); }

        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/hackable.svg");
            var svg = new SKSvg();
            _hackableIconPicture = svg.Load(stream);
        }
        catch (Exception ex) { Console.Error.WriteLine($"SeasonPage hackable icon load failed: {ex.Message}"); }

        TeamUnitDisplayView.RegularOrderIconPicture = _regularOrderIconPicture;
        TeamUnitDisplayView.IrregularOrderIconPicture = _irregularOrderIconPicture;
        TeamUnitDisplayView.LieutenantIconPicture = _lieutenantOrderIconPicture;
        TeamUnitDisplayView.PeripheralIconPicture = _peripheralIconPicture;
        TeamUnitDisplayView.ImpetuousIconPicture = _impetuousIconPicture;
        TeamUnitDisplayView.TacticalAwarenessIconPicture = _tacticalAwarenessIconPicture;
        TeamUnitDisplayView.CubeIconPicture = _cubeIconPicture;
        TeamUnitDisplayView.Cube2IconPicture = _cube2IconPicture;
        TeamUnitDisplayView.HackableIconPicture = _hackableIconPicture;
        TeamUnitDisplayView.InvalidateHeaderIconsCanvas();
    }

    private async Task LoadSelectedUnitLogoAsync(ViewerUnitItem? unit)
    {
        await LoadSelectedLogoPictureAsync(unit);
    }

    private async Task LoadSelectedCompanyUnitLogoAsync(CompanyViewerUnitListItem? item)
    {
        await LoadSelectedLogoPictureAsync(item);
    }

    private async Task LoadSelectedLogoPictureAsync(IViewerListItem? item)
    {
        var loadVersion = ++_selectedUnitLogoLoadVersion;
        SKPicture? loadedPicture = null;

        try
        {
            if (item is not null)
            {
                try
                {
                    var stream = await OpenBestLogoStreamAsync(item);
                    if (stream is not null)
                    {
                        await using (stream)
                        {
                            var svg = new SKSvg();
                            loadedPicture = svg.Load(stream);
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.Error.WriteLine($"SeasonPage selected unit logo load failed: {ex.Message}");
                }
            }

            if (loadVersion != _selectedUnitLogoLoadVersion)
            {
                loadedPicture?.Dispose();
                return;
            }

            _selectedUnitPicture?.Dispose();
            _selectedUnitPicture = loadedPicture;
            TeamUnitDisplayView.SelectedUnitPicture = _selectedUnitPicture;
            TeamUnitDisplayView.InvalidateSelectedUnitCanvas();
        }
        catch
        {
            loadedPicture?.Dispose();
            throw;
        }
    }

    private static async Task<Stream?> OpenBestLogoStreamAsync(IViewerListItem item)
    {
        if (!string.IsNullOrWhiteSpace(item.CachedLogoPath) && File.Exists(item.CachedLogoPath))
        {
            return File.OpenRead(item.CachedLogoPath);
        }

        if (!string.IsNullOrWhiteSpace(item.PackagedLogoPath))
        {
            foreach (var candidate in BuildPackagedCandidates(item.PackagedLogoPath))
            {
                try { return await FileSystem.Current.OpenAppPackageFileAsync(candidate); }
                catch { /* Try next candidate. */ }
            }
        }

        foreach (var fallback in BuildFallbackLogoCandidates(item))
        {
            try { return await FileSystem.Current.OpenAppPackageFileAsync(fallback); }
            catch { /* Try next fallback. */ }
        }

        return null;
    }

    private static IEnumerable<string> BuildPackagedCandidates(string packagedPath)
    {
        var normalized = packagedPath.Replace('\\', '/').TrimStart('/');
        yield return normalized;
        yield return normalized.ToLowerInvariant();
    }

    private static IEnumerable<string> BuildFallbackLogoCandidates(IViewerListItem item)
    {
        if (item is not CompanyViewerUnitListItem companyItem)
        {
            yield break;
        }

        var isTagCompanyUnit = companyItem.VisualFactionId == TagCompanyFactionId ||
                               companyItem.SourceFactionId == TagCompanyFactionId ||
                               companyItem.BaseUnitName.Contains("Repurposed Mining Equipment", StringComparison.OrdinalIgnoreCase) ||
                               companyItem.BaseUnitName.Contains("Turtlemek", StringComparison.OrdinalIgnoreCase);

        if (isTagCompanyUnit)
        {
            yield return TagCompanyFallbackIconPath;
        }
    }

    private void UpdateCurrentWeaponsDisplay()
    {
        var profile = _viewerViewModel.Profiles.FirstOrDefault();
        if (profile is null)
        {
            CurrentRangedWeaponsFormatted = BuildSimpleFormatted("-", Color.FromArgb("#F87171"));
            CurrentMeleeWeaponsFormatted = BuildSimpleFormatted("-", Color.FromArgb("#34D399"));
            CurrentPeripheralsFormatted = BuildSimpleFormatted("-", Color.FromArgb("#F59E0B"));
            HasCurrentPeripherals = false;
            return;
        }

        CurrentRangedWeaponsFormatted = BuildWeaponsFormatted(profile.RangedWeapons, Color.FromArgb("#F87171"));
        CurrentMeleeWeaponsFormatted = BuildWeaponsFormatted(profile.MeleeWeapons, Color.FromArgb("#34D399"));
        CurrentPeripheralsFormatted = profile.PeripheralsFormatted
            ?? BuildSimpleFormatted(profile.Peripherals, Color.FromArgb("#F59E0B"));
        HasCurrentPeripherals = !IsDashOrEmpty(profile.Peripherals);
    }

    private ViewerProfileItem BuildMergedProfileItem(CompanyViewerUnitListItem item, ViewerProfileItem? loadedProfile)
    {
        var rangedWeapons = MergeProfileSectionText(loadedProfile?.RangedWeapons, item.SavedRangedWeapons);
        var meleeWeapons = MergeProfileSectionText(loadedProfile?.MeleeWeapons, item.SavedCcWeapons);
        var uniqueEquipment = MergeProfileSectionText(loadedProfile?.UniqueEquipment, item.SavedEquipment);
        var isLieutenant = item.IsLieutenant || loadedProfile?.IsLieutenant == true;
        var uniqueSkills = MergeProfileSectionText(loadedProfile?.UniqueSkills, item.SavedSkills);
        var characteristics = MergeProfileSectionText(loadedProfile?.Characteristics, item.SavedCharacteristics);
        if (isLieutenant)
        {
            uniqueSkills = NormalizeLieutenantOrderEntries(uniqueSkills);
        }
        uniqueSkills = NormalizeSkillsForDisplay(uniqueSkills);
        uniqueEquipment = SeasonDisplayUnitFormatter.ConvertExplicitDistances(uniqueEquipment, _showUnitsInInches);
        uniqueSkills = SeasonDisplayUnitFormatter.ConvertExplicitDistances(uniqueSkills, _showUnitsInInches);
        characteristics = SeasonDisplayUnitFormatter.ConvertExplicitDistances(characteristics, _showUnitsInInches);
        var keepLoadedRangedFormatting = string.Equals(loadedProfile?.RangedWeapons?.Trim(), rangedWeapons.Trim(), StringComparison.OrdinalIgnoreCase);
        var keepLoadedMeleeFormatting = string.Equals(loadedProfile?.MeleeWeapons?.Trim(), meleeWeapons.Trim(), StringComparison.OrdinalIgnoreCase);
        var keepLoadedEquipmentFormatting = string.Equals(loadedProfile?.UniqueEquipment?.Trim(), uniqueEquipment.Trim(), StringComparison.OrdinalIgnoreCase);
        var keepLoadedSkillsFormatting = string.Equals(loadedProfile?.UniqueSkills?.Trim(), uniqueSkills.Trim(), StringComparison.OrdinalIgnoreCase);

        return new ViewerProfileItem
        {
            Name = loadedProfile?.Name ?? item.Name,
            NameFormatted = loadedProfile?.NameFormatted,
            RangedWeapons = rangedWeapons,
            RangedWeaponsFormatted = keepLoadedRangedFormatting && loadedProfile?.RangedWeaponsFormatted is not null
                ? loadedProfile.RangedWeaponsFormatted
                : BuildSimpleFormatted(rangedWeapons, Color.FromArgb("#F87171")),
            MeleeWeapons = meleeWeapons,
            MeleeWeaponsFormatted = keepLoadedMeleeFormatting && loadedProfile?.MeleeWeaponsFormatted is not null
                ? loadedProfile.MeleeWeaponsFormatted
                : BuildSimpleFormatted(meleeWeapons, Color.FromArgb("#34D399")),
            UniqueEquipment = uniqueEquipment,
            UniqueEquipmentFormatted = keepLoadedEquipmentFormatting && loadedProfile?.UniqueEquipmentFormatted is not null
                ? loadedProfile.UniqueEquipmentFormatted
                : BuildSimpleFormatted(uniqueEquipment, Color.FromArgb("#B5C0CE")),
            UniqueSkills = uniqueSkills,
            UniqueSkillsFormatted = keepLoadedSkillsFormatting && loadedProfile?.UniqueSkillsFormatted is not null
                ? loadedProfile.UniqueSkillsFormatted
                : BuildSimpleFormatted(uniqueSkills, Color.FromArgb("#F59E0B")),
            Characteristics = characteristics,
            Peripherals = loadedProfile?.Peripherals ?? BuildPeripheralDisplay(item.SavedPeripheralNameHeading),
            PeripheralsFormatted = loadedProfile?.PeripheralsFormatted,
            Cost = loadedProfile?.Cost ?? item.Cost.ToString(),
            Swc = loadedProfile?.Swc ?? "-",
            SwcDisplay = loadedProfile?.SwcDisplay ?? string.Empty,
            IsLieutenant = isLieutenant,
            ProfileKey = loadedProfile?.ProfileKey ?? item.ProfileKey
        };
    }

    private static string BuildPeripheralDisplay(string? peripheralHeading)
    {
        if (string.IsNullOrWhiteSpace(peripheralHeading))
        {
            return "-";
        }

        var name = CompanySelectionSharedUtilities.ExtractFirstPeripheralName(peripheralHeading);
        return string.IsNullOrWhiteSpace(name) ? "-" : $"{name} (1)";
    }

    private void ApplySavedOrderIconOverrides(CompanyViewerUnitListItem item)
    {
        if (!item.IsPeripheralUnit)
        {
            return;
        }

        var savedSkillLines = SplitProfileText(item.SavedSkills);
        var savedCharacteristicLines = SplitProfileText(item.SavedCharacteristics);
        var hasIrregular = savedSkillLines.Any(x => x.Contains("irregular", StringComparison.OrdinalIgnoreCase)) ||
                           savedCharacteristicLines.Any(x => x.Contains("irregular", StringComparison.OrdinalIgnoreCase));
        if (!hasIrregular)
        {
            return;
        }

        _viewerViewModel.SetOrderTypeIconState(showRegular: false, showIrregular: true);
    }

    private void ApplyProfileTraitIconOverrides(CompanyViewerUnitListItem item, ViewerProfileItem mergedProfile)
    {
        var characteristicsText = mergedProfile.Characteristics ?? string.Empty;
        var skillsText = mergedProfile.UniqueSkills ?? string.Empty;
        var equipmentText = mergedProfile.UniqueEquipment ?? string.Empty;
        var combined = NormalizeTraitText(string.Join(" ", characteristicsText, skillsText, equipmentText));

        var hasRegular = ContainsWholeWord(combined, "regular");
        var hasIrregular = ContainsWholeWord(combined, "irregular");
        var hasTacticalAwareness = Regex.IsMatch(combined, @"\btactical\s*awareness\b", RegexOptions.IgnoreCase);
        var hasCube2 = Regex.IsMatch(combined, @"\bcube\s*2(?:\.0)?\b|\bcube2(?:\.0)?\b", RegexOptions.IgnoreCase);
        var hasNegativeCube = Regex.IsMatch(combined, @"\b(no[\s-]*cube|without[\s-]*cube|cube[\s-]*none)\b", RegexOptions.IgnoreCase);
        var hasCube = !hasCube2 && !hasNegativeCube && ContainsWholeWord(combined, "cube");
        var hasHackable = IsHackableFromTraitText(combined);
        var hasPeripheral = item.IsPeripheralUnit ||
                            Regex.IsMatch(combined, @"\bservant\b", RegexOptions.IgnoreCase) ||
                            Regex.IsMatch(combined, @"\bancillary\b", RegexOptions.IgnoreCase);

        _viewerViewModel.SetOrderTypeIconState(showRegular: hasRegular, showIrregular: hasIrregular);
        _viewerViewModel.ApplyTacticalAwarenessOverride(hasTacticalAwareness);
        _viewerViewModel.SetTechTraitIconState(showCube: hasCube, showCube2: hasCube2, showHackable: hasHackable);
        TeamUnitDisplayView.ShowPeripheralIcon = hasPeripheral;
    }

    private void ApplyUnitHeaderThemeForFaction(int sourceFactionId)
    {
        var defaultPrimary = Controls.UnitDisplayConfigurationsView.DefaultHeaderPrimaryColor;
        var defaultSecondary = Controls.UnitDisplayConfigurationsView.DefaultHeaderSecondaryColor;
        var factionName = ResolveFactionThemeName(sourceFactionId);
        var colors = CompanySelectionVisualThemeWorkflow.GetHeaderColors(factionName, defaultPrimary, defaultSecondary);
        TeamUnitDisplayView.UnitHeaderPrimaryColor = colors.Primary;
        TeamUnitDisplayView.UnitHeaderSecondaryColor = colors.Secondary;
        TeamUnitDisplayView.UnitHeaderPrimaryTextColor = colors.PrimaryText;
        TeamUnitDisplayView.UnitHeaderSecondaryTextColor = colors.SecondaryText;
    }

    private Color ResolveSelectionAccentColor(int sourceFactionId)
    {
        var defaultAccent = Color.FromArgb("#93C5FD");
        var factionName = ResolveFactionThemeName(sourceFactionId);
        var (primary, _) = CompanySelectionVisualThemeWorkflow.GetFactionTheme(
            factionName,
            defaultAccent,
            Color.FromArgb("#4B5563"));
        return primary;
    }

    private string? ResolveFactionThemeName(int sourceFactionId)
    {
        if (sourceFactionId <= 0)
        {
            return null;
        }

        var metadataFactionName = _armyDataService?.GetMetadataFactionById(sourceFactionId)?.Name;
        if (!string.IsNullOrWhiteSpace(metadataFactionName) &&
            CompanySelectionSharedUtilities.IsThemeFactionName(metadataFactionName))
        {
            return metadataFactionName;
        }

        return CompanySelectionVisualThemeWorkflow.InferThemeFactionNameFromFactionId(sourceFactionId);
    }

    private string ResolveSavedSkills(int sourceFactionId, SavedCompanyEntry entry)
    {
        var names = ResolveCodeNames(sourceFactionId, entry.CurrentSkillCodes, "skills");
        names.AddRange(entry.CustomSkills ?? []);
        return JoinCodesOrDash(names);
    }

    private string ResolveSavedEquipment(int sourceFactionId, SavedCompanyEntry entry)
    {
        var names = ResolveCodeNames(sourceFactionId, entry.CurrentEquipmentCodes, "equip");
        names.AddRange(entry.CustomEquipment ?? []);
        return JoinCodesOrDash(names);
    }

    private string ResolveSavedCharacteristics(int sourceFactionId, SavedCompanyEntry entry)
    {
        var names = ResolveCodeNames(sourceFactionId, entry.CurrentCharacteristicCodes, "chars");
        names.AddRange(entry.CustomCharacteristics ?? []);
        return JoinCodesOrDash(names);
    }

    private (string SavedRangedWeapons, string SavedCcWeapons) ResolveSavedWeapons(int sourceFactionId, SavedCompanyEntry entry)
    {
        var currentWeapons = ResolveCodeNames(sourceFactionId, entry.CurrentWeaponCodes, "weapons")
            .Where(x => !string.IsNullOrWhiteSpace(x) && x.Trim() != "-")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        currentWeapons.AddRange((entry.CustomWeapons ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x) && x.Trim() != "-"));

        currentWeapons = currentWeapons
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (currentWeapons.Count == 0)
        {
            return ("-", "-");
        }

        var rangedWeapons = currentWeapons
            .Where(x => !CompanyProfileTextService.IsMeleeWeaponName(x))
            .ToList();
        var ccWeapons = currentWeapons
            .Where(CompanyProfileTextService.IsMeleeWeaponName)
            .ToList();

        return (JoinCodesOrDash(rangedWeapons), JoinCodesOrDash(ccWeapons));
    }

    private List<string> ResolveCodeNames(int sourceFactionId, IEnumerable<CompanySavedCodeRef> codes, string sectionName)
    {
        var codeList = (codes ?? [])
            .Where(x => x is not null && x.Id > 0)
            .ToList();
        if (codeList.Count == 0)
        {
            return [];
        }

        var lookup = BuildCodeNameLookup(sourceFactionId, sectionName);
        var extrasLookup = BuildExtraNameLookup(sourceFactionId);
        var resolved = new List<string>();
        foreach (var code in codeList)
        {
            if (!lookup.TryGetValue(code.Id, out var name) || string.IsNullOrWhiteSpace(name))
            {
                continue;
            }

            var display = name.Trim();
            var extras = (code.Extra ?? [])
                .Distinct()
                .Select(extraId => extrasLookup.TryGetValue(extraId, out var extraName) ? extraName?.Trim() : null)
                .Where(extraName => !string.IsNullOrWhiteSpace(extraName))
                .Cast<string>()
                .ToList();

            if (extras.Count > 0)
            {
                display = $"{display} ({string.Join(", ", extras)})";
            }

            resolved.Add(display);
        }

        return resolved;
    }

    private Dictionary<int, string> BuildCodeNameLookup(int sourceFactionId, string sectionName)
    {
        if (_armyDataService is null || sourceFactionId <= 0)
        {
            return [];
        }

        var snapshot = _armyDataService.GetFactionSnapshot(sourceFactionId);
        return CompanyUnitDetailsShared.BuildIdNameLookup(snapshot?.FiltersJson, sectionName);
    }

    private Dictionary<int, string> BuildExtraNameLookup(int sourceFactionId)
    {
        if (_armyDataService is null || sourceFactionId <= 0)
        {
            return [];
        }

        var snapshot = _armyDataService.GetFactionSnapshot(sourceFactionId);
        return CompanyUnitDetailsShared.BuildIdNameLookup(snapshot?.FiltersJson, "extras");
    }

    private static string JoinCodesOrDash(IEnumerable<string> values)
    {
        var lines = values
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Where(x => x != "-")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        return lines.Count == 0 ? "-" : string.Join(Environment.NewLine, lines);
    }

    private static FormattedString BuildSimpleFormatted(string? text, Color color)
    {
        var formatted = new FormattedString();
        formatted.Spans.Add(new Span
        {
            Text = string.IsNullOrWhiteSpace(text) ? "-" : text,
            TextColor = color
        });
        return formatted;
    }

    private FormattedString BuildWeaponsFormatted(string? text, Color color)
    {
        var formatted = new FormattedString();
        var lines = SplitProfileText(text);

        if (lines.Count == 0)
        {
            formatted.Spans.Add(new Span { Text = "-", TextColor = color });
            return formatted;
        }

        for (int i = 0; i < lines.Count; i++)
        {
            var weaponName = lines[i];
            var span = new Span { Text = weaponName, TextColor = color };
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) => ShowWeaponPopup(weaponName);
            span.GestureRecognizers.Add(tap);
            formatted.Spans.Add(span);

            if (i < lines.Count - 1)
                formatted.Spans.Add(new Span { Text = Environment.NewLine });
        }

        return formatted;
    }

    private void ShowWeaponPopup(string weaponName)
    {
        var baseName = Regex.Match(weaponName, @"^[^(]+").Value.Trim();
        PopupTitleLabel.Text = weaponName;
        PopupContentArea.Children.Clear();
        AppendWeaponDetails(baseName);
        MarketplacePopupOverlay.IsVisible = true;
    }

    private void AppendWeaponDetails(string weaponName)
    {
        var baseName = Regex.Match(weaponName, @"^[^(]+").Value.Trim();
        var weapons = FindAllWeaponsByName(baseName);
        foreach (var weapon in weapons)
        {
            PopupContentArea.Children.Add(new WeaponDetailCardView
            {
                Weapon = weapon,
                ShowUnitsInInches = _showUnitsInInches,
                Margin = new Thickness(0, 0, 0, 8)
            });
        }
    }

    public void UpdateSeasonTitleBarLabel(string? text)
    {
        SeasonTitleBarText = string.IsNullOrWhiteSpace(text) ? "Season" : text.Trim();
    }

    public void UpdateSeasonResourcesTitleBarLabel(string? text)
    {
        SeasonResourcesTitleBarText = string.IsNullOrWhiteSpace(text) ? "0 CR - 0 SWC" : text.Trim();
    }

    private async Task UpdateSeasonTitleBarLabelAsync()
    {
        var seasonFile = await SeasonFileService.LoadSeasonFileAsync(_seasonFilePath);
        var currentRound = SeasonFileService.ResolveCurrentRound(seasonFile);
        UpdateSeasonTitleBarLabel(currentRound == 0
            ? "Round 0"
            : $"Round {currentRound} Marketplace");
    }

    private async Task RefreshMarketplaceResourcesAsync()
    {
        var seasonFile = await SeasonFileService.LoadSeasonFileAsync(_seasonFilePath);
        var gross = SeasonFileService.ComputeAvailableResources(seasonFile);

        var cartCr = 0;
        var cartSwc = 0.0;
        foreach (var (key, count) in _inventoryCounts)
        {
            if (count <= 0) continue;
            if (!_inventoryItemCosts.TryGetValue(key, out var cost)) continue;
            cartCr += cost.CostCr * count;
            cartSwc += cost.CostSwc * count;
        }

        var remainingCr = gross.CreditsBalance - cartCr;
        var remainingSwc = gross.SwcBalance - cartSwc;

        var swcText = remainingSwc % 1 == 0
            ? ((int)remainingSwc).ToString()
            : remainingSwc.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
        UpdateSeasonResourcesTitleBarLabel($"{remainingCr} CR - {swcText} SWC");

        foreach (var recolor in _marketplaceRecolorActions)
            recolor(remainingCr, remainingSwc);

        if (_buyButton is not null)
        {
            var hasItems = _inventoryCounts.Any(kvp => kvp.Value > 0);
            _buyButton.IsEnabled = hasItems;
            _buyButton.Opacity = hasItems ? 1.0 : 0.45;
        }
    }

    // ── Gear slot assignment ───────────────────────────────────────────────
    private sealed class UnitGearSlots
    {
        public string? Primary;
        public string? Secondary;
        public string? Sidearm;
        public string? Accessories;
        public string? Roles;
        public string? Armor;
        public string? Augment;
    }

    private readonly Dictionary<int, UnitGearSlots> _unitGearSlots = new();
    private readonly Dictionary<string, string> _inventoryItemCategories = new(StringComparer.OrdinalIgnoreCase);
    private bool _suppressPickerEvents;
    private ViewerProfileItem? _currentMergedProfile;

    private sealed record InventoryCatalogItem(string Name, string Category, string StoreName, int CostCr = 0);

    private sealed class InventoryBucket
    {
        public InventoryBucket(string name, string category, int costCr = 0)
        {
            Name = name;
            Category = category;
            CostCr = costCr;
        }

        public string Name { get; }
        public string Category { get; }
        public int CostCr { get; set; }
        public int Count { get; set; }
        public SortedSet<string> Sources { get; } = new(StringComparer.OrdinalIgnoreCase);
        public SortedSet<int> Rounds { get; } = new();
    }

    private async Task RefreshInventoryAsync()
    {
        InventoryContentArea.Children.Clear();

        if (_storeProvider is null)
        {
            AddInventoryMessage("Inventory is unavailable.");
            return;
        }

        var seasonFile = await SeasonFileService.LoadSeasonFileAsync(_seasonFilePath);
        if (seasonFile is null)
        {
            AddInventoryMessage("No season inventory found.");
            return;
        }

        var catalogByStoreAndName = await BuildInventoryCatalogAsync();
        var catalogByName = catalogByStoreAndName.Values
            .GroupBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        var buckets = new Dictionary<string, InventoryBucket>(StringComparer.OrdinalIgnoreCase);

        void AddItem(string name, string category, string source, int costCr = 0, int? roundIndex = null)
        {
            if (string.IsNullOrWhiteSpace(name)) return;

            var normalizedCategory = NormalizeInventoryCategory(category);
            if (string.Equals(normalizedCategory, "Exchange", StringComparison.OrdinalIgnoreCase)) return;
            var key = $"{normalizedCategory}|{name.Trim()}";
            if (!buckets.TryGetValue(key, out var bucket))
            {
                bucket = new InventoryBucket(name.Trim(), normalizedCategory, costCr);
                buckets[key] = bucket;
            }

            if (costCr > 0 && bucket.CostCr == 0)
                bucket.CostCr = costCr;

            bucket.Count++;
            if (!string.IsNullOrWhiteSpace(source))
                bucket.Sources.Add(source.Trim());
            if (roundIndex.HasValue)
                bucket.Rounds.Add(roundIndex.Value);
        }

        void RemoveItem(string name)
        {
            var matchKey = buckets.Keys.FirstOrDefault(k =>
                k.EndsWith("|" + name.Trim(), StringComparison.OrdinalIgnoreCase));
            if (matchKey is not null && buckets[matchKey].Count > 0)
                buckets[matchKey].Count--;
        }

        void AddTransaction(SeasonTransaction transaction, int roundIndex)
        {
            if (transaction.IsSale)
            {
                RemoveItem(transaction.ItemName);
                return;
            }

            var storeName = NormalizeInventoryStoreName(transaction.OriginStore);
            if (InventoryExcludedStores.Contains(storeName)) return;

            var storeKey = MakeInventoryCatalogKey(storeName, transaction.ItemName);
            if (!catalogByStoreAndName.TryGetValue(storeKey, out var catalogItem) &&
                !catalogByName.TryGetValue(transaction.ItemName, out catalogItem))
            {
                return;
            }

            AddItem(catalogItem.Name, catalogItem.Category, catalogItem.StoreName, catalogItem.CostCr, roundIndex);
        }

        // Round 0 = the company's initial/setup purchases.
        foreach (var transaction in seasonFile.InitialPurchases.Transactions)
            AddTransaction(transaction, 0);

        foreach (var round in seasonFile.Rounds.OrderBy(round => round.RoundIndex))
        {
            foreach (var transaction in round.Marketplace.Transactions)
                AddTransaction(transaction, round.RoundIndex);

            var wonItemCount = round.Downtime.WonItems.Count;
            foreach (var wonItem in round.Downtime.WonItems)
            {
                var category = wonItem.Category;
                var wonCostCr = 0;
                if (catalogByName.TryGetValue(wonItem.Name, out var wonCatalogItem))
                {
                    if (string.IsNullOrWhiteSpace(category)) category = wonCatalogItem.Category;
                    wonCostCr = wonCatalogItem.CostCr;
                }

                AddItem(wonItem.Name, category, string.IsNullOrWhiteSpace(wonItem.Source) ? "Downtime" : wonItem.Source, wonCostCr, round.RoundIndex);
            }

            if (wonItemCount == 0 &&
                round.Downtime.OtherEffects.Contains("Random pistol", StringComparison.OrdinalIgnoreCase))
            {
                AddItem("Random pistol", "Sidearm", "Downtime", roundIndex: round.RoundIndex);
            }
        }

        if (buckets.Count == 0)
        {
            AddInventoryMessage("No inventory items yet.");
            return;
        }

        foreach (var group in buckets.Values
                     .Where(b => b.Count > 0)
                     .OrderBy(item => InventoryCategoryOrder(item.Category))
                     .ThenBy(item => item.Category)
                     .ThenBy(item => item.Name)
                     .GroupBy(item => item.Category))
        {
            InventoryContentArea.Children.Add(BuildSectionHeader(group.Key.ToUpperInvariant()));
            foreach (var item in group)
                InventoryContentArea.Children.Add(BuildInventoryRow(item));
        }
    }

    private async Task<Dictionary<string, InventoryCatalogItem>> BuildInventoryCatalogAsync()
    {
        var catalog = new Dictionary<string, InventoryCatalogItem>(StringComparer.OrdinalIgnoreCase);
        if (_storeProvider is null) return catalog;

        foreach (var storeName in _storeProvider.GetAllStoreNames())
        {
            var normalizedStoreName = NormalizeInventoryStoreName(storeName);
            if (InventoryExcludedStores.Contains(normalizedStoreName)) continue;

            var store = await _storeProvider.GetStoreByNameAsync(normalizedStoreName);
            if (store is null) continue;

            foreach (var item in store.Items)
            {
                if (string.IsNullOrWhiteSpace(item.Name)) continue;

                catalog[MakeInventoryCatalogKey(store.Name, item.Name)] = new InventoryCatalogItem(
                    item.Name.Trim(),
                    NormalizeInventoryCategory(item.Category),
                    store.Name,
                    item.CostCr);
            }

            // Troop types are sold as armour; a purchase records the "{ArmorName} ({Type})"
            // display name, so index them under the "Armor" category with that same name.
            foreach (var troop in store.TroopTypes)
            {
                if (string.IsNullOrWhiteSpace(troop.ArmorName)) continue;

                var armorName = string.IsNullOrWhiteSpace(troop.Type)
                    ? troop.ArmorName.Trim()
                    : $"{troop.ArmorName.Trim()} ({troop.Type})";
                catalog[MakeInventoryCatalogKey(store.Name, armorName)] = new InventoryCatalogItem(
                    armorName,
                    "Armor",
                    store.Name,
                    troop.CostCr);
            }

            // Augments are a separate store list with no category field; index them as the
            // "Augments" gear category so purchased augments show up in the inventory.
            foreach (var augment in store.Augments)
            {
                if (string.IsNullOrWhiteSpace(augment.Name)) continue;

                catalog[MakeInventoryCatalogKey(store.Name, augment.Name)] = new InventoryCatalogItem(
                    augment.Name.Trim(),
                    "Augments",
                    store.Name,
                    augment.CostCr);
            }
        }

        return catalog;
    }

    private void AddInventoryMessage(string text)
    {
        InventoryContentArea.Children.Add(new Label
        {
            Text = text,
            Style = (Style)Application.Current!.Resources["LabelBody"],
            TextColor = Color.FromArgb("#8A97A8"),
            HorizontalTextAlignment = TextAlignment.Center,
            Margin = new Thickness(0, 40, 0, 0)
        });
    }

    private Border BuildInventoryRow(InventoryBucket item)
    {
        var sellPrice = item.CostCr > 0 ? (int)Math.Ceiling(item.CostCr / 2.0) : 0;

        var nameLabel = new Label
        {
            Text = item.Name,
            Style = (Style)Application.Current!.Resources["LabelBody"],
            TextColor = Color.FromArgb("#E6EBF2"),
            VerticalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.WordWrap
        };

        var sourceLabel = new Label
        {
            Text = item.Sources.Count > 0 ? string.Join(", ", item.Sources) : string.Empty,
            Style = (Style)Application.Current!.Resources["LabelCaption"],
            TextColor = Color.FromArgb("#8A97A8"),
            IsVisible = item.Sources.Count > 0
        };

        var roundsNote = item.Rounds.Count == 0
            ? string.Empty
            : "Bought: " + (item.Rounds.Count == 1 ? "Round " : "Rounds ") + string.Join(", ", item.Rounds);
        var roundsLabel = new Label
        {
            Text = roundsNote,
            Style = (Style)Application.Current!.Resources["LabelCaption"],
            TextColor = Color.FromArgb("#B5C0CE"),
            IsVisible = item.Rounds.Count > 0
        };

        var textStack = new VerticalStackLayout { Spacing = 2, VerticalOptions = LayoutOptions.Center };
        textStack.Children.Add(nameLabel);
        textStack.Children.Add(sourceLabel);
        textStack.Children.Add(roundsLabel);

        var countLabel = new Label
        {
            Text = $"x{item.Count}",
            Style = (Style)Application.Current!.Resources["LabelBody"],
            TextColor = Color.FromArgb("#34D399"),
            FontAttributes = FontAttributes.Bold,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.End,
            MinimumWidthRequest = 32
        };

        var rightStack = new HorizontalStackLayout { Spacing = 8, VerticalOptions = LayoutOptions.Center };
        rightStack.Children.Add(countLabel);

        if (sellPrice > 0)
        {
            var sellBtn = new Button
            {
                Text = $"SELL ({sellPrice}cr)",
                BackgroundColor = Color.FromArgb("#3A4554"),
                TextColor = Color.FromArgb("#F59E0B"),
                BorderColor = Color.FromArgb("#F59E0B"),
                BorderWidth = 1,
                CornerRadius = 4,
                FontSize = 11,
                FontAttributes = FontAttributes.Bold,
                Padding = new Thickness(8, 4),
                HeightRequest = 36
            };
            var capturedItem = item;
            var capturedSellPrice = sellPrice;
            sellBtn.Clicked += async (_, _) =>
            {
                var confirmed = await DisplayAlert(
                    "Sell Equipment",
                    $"Sell {capturedItem.Name} for {capturedSellPrice} CR?\n(SWC is not refunded)",
                    "Sell", "Cancel");
                if (!confirmed) return;

                await SeasonFileService.UpdateLatestRoundAsync(_seasonFilePath, round =>
                {
                    round.Marketplace.Transactions.Add(new SeasonTransaction
                    {
                        OriginStore = capturedItem.Sources.FirstOrDefault() ?? string.Empty,
                        ItemName = capturedItem.Name,
                        CostCr = -capturedSellPrice,
                        IsSale = true
                    });
                });

                await RefreshMarketplaceResourcesAsync();
                await RefreshInventoryAsync();
                _ = RebuildGearPickersAsync();
            };
            rightStack.Children.Add(sellBtn);
        }

        var rowGrid = new Grid { MinimumHeightRequest = 52 };
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        Grid.SetColumn(textStack, 0);
        Grid.SetColumn(rightStack, 1);
        rowGrid.Children.Add(textStack);
        rowGrid.Children.Add(rightStack);

        return new Border
        {
            BackgroundColor = Color.FromArgb("#161B22"),
            Stroke = Color.FromArgb("#3A4554"),
            StrokeThickness = 1,
            Padding = new Thickness(12, 8),
            Margin = new Thickness(0, 0, 0, 6),
            StrokeShape = new MauiShapes.RoundRectangle { CornerRadius = 6 },
            Content = rowGrid
        };
    }

    private static string MakeInventoryCatalogKey(string storeName, string itemName) =>
        $"{NormalizeInventoryStoreName(storeName)}|{itemName.Trim()}";

    private static string NormalizeInventoryStoreName(string storeName)
    {
        return StripPickerDecoration((storeName ?? string.Empty).Trim());
    }

    private static string NormalizeInventoryCategory(string category) =>
        string.IsNullOrWhiteSpace(category) ? "General" : category.Trim();

    private static int InventoryCategoryOrder(string category) => category switch
    {
        "Primary" => 0,
        "Secondary" => 1,
        "Sidearm" => 2,
        "Accessories" => 3,
        "Roles" => 4,
        "Armor" => 5,
        "Augments" => 6,
        "Exchange" => 7,
        "General" => 99,
        _ => 50
    };

    private bool ApplyGlobalDisplayUnitsPreference()
    {
        var showUnitsInInches = SeasonDisplayUnitFormatter.GetShowUnitsInInches(_appSettingsProvider);
        var changed = _showUnitsInInches != showUnitsInInches;
        _showUnitsInInches = showUnitsInInches;
        _viewerViewModel.ShowUnitsInInches = showUnitsInInches;
        return changed;
    }

    private IReadOnlyList<Domain.Models.Metadata.Weapon> FindAllWeaponsByName(string name)
    {
        var matches = _metadataProvider?.SearchWeaponsByName(name) ?? [];
        var exact = matches.Where(w => string.Equals(w.Name, name, StringComparison.OrdinalIgnoreCase)).ToList();
        return exact.Count > 0 ? exact : [.. matches];
    }

    private static string BuildPropertyWikiUrl(string propertyName)
    {
        var baseName = Regex.Match(propertyName, @"^[^(]+").Value.Trim();
        return $"https://infinitythewiki.com/{baseName.Replace(' ', '_')}?version=n4";
    }

    private Dictionary<string, string?> BuildSkillsWikiLookup()
    {
        var skills = _metadataProvider?.GetSkills() ?? [];
        var lookup = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var s in skills)
        {
            var url = string.IsNullOrWhiteSpace(s.Wiki) ? BuildPropertyWikiUrl(s.Name) : s.Wiki;
            lookup.TryAdd(s.Name.Trim(), url);
        }
        return lookup;
    }

    private static string ExtractSkillBaseName(string ability)
    {
        // Strip parenthetical level/modifier, then strip trailing non-word chars (e.g. asterisks)
        var beforeParen = Regex.Match(ability, @"^[^(]+").Value.Trim();
        return Regex.Replace(beforeParen, @"\W+$", string.Empty).Trim();
    }

    private static bool IsStatDash(string? value) =>
        string.IsNullOrWhiteSpace(value) || value.Trim() == "-";

    private static async Task OpenLinkAsync(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return;
        try
        {
            await Launcher.Default.OpenAsync(url);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"Failed to open link '{url}': {ex.Message}");
        }
    }

    private string NormalizeSkillsForDisplay(string? skillsText)
    {
        var result = new List<string>();
        var seenCanonical = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var dodgeExtras = new List<string>();
        var dodgeExtrasSeen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var hasPlainDodge = false;

        foreach (var rawLine in SplitProfileText(skillsText))
        {
            var line = rawLine.Trim();
            var dodgeMatch = Regex.Match(line, @"^\s*dodge(?:\s*\((?<extra>.+)\))?\s*$", RegexOptions.IgnoreCase);
            if (dodgeMatch.Success)
            {
                var extra = dodgeMatch.Groups["extra"].Value.Trim();
                if (string.IsNullOrWhiteSpace(extra))
                {
                    hasPlainDodge = true;
                }
                else
                {
                    extra = Regex.Replace(extra, @"\s+", " ").Trim();
                    if (dodgeExtrasSeen.Add(extra))
                    {
                        dodgeExtras.Add(extra);
                    }
                }

                continue;
            }

            var canonical = NormalizeSkillLineForDedup(line);
            if (seenCanonical.Add(canonical))
            {
                result.Add(line);
            }
        }

        if (dodgeExtras.Count > 0)
        {
            foreach (var extra in dodgeExtras)
            {
                var dodgeLine = $"Dodge ({extra})";
                if (seenCanonical.Add(NormalizeSkillLineForDedup(dodgeLine)))
                {
                    result.Add(dodgeLine);
                }
            }
        }
        else if (hasPlainDodge)
        {
            const string dodgeLine = "Dodge";
            if (seenCanonical.Add(NormalizeSkillLineForDedup(dodgeLine)))
            {
                result.Add(dodgeLine);
            }
        }

        return result.Count == 0 ? "-" : string.Join(Environment.NewLine, result);
    }

    private static string NormalizeSkillLineForDedup(string line)
    {
        if (string.IsNullOrWhiteSpace(line))
        {
            return string.Empty;
        }

        var normalizedLine = Regex.Replace(line.Trim(), @"\s+", " ");
        var match = Regex.Match(normalizedLine, @"^(?<name>[^()]+?)\s*\((?<args>[^()]*)\)\s*$");
        if (!match.Success)
        {
            return normalizedLine;
        }

        var name = match.Groups["name"].Value.Trim();
        var args = match.Groups["args"].Value
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return args.Count == 0
            ? name
            : $"{name} ({string.Join(", ", args)})";
    }

    private static string NormalizeLieutenantOrderEntries(string? skillsText)
    {
        var lines = SplitProfileText(skillsText);
        if (lines.Count == 0)
        {
            return "-";
        }

        for (var i = 0; i < lines.Count; i++)
        {
            lines[i] = Regex.Replace(
                lines[i],
                @"\+(\d+)\s*(?:regular\s*)?orders?\b",
                "+$1 Lt Order",
                RegexOptions.IgnoreCase);
        }

        var detailedLtOrderValues = new HashSet<int>();
        foreach (var line in lines)
        {
            var detailMatches = Regex.Matches(
                line,
                @"\blieutenant\b[^\n\r]*\(\s*\+(\d+)\s*(?:lt|lieutenant)?\s*orders?\s*\)",
                RegexOptions.IgnoreCase);
            foreach (Match match in detailMatches)
            {
                if (match.Groups.Count < 2 || !int.TryParse(match.Groups[1].Value, out var value))
                {
                    continue;
                }

                detailedLtOrderValues.Add(Math.Max(0, value));
            }
        }

        var deduped = new List<string>(lines.Count);
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var line in lines)
        {
            var standaloneLtOrderMatch = Regex.Match(
                line,
                @"^\+(\d+)\s*(?:lt|lieutenant)\s*orders?\s*$",
                RegexOptions.IgnoreCase);
            if (standaloneLtOrderMatch.Success &&
                standaloneLtOrderMatch.Groups.Count >= 2 &&
                int.TryParse(standaloneLtOrderMatch.Groups[1].Value, out var standaloneValue) &&
                detailedLtOrderValues.Contains(Math.Max(0, standaloneValue)))
            {
                continue;
            }

            if (seen.Add(line))
            {
                deduped.Add(line);
            }
        }

        return deduped.Count == 0 ? "-" : string.Join(Environment.NewLine, deduped);
    }

    private static List<string> SplitProfileText(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        return text
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x) && x != "-")
            .ToList();
    }

    private static string MergeProfileSectionText(string? loadedText, string? savedText)
    {
        var lines = SplitProfileText(loadedText);
        var existing = new HashSet<string>(lines, StringComparer.OrdinalIgnoreCase);
        foreach (var savedLine in SplitProfileText(savedText))
        {
            if (existing.Add(savedLine))
            {
                lines.Add(savedLine);
            }
        }

        return lines.Count == 0 ? "-" : string.Join(Environment.NewLine, lines);
    }

    private static string AppendChoices(string? baseText, IReadOnlyList<string> additions)
    {
        var lines = SplitProfileText(baseText);
        var existing = new HashSet<string>(lines, StringComparer.OrdinalIgnoreCase);
        foreach (var addition in additions)
        {
            if (existing.Add(addition))
            {
                lines.Add(addition);
            }
        }

        return lines.Count == 0 ? "-" : string.Join(Environment.NewLine, lines);
    }

    private static int CountBonusLieutenantOrders(string? uniqueSkills)
    {
        if (string.IsNullOrWhiteSpace(uniqueSkills))
        {
            return 0;
        }

        var total = 0;
        foreach (var line in SplitProfileText(uniqueSkills))
        {
            if (!line.Contains("lt order", StringComparison.OrdinalIgnoreCase) &&
                !line.Contains("lieutenant order", StringComparison.OrdinalIgnoreCase) &&
                !line.Contains("lieutenant", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var matches = Regex.Matches(
                line,
                @"^\s*\+(\d+)\s*(?:lt|lieutenant)\s*orders?\s*$",
                RegexOptions.IgnoreCase);
            foreach (Match match in matches)
            {
                if (match.Groups.Count < 2 || !int.TryParse(match.Groups[1].Value, out var value))
                {
                    continue;
                }

                total += Math.Max(0, value);
            }

            var lieutenantDetailMatches = Regex.Matches(
                line,
                @"\blieutenant\b[^\n\r]*\(\s*\+(\d+)\s*(?:(?:lt|lieutenant)\s*)?orders?\s*\)",
                RegexOptions.IgnoreCase);
            foreach (Match match in lieutenantDetailMatches)
            {
                if (match.Groups.Count < 2 || !int.TryParse(match.Groups[1].Value, out var value))
                {
                    continue;
                }

                total += Math.Max(0, value);
            }
        }

        return total;
    }

    private static List<string> CollectCaptainChoices(params string[] choices)
    {
        return choices
            .Select(NormalizeCaptainChoice)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeCaptainChoice(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var trimmed = value.Trim();
        if (trimmed == "-")
        {
            return string.Empty;
        }

        return Regex.Replace(trimmed, @"^\s*\([-+]?\d+\)\s*-\s*", string.Empty).Trim();
    }

    private static string GetExperienceIconPackagedPath(int experiencePoints)
    {
        var level = CompanyUnitExperienceRanks.GetRankLevel(experiencePoints);
        return level <= 0 ? string.Empty : $"SVGCache/NonCBIcons/Experience/noun-{level}-stars.svg";
    }

    private static string NormalizeUnitTypeCode(string? unitTypeCode)
    {
        if (string.IsNullOrWhiteSpace(unitTypeCode))
        {
            return string.Empty;
        }

        var normalized = unitTypeCode.Trim().ToUpperInvariant();
        return normalized == "MOV" ? string.Empty : normalized;
    }

    private static string BuildUnitBaseDisplayName(string? baseUnitName)
    {
        if (string.IsNullOrWhiteSpace(baseUnitName))
        {
            return "Unit";
        }

        var withoutParens = Regex.Replace(baseUnitName, @"\s*\([^)]*\)\s*", " ").Trim();
        var collapsed = Regex.Replace(withoutParens, @"\s{2,}", " ").Trim();
        return string.IsNullOrWhiteSpace(collapsed) ? "Unit" : collapsed;
    }

    private static string InferVitalityHeader(string? unitTypeCode)
    {
        var normalized = unitTypeCode?.Trim().ToUpperInvariant();
        return normalized is "TAG" or "REM" or "PERIPHERAL" ? "STR" : "VITA";
    }

    private static int ResolveEffectiveSourceFactionId(SavedCompanyEntry entry)
    {
        if (entry.SourceFactionId == TagCompanyFactionId || entry.LogoSourceFactionId == TagCompanyFactionId)
        {
            return TagCompanyFactionId;
        }

        var baseName = entry.BaseUnitName ?? string.Empty;
        if (baseName.Contains("Repurposed Mining Equipment", StringComparison.OrdinalIgnoreCase) ||
            baseName.Contains("Turtlemek", StringComparison.OrdinalIgnoreCase))
        {
            return TagCompanyFactionId;
        }

        return entry.SourceFactionId;
    }

    private static int ResolveLogoSourceFactionId(SavedCompanyEntry entry)
    {
        return entry.LogoSourceFactionId > 0 ? entry.LogoSourceFactionId : entry.SourceFactionId;
    }

    private static int ResolveLogoSourceUnitId(SavedCompanyEntry entry)
    {
        return entry.LogoSourceUnitId > 0 ? entry.LogoSourceUnitId : entry.SourceUnitId;
    }

    private static bool IsDashOrEmpty(string? text)
    {
        return string.IsNullOrWhiteSpace(text) || text.Trim() == "-";
    }

    private static bool IsHackableFromTraitText(string normalizedText)
    {
        return Regex.IsMatch(normalizedText, @"\bhackable\b", RegexOptions.IgnoreCase) ||
               Regex.IsMatch(normalizedText, @"\bhacking\s*device\b", RegexOptions.IgnoreCase) ||
               Regex.IsMatch(normalizedText, @"\bkiller\s*hacking\s*device\b", RegexOptions.IgnoreCase) ||
               Regex.IsMatch(normalizedText, @"\bevo\s*hacking\s*device\b", RegexOptions.IgnoreCase) ||
               Regex.IsMatch(normalizedText, @"\bhacking\s*device\s*plus\b|\bhd\s*\+\b", RegexOptions.IgnoreCase) ||
               Regex.IsMatch(normalizedText, @"\bkhd\b|\bevo\s*hd\b", RegexOptions.IgnoreCase);
    }

    private static bool ContainsWholeWord(string text, string word)
    {
        return Regex.IsMatch(text, $@"\b{Regex.Escape(word)}\b", RegexOptions.IgnoreCase);
    }

    private static string NormalizeTraitText(string value)
    {
        var lowered = value.ToLowerInvariant();
        var sb = new System.Text.StringBuilder(lowered.Length);
        foreach (var c in lowered)
        {
            sb.Append(char.IsLetterOrDigit(c) || c == '.' ? c : ' ');
        }

        return Regex.Replace(sb.ToString(), @"\s+", " ").Trim();
    }

    private async Task PopulateMarketplaceStoresAsync(IReadOnlyList<SavedCompanyFaction> sourceFactions)
    {
        StoreSelectorPicker.Items.Clear();
        StoreContentArea.Children.Clear();

        if (_storeProvider is null || _armyDataService is null)
            return;

        var allFactions = _armyDataService.GetMetadataFactions();
        var teamFactions = sourceFactions
            .Select(sf => _armyDataService.GetMetadataFactionById(sf.FactionId))
            .OfType<InfinityMercsApp.Domain.Models.Metadata.Faction>()
            .ToList();

        var factionNames = FactionResolver.GetExpandedFactionNames(teamFactions, allFactions);
        _availableStores = await _storeProvider.GetAvailableStoresAsync(factionNames.ToList());

        // Split "always at end" stores from regular stores.
        var endStoreNames = new HashSet<string>(new[] { "Medical Services", "Additional Recruitment" }, StringComparer.OrdinalIgnoreCase);
        var regularStores = _availableStores.Where(s => !endStoreNames.Contains(s.Name)).ToList();
        var endStores     = _availableStores.Where(s => endStoreNames.Contains(s.Name))
                                            .OrderBy(s => s.Name.Equals("Medical Services", StringComparison.OrdinalIgnoreCase) ? 0 : 1)
                                            .ToList();

        foreach (var (name, type, alignment, factions) in regularStores)
            StoreSelectorPicker.Items.Add(BuildStorePickerLabel(name, type, alignment, factions));

        // Append the latest round's temporary store as an extra entry (if any).
        var seasonFile = await SeasonFileService.LoadSeasonFileAsync(_seasonFilePath);
        var latestTempRound = seasonFile?.Rounds
            .OrderByDescending(r => r.RoundIndex)
            .FirstOrDefault(r => !string.IsNullOrWhiteSpace(r.TemporaryStore));
        var latestTemp = latestTempRound?.TemporaryStore;

        if (!string.IsNullOrWhiteSpace(latestTemp) &&
            !_availableStores.Any(s => string.Equals(s.Name, latestTemp, StringComparison.OrdinalIgnoreCase)))
        {
            _temporaryStoreName = latestTemp;
            _temporaryStoreRoundIndex = latestTempRound?.RoundIndex ?? 0;
            StoreSelectorPicker.Items.Add($"{latestTemp} - Round {_temporaryStoreRoundIndex} Store");
        }
        else
        {
            _temporaryStoreName = null;
            _temporaryStoreRoundIndex = 0;
        }

        // Medical Services and Additional Recruitment always last.
        foreach (var (name, type, alignment, factions) in endStores)
            StoreSelectorPicker.Items.Add(BuildStorePickerLabel(name, type, alignment, factions));

        if (StoreSelectorPicker.Items.Count > 0)
            StoreSelectorPicker.SelectedIndex = 0;

        await PopulateItemCategoryCacheAsync();
    }

    private async void OnStoreSelectorPickerChanged(object? sender, EventArgs e)
    {
        if (StoreSelectorPicker.SelectedIndex < 0) return;
        var storeName = StoreSelectorPicker.Items[StoreSelectorPicker.SelectedIndex];
        await SelectStoreAsync(storeName);
    }

    private async Task SelectStoreAsync(string storeName)
    {
        StoreContentArea.Children.Clear();
        _marketplaceRecolorActions.Clear();
        MarketplacePopupOverlay.IsVisible = false;

        // Strip picker decorations to get the canonical store name for lookup.
        var lookupName = StripPickerDecoration(storeName);

        var store = await _storeProvider!.GetStoreByNameAsync(lookupName);
        if (store is null) return;

        // Notoriety adjusts CR price for Lawful (+surcharge) and Chaotic (-discount) markets.
        var isLawful  = string.Equals(store.Alignment, "Lawful",  StringComparison.OrdinalIgnoreCase);
        var isChaotic = string.Equals(store.Alignment, "Chaotic", StringComparison.OrdinalIgnoreCase);
        var notoriety = 0;
        var rawNotoriety = 0;
        if (isLawful || isChaotic)
        {
            var sf = await SeasonFileService.LoadSeasonFileAsync(_seasonFilePath);
            rawNotoriety = SeasonFileService.ComputeCompanyNotoriety(sf);
            // Chaotic: positive notoriety = discount (invert sign before passing to ApplyNotorietyToCost)
            notoriety = isChaotic ? -rawNotoriety : rawNotoriety;
        }

        var storeHeaderGrid = new Grid { Margin = new Thickness(0, 0, 0, 2) };
        storeHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        storeHeaderGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

        var storeNameLabel = new Label
        {
            Text = store.Name,
            Style = (Style)Application.Current!.Resources["LabelTitleSmall"],
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#E6EBF2"),
            VerticalTextAlignment = TextAlignment.Center
        };

        _buyButton = new Button
        {
            Text = "BUY",
            BackgroundColor = Color.FromArgb("#2A6B57"),
            TextColor = Color.FromArgb("#34D399"),
            BorderColor = Color.FromArgb("#34D399"),
            BorderWidth = 1,
            CornerRadius = 6,
            Style = (Style)Application.Current!.Resources["ButtonCaption"],
            Padding = new Thickness(12, 6),
            HeightRequest = 36,
            IsEnabled = false,
            Opacity = 0.45
        };
        _buyButton.Clicked += OnBuyClicked;

        Grid.SetColumn(_buyButton, 1);
        storeHeaderGrid.Children.Add(storeNameLabel);
        storeHeaderGrid.Children.Add(_buyButton);
        StoreContentArea.Children.Add(storeHeaderGrid);

        var alignmentText = store.Alignment;
        if ((isLawful || isChaotic) && rawNotoriety != 0)
            alignmentText += $"  (Notoriety: {(notoriety >= 0 ? "+" : "")}{notoriety} CR)";

        if (!string.IsNullOrWhiteSpace(alignmentText))
        {
            StoreContentArea.Children.Add(new Label
            {
                Text = alignmentText,
                Style = (Style)Application.Current!.Resources["LabelBody"],
                TextColor = Color.FromArgb("#8A97A8"),
                Margin = new Thickness(0, 0, 0, 10)
            });
        }

        var itemsByCategory = store.Items
            .GroupBy(x => string.IsNullOrWhiteSpace(x.Category) ? "General" : x.Category,
                     StringComparer.OrdinalIgnoreCase);

        foreach (var group in itemsByCategory)
        {
            StoreContentArea.Children.Add(BuildSectionHeader(group.Key.ToUpperInvariant()));
            foreach (var item in group)
            {
                var effectiveCr = ApplyNotorietyToCost(item.CostCr, notoriety);
                StoreContentArea.Children.Add(
                    BuildItemRow(storeName, item.Name, effectiveCr, item.CostSwc.HasValue ? (double?)(double)item.CostSwc.Value : null, () => ShowItemPopup(item)));
            }
        }

        if (store.TroopTypes.Count > 0)
        {
            StoreContentArea.Children.Add(BuildSectionHeader("ARMOR UPGRADES"));
            foreach (var troop in store.TroopTypes)
            {
                var effectiveTroopCr = ApplyNotorietyToCost(troop.CostCr, notoriety);
                StoreContentArea.Children.Add(BuildArmorRow(storeName, troop, effectiveTroopCr));
            }
        }

        if (store.Augments.Count > 0)
        {
            StoreContentArea.Children.Add(BuildSectionHeader("AUGMENTS"));
            foreach (var augment in store.Augments)
            {
                var effectiveAugmentCr = ApplyNotorietyToCost(augment.CostCr, notoriety);
                StoreContentArea.Children.Add(
                    BuildItemRow(storeName, augment.Name, effectiveAugmentCr, null, () => ShowAugmentPopup(augment)));
            }
        }

        await RefreshMarketplaceResourcesAsync();
        if (InventoryTabContent.IsVisible)
            await RefreshInventoryAsync();
    }

    private static Label BuildSectionHeader(string text) => new()
    {
        Text = text,
        Style = (Style)Application.Current!.Resources["LabelCaption"],
        FontAttributes = FontAttributes.Bold,
        TextColor = Color.FromArgb("#8A97A8"),
        Margin = new Thickness(0, 10, 0, 2)
    };

    // Builds a standard item/augment row: left = name + cost (tappable), right = + count -
    private Border BuildItemRow(string storeName, string itemName, int costCr, double? costSwc, Action onTap)
    {
        var key = $"{storeName}|{itemName}";
        _inventoryItemCosts[key] = (costCr, costSwc ?? 0);

        var costText = costSwc.HasValue ? $"{costCr}cr / {costSwc}swc" : $"{costCr}cr";

        var nameLabel = new Label { Text = itemName, Style = (Style)Application.Current!.Resources["LabelBody"], TextColor = Color.FromArgb("#E6EBF2") };
        var costLabel = new Label { Text = costText, Style = (Style)Application.Current!.Resources["LabelCaption"], TextColor = Color.FromArgb("#34D399") };

        var leftStack = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Spacing = 2 };
        leftStack.Children.Add(nameLabel);
        leftStack.Children.Add(costLabel);
        leftStack.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(onTap) });

        var (border, plusBtn) = BuildRowBorder(key, leftStack);

        _marketplaceRecolorActions.Add((remainingCr, remainingSwc) =>
        {
            var affordable = costCr <= remainingCr && (costSwc ?? 0) <= remainingSwc;
            nameLabel.TextColor = affordable ? Color.FromArgb("#E6EBF2") : Color.FromArgb("#F87171");
            costLabel.TextColor = affordable ? Color.FromArgb("#34D399") : Color.FromArgb("#F87171");
            plusBtn.IsEnabled = affordable;
        });

        return border;
    }

    private static int ApplyNotorietyToCost(int baseCost, int notoriety)
    {
        if (notoriety == 0) return baseCost;
        var effective = baseCost + notoriety;
        var minimum = (int)Math.Ceiling(baseCost / 2.0);
        return Math.Max(effective, minimum);
    }

    // Builds an armor row: left = name + type + ARM/BTS + abilities (tappable), right = + count -
    private Border BuildArmorRow(string storeName, StoreTroopType troop, int effectiveCostCr = -1)
    {
        var displayName = string.IsNullOrWhiteSpace(troop.Type)
            ? troop.ArmorName
            : $"{troop.ArmorName} ({troop.Type})";
        var armText = troop.Arm.HasValue ? troop.Arm.Value.ToString() : "—";
        var btsText = troop.Bts.HasValue ? troop.Bts.Value.ToString() : "—";
        var costCr = effectiveCostCr >= 0 ? effectiveCostCr : troop.CostCr;

        var key = $"{storeName}|{displayName}";
        _inventoryItemCosts[key] = (costCr, 0);

        var nameLabel = new Label { Text = displayName, Style = (Style)Application.Current!.Resources["LabelBody"], TextColor = Color.FromArgb("#E6EBF2") };
        var costLabel = new Label
        {
            Text = $"{costCr}cr",
            Style = (Style)Application.Current!.Resources["LabelCaption"],
            TextColor = Color.FromArgb("#34D399")
        };

        var leftStack = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Spacing = 2 };
        leftStack.Children.Add(nameLabel);
        leftStack.Children.Add(new Label
        {
            Text = $"ARM {armText}  BTS {btsText}",
            Style = (Style)Application.Current!.Resources["LabelCaption"],
            TextColor = Color.FromArgb("#8A97A8")
        });
        if (!string.IsNullOrWhiteSpace(troop.Abilities))
        {
            leftStack.Children.Add(new Label
            {
                Text = troop.Abilities,
                Style = (Style)Application.Current!.Resources["LabelCaption"],
                TextColor = Color.FromArgb("#8A97A8"),
                LineBreakMode = LineBreakMode.WordWrap
            });
        }
        leftStack.Children.Add(costLabel);
        leftStack.GestureRecognizers.Add(
            new TapGestureRecognizer { Command = new Command(() => ShowArmorPopup(troop)) });

        var (border, plusBtn) = BuildRowBorder(key, leftStack);

        _marketplaceRecolorActions.Add((remainingCr, _) =>
        {
            var affordable = costCr <= remainingCr;
            nameLabel.TextColor = affordable ? Color.FromArgb("#E6EBF2") : Color.FromArgb("#F87171");
            costLabel.TextColor = affordable ? Color.FromArgb("#34D399") : Color.FromArgb("#F87171");
            plusBtn.IsEnabled = affordable;
        });

        return border;
    }

    // Shared core: wraps a left-content view in a bordered row with + count - controls on the right.
    private (Border Border, Button PlusButton) BuildRowBorder(string key, View leftContent)
    {
        var countLabel = new Label
        {
            Text = _inventoryCounts.GetValueOrDefault(key, 0).ToString(),
            Style = (Style)Application.Current!.Resources["LabelBody"],
            TextColor = Color.FromArgb("#E6EBF2"),
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            MinimumWidthRequest = 28
        };

        var plusBtn = new Button
        {
            Text = "+",
            BackgroundColor = Color.FromArgb("#3A4554"),
            TextColor = Color.FromArgb("#34D399"),
            BorderColor = Color.FromArgb("#3A4554"),
            BorderWidth = 1,
            CornerRadius = 4,
            Style = (Style)Application.Current!.Resources["ButtonSubHeadline"],
            WidthRequest = 44,
            HeightRequest = 44,
            Padding = new Thickness(0)
        };
        plusBtn.Clicked += (_, _) =>
        {
            _inventoryCounts[key] = _inventoryCounts.GetValueOrDefault(key, 0) + 1;
            countLabel.Text = _inventoryCounts[key].ToString();
            _ = RefreshMarketplaceResourcesAsync();
        };

        var minusBtn = new Button
        {
            Text = "−",
            BackgroundColor = Color.FromArgb("#3A4554"),
            TextColor = Color.FromArgb("#F87171"),
            BorderColor = Color.FromArgb("#3A4554"),
            BorderWidth = 1,
            CornerRadius = 4,
            Style = (Style)Application.Current!.Resources["ButtonSubHeadline"],
            WidthRequest = 44,
            HeightRequest = 44,
            Padding = new Thickness(0)
        };
        minusBtn.Clicked += (_, _) =>
        {
            var current = _inventoryCounts.GetValueOrDefault(key, 0);
            if (current <= 0) return;
            _inventoryCounts[key] = current - 1;
            countLabel.Text = _inventoryCounts[key].ToString();
            _ = RefreshMarketplaceResourcesAsync();
        };

        var controls = new HorizontalStackLayout { VerticalOptions = LayoutOptions.Center, Spacing = 6 };
        controls.Children.Add(plusBtn);
        controls.Children.Add(countLabel);
        controls.Children.Add(minusBtn);

        var rowGrid = new Grid { MinimumHeightRequest = 52 };
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
        rowGrid.Children.Add(leftContent);
        Grid.SetColumn(controls, 1);
        rowGrid.Children.Add(controls);

        var border = new Border
        {
            Content = rowGrid,
            Stroke = Color.FromArgb("#3A4554"),
            StrokeThickness = 1,
            StrokeShape = new MauiShapes.RoundRectangle { CornerRadius = new CornerRadius(4) },
            Padding = new Thickness(12, 6, 8, 6),
            Margin = new Thickness(0, 0, 0, 3)
        };
        return (border, plusBtn);
    }

    private void ShowItemPopup(StoreItem item)
    {
        PopupTitleLabel.Text = item.Name;
        PopupContentArea.Children.Clear();

        AddPopupRow("Cost", item.CostSwc.HasValue
            ? $"{item.CostCr}cr / {item.CostSwc}swc"
            : $"{item.CostCr}cr");
        AddPopupRow("Category", item.Category);

        AppendWeaponDetails(item.Name);

        MarketplacePopupOverlay.IsVisible = true;
    }

    private void ShowArmorPopup(StoreTroopType troop)
    {
        var displayName = string.IsNullOrWhiteSpace(troop.Type)
            ? troop.ArmorName
            : $"{troop.ArmorName} ({troop.Type})";
        PopupTitleLabel.Text = displayName;
        PopupContentArea.Children.Clear();

        AddPopupRow("Cost", $"{troop.CostCr}cr");
        AddPopupRow("ARM", troop.Arm.HasValue ? troop.Arm.Value.ToString() : "—");
        AddPopupRow("BTS", troop.Bts.HasValue ? troop.Bts.Value.ToString() : "—");

        if (!string.IsNullOrWhiteSpace(troop.Abilities))
        {
            PopupContentArea.Children.Add(new Label
            {
                Text = "ABILITIES & EQUIPMENT",
                Style = (Style)Application.Current!.Resources["LabelCaption"],
                FontAttributes = FontAttributes.Bold,
                TextColor = Color.FromArgb("#8A97A8"),
                Margin = new Thickness(0, 8, 0, 4)
            });

            var skillsLookup = BuildSkillsWikiLookup();
            var abilities = troop.Abilities
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var ability in abilities)
            {
                var baseName = ExtractSkillBaseName(ability);
                var isSkill = skillsLookup.TryGetValue(baseName, out var wikiUrl);

                var label = new Label
                {
                    Text = $"• {ability}",
                    Style = (Style)Application.Current!.Resources["LabelBody"],
                    TextColor = isSkill ? Color.FromArgb("#B5C0CE") : Color.FromArgb("#E6EBF2"),
                    TextDecorations = isSkill ? TextDecorations.Underline : TextDecorations.None,
                    LineBreakMode = LineBreakMode.WordWrap
                };

                if (isSkill && !string.IsNullOrWhiteSpace(wikiUrl))
                {
                    var url = wikiUrl;
                    var tap = new TapGestureRecognizer();
                    tap.Tapped += async (_, _) => await OpenLinkAsync(url);
                    label.GestureRecognizers.Add(tap);
                }

                PopupContentArea.Children.Add(label);
            }
        }

        MarketplacePopupOverlay.IsVisible = true;
    }

    private void ShowAugmentPopup(StoreAugment augment)
    {
        PopupTitleLabel.Text = augment.Name;
        PopupContentArea.Children.Clear();

        AddPopupRow("Cost", $"{augment.CostCr}cr");
        if (!string.IsNullOrWhiteSpace(augment.Requirement))
            AddPopupRow("Requires", augment.Requirement);
        if (!string.IsNullOrWhiteSpace(augment.CostNote))
            AddPopupRow("Note", augment.CostNote!);

        MarketplacePopupOverlay.IsVisible = true;
    }

    private void AddPopupRow(string label, string value)
    {
        var row = new Grid();
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(90) });
        row.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        row.Margin = new Thickness(0, 2);

        var labelView = new Label
        {
            Text = label,
            Style = (Style)Application.Current!.Resources["LabelBody"],
            TextColor = Color.FromArgb("#8A97A8"),
            VerticalTextAlignment = TextAlignment.Center
        };
        var valueView = new Label
        {
            Text = value,
            Style = (Style)Application.Current!.Resources["LabelBody"],
            TextColor = Color.FromArgb("#E6EBF2"),
            VerticalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.WordWrap
        };
        Grid.SetColumn(valueView, 1);
        row.Children.Add(labelView);
        row.Children.Add(valueView);
        PopupContentArea.Children.Add(row);
    }

    private void OnPopupBackClicked(object sender, EventArgs e)
    {
        MarketplacePopupOverlay.IsVisible = false;
    }

    private static void DrawPictureOnCanvas(SKPaintSurfaceEventArgs e, SKPicture? picture)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        if (picture is null) return;
        var bounds = picture.CullRect;
        if (bounds.Width <= 0 || bounds.Height <= 0) return;
        var scale = Math.Min(e.Info.Width / bounds.Width, e.Info.Height / bounds.Height);
        var x = (e.Info.Width - bounds.Width * scale) / 2f;
        var y = (e.Info.Height - bounds.Height * scale) / 2f;
        canvas.Translate(x, y);
        canvas.Scale(scale);
        canvas.DrawPicture(picture);
    }

    private void BuildUnitPickerDropdown()
    {
        UnitPickerDropdownStack.Children.Clear();
        foreach (var unit in CompanyUnits)
            UnitPickerDropdownStack.Children.Add(BuildUnitPickerItem(unit));
    }

    private View BuildUnitPickerItem(CompanyViewerUnitListItem unit)
    {
        SKPicture? logoPicture = null;

        var logoCanvas = new SKCanvasView { WidthRequest = 36, HeightRequest = 36 };
        logoCanvas.PaintSurface += (_, e) => DrawPictureOnCanvas(e, logoPicture);
        _ = LoadAndBindPickerLogoAsync(unit, pic => { logoPicture = pic; logoCanvas.InvalidateSurface(); });

        var logoGrid = new Grid { WidthRequest = 36, HeightRequest = 36 };
        logoGrid.Children.Add(logoCanvas);

        if (unit.IsLieutenant)
        {
            var ltCanvas = new SKCanvasView
            {
                WidthRequest = 14,
                HeightRequest = 14,
                HorizontalOptions = LayoutOptions.Start,
                VerticalOptions = LayoutOptions.Start
            };
            ltCanvas.PaintSurface += (_, e) => DrawPictureOnCanvas(e, _lieutenantOrderIconPicture);
            logoGrid.Children.Add(ltCanvas);
        }

        var nameLabel = new Label
        {
            Text = unit.Name,
            TextColor = Color.FromArgb("#E6EBF2"),
            VerticalTextAlignment = TextAlignment.Center,
            LineBreakMode = LineBreakMode.TailTruncation
        };
        Grid.SetColumn(nameLabel, 1);

        var rowGrid = new Grid { MinimumHeightRequest = 48, Padding = new Thickness(10, 6) };
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(44) });
        rowGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Star });
        rowGrid.Children.Add(logoGrid);
        rowGrid.Children.Add(nameLabel);

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) => { _ = SelectCompanyUnitAsync(unit); CloseUnitPicker(); };
        rowGrid.GestureRecognizers.Add(tap);

        var separator = new BoxView { HeightRequest = 1, Color = Color.FromArgb("#3A4554") };
        var item = new VerticalStackLayout { Spacing = 0 };
        item.Children.Add(rowGrid);
        item.Children.Add(separator);
        return item;
    }

    private static async Task LoadAndBindPickerLogoAsync(CompanyViewerUnitListItem unit, Action<SKPicture?> onLoaded)
    {
        try
        {
            var stream = await OpenBestLogoStreamAsync(unit);
            if (stream is null) { onLoaded(null); return; }
            await using (stream)
            {
                var svg = new SKSvg();
                onLoaded(svg.Load(stream));
            }
        }
        catch { onLoaded(null); }
    }

    private void UpdatePickerHeader(CompanyViewerUnitListItem? unit)
    {
        if (unit is null)
        {
            PickerHeaderNameLabel.Text = "Select unit...";
            _pickerHeaderLogoPicture = null;
            PickerHeaderLogoCanvas.InvalidateSurface();
            PickerHeaderLtCanvas.IsVisible = false;
            return;
        }

        PickerHeaderNameLabel.Text = unit.Name;
        PickerHeaderLtCanvas.IsVisible = unit.IsLieutenant;
        PickerHeaderLtCanvas.InvalidateSurface();
        _ = LoadAndBindPickerLogoAsync(unit, pic => { _pickerHeaderLogoPicture = pic; PickerHeaderLogoCanvas.InvalidateSurface(); });
    }

    private void OnPickerHeaderLogoCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
        => DrawPictureOnCanvas(e, _pickerHeaderLogoPicture);

    private void OnPickerHeaderLtCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
        => DrawPictureOnCanvas(e, _lieutenantOrderIconPicture);

    private void OnUnitPickerTapped(object? sender, TappedEventArgs e)
    {
        if (_unitPickerIsOpen) CloseUnitPicker(); else OpenUnitPicker();
    }

    private void OpenUnitPicker()
    {
        _unitPickerIsOpen = true;
        UnitPickerDropdown.IsVisible = true;
        UnitPickerDismissOverlay.IsVisible = true;
    }

    private void CloseUnitPicker()
    {
        _unitPickerIsOpen = false;
        UnitPickerDropdown.IsVisible = false;
        UnitPickerDismissOverlay.IsVisible = false;
    }

    private void OnUnitPickerDismissTapped(object? sender, TappedEventArgs e) => CloseUnitPicker();

    private void OnTeamTabClicked(object sender, EventArgs e) => SetActiveTab(0);
    private void OnInventoryTabClicked(object sender, EventArgs e)
    {
        SetActiveTab(1);
        _ = RefreshInventoryAsync();
    }
    private void OnMarketplaceTabClicked(object sender, EventArgs e)
    {
        SetActiveTab(2);
        _ = RefreshMarketplaceResourcesAsync();
    }

    private void SetActiveTab(int index)
    {
        TeamTabContent.IsVisible = index == 0;
        InventoryTabContent.IsVisible = index == 1;
        MarketplaceTabContent.IsVisible = index == 2;

        var activeColor = (Color)Application.Current!.Resources["Signal"];
        var inactiveColor = (Color)Application.Current!.Resources["TextMuted"];

        TeamTabButton.TextColor = index == 0 ? activeColor : inactiveColor;
        InventoryTabButton.TextColor = index == 1 ? activeColor : inactiveColor;
        MarketplaceTabButton.TextColor = index == 2 ? activeColor : inactiveColor;

        TeamTabButton.FontAttributes = index == 0 ? FontAttributes.Bold : FontAttributes.None;
        InventoryTabButton.FontAttributes = index == 1 ? FontAttributes.Bold : FontAttributes.None;
        MarketplaceTabButton.FontAttributes = index == 2 ? FontAttributes.Bold : FontAttributes.None;

        // Command rail lights under the active mode key.
        TeamTabRail.IsVisible = index == 0;
        InventoryTabRail.IsVisible = index == 1;
        MarketplaceTabRail.IsVisible = index == 2;
    }

    private async void OnPlayRoundClicked(object? sender, EventArgs e)
    {
        var encodedPath = Uri.EscapeDataString(_companyFilePath ?? string.Empty);
        var encodedSeasonPath = Uri.EscapeDataString(_seasonFilePath ?? string.Empty);
        await Shell.Current.GoToAsync($"{nameof(PlayModePage)}?companyFilePath={encodedPath}&seasonFilePath={encodedSeasonPath}");
    }

    private async Task LoadPlayRoundFlagsIconAsync()
    {
        if (_playRoundFlagsPicture is not null) return;
        try
        {
            await using var stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-flags.svg");
            var svg = new SKSvg();
            _playRoundFlagsPicture = svg.Load(stream);
            PlayRoundFlagsCanvas.InvalidateSurface();
        }
        catch
        {
            // Icon unavailable — button still functional without it.
        }
    }

    private void OnPlayRoundFlagsCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
        => DrawPictureOnCanvas(e, _playRoundFlagsPicture);

    private void OnPlayRoundButtonSizeChanged(object? sender, EventArgs e)
    {
        PlayRoundLabel.IsVisible = true;
    }

    // ── Marketplace BUY ────────────────────────────────────────────────────

    private async void OnBuyClicked(object? sender, EventArgs e)
    {
        var purchases = _inventoryCounts.Where(kvp => kvp.Value > 0).ToList();
        if (purchases.Count == 0) return;

        var seasonFile = await SeasonFileService.LoadSeasonFileAsync(_seasonFilePath);
        var gross = SeasonFileService.ComputeAvailableResources(seasonFile);

        var transactions = new List<SeasonTransaction>();
        foreach (var (key, count) in purchases)
        {
            var sep = key.IndexOf('|');
            var storeName = sep >= 0 ? key[..sep] : string.Empty;
            var itemName = sep >= 0 ? key[(sep + 1)..] : key;
            if (!_inventoryItemCosts.TryGetValue(key, out var cost)) continue;

            var isExchange = _inventoryItemCategories.TryGetValue(key, out var itemCat) &&
                             string.Equals(itemCat, "Exchange", StringComparison.OrdinalIgnoreCase);
            var swcGrant = isExchange ? ParseSwcGrantFromName(itemName) : null;

            for (var i = 0; i < count; i++)
            {
                transactions.Add(new SeasonTransaction
                {
                    OriginStore = storeName,
                    ItemName = itemName,
                    CostCr = cost.CostCr,
                    CostSwc = cost.CostSwc > 0 ? (decimal?)(decimal)cost.CostSwc : null,
                    SwcGrant = swcGrant
                });
            }
        }

        var storeNames = _availableStores.Select(s => s.Name).ToList();
        if (!string.IsNullOrWhiteSpace(_temporaryStoreName))
            storeNames.Add(_temporaryStoreName);

        await SeasonFileService.UpdateLatestRoundAsync(_seasonFilePath, round =>
        {
            if (round.StartingCr == 0 && round.StartingSwc == 0)
            {
                round.StartingCr = gross.CreditsBalance;
                round.StartingSwc = gross.SwcBalance;
            }
            round.Marketplace.Transactions.AddRange(transactions);
            round.Marketplace.Stores = storeNames;
        });

        foreach (var (key, _) in purchases)
            _inventoryCounts[key] = 0;

        await RefreshMarketplaceResourcesAsync();
        if (InventoryTabContent.IsVisible)
            await RefreshInventoryAsync();
        _ = RebuildGearPickersAsync();
    }

    // ── Store search popup ──────────────────────────────────────────────────

    private static readonly (int Min, int Max, string Name)[] StoreRollTable =
    [
        (1,  2,  "Number One"),
        (3,  4,  "Jade Temu"),
        (5,  6,  "Arachne Req"),
        (7,  8,  "Salaam Suuk"),
        (9,  10, "Frontier General"),
        (11, 12, "Alpha Sec"),
        (13, 14, "Greengrocer"),
        (15, 16, "Bantai Yamaco"),
        (17, 18, "Exrah Surplus"),
    ];

    private static string? StoreForRoll(int roll) =>
        StoreRollTable.FirstOrDefault(e => roll >= e.Min && roll <= e.Max).Name;

    private async Task MaybeShowStoreSearchPopupAsync()
    {
        var seasonFile = await SeasonFileService.LoadSeasonFileAsync(_seasonFilePath);
        if (seasonFile is null || seasonFile.Rounds.Count == 0)
        {
            StoreSearchOverlay.IsVisible = false;
            return;
        }

        var latest = seasonFile.Rounds.OrderByDescending(r => r.RoundIndex).First();
        if (!string.IsNullOrWhiteSpace(latest.TemporaryStore))
        {
            StoreSearchOverlay.IsVisible = false;
            return;
        }

        BuildStoreSearchPickers();
        StoreSearchOverlay.IsVisible = true;
    }

    private void BuildStoreSearchPickers()
    {
        StoreRollResultLabel.Text = "—";
        StoreRollResultLabel.TextColor = Color.FromArgb("#E6EBF2");
        StoreChooseArea.IsVisible = false;
        _pendingTemporaryStoreSelection = null;
        StoreSearchConfirmButton.IsEnabled = false;
        StoreSearchConfirmButton.Opacity = 0.45;

        StoreRollPicker.Items.Clear();
        for (var i = 1; i <= 18; i++)
        {
            var store = StoreForRoll(i);
            StoreRollPicker.Items.Add($"{i} — {store}");
        }
        StoreRollPicker.Items.Add("19-20 — Choose any");
        StoreRollPicker.SelectedIndex = -1;

        var ownedNames = new HashSet<string>(
            _availableStores.Select(s => s.Name),
            StringComparer.OrdinalIgnoreCase);

        StoreChoicePicker.Items.Clear();
        foreach (var name in (_storeProvider?.GetAllStoreNames() ?? []).Where(n => !ownedNames.Contains(n)))
        {
            StoreChoicePicker.Items.Add(name);
        }
        StoreChoicePicker.SelectedIndex = -1;
    }

    private void OnStoreRollClicked(object sender, EventArgs e)
    {
        var ownedNames = new HashSet<string>(
            _availableStores.Select(s => s.Name),
            StringComparer.OrdinalIgnoreCase);

        for (var attempt = 0; attempt < 50; attempt++)
        {
            var roll = Random.Shared.Next(1, 21);
            if (roll >= 19)
            {
                StoreRollResultLabel.Text = $"Rolled {roll} — choose any store";
                StoreRollResultLabel.TextColor = Color.FromArgb("#F59E0B");
                StoreChooseArea.IsVisible = true;
                _pendingTemporaryStoreSelection = null;
                StoreSearchConfirmButton.IsEnabled = false;
                StoreSearchConfirmButton.Opacity = 0.45;
                return;
            }

            var store = StoreForRoll(roll);
            if (string.IsNullOrWhiteSpace(store) || ownedNames.Contains(store)) continue;

            StoreRollResultLabel.Text = $"Rolled {roll} — {store}";
            StoreRollResultLabel.TextColor = Color.FromArgb("#34D399");
            StoreChooseArea.IsVisible = false;
            _pendingTemporaryStoreSelection = store;
            StoreSearchConfirmButton.IsEnabled = true;
            StoreSearchConfirmButton.Opacity = 1.0;
            return;
        }

        // All unowned exhausted — fall back to choose any.
        StoreRollResultLabel.Text = "No unowned store reached after rerolls — choose any";
        StoreRollResultLabel.TextColor = Color.FromArgb("#F59E0B");
        StoreChooseArea.IsVisible = true;
    }

    private void OnStoreRollPickerChanged(object sender, EventArgs e)
    {
        if (StoreRollPicker.SelectedIndex < 0) return;

        var ownedNames = new HashSet<string>(
            _availableStores.Select(s => s.Name),
            StringComparer.OrdinalIgnoreCase);

        if (StoreRollPicker.SelectedIndex >= 18)
        {
            StoreRollResultLabel.Text = "Picked 19-20 — choose any store";
            StoreRollResultLabel.TextColor = Color.FromArgb("#F59E0B");
            StoreChooseArea.IsVisible = true;
            _pendingTemporaryStoreSelection = null;
            StoreSearchConfirmButton.IsEnabled = false;
            StoreSearchConfirmButton.Opacity = 0.45;
            return;
        }

        var value = StoreRollPicker.SelectedIndex + 1;
        var store = StoreForRoll(value);
        if (string.IsNullOrWhiteSpace(store))
        {
            StoreRollResultLabel.Text = "Invalid selection";
            StoreRollResultLabel.TextColor = Color.FromArgb("#F87171");
            return;
        }

        if (ownedNames.Contains(store))
        {
            StoreRollResultLabel.Text = $"{value} — {store} (already owned — pick another)";
            StoreRollResultLabel.TextColor = Color.FromArgb("#F87171");
            StoreChooseArea.IsVisible = false;
            _pendingTemporaryStoreSelection = null;
            StoreSearchConfirmButton.IsEnabled = false;
            StoreSearchConfirmButton.Opacity = 0.45;
            return;
        }

        StoreRollResultLabel.Text = $"{value} — {store}";
        StoreRollResultLabel.TextColor = Color.FromArgb("#34D399");
        StoreChooseArea.IsVisible = false;
        _pendingTemporaryStoreSelection = store;
        StoreSearchConfirmButton.IsEnabled = true;
        StoreSearchConfirmButton.Opacity = 1.0;
    }

    private void OnStoreChoicePickerChanged(object sender, EventArgs e)
    {
        if (StoreChoicePicker.SelectedIndex < 0) return;
        _pendingTemporaryStoreSelection = StoreChoicePicker.Items[StoreChoicePicker.SelectedIndex];
        StoreSearchConfirmButton.IsEnabled = true;
        StoreSearchConfirmButton.Opacity = 1.0;
    }

    private async void OnStoreSearchConfirmClicked(object sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_pendingTemporaryStoreSelection)) return;

        var chosen = _pendingTemporaryStoreSelection;
        await SeasonFileService.UpdateLatestRoundAsync(_seasonFilePath, round =>
        {
            round.TemporaryStore = chosen;
        });

        _temporaryStoreName = chosen;
        StoreSearchOverlay.IsVisible = false;

        // Rebuild marketplace tabs so the new temp store appears immediately.
        if (!string.IsNullOrWhiteSpace(_companyFilePath))
            await LoadCompanyFromFileAsync(_companyFilePath);
    }

    // ── Team tab sections ─────────────────────────────────────────────────

    private async Task UpdateTeamTabSectionsAsync(ViewerProfileItem mergedProfile, CompanyViewerUnitListItem item)
    {
        var equipItems = SplitProfileText(mergedProfile.UniqueEquipment);
        PopulateTwoColumnLayout(EquipmentLeftColumn, EquipmentRightColumn, equipItems,
            Color.FromArgb("#B5C0CE"));

        var skillItems = SplitProfileText(mergedProfile.UniqueSkills);
        OrderSkillsLieutenantFirst(skillItems);
        AppendAugmentSkills(skillItems, item);
        PopulateTwoColumnLayout(SkillsLeftColumn, SkillsRightColumn, skillItems,
            Color.FromArgb("#F59E0B"));

        RefreshWeaponSections(item, mergedProfile);
        ApplyAugmentStatOverrides(item);

        var ownedGear = await BuildOwnedGearAsync();
        RefreshGearPickers(item.EntryIndex, ownedGear);
    }

    private void RefreshWeaponSections(CompanyViewerUnitListItem item, ViewerProfileItem mergedProfile)
    {
        var rangedItems = SplitProfileText(mergedProfile.RangedWeapons);
        var ccItems = SplitProfileText(mergedProfile.MeleeWeapons);

        // Append any gear-slot weapons that aren't already in the base lists.
        var slots = GetOrCreateGearSlots(item.EntryIndex);
        foreach (var slotCat in new[] { "Primary", "Secondary", "Sidearm", "Accessories" })
        {
            var assigned = GetSlotValue(slots, slotCat);
            if (string.IsNullOrWhiteSpace(assigned)) continue;

            if (CompanyProfileTextService.IsMeleeWeaponName(assigned))
            {
                if (!ccItems.Contains(assigned, StringComparer.OrdinalIgnoreCase))
                    ccItems.Add(assigned);
            }
            else
            {
                if (!rangedItems.Contains(assigned, StringComparer.OrdinalIgnoreCase))
                    rangedItems.Add(assigned);
            }
        }

        PopulateTwoColumnLayout(WeaponsLeftColumn, WeaponsRightColumn, rangedItems,
            Color.FromArgb("#F87171"), name => ShowWeaponPopup(name));
        PopulateTwoColumnLayout(CcWeaponsLeftColumn, CcWeaponsRightColumn, ccItems,
            Color.FromArgb("#34D399"), name => ShowWeaponPopup(name));
    }

    private static void OrderSkillsLieutenantFirst(List<string> skills)
    {
        var ltIndex = skills.FindIndex(s =>
            s.Contains("Lieutenant", StringComparison.OrdinalIgnoreCase));
        if (ltIndex > 0)
        {
            var lt = skills[ltIndex];
            skills.RemoveAt(ltIndex);
            skills.Insert(0, lt);
        }
    }

    private void PopulateTwoColumnLayout(
        VerticalStackLayout leftCol,
        VerticalStackLayout rightCol,
        IList<string> items,
        Color textColor,
        Action<string>? tapAction = null)
    {
        leftCol.Children.Clear();
        rightCol.Children.Clear();

        if (items.Count == 0)
        {
            leftCol.Children.Add(new Label
            {
                Text = "(none)",
                Style = (Style)Application.Current!.Resources["LabelBody"],
                TextColor = Color.FromArgb("#8A97A8")
            });
            return;
        }

        for (var i = 0; i < items.Count; i++)
        {
            var itemText = items[i];
            var label = new Label
            {
                Text = itemText,
                Style = (Style)Application.Current!.Resources["LabelBody"],
                TextColor = textColor,
                LineBreakMode = LineBreakMode.WordWrap
            };

            if (tapAction is not null)
            {
                var captured = itemText;
                var tap = new TapGestureRecognizer();
                tap.Tapped += (_, _) => tapAction(captured);
                label.GestureRecognizers.Add(tap);
            }

            if (i % 2 == 0) leftCol.Children.Add(label);
            else rightCol.Children.Add(label);
        }
    }

    // ── Gear inventory ────────────────────────────────────────────────────

    private async Task PopulateItemCategoryCacheAsync()
    {
        if (_storeProvider is null) return;

        foreach (var storeName in _storeProvider.GetAllStoreNames())
        {
            var normalized = NormalizeInventoryStoreName(storeName);
            if (InventoryExcludedStores.Contains(normalized)) continue;

            var store = await _storeProvider.GetStoreByNameAsync(normalized);
            if (store is null) continue;

            foreach (var item in store.Items)
            {
                if (string.IsNullOrWhiteSpace(item.Name)) continue;
                var key = MakeInventoryCatalogKey(store.Name, item.Name);
                _inventoryItemCategories[key] = NormalizeInventoryCategory(item.Category);
            }

            foreach (var troop in store.TroopTypes)
            {
                if (string.IsNullOrWhiteSpace(troop.ArmorName)) continue;
                var displayName = string.IsNullOrWhiteSpace(troop.Type)
                    ? troop.ArmorName
                    : $"{troop.ArmorName} ({troop.Type})";
                _inventoryItemCategories[MakeInventoryCatalogKey(store.Name, displayName)] = "Armor";
            }

            foreach (var augment in store.Augments)
            {
                if (string.IsNullOrWhiteSpace(augment.Name)) continue;
                _inventoryItemCategories[MakeInventoryCatalogKey(store.Name, augment.Name)] = "Augments";
            }
        }
    }

    // Returns owned items grouped by normalised category → item names (deduped).
    private async Task<ILookup<string, string>> BuildOwnedGearAsync()
    {
        var result = new List<(string Category, string Name)>();

        if (_storeProvider is null) return result.ToLookup(x => x.Category, x => x.Name);

        // Populate category cache on first use so pickers work even before the
        // marketplace tab has been visited.
        if (_inventoryItemCategories.Count == 0)
            await PopulateItemCategoryCacheAsync();

        var seasonFile = await SeasonFileService.LoadSeasonFileAsync(_seasonFilePath);
        if (seasonFile is null) return result.ToLookup(x => x.Category, x => x.Name);

        // owned[itemName] = (normalised category, net count)
        var owned = new Dictionary<string, (string Category, int Count)>(StringComparer.OrdinalIgnoreCase);

        void AddPurchased(string originStore, string itemName)
        {
            var storeName = NormalizeInventoryStoreName(originStore);
            if (InventoryExcludedStores.Contains(storeName)) return;
            var key = MakeInventoryCatalogKey(storeName, itemName);
            if (!_inventoryItemCategories.TryGetValue(key, out var cat)) return;
            var normalized = NormalizeInventoryCategory(cat);
            if (owned.TryGetValue(itemName, out var ex))
                owned[itemName] = (normalized, ex.Count + 1);
            else
                owned[itemName] = (normalized, 1);
        }

        void AddWon(string itemName, string category)
        {
            if (string.IsNullOrWhiteSpace(itemName)) return;
            var normalized = NormalizeInventoryCategory(category);
            if (owned.TryGetValue(itemName, out var ex))
                owned[itemName] = (normalized, ex.Count + 1);
            else
                owned[itemName] = (normalized, 1);
        }

        void Remove(string itemName)
        {
            if (owned.TryGetValue(itemName, out var ex) && ex.Count > 0)
                owned[itemName] = (ex.Category, ex.Count - 1);
        }

        foreach (var t in seasonFile.InitialPurchases.Transactions)
        {
            if (t.IsSale) Remove(t.ItemName);
            else AddPurchased(t.OriginStore, t.ItemName);
        }

        foreach (var round in seasonFile.Rounds.OrderBy(r => r.RoundIndex))
        {
            foreach (var t in round.Marketplace.Transactions)
            {
                if (t.IsSale) Remove(t.ItemName);
                else AddPurchased(t.OriginStore, t.ItemName);
            }

            foreach (var wonItem in round.Downtime.WonItems)
                AddWon(wonItem.Name, wonItem.Category);

            // "Random pistol" effect uses OtherEffects string rather than WonItems
            if (round.Downtime.WonItems.Count == 0 &&
                round.Downtime.OtherEffects.Contains("Random pistol", StringComparison.OrdinalIgnoreCase))
                AddWon("Random pistol", "Sidearm");
        }

        foreach (var (name, (cat, count)) in owned)
        {
            if (count > 0) result.Add((cat, name));
        }

        return result.ToLookup(x => x.Category, x => x.Name, StringComparer.OrdinalIgnoreCase);
    }

    // ── Gear pickers ──────────────────────────────────────────────────────

    private void RefreshGearPickers(int currentUnitEntryIndex, ILookup<string, string> ownedGear)
    {
        _suppressPickerEvents = true;
        try
        {
            RefreshSingleGearPicker(PrimaryGearPicker,     "Primary",     currentUnitEntryIndex, ownedGear);
            RefreshSingleGearPicker(SecondaryGearPicker,   "Secondary",   currentUnitEntryIndex, ownedGear);
            RefreshSingleGearPicker(SidearmGearPicker,     "Sidearm",     currentUnitEntryIndex, ownedGear);
            RefreshSingleGearPicker(AccessoriesGearPicker, "Accessories", currentUnitEntryIndex, ownedGear);
            RefreshSingleGearPicker(RolesGearPicker,       "Roles",       currentUnitEntryIndex, ownedGear);
            RefreshSingleGearPicker(ArmorGearPicker,       "Armor",       currentUnitEntryIndex, ownedGear);
            RefreshSingleGearPicker(AugmentsGearPicker,    "Augments",    currentUnitEntryIndex, ownedGear);
        }
        finally
        {
            _suppressPickerEvents = false;
        }
    }

    private void RefreshSingleGearPicker(Picker picker, string slotCategory,
        int currentUnitEntryIndex, ILookup<string, string> ownedGear)
    {
        picker.Items.Clear();
        picker.Items.Add("(none)");

        var slots = GetOrCreateGearSlots(currentUnitEntryIndex);
        var currentValue = GetSlotValue(slots, slotCategory);

        var selectedIndex = 0;
        foreach (var itemName in ownedGear[slotCategory].Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var attachedTo = GetAttachedUnitName(itemName, currentUnitEntryIndex, slotCategory);
            var displayText = string.IsNullOrWhiteSpace(attachedTo)
                ? itemName
                : $"{itemName}  [Attached: {attachedTo}]";
            picker.Items.Add(displayText);

            if (string.Equals(itemName, currentValue, StringComparison.OrdinalIgnoreCase))
                selectedIndex = picker.Items.Count - 1;
        }

        picker.SelectedIndex = selectedIndex;
    }

    private string? GetAttachedUnitName(string itemName, int currentUnitEntryIndex, string slotCategory)
    {
        foreach (var (unitIndex, slots) in _unitGearSlots)
        {
            if (unitIndex == currentUnitEntryIndex) continue;
            if (string.Equals(GetSlotValue(slots, slotCategory), itemName, StringComparison.OrdinalIgnoreCase))
                return CompanyUnits.FirstOrDefault(u => u.EntryIndex == unitIndex)?.Name;
        }
        return null;
    }

    private UnitGearSlots GetOrCreateGearSlots(int unitEntryIndex)
    {
        if (!_unitGearSlots.TryGetValue(unitEntryIndex, out var slots))
        {
            slots = new UnitGearSlots();
            _unitGearSlots[unitEntryIndex] = slots;
        }
        return slots;
    }

    private static string? GetSlotValue(UnitGearSlots slots, string category) => category switch
    {
        "Primary"     => slots.Primary,
        "Secondary"   => slots.Secondary,
        "Sidearm"     => slots.Sidearm,
        "Accessories" => slots.Accessories,
        "Roles"       => slots.Roles,
        "Armor"       => slots.Armor,
        "Augments"    => slots.Augment,
        _             => null
    };

    private static void SetSlotValue(UnitGearSlots slots, string category, string? value)
    {
        switch (category)
        {
            case "Primary":     slots.Primary     = value; break;
            case "Secondary":   slots.Secondary   = value; break;
            case "Sidearm":     slots.Sidearm     = value; break;
            case "Accessories": slots.Accessories = value; break;
            case "Roles":       slots.Roles       = value; break;
            case "Armor":       slots.Armor       = value; break;
            case "Augments":    slots.Augment     = value; break;
        }
    }

    private void OnGearPickerSelectionChanged(Picker picker, string slotCategory)
    {
        if (_suppressPickerEvents || _selectedCompanyUnit is null) return;

        var slots = GetOrCreateGearSlots(_selectedCompanyUnit.EntryIndex);

        string? itemName = null;
        if (picker.SelectedIndex > 0)
        {
            var rawText = picker.Items[picker.SelectedIndex];
            var bracketIdx = rawText.IndexOf("  [Attached:", StringComparison.OrdinalIgnoreCase);
            itemName = bracketIdx >= 0 ? rawText[..bracketIdx].Trim() : rawText.Trim();
        }

        // If this item is currently assigned to another unit in the same slot, steal it.
        if (itemName is not null)
        {
            foreach (var (otherIndex, otherSlots) in _unitGearSlots)
            {
                if (otherIndex == _selectedCompanyUnit.EntryIndex) continue;
                if (string.Equals(GetSlotValue(otherSlots, slotCategory), itemName, StringComparison.OrdinalIgnoreCase))
                    SetSlotValue(otherSlots, slotCategory, null);
            }
        }

        SetSlotValue(slots, slotCategory, itemName);

        // Refresh weapon sections so the assigned weapon appears immediately.
        if (_currentMergedProfile is not null)
            RefreshWeaponSections(_selectedCompanyUnit, _currentMergedProfile);

        // Augment changes also reset stats and rebuild the skills column.
        if (slotCategory == "Augments")
            RefreshUnitStatsAndSkillsForAugmentChange();

        // Rebuild all pickers so "Attached" labels refresh across all units.
        _ = RebuildGearPickersAsync();
        _ = SaveGearAssignmentsAsync();
    }

    private async Task RebuildGearPickersAsync()
    {
        if (_selectedCompanyUnit is null) return;
        var ownedGear = await BuildOwnedGearAsync();
        RefreshGearPickers(_selectedCompanyUnit.EntryIndex, ownedGear);
    }

    private void OnPrimaryGearPickerChanged(object sender, EventArgs e)     => OnGearPickerSelectionChanged(PrimaryGearPicker,     "Primary");
    private void OnSecondaryGearPickerChanged(object sender, EventArgs e)   => OnGearPickerSelectionChanged(SecondaryGearPicker,   "Secondary");
    private void OnSidearmGearPickerChanged(object sender, EventArgs e)     => OnGearPickerSelectionChanged(SidearmGearPicker,     "Sidearm");
    private void OnAccessoriesGearPickerChanged(object sender, EventArgs e) => OnGearPickerSelectionChanged(AccessoriesGearPicker, "Accessories");
    private void OnRolesGearPickerChanged(object sender, EventArgs e)       => OnGearPickerSelectionChanged(RolesGearPicker,       "Roles");
    private void OnArmorGearPickerChanged(object sender, EventArgs e)       => OnGearPickerSelectionChanged(ArmorGearPicker,       "Armor");
    private void OnAugmentsGearPickerChanged(object sender, EventArgs e)    => OnGearPickerSelectionChanged(AugmentsGearPicker,    "Augments");

    // ── Augment effects ───────────────────────────────────────────────────

    private void AppendAugmentSkills(List<string> skills, CompanyViewerUnitListItem item, UnitGearSlots? slots = null)
    {
        slots ??= GetOrCreateGearSlots(item.EntryIndex);
        var augmentName = slots.Augment;
        if (string.IsNullOrWhiteSpace(augmentName)) return;
        // Pure stat-setters like "WIP=12" don't produce a skill entry.
        if (Regex.IsMatch(augmentName.Trim(), @"^[A-Za-z]+=\d+$")) return;
        if (!skills.Contains(augmentName, StringComparer.OrdinalIgnoreCase))
            skills.Add(augmentName);
    }

    private void ApplyAugmentStatOverrides(CompanyViewerUnitListItem item)
    {
        var slots = GetOrCreateGearSlots(item.EntryIndex);
        var augmentName = slots.Augment;
        if (string.IsNullOrWhiteSpace(augmentName)) return;
        var match = Regex.Match(augmentName.Trim(), @"^([A-Za-z]+)\s*=\s*(\d+)$");
        if (!match.Success) return;
        _viewerViewModel.ApplyAugmentStatOverride(match.Groups[1].Value, match.Groups[2].Value);
    }

    // Called when the Augments picker changes — resets stats to base then re-applies overrides.
    private void RefreshUnitStatsAndSkillsForAugmentChange()
    {
        if (_selectedCompanyUnit is null || _currentMergedProfile is null) return;
        var item = _selectedCompanyUnit;
        var mergedProfile = _currentMergedProfile;

        _viewerViewModel.ApplySavedUnitSnapshot(
            item.Name, item.UnitMov, item.UnitCc, item.UnitBs, item.UnitPh,
            item.UnitWip, item.UnitArm, item.UnitBts,
            InferVitalityHeader(item.UnitTypeCode), item.UnitVitality, item.UnitS,
            item.IsLieutenant);

        if (item.IsLieutenant && _loadedCaptainStats.IsEnabled)
            _viewerViewModel.ApplyCaptainStatBonuses(
                _loadedCaptainStats.CcBonus, _loadedCaptainStats.BsBonus,
                _loadedCaptainStats.PhBonus, _loadedCaptainStats.WipBonus,
                _loadedCaptainStats.ArmBonus, _loadedCaptainStats.BtsBonus,
                _loadedCaptainStats.VitalityBonus);

        ApplySavedOrderIconOverrides(item);
        ApplyProfileTraitIconOverrides(item, mergedProfile);
        _viewerViewModel.ApplyHackableOverrideFromCurrentConfiguration(
            mergedProfile.UniqueEquipment, mergedProfile.UniqueSkills);
        var ltIconCount = (mergedProfile.IsLieutenant ? 1 : 0) + CountBonusLieutenantOrders(mergedProfile.UniqueSkills);
        TeamUnitDisplayView.ShowLieutenantIcon = mergedProfile.IsLieutenant;
        TeamUnitDisplayView.LieutenantIconCount = ltIconCount;

        ApplyAugmentStatOverrides(item);

        var slots = GetOrCreateGearSlots(item.EntryIndex);
        var skillItems = SplitProfileText(mergedProfile.UniqueSkills);
        OrderSkillsLieutenantFirst(skillItems);
        AppendAugmentSkills(skillItems, item, slots);
        PopulateTwoColumnLayout(SkillsLeftColumn, SkillsRightColumn, skillItems, Color.FromArgb("#F59E0B"));
    }

    private async Task SaveGearAssignmentsAsync()
    {
        if (string.IsNullOrWhiteSpace(_seasonFilePath)) return;
        var seasonFile = await SeasonFileService.LoadSeasonFileAsync(_seasonFilePath);
        if (seasonFile is null) return;

        seasonFile.UnitGear = _unitGearSlots.ToDictionary(
            kvp => kvp.Key,
            kvp =>
            {
                var list = new List<SeasonUnitGear>();
                void AddGear(string slot, string? value)
                {
                    if (!string.IsNullOrWhiteSpace(value))
                        list.Add(new SeasonUnitGear { Slot = slot, ItemName = value });
                }
                AddGear("Primary",     kvp.Value.Primary);
                AddGear("Secondary",   kvp.Value.Secondary);
                AddGear("Sidearm",     kvp.Value.Sidearm);
                AddGear("Accessories", kvp.Value.Accessories);
                AddGear("Roles",       kvp.Value.Roles);
                AddGear("Armor",       kvp.Value.Armor);
                AddGear("Augments",    kvp.Value.Augment);
                return list;
            });

        await SeasonFileService.SaveSeasonFileAsync(_seasonFilePath, seasonFile);
    }

    private async Task LoadGearAssignmentsAsync()
    {
        if (string.IsNullOrWhiteSpace(_seasonFilePath)) return;
        var seasonFile = await SeasonFileService.LoadSeasonFileAsync(_seasonFilePath);
        if (seasonFile is null) return;

        _unitGearSlots.Clear();
        foreach (var (entryIndex, gearList) in seasonFile.UnitGear)
        {
            var slots = GetOrCreateGearSlots(entryIndex);
            foreach (var gear in gearList)
                SetSlotValue(slots, gear.Slot, gear.ItemName);
        }
    }

    private static decimal? ParseSwcGrantFromName(string itemName)
    {
        var match = Regex.Match(itemName,
            @"SWC\s*\+\s*(\d+\.?\d*)|^\+\s*(\d+\.?\d*)\s*SWC",
            RegexOptions.IgnoreCase);
        if (!match.Success) return null;
        var raw = match.Groups[1].Success ? match.Groups[1].Value : match.Groups[2].Value;
        return decimal.TryParse(raw, System.Globalization.NumberStyles.Number,
            System.Globalization.CultureInfo.InvariantCulture, out var result) ? result : null;
    }
}
