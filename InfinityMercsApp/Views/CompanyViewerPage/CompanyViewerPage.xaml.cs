using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Input;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.StandardCompany;
using InfinityMercsApp.Views.Common;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Svg.Skia;

namespace InfinityMercsApp.Views;

public partial class CompanyViewerPage : ContentPage, IQueryAttributable
{
    private const int MaxIconsPerRow = 6;
    private const float IconSize = 75f;
    private const float IconGap = 20f;
    private const float RightPadding = 24f;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly ViewerViewModel _viewerViewModel;
    private readonly FactionLogoCacheService? _factionLogoCacheService;
    private readonly IArmyDataService? _armyDataService;

    private SKPicture? _regularOrderIconPicture;
    private SKPicture? _irregularOrderIconPicture;
    private SKPicture? _lieutenantOrderIconPicture;
    private SKPicture? _commandTokenIconPicture;
    private SKPicture? _impetuousIconPicture;
    private SKPicture? _tacticalAwarenessIconPicture;
    private SKPicture? _cubeIconPicture;
    private SKPicture? _cube2IconPicture;
    private SKPicture? _hackableIconPicture;
    private SKPicture? _nameEditIconPicture;
    private SKPicture? _nameSaveIconPicture;
    private SKPicture? _nameRejectIconPicture;
    private SKPicture? _selectedUnitPicture;
    private int _selectedUnitLogoLoadVersion;
    private string? _companyFilePath;
    private bool _loadAttempted;
    private CompanyViewerUnitListItem? _selectedCompanyUnit;
    private string _companyNameHeading = "Company Viewer";
    private string _companySubtitle = string.Empty;
    private string _companyUnitsStatus = string.Empty;
    private FormattedString _currentRangedWeaponsFormatted = BuildSimpleFormatted("-", Color.FromArgb("#EF4444"));
    private FormattedString _currentMeleeWeaponsFormatted = BuildSimpleFormatted("-", Color.FromArgb("#22C55E"));
    private FormattedString _currentPeripheralsFormatted = BuildSimpleFormatted("-", Color.FromArgb("#FACC15"));
    private bool _hasCurrentPeripherals;
    private SavedImprovedCaptainStats _loadedCaptainStats = new();
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
            if (_selectedCaptainNameHeading == value)
            {
                return;
            }

