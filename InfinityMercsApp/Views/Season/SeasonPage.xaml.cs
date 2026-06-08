using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Input;
using InfinityMercsApp.Domain.Models.Stores;
using InfinityMercsApp.Domain.Utilities;
using MauiShapes = Microsoft.Maui.Controls.Shapes;
using InfinityMercsApp.Services;
using InfinityMercsApp.Services.Season;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.StandardCompany;
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
    private IReadOnlyList<(string Name, string? AssociatedType, string Alignment)> _availableStores = [];
    private readonly Dictionary<string, int> _inventoryCounts = new(StringComparer.OrdinalIgnoreCase);

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

    private FormattedString _currentRangedWeaponsFormatted = BuildSimpleFormatted("-", Color.FromArgb("#EF4444"));
    private FormattedString _currentMeleeWeaponsFormatted = BuildSimpleFormatted("-", Color.FromArgb("#22C55E"));
    private FormattedString _currentPeripheralsFormatted = BuildSimpleFormatted("-", Color.FromArgb("#FACC15"));
    private bool _hasCurrentPeripherals;
    private string _selectedCaptainNameHeading = string.Empty;
    private string _selectedProfileBaseNameHeading = string.Empty;
    private bool _hasSelectedProfileBaseNameHeading;
    private string _selectedUnitTypeHeading = string.Empty;
    private bool _hasSelectedUnitTypeHeading;

    public ObservableCollection<CompanyViewerUnitListItem> CompanyUnits { get; } = [];
    public ICommand SelectCompanyUnitCommand { get; }

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
        IMetadataProvider? metadataProvider = null)
    {
        InitializeComponent();
        _viewerViewModel = viewerViewModel;
        _factionLogoCacheService = factionLogoCacheService;
        _armyDataService = armyDataService;
        _storeProvider = storeProvider;
        _metadataProvider = metadataProvider;
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

        await LoadCompanyFromFileAsync(_companyFilePath);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        CaptureOriginalFontSizes();
        ApplyResponsiveFontSizes(Width);
        await LoadPlayRoundFlagsIconAsync();
        if (!_loadAttempted && !string.IsNullOrWhiteSpace(_companyFilePath))
        {
            await LoadCompanyFromFileAsync(_companyFilePath);
        }
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
                    UnitMov = entry.CurrentMov,
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
            CurrentRangedWeaponsFormatted = BuildSimpleFormatted("-", Color.FromArgb("#EF4444"));
            CurrentMeleeWeaponsFormatted = BuildSimpleFormatted("-", Color.FromArgb("#22C55E"));
            CurrentPeripheralsFormatted = BuildSimpleFormatted("-", Color.FromArgb("#FACC15"));
            HasCurrentPeripherals = false;
            return;
        }

        CurrentRangedWeaponsFormatted = BuildWeaponsFormatted(profile.RangedWeapons, Color.FromArgb("#EF4444"));
        CurrentMeleeWeaponsFormatted = BuildWeaponsFormatted(profile.MeleeWeapons, Color.FromArgb("#22C55E"));
        CurrentPeripheralsFormatted = profile.PeripheralsFormatted
            ?? BuildSimpleFormatted(profile.Peripherals, Color.FromArgb("#FACC15"));
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
                : BuildSimpleFormatted(rangedWeapons, Color.FromArgb("#EF4444")),
            MeleeWeapons = meleeWeapons,
            MeleeWeaponsFormatted = keepLoadedMeleeFormatting && loadedProfile?.MeleeWeaponsFormatted is not null
                ? loadedProfile.MeleeWeaponsFormatted
                : BuildSimpleFormatted(meleeWeapons, Color.FromArgb("#22C55E")),
            UniqueEquipment = uniqueEquipment,
            UniqueEquipmentFormatted = keepLoadedEquipmentFormatting && loadedProfile?.UniqueEquipmentFormatted is not null
                ? loadedProfile.UniqueEquipmentFormatted
                : BuildSimpleFormatted(uniqueEquipment, Color.FromArgb("#06B6D4")),
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
                Margin = new Thickness(0, 0, 0, 8)
            });
        }
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
        StoreTabStrip.Children.Clear();
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

        foreach (var (name, _, _) in _availableStores)
        {
            StoreTabStrip.Children.Add(BuildStoreTabButton(name));
        }

        if (_availableStores.Count > 0)
        {
            await SelectStoreAsync(_availableStores[0].Name);
        }
    }

    private Button BuildStoreTabButton(string storeName)
    {
        var btn = new Button
        {
            Text = storeName,
            BackgroundColor = Color.FromArgb("#374151"),
            TextColor = Color.FromArgb("#9CA3AF"),
            BorderColor = Color.FromArgb("#4B5563"),
            BorderWidth = 1,
            CornerRadius = 6,
            Style = (Style)Application.Current!.Resources["ButtonCaption"],
            Padding = new Thickness(10, 4),
            HeightRequest = 34
        };
        btn.Clicked += (_, _) => _ = SelectStoreAsync(storeName);
        return btn;
    }

    private async Task SelectStoreAsync(string storeName)
    {
        foreach (var child in StoreTabStrip.Children)
        {
            if (child is not Button btn) continue;
            var isActive = string.Equals(btn.Text, storeName, StringComparison.OrdinalIgnoreCase);
            btn.TextColor = isActive ? Color.FromArgb("#22C55E") : Color.FromArgb("#9CA3AF");
            btn.BackgroundColor = isActive ? Color.FromArgb("#111827") : Color.FromArgb("#374151");
            btn.BorderColor = isActive ? Color.FromArgb("#22C55E") : Color.FromArgb("#4B5563");
        }

        StoreContentArea.Children.Clear();
        MarketplacePopupOverlay.IsVisible = false;

        var store = await _storeProvider!.GetStoreByNameAsync(storeName);
        if (store is null) return;

        StoreContentArea.Children.Add(new Label
        {
            Text = store.Name,
            Style = (Style)Application.Current!.Resources["LabelTitleSmall"],
            FontAttributes = FontAttributes.Bold,
            TextColor = Colors.White,
            Margin = new Thickness(0, 0, 0, 2)
        });

        if (!string.IsNullOrWhiteSpace(store.Alignment))
        {
            StoreContentArea.Children.Add(new Label
            {
                Text = store.Alignment,
                Style = (Style)Application.Current!.Resources["LabelBody"],
                TextColor = Color.FromArgb("#9CA3AF"),
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
                var costText = item.CostSwc.HasValue
                    ? $"{item.CostCr}cr / {item.CostSwc}swc"
                    : $"{item.CostCr}cr";
                StoreContentArea.Children.Add(
                    BuildItemRow(storeName, item.Name, costText, () => ShowItemPopup(item)));
            }
        }

        if (store.TroopTypes.Count > 0)
        {
            StoreContentArea.Children.Add(BuildSectionHeader("ARMOR UPGRADES"));
            foreach (var troop in store.TroopTypes)
            {
                StoreContentArea.Children.Add(BuildArmorRow(storeName, troop));
            }
        }

        if (store.Augments.Count > 0)
        {
            StoreContentArea.Children.Add(BuildSectionHeader("AUGMENTS"));
            foreach (var augment in store.Augments)
            {
                var costText = $"{augment.CostCr}cr";
                StoreContentArea.Children.Add(
                    BuildItemRow(storeName, augment.Name, costText, () => ShowAugmentPopup(augment)));
            }
        }
    }

    private static Label BuildSectionHeader(string text) => new()
    {
        Text = text,
        Style = (Style)Application.Current!.Resources["LabelCaption"],
        FontAttributes = FontAttributes.Bold,
        TextColor = Color.FromArgb("#6B7280"),
        Margin = new Thickness(0, 10, 0, 2)
    };

    // Builds a standard item/augment row: left = name + cost (tappable), right = + count -
    private Border BuildItemRow(string storeName, string itemName, string costText, Action onTap)
    {
        var leftStack = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Spacing = 2 };
        leftStack.Children.Add(new Label { Text = itemName, Style = (Style)Application.Current!.Resources["LabelBody"], TextColor = Colors.White });
        leftStack.Children.Add(new Label { Text = costText, Style = (Style)Application.Current!.Resources["LabelCaption"], TextColor = Color.FromArgb("#22C55E") });
        leftStack.GestureRecognizers.Add(new TapGestureRecognizer { Command = new Command(onTap) });

        return BuildRowBorder($"{storeName}|{itemName}", leftStack);
    }

    // Builds an armor row: left = name + type + ARM/BTS + abilities (tappable), right = + count -
    private Border BuildArmorRow(string storeName, StoreTroopType troop)
    {
        var displayName = string.IsNullOrWhiteSpace(troop.Type)
            ? troop.ArmorName
            : $"{troop.ArmorName} ({troop.Type})";
        var armText = troop.Arm.HasValue ? troop.Arm.Value.ToString() : "—";
        var btsText = troop.Bts.HasValue ? troop.Bts.Value.ToString() : "—";

        var leftStack = new VerticalStackLayout { VerticalOptions = LayoutOptions.Center, Spacing = 2 };
        leftStack.Children.Add(new Label { Text = displayName, Style = (Style)Application.Current!.Resources["LabelBody"], TextColor = Colors.White });
        leftStack.Children.Add(new Label
        {
            Text = $"ARM {armText}  BTS {btsText}",
            Style = (Style)Application.Current!.Resources["LabelCaption"],
            TextColor = Color.FromArgb("#9CA3AF")
        });
        if (!string.IsNullOrWhiteSpace(troop.Abilities))
        {
            leftStack.Children.Add(new Label
            {
                Text = troop.Abilities,
                Style = (Style)Application.Current!.Resources["LabelCaption"],
                TextColor = Color.FromArgb("#9CA3AF"),
                LineBreakMode = LineBreakMode.WordWrap
            });
        }
        leftStack.Children.Add(new Label
        {
            Text = $"{troop.CostCr}cr",
            Style = (Style)Application.Current!.Resources["LabelCaption"],
            TextColor = Color.FromArgb("#22C55E")
        });
        leftStack.GestureRecognizers.Add(
            new TapGestureRecognizer { Command = new Command(() => ShowArmorPopup(troop)) });

        return BuildRowBorder($"{storeName}|{displayName}", leftStack);
    }

    // Shared core: wraps a left-content view in a bordered row with + count - controls on the right.
    private Border BuildRowBorder(string key, View leftContent)
    {
        var countLabel = new Label
        {
            Text = _inventoryCounts.GetValueOrDefault(key, 0).ToString(),
            Style = (Style)Application.Current!.Resources["LabelBody"],
            TextColor = Colors.White,
            VerticalTextAlignment = TextAlignment.Center,
            HorizontalTextAlignment = TextAlignment.Center,
            MinimumWidthRequest = 28
        };

        var plusBtn = new Button
        {
            Text = "+",
            BackgroundColor = Color.FromArgb("#374151"),
            TextColor = Color.FromArgb("#22C55E"),
            BorderColor = Color.FromArgb("#4B5563"),
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
        };

        var minusBtn = new Button
        {
            Text = "−",
            BackgroundColor = Color.FromArgb("#374151"),
            TextColor = Color.FromArgb("#EF4444"),
            BorderColor = Color.FromArgb("#4B5563"),
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

        return new Border
        {
            Content = rowGrid,
            Stroke = Color.FromArgb("#374151"),
            StrokeThickness = 1,
            StrokeShape = new MauiShapes.RoundRectangle { CornerRadius = new CornerRadius(4) },
            Padding = new Thickness(12, 6, 8, 6),
            Margin = new Thickness(0, 0, 0, 3)
        };
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
                TextColor = Color.FromArgb("#6B7280"),
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
                    TextColor = isSkill ? Color.FromArgb("#60A5FA") : Colors.White,
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
            TextColor = Color.FromArgb("#9CA3AF"),
            VerticalTextAlignment = TextAlignment.Center
        };
        var valueView = new Label
        {
            Text = value,
            Style = (Style)Application.Current!.Resources["LabelBody"],
            TextColor = Colors.White,
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
            TextColor = Colors.White,
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

        var separator = new BoxView { HeightRequest = 1, Color = Color.FromArgb("#374151") };
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
    private void OnInventoryTabClicked(object sender, EventArgs e) => SetActiveTab(1);
    private void OnMarketplaceTabClicked(object sender, EventArgs e) => SetActiveTab(2);

    private void SetActiveTab(int index)
    {
        TeamTabContent.IsVisible = index == 0;
        InventoryTabContent.IsVisible = index == 1;
        MarketplaceTabContent.IsVisible = index == 2;

        TeamTabButton.TextColor = index == 0 ? Color.FromArgb("#22C55E") : Color.FromArgb("#6B7280");
        InventoryTabButton.TextColor = index == 1 ? Color.FromArgb("#22C55E") : Color.FromArgb("#6B7280");
        MarketplaceTabButton.TextColor = index == 2 ? Color.FromArgb("#22C55E") : Color.FromArgb("#6B7280");

        TeamTabButton.FontAttributes = index == 0 ? FontAttributes.Bold : FontAttributes.None;
        InventoryTabButton.FontAttributes = index == 1 ? FontAttributes.Bold : FontAttributes.None;
        MarketplaceTabButton.FontAttributes = index == 2 ? FontAttributes.Bold : FontAttributes.None;
    }

    private async void OnPlayRoundClicked(object? sender, EventArgs e)
    {
        var encodedPath = Uri.EscapeDataString(_companyFilePath ?? string.Empty);
        await Shell.Current.GoToAsync($"{nameof(PlayModePage)}?companyFilePath={encodedPath}");
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
}