            _selectedCaptainNameHeading = value;
            OnPropertyChanged();
        }
    }

    public string SelectedProfileBaseNameHeading
    {
        get => _selectedProfileBaseNameHeading;
        private set
        {
            if (_selectedProfileBaseNameHeading == value)
            {
                return;
            }

            _selectedProfileBaseNameHeading = value;
            OnPropertyChanged();
        }
    }

    public string SelectedUnitTypeHeading
    {
        get => _selectedUnitTypeHeading;
        private set
        {
            if (_selectedUnitTypeHeading == value)
            {
                return;
            }

            _selectedUnitTypeHeading = value;
            OnPropertyChanged();
        }
    }

    public bool HasSelectedProfileBaseNameHeading
    {
        get => _hasSelectedProfileBaseNameHeading;
        private set
        {
            if (_hasSelectedProfileBaseNameHeading == value)
            {
                return;
            }

            _hasSelectedProfileBaseNameHeading = value;
            OnPropertyChanged();
        }
    }

    public bool HasSelectedUnitTypeHeading
    {
        get => _hasSelectedUnitTypeHeading;
        private set
        {
            if (_hasSelectedUnitTypeHeading == value)
            {
                return;
            }

            _hasSelectedUnitTypeHeading = value;
            OnPropertyChanged();
        }
    }

    public string CompanyNameHeading
    {
        get => _companyNameHeading;
        private set
        {
            if (_companyNameHeading == value)
            {
                return;
            }

            _companyNameHeading = value;
            OnPropertyChanged();
        }
    }

    public string CompanySubtitle
    {
        get => _companySubtitle;
        private set
        {
            if (_companySubtitle == value)
            {
                return;
            }

            _companySubtitle = value;
            OnPropertyChanged();
        }
    }

    public string CompanyUnitsStatus
    {
        get => _companyUnitsStatus;
        private set
        {
            if (_companyUnitsStatus == value)
            {
                return;
            }

            _companyUnitsStatus = value;
            OnPropertyChanged();
        }
    }

    public FormattedString CurrentRangedWeaponsFormatted
    {
        get => _currentRangedWeaponsFormatted;
        private set
        {
            _currentRangedWeaponsFormatted = value;
            OnPropertyChanged();
        }
    }

    public FormattedString CurrentMeleeWeaponsFormatted
    {
        get => _currentMeleeWeaponsFormatted;
        private set
        {
            _currentMeleeWeaponsFormatted = value;
            OnPropertyChanged();
        }
    }

    public FormattedString CurrentPeripheralsFormatted
    {
        get => _currentPeripheralsFormatted;
        private set
        {
            _currentPeripheralsFormatted = value;
            OnPropertyChanged();
        }
    }

    public bool HasCurrentPeripherals
    {
        get => _hasCurrentPeripherals;
        private set
        {
            if (_hasCurrentPeripherals == value)
            {
                return;
            }

            _hasCurrentPeripherals = value;
            OnPropertyChanged();
        }
    }

    public CompanyViewerPage(
        ViewerViewModel viewModel,
        FactionLogoCacheService? factionLogoCacheService = null,
        IArmyDataService? armyDataService = null)
    {
        InitializeComponent();
        _viewerViewModel = viewModel;
        _factionLogoCacheService = factionLogoCacheService;
        _armyDataService = armyDataService;
        BindingContext = _viewerViewModel;
        SelectCompanyUnitCommand = new Command<CompanyViewerUnitListItem>(item => _ = SelectCompanyUnitAsync(item));
        _viewerViewModel.PropertyChanged += OnViewerViewModelPropertyChanged;

        var topTap = new TapGestureRecognizer();
        topTap.Tapped += OnTopIconRowTapped;
        TopIconRowCanvas.GestureRecognizers.Add(topTap);
        var bottomTap = new TapGestureRecognizer();
        bottomTap.Tapped += OnBottomIconRowTapped;
        BottomIconRowCanvas.GestureRecognizers.Add(bottomTap);
        var saveNameTap = new TapGestureRecognizer();
        saveNameTap.Tapped += OnSelectedNameSaveTapped;
        SelectedNameSaveCanvas.GestureRecognizers.Add(saveNameTap);
        var rejectNameTap = new TapGestureRecognizer();
        rejectNameTap.Tapped += OnSelectedNameRejectTapped;
        SelectedNameRejectCanvas.GestureRecognizers.Add(rejectNameTap);
        var editNameTap = new TapGestureRecognizer();
        editNameTap.Tapped += OnSelectedNameEditTapped;
        SelectedNameEditCanvas.GestureRecognizers.Add(editNameTap);

        _ = LoadHeaderIconsAsync();
        _ = LoadNameEditIconsAsync();
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        if (!_loadAttempted && !string.IsNullOrWhiteSpace(_companyFilePath))
        {
            await LoadCompanyFromFileAsync(_companyFilePath);
        }
    }

    public async void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (!query.TryGetValue("companyFilePath", out var companyFileValue))
        {
            return;
        }

        var rawPath = companyFileValue?.ToString() ?? string.Empty;
        _companyFilePath = Uri.UnescapeDataString(rawPath);
        await LoadCompanyFromFileAsync(_companyFilePath);
    }

    private async Task LoadCompanyFromFileAsync(string? filePath)
    {
        _loadAttempted = true;
        CompanyUnits.Clear();
        _loadedCaptainStats = new SavedImprovedCaptainStats();

        if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
        {
            CompanyNameHeading = "Company Viewer";
            CompanySubtitle = "Saved company file was not found.";
            CompanyUnitsStatus = "Saved company file was not found.";
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
                CompanyNameHeading = "Company Viewer";
                CompanySubtitle = "Unable to read saved company data.";
                CompanyUnitsStatus = "Unable to read saved company data.";
                SelectedCaptainNameHeading = string.Empty;
                SelectedProfileBaseNameHeading = string.Empty;
                HasSelectedProfileBaseNameHeading = false;
                SelectedUnitTypeHeading = string.Empty;
                HasSelectedUnitTypeHeading = false;
                return;
            }

            var companyName = string.IsNullOrWhiteSpace(payload.CompanyName)
                ? Path.GetFileNameWithoutExtension(filePath)
                : payload.CompanyName;

            CompanyNameHeading = companyName;
            CompanySubtitle = string.Empty;
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

            for (var i = 0; i < payload.Entries.Count; i++)
            {
                var entry = payload.Entries[i];
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
                var (savedRangedWeapons, savedCcWeapons) = ResolveSavedWeapons(entry.SourceFactionId, entry);
                var savedSkills = ResolveSavedSkills(entry.SourceFactionId, entry);
                var savedEquipment = ResolveSavedEquipment(entry.SourceFactionId, entry);
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
                    UnitTypeCode = NormalizeUnitTypeCode(entry.UnitTypeCode),
                    Subtitle = subtitle,
                    SourceFactionId = entry.SourceFactionId,
                    SourceUnitId = entry.SourceUnitId,
                    ProfileKey = entry.ProfileKey,
                    IsPeripheralUnit = entry.IsPeripheralUnit,
                    IsLieutenant = entry.IsLieutenant,
                    Cost = entry.Cost,
                    SavedEquipment = savedEquipment,
                    SavedSkills = savedSkills,
                    SavedRangedWeapons = savedRangedWeapons,
                    SavedCcWeapons = savedCcWeapons,
                    ExperiencePoints = Math.Max(0, entry.ExperiencePoints),
                    CaptainIconPackagedPath = entry.IsLieutenant ? "SVGCache/NonCBIcons/noun-captain-8115950.svg" : string.Empty,
                    ExperienceIconPackagedPath = GetExperienceIconPackagedPath(entry.ExperiencePoints),
                    CachedLogoPath = _factionLogoCacheService?.TryGetCachedUnitLogoPath(logoSourceFactionId, logoSourceUnitId),
                    PackagedLogoPath = _factionLogoCacheService?.GetPackagedUnitLogoPath(logoSourceFactionId, logoSourceUnitId)
                        ?? $"SVGCache/units/{logoSourceFactionId}-{logoSourceUnitId}.svg"
                });
            }

            if (CompanyUnits.Count == 0)
            {
                CompanyUnitsStatus = "No units found in this company.";
                return;
            }

            CompanyUnitsStatus = string.Empty;
            await SelectCompanyUnitAsync(CompanyUnits[0]);
        }
        catch (Exception ex)
        {
            CompanyNameHeading = "Company Viewer";
            CompanySubtitle = $"Failed to load company: {ex.Message}";
            CompanyUnitsStatus = $"Failed to load company: {ex.Message}";
            SelectedCaptainNameHeading = string.Empty;
            SelectedProfileBaseNameHeading = string.Empty;
            HasSelectedProfileBaseNameHeading = false;
            SelectedUnitTypeHeading = string.Empty;
            HasSelectedUnitTypeHeading = false;
        }
    }

    private async Task SelectCompanyUnitAsync(CompanyViewerUnitListItem? item)
    {
        if (item is null)
        {
            return;
        }

        foreach (var unit in CompanyUnits)
        {
            unit.IsSelected = ReferenceEquals(unit, item);
        }
        _selectedCompanyUnit = item;

        await _viewerViewModel.LoadSpecificConfigurationAsync(
            item.SourceFactionId,
            item.SourceUnitId,
            item.Name,
            item.ProfileKey,
            item.IsLieutenant,
            item.CachedLogoPath,
            item.PackagedLogoPath);

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

        var loadedProfile = _viewerViewModel.Profiles.FirstOrDefault();
        _viewerViewModel.Profiles.Clear();
        var mergedProfile = BuildMergedProfileItem(item, loadedProfile);
        _viewerViewModel.Profiles.Add(mergedProfile);
        _viewerViewModel.ApplySelectedProfileTopSummaries(mergedProfile);
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
        SetSelectedNameEditMode(false);
        SelectedNameEntry.Text = item.Name;

        UpdateCurrentWeaponsDisplay();
        TopIconRowCanvas.InvalidateSurface();
    }

    private bool HasSufficientLoadedProfileDetails(CompanyViewerUnitListItem item)
    {
        var profile = _viewerViewModel.Profiles.FirstOrDefault();
        if (profile is null)
        {
            return false;
        }

        var hasSavedRanged = !IsDashOrEmpty(item.SavedRangedWeapons);
        var hasSavedCc = !IsDashOrEmpty(item.SavedCcWeapons);
        var hasSavedSkills = !IsDashOrEmpty(item.SavedSkills);
        var hasSavedEquipment = !IsDashOrEmpty(item.SavedEquipment);

        if (hasSavedRanged && IsDashOrEmpty(profile.RangedWeapons))
        {
            return false;
        }

        if (hasSavedCc && IsDashOrEmpty(profile.MeleeWeapons))
        {
            return false;
        }

        if (hasSavedSkills && IsDashOrEmpty(profile.UniqueSkills))
        {
            return false;
        }

        if (hasSavedEquipment && IsDashOrEmpty(profile.UniqueEquipment))
        {
            return false;
        }

        return true;
    }

    private static bool IsDashOrEmpty(string? text)
    {
        return string.IsNullOrWhiteSpace(text) || text.Trim() == "-";
    }

    private static int ResolveLogoSourceFactionId(SavedCompanyEntry entry)
    {
        return entry.LogoSourceFactionId > 0 ? entry.LogoSourceFactionId : entry.SourceFactionId;
    }

    private static int ResolveLogoSourceUnitId(SavedCompanyEntry entry)
    {
        return entry.LogoSourceUnitId > 0 ? entry.LogoSourceUnitId : entry.SourceUnitId;
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

    private void ApplySavedOrderIconOverrides(CompanyViewerUnitListItem item)
    {
        if (!item.IsPeripheralUnit)
        {
            return;
        }

        var savedSkillLines = SplitProfileText(item.SavedSkills);
        var hasIrregular = savedSkillLines.Any(x => x.Contains("irregular", StringComparison.OrdinalIgnoreCase));
        if (!hasIrregular)
        {
            return;
        }

        _viewerViewModel.SetOrderTypeIconState(showRegular: false, showIrregular: true);
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

    private static ViewerProfileItem BuildMergedProfileItem(CompanyViewerUnitListItem item, ViewerProfileItem? loadedProfile)
    {
        var rangedWeapons = MergeProfileSectionText(loadedProfile?.RangedWeapons, item.SavedRangedWeapons);
        var meleeWeapons = MergeProfileSectionText(loadedProfile?.MeleeWeapons, item.SavedCcWeapons);
        var uniqueEquipment = MergeProfileSectionText(loadedProfile?.UniqueEquipment, item.SavedEquipment);
        var isLieutenant = loadedProfile?.IsLieutenant ?? item.IsLieutenant;
        var uniqueSkills = MergeProfileSectionText(loadedProfile?.UniqueSkills, item.SavedSkills);
        if (isLieutenant)
        {
            uniqueSkills = NormalizeLieutenantOrderEntries(uniqueSkills);
        }
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
            Peripherals = loadedProfile?.Peripherals ?? "-",
            PeripheralsFormatted = loadedProfile?.PeripheralsFormatted,
            Cost = loadedProfile?.Cost ?? item.Cost.ToString(),
            Swc = loadedProfile?.Swc ?? "-",
            SwcDisplay = loadedProfile?.SwcDisplay ?? string.Empty,
            IsLieutenant = isLieutenant,
            ProfileKey = loadedProfile?.ProfileKey ?? item.ProfileKey
        };
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

        CurrentRangedWeaponsFormatted = profile.RangedWeaponsFormatted
            ?? BuildSimpleFormatted(profile.RangedWeapons, Color.FromArgb("#EF4444"));
        CurrentMeleeWeaponsFormatted = profile.MeleeWeaponsFormatted
            ?? BuildSimpleFormatted(profile.MeleeWeapons, Color.FromArgb("#22C55E"));
        CurrentPeripheralsFormatted = profile.PeripheralsFormatted
            ?? BuildSimpleFormatted(profile.Peripherals, Color.FromArgb("#FACC15"));
        HasCurrentPeripherals = !IsDashOrEmpty(profile.Peripherals);
    }

    private async Task LoadHeaderIconsAsync()
    {
        _regularOrderIconPicture?.Dispose();
        _regularOrderIconPicture = null;
        _irregularOrderIconPicture?.Dispose();
        _irregularOrderIconPicture = null;
        _lieutenantOrderIconPicture?.Dispose();
        _lieutenantOrderIconPicture = null;
        _commandTokenIconPicture?.Dispose();
        _commandTokenIconPicture = null;
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
            await using var regularStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/regular.svg");
            var regularSvg = new SKSvg();
            _regularOrderIconPicture = regularSvg.Load(regularStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CompanyViewerPage regular order icon load failed: {ex.Message}");
        }

        try
        {
            await using var irregularStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/irregular.svg");
            var irregularSvg = new SKSvg();
            _irregularOrderIconPicture = irregularSvg.Load(irregularStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CompanyViewerPage irregular order icon load failed: {ex.Message}");
        }

        try
        {
            await using var lieutenantStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/lieutenant.svg");
            var lieutenantSvg = new SKSvg();
            _lieutenantOrderIconPicture = lieutenantSvg.Load(lieutenantStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CompanyViewerPage lieutenant order icon load failed: {ex.Message}");
        }

        try
        {
            await using var commandTokenStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-options-6682476.svg");
            var commandTokenSvg = new SKSvg();
            _commandTokenIconPicture = commandTokenSvg.Load(commandTokenStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CompanyViewerPage command token icon load failed: {ex.Message}");
        }

        try
        {
            await using var impetuousStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/impetuous.svg");
            var impetuousSvg = new SKSvg();
            _impetuousIconPicture = impetuousSvg.Load(impetuousStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CompanyViewerPage impetuous icon load failed: {ex.Message}");
        }

        try
        {
            await using var tacticalStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/tactical.svg");
            var tacticalSvg = new SKSvg();
            _tacticalAwarenessIconPicture = tacticalSvg.Load(tacticalStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CompanyViewerPage tactical awareness icon load failed: {ex.Message}");
        }

        try
        {
            await using var cubeStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/cube.svg");
            var cubeSvg = new SKSvg();
            _cubeIconPicture = cubeSvg.Load(cubeStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CompanyViewerPage cube icon load failed: {ex.Message}");
        }

        try
        {
            await using var cube2Stream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/cube2.svg");
            var cube2Svg = new SKSvg();
            _cube2IconPicture = cube2Svg.Load(cube2Stream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CompanyViewerPage cube2 icon load failed: {ex.Message}");
        }

        try
        {
            await using var hackableStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/CBIcons/hackable.svg");
            var hackableSvg = new SKSvg();
            _hackableIconPicture = hackableSvg.Load(hackableStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CompanyViewerPage hackable icon load failed: {ex.Message}");
        }

        TopIconRowCanvas.InvalidateSurface();
        BottomIconRowCanvas.InvalidateSurface();
        UnitDisplayConfigurationsView.RegularOrderIconPicture = _regularOrderIconPicture;
        UnitDisplayConfigurationsView.IrregularOrderIconPicture = _irregularOrderIconPicture;
        UnitDisplayConfigurationsView.ImpetuousIconPicture = _impetuousIconPicture;
        UnitDisplayConfigurationsView.TacticalAwarenessIconPicture = _tacticalAwarenessIconPicture;
        UnitDisplayConfigurationsView.CubeIconPicture = _cubeIconPicture;
        UnitDisplayConfigurationsView.Cube2IconPicture = _cube2IconPicture;
        UnitDisplayConfigurationsView.HackableIconPicture = _hackableIconPicture;
        UnitDisplayConfigurationsView.InvalidateHeaderIconsCanvas();
    }

    private async Task LoadNameEditIconsAsync()
    {
        _nameEditIconPicture?.Dispose();
        _nameEditIconPicture = null;
        _nameSaveIconPicture?.Dispose();
        _nameSaveIconPicture = null;
        _nameRejectIconPicture?.Dispose();
        _nameRejectIconPicture = null;

        try
        {
            await using var editStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-edit-333556.svg");
            var editSvg = new SKSvg();
            _nameEditIconPicture = editSvg.Load(editStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CompanyViewerPage name edit icon load failed: {ex.Message}");
        }

        try
        {
            await using var saveStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-check-3612574.svg");
            var saveSvg = new SKSvg();
            _nameSaveIconPicture = saveSvg.Load(saveStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CompanyViewerPage name save icon load failed: {ex.Message}");
        }

        try
        {
            await using var rejectStream = await FileSystem.Current.OpenAppPackageFileAsync("SVGCache/NonCBIcons/noun-x-1890844.svg");
            var rejectSvg = new SKSvg();
            _nameRejectIconPicture = rejectSvg.Load(rejectStream);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CompanyViewerPage name reject icon load failed: {ex.Message}");
        }

        SelectedNameEditCanvas.InvalidateSurface();
        SelectedNameSaveCanvas.InvalidateSurface();
        SelectedNameRejectCanvas.InvalidateSurface();
    }

    private void OnViewerViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(ViewerViewModel.ShowRegularOrderIcon)
            or nameof(ViewerViewModel.ShowIrregularOrderIcon)
            or nameof(ViewerViewModel.ShowImpetuousIcon)
            or nameof(ViewerViewModel.ShowTacticalAwarenessIcon))
        {
            TopIconRowCanvas.InvalidateSurface();
        }

        if (e.PropertyName is nameof(ViewerViewModel.ShowCubeIcon)
            or nameof(ViewerViewModel.ShowCube2Icon)
            or nameof(ViewerViewModel.ShowHackableIcon))
        {
            BottomIconRowCanvas.InvalidateSurface();
        }

        if (e.PropertyName is nameof(ViewerViewModel.ShowRegularOrderIcon)
            or nameof(ViewerViewModel.ShowIrregularOrderIcon)
            or nameof(ViewerViewModel.ShowImpetuousIcon)
            or nameof(ViewerViewModel.ShowTacticalAwarenessIcon)
            or nameof(ViewerViewModel.ShowCubeIcon)
            or nameof(ViewerViewModel.ShowCube2Icon)
            or nameof(ViewerViewModel.ShowHackableIcon))
        {
            UnitDisplayConfigurationsView.InvalidateHeaderIconsCanvas();
        }

        if (e.PropertyName == nameof(ViewerViewModel.SelectedUnit))
        {
            _ = LoadSelectedUnitLogoAsync(_viewerViewModel.SelectedUnit);
        }
    }

    private async Task LoadSelectedUnitLogoAsync(ViewerUnitItem? unit)
    {
        var loadVersion = ++_selectedUnitLogoLoadVersion;
        SKPicture? loadedPicture = null;

        try
        {
            if (unit is not null)
            {
                Stream? stream = null;
                try
                {
                    if (!string.IsNullOrWhiteSpace(unit.CachedLogoPath) && File.Exists(unit.CachedLogoPath))
                    {
                        stream = File.OpenRead(unit.CachedLogoPath);
                    }
                    else if (!string.IsNullOrWhiteSpace(unit.PackagedLogoPath))
                    {
                        stream = await FileSystem.Current.OpenAppPackageFileAsync(unit.PackagedLogoPath);
                    }

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
                    Console.Error.WriteLine($"CompanyViewerPage selected unit logo load failed: {ex.Message}");
                }
            }

            if (loadVersion != _selectedUnitLogoLoadVersion)
            {
                loadedPicture?.Dispose();
                return;
            }

            _selectedUnitPicture?.Dispose();
            _selectedUnitPicture = loadedPicture;
            UnitDisplayConfigurationsView.SelectedUnitPicture = _selectedUnitPicture;
            UnitDisplayConfigurationsView.InvalidateSelectedUnitCanvas();
        }
        catch
        {
            loadedPicture?.Dispose();
            throw;
        }
    }

    private void OnTopIconRowCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        DrawIconRow(canvas, e.Info, BuildTopIconEntries().Select(x => x.Picture).ToList());
    }

    private void OnBottomIconRowCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        DrawIconRow(canvas, e.Info, BuildBottomIconEntries().Select(x => x.Picture).ToList());
    }

    private void OnSelectedNameSaveCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        if (_nameSaveIconPicture is null)
        {
            return;
        }

        DrawPictureInRect(canvas, _nameSaveIconPicture, new SKRect(0, 0, e.Info.Width, e.Info.Height));
    }

    private void OnSelectedNameEditCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        if (_nameEditIconPicture is null)
        {
            return;
        }

        DrawPictureInRect(canvas, _nameEditIconPicture, new SKRect(0, 0, e.Info.Width, e.Info.Height));
    }

    private void OnSelectedNameRejectCanvasPaintSurface(object? sender, SKPaintSurfaceEventArgs e)
    {
        var canvas = e.Surface.Canvas;
        canvas.Clear(SKColors.Transparent);
        if (_nameRejectIconPicture is null)
        {
            return;
        }

        DrawPictureInRect(canvas, _nameRejectIconPicture, new SKRect(0, 0, e.Info.Width, e.Info.Height));
    }

    private void OnSelectedNameLabelTapped(object? sender, TappedEventArgs args)
    {
        BeginSelectedNameEdit();
    }

    private void OnSelectedNameEditTapped(object? sender, TappedEventArgs args)
    {
        BeginSelectedNameEdit();
    }

    private void BeginSelectedNameEdit()
    {
        if (_selectedCompanyUnit is null)
        {
            return;
        }

        SelectedNameEntry.Text = SelectedCaptainNameHeading;
        SetSelectedNameEditMode(true);
        SelectedNameEntry.Focus();
    }

    private async void OnSelectedNameSaveTapped(object? sender, TappedEventArgs args)
    {
        if (_selectedCompanyUnit is null)
        {
            return;
        }

        var fallback = string.IsNullOrWhiteSpace(SelectedCaptainNameHeading)
            ? (_selectedCompanyUnit.IsPeripheralUnit
                ? (_selectedCompanyUnit.BaseUnitName ?? "Peripheral")
                : (_selectedCompanyUnit.IsLieutenant ? "Captain" : (_selectedCompanyUnit.BaseUnitName ?? "Unit")))
            : SelectedCaptainNameHeading;
        var normalized = string.IsNullOrWhiteSpace(SelectedNameEntry.Text)
            ? fallback
            : SelectedNameEntry.Text.Trim();

        _selectedCompanyUnit.Name = normalized;
        SelectedCaptainNameHeading = normalized;
        SelectedNameEntry.Text = normalized;
        await PersistUnitCustomNameAsync(_selectedCompanyUnit, normalized);
        SetSelectedNameEditMode(false);
    }

    private void OnSelectedNameRejectTapped(object? sender, TappedEventArgs args)
    {
        SelectedNameEntry.Text = SelectedCaptainNameHeading;
        SetSelectedNameEditMode(false);
    }

    private void SetSelectedNameEditMode(bool isEditing)
    {
        SelectedNameDisplayRow.IsVisible = !isEditing;
        SelectedNameEditRow.IsVisible = isEditing;
    }

    private async Task PersistUnitCustomNameAsync(CompanyViewerUnitListItem item, string customName)
    {
        if (string.IsNullOrWhiteSpace(_companyFilePath) || !File.Exists(_companyFilePath))
        {
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(_companyFilePath);
            var payload = JsonSerializer.Deserialize<SavedCompanyFile>(json, JsonOptions);
            if (payload is null)
            {
                return;
            }

            var matched = payload.Entries.FirstOrDefault(x => x.EntryIndex == item.EntryIndex)
                ?? payload.Entries.FirstOrDefault(x =>
                    x.SourceFactionId == item.SourceFactionId &&
                    x.SourceUnitId == item.SourceUnitId &&
                    string.Equals(x.ProfileKey ?? string.Empty, item.ProfileKey ?? string.Empty, StringComparison.OrdinalIgnoreCase));

            if (matched is null)
            {
                return;
            }

            matched.CustomName = customName;
            if (string.IsNullOrWhiteSpace(matched.BaseUnitName))
            {
                matched.BaseUnitName = string.IsNullOrWhiteSpace(item.BaseUnitName) ? matched.Name : item.BaseUnitName;
            }

            await File.WriteAllTextAsync(
                _companyFilePath,
                JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CompanyViewerPage failed to persist unit custom name: {ex.Message}");
        }
    }

    private List<(SKPicture Picture, string? Url)> BuildTopIconEntries()
    {
        var entries = new List<(SKPicture Picture, string? Url)>(MaxIconsPerRow);
        var orderTypePicture = _viewerViewModel.ShowIrregularOrderIcon ? _irregularOrderIconPicture : _regularOrderIconPicture;
        if (_viewerViewModel.HasOrderTypeIcon && orderTypePicture is not null)
        {
            entries.Add((orderTypePicture, null));
        }

        var profile = _viewerViewModel.Profiles.FirstOrDefault();
        var isLieutenantProfile = profile?.IsLieutenant == true;
        if (isLieutenantProfile && _lieutenantOrderIconPicture is not null)
        {
            entries.Add((_lieutenantOrderIconPicture, null));
        }

        var bonusRegularOrders = CountBonusRegularOrders(profile?.UniqueSkills);
        if (!isLieutenantProfile)
        {
            for (var i = 0; i < bonusRegularOrders; i++)
            {
                if (_regularOrderIconPicture is null)
                {
                    break;
                }

                entries.Add((_regularOrderIconPicture, null));
            }
        }

        var bonusLieutenantOrders = CountBonusLieutenantOrders(profile?.UniqueSkills);
        for (var i = 0; i < bonusLieutenantOrders; i++)
        {
            if (_lieutenantOrderIconPicture is null)
            {
                break;
            }

            entries.Add((_lieutenantOrderIconPicture, null));
        }

        var bonusCommandTokens = CountBonusCommandTokens(profile?.UniqueSkills);
        for (var i = 0; i < bonusCommandTokens; i++)
        {
            if (_commandTokenIconPicture is null)
            {
                break;
            }

            entries.Add((_commandTokenIconPicture, null));
        }

        if (_viewerViewModel.ShowImpetuousIcon && _impetuousIconPicture is not null)
        {
            entries.Add((_impetuousIconPicture, _viewerViewModel.ImpetuousIconUrl));
        }

        if (_viewerViewModel.ShowTacticalAwarenessIcon && _tacticalAwarenessIconPicture is not null)
        {
            entries.Add((_tacticalAwarenessIconPicture, _viewerViewModel.TacticalAwarenessIconUrl));
        }

        return entries;
    }

    private List<(SKPicture Picture, string? Url)> BuildBottomIconEntries()
    {
        var entries = new List<(SKPicture Picture, string? Url)>(MaxIconsPerRow);
        if (_viewerViewModel.ShowCubeIcon && _cubeIconPicture is not null)
        {
            entries.Add((_cubeIconPicture, _viewerViewModel.CubeIconUrl));
        }

        if (_viewerViewModel.ShowCube2Icon && _cube2IconPicture is not null)
        {
            entries.Add((_cube2IconPicture, _viewerViewModel.Cube2IconUrl));
        }

        if (_viewerViewModel.ShowHackableIcon && _hackableIconPicture is not null)
        {
            entries.Add((_hackableIconPicture, _viewerViewModel.HackableIconUrl));
        }

        return entries;
    }

    private async void OnTopIconRowTapped(object? sender, TappedEventArgs args)
    {
        await HandleIconRowTapAsync(TopIconRowCanvas, args, BuildTopIconEntries());
    }

    private async void OnBottomIconRowTapped(object? sender, TappedEventArgs args)
    {
        await HandleIconRowTapAsync(BottomIconRowCanvas, args, BuildBottomIconEntries());
    }

    private static async Task HandleIconRowTapAsync(
        SKCanvasView canvasView,
        TappedEventArgs args,
        IReadOnlyList<(SKPicture Picture, string? Url)> entries)
    {
        if (entries.Count == 0)
        {
            return;
        }

        var point = args.GetPosition(canvasView);
        if (point is null)
        {
            return;
        }

        var slot = GetTappedIconSlot(point.Value.X, canvasView.Width);
        if (!slot.HasValue || slot.Value < 0 || slot.Value >= entries.Count)
        {
            return;
        }

        var url = entries[slot.Value].Url;
        if (string.IsNullOrWhiteSpace(url))
        {
            return;
        }

        try
        {
            await Launcher.Default.OpenAsync(url);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"CompanyViewerPage icon link open failed: {ex.Message}");
        }
    }

    private static int? GetTappedIconSlot(double tapX, double controlWidth)
    {
        var rowWidth = (MaxIconsPerRow * IconSize) + ((MaxIconsPerRow - 1) * IconGap);
        var startX = controlWidth - RightPadding - rowWidth;
        if (startX < 0)
        {
            startX = 0;
        }

        for (var i = 0; i < MaxIconsPerRow; i++)
        {
            var iconLeft = startX + (i * (IconSize + IconGap));
            var iconRight = iconLeft + IconSize;
            if (tapX >= iconLeft && tapX <= iconRight)
            {
                return i;
            }
        }

        return null;
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
        if (normalized == "MOV")
        {
            return string.Empty;
        }

        return normalized;
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

        return lines.Count == 0 ? "-" : string.Join(Environment.NewLine, lines);
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

    private static int CountBonusRegularOrders(string? uniqueSkills)
    {
        if (string.IsNullOrWhiteSpace(uniqueSkills))
        {
            return 0;
        }

        var total = 0;
        foreach (var line in SplitProfileText(uniqueSkills))
        {
            if (!line.Contains("order", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var matches = Regex.Matches(line, @"\+(\d+)\s*(?:regular\s*)?orders?\b", RegexOptions.IgnoreCase);
            foreach (Match match in matches)
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

    private static int CountBonusCommandTokens(string? uniqueSkills)
    {
        if (string.IsNullOrWhiteSpace(uniqueSkills))
        {
            return 0;
        }

        var total = 0;
        foreach (var line in SplitProfileText(uniqueSkills))
        {
            if (!line.Contains("command token", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var matches = Regex.Matches(line, @"\+(\d+)\s*command\s*tokens?\b", RegexOptions.IgnoreCase);
            foreach (Match match in matches)
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
                !line.Contains("lieutenant order", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var matches = Regex.Matches(line, @"\+(\d+)\s*(?:lt|lieutenant)\s*orders?\b", RegexOptions.IgnoreCase);
            foreach (Match match in matches)
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
}

public sealed class CompanyViewerUnitListItem : BaseViewModel, IViewerListItem
{
    public int EntryIndex { get; init; }
    public int SourceFactionId { get; init; }
    public int SourceUnitId { get; init; }
    public string ProfileKey { get; init; } = string.Empty;
    public bool IsPeripheralUnit { get; init; }
    public bool IsLieutenant { get; init; }
    public int Cost { get; init; }
    public string SavedEquipment { get; init; } = "-";
    public string SavedSkills { get; init; } = "-";
    public string SavedRangedWeapons { get; init; } = "-";
    public string SavedCcWeapons { get; init; } = "-";
    public int ExperiencePoints { get; init; }
    public int ExperienceLevel => CompanyUnitExperienceRanks.GetRankLevel(ExperiencePoints);
    public string ExperienceRankName => CompanyUnitExperienceRanks.GetRankName(ExperiencePoints);
    public string CaptainIconPackagedPath { get; init; } = string.Empty;
    public string ExperienceIconPackagedPath { get; init; } = string.Empty;
    private string _name = string.Empty;
    public string Name
    {
        get => _name;
        set
        {
            if (_name == value)
            {
                return;
            }

            _name = value;
            OnPropertyChanged();
        }
    }
    public string BaseUnitName { get; init; } = string.Empty;
    public string UnitTypeCode { get; init; } = string.Empty;
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


