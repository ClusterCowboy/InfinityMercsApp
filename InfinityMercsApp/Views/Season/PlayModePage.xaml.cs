using System.Collections.ObjectModel;
using System.Text.Json;
using System.Windows.Input;
using InfinityMercsApp.Domain.Models.Perks;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Common;
using InfinityMercsApp.Views.StandardCompany;
using InfinityMercsApp.Infrastructure.Providers;

namespace InfinityMercsApp.Views.Season;

public partial class PlayModePage : ContentPage, IQueryAttributable
{
    private const int TagCompanyFactionId = 2003;

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly IArmyDataService? _armyDataService;
    private readonly FactionLogoCacheService? _factionLogoCacheService;
    private readonly IAppSettingsProvider? _appSettingsProvider;
    private string _companyFilePath = string.Empty;
    private bool _loadAttempted;
    private bool _showUnitsInInches = true;
    private DeploymentUnitItem? _selectedUnit;
    private int _deployedSlotCount;


    public ObservableCollection<DeploymentUnitItem> DeploymentUnits { get; } = [];
    public ICommand SelectUnitCommand { get; }
    public ICommand TileStripPanCommand { get; }

    public DeploymentUnitItem? SelectedUnit
    {
        get => _selectedUnit;
        private set
        {
            _selectedUnit = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasSelectedUnit));
            OnPropertyChanged(nameof(NoUnitSelected));
        }
    }

    public bool HasSelectedUnit => _selectedUnit is not null;
    public bool NoUnitSelected => _selectedUnit is null;

    public int DeployedSlotCount
    {
        get => _deployedSlotCount;
        private set
        {
            if (_deployedSlotCount == value) return;
            _deployedSlotCount = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsEliteDeployment));
            OnPropertyChanged(nameof(DeploymentStatusText));
            OnPropertyChanged(nameof(DeploymentStatusColor));
        }
    }

    public bool IsEliteDeployment => _deployedSlotCount > 0 && _deployedSlotCount <= 4;

    public string DeploymentStatusText => _deployedSlotCount == 0
        ? "0 / 6 slots"
        : IsEliteDeployment
            ? $"{_deployedSlotCount} / 6 slots · ELITE"
            : $"{_deployedSlotCount} / 6 slots";

    public Color DeploymentStatusColor => IsEliteDeployment
        ? Color.FromArgb("#22C55E")
        : Color.FromArgb("#9CA3AF");


    public PlayModePage(
        IArmyDataService? armyDataService = null,
        FactionLogoCacheService? factionLogoCacheService = null,
        IAppSettingsProvider? appSettingsProvider = null)
    {
        InitializeComponent();
        _armyDataService = armyDataService;
        _factionLogoCacheService = factionLogoCacheService;
        _appSettingsProvider = appSettingsProvider;
        ApplyGlobalDisplayUnitsPreference();
        BindingContext = this;
        SelectUnitCommand = new Command<DeploymentUnitItem>(SelectUnit);
        TileStripPanCommand = new Command<object>(delta =>
        {
            if (delta is double dx)
                UnitStripScrollView.ScrollToAsync(
                    Math.Max(0, UnitStripScrollView.ScrollX - dx), 0, false);
        });
    }

    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("companyFilePath", out var raw))
        {
            _companyFilePath = Uri.UnescapeDataString(raw?.ToString() ?? string.Empty);
            _loadAttempted = false;
        }
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        var unitsPreferenceChanged = ApplyGlobalDisplayUnitsPreference();
        if ((!_loadAttempted || unitsPreferenceChanged) && !string.IsNullOrWhiteSpace(_companyFilePath))
        {
            await LoadCompanyFromFileAsync(_companyFilePath);
        }
    }

    private async Task LoadCompanyFromFileAsync(string filePath)
    {
        ApplyGlobalDisplayUnitsPreference();
        _loadAttempted = true;
        foreach (var unit in DeploymentUnits)
            unit.PropertyChanged -= OnUnitCheckedChanged;
        DeploymentUnits.Clear();
        SelectedUnit = null;
        DeployedSlotCount = 0;

        if (!File.Exists(filePath))
        {
            return;
        }

        try
        {
            var json = await File.ReadAllTextAsync(filePath);
            var payload = JsonSerializer.Deserialize<SavedCompanyFile>(json, JsonOptions);
            if (payload?.Entries is null)
            {
                return;
            }

            var captainStats = payload.ImprovedCaptainStats ?? new SavedImprovedCaptainStats();
            var captainWeaponChoices = captainStats.IsEnabled
                ? CollectCaptainChoices(captainStats.WeaponChoice1, captainStats.WeaponChoice2, captainStats.WeaponChoice3)
                : (IReadOnlyList<string>)[];
            var captainSkillChoices = captainStats.IsEnabled
                ? CollectCaptainChoices(captainStats.SkillChoice1, captainStats.SkillChoice2, captainStats.SkillChoice3)
                : (IReadOnlyList<string>)[];
            var captainEquipmentChoices = captainStats.IsEnabled
                ? CollectCaptainChoices(captainStats.EquipmentChoice1, captainStats.EquipmentChoice2, captainStats.EquipmentChoice3)
                : (IReadOnlyList<string>)[];

            var orderedEntries = payload.Entries
                .Where(e => !e.IsPeripheralUnit)
                .OrderByDescending(x => x.IsLieutenant)
                .ThenBy(x => x.EntryIndex)
                .ToList();

            foreach (var entry in orderedEntries)
            {
                var effectiveSourceFactionId = ResolveEffectiveSourceFactionId(entry);
                var logoSourceFactionId = entry.LogoSourceFactionId > 0 ? entry.LogoSourceFactionId : entry.SourceFactionId;
                var logoSourceUnitId = entry.LogoSourceUnitId > 0 ? entry.LogoSourceUnitId : entry.SourceUnitId;

                var baseUnitName = string.IsNullOrWhiteSpace(entry.BaseUnitName) ? entry.Name : entry.BaseUnitName;
                var displayName = string.IsNullOrWhiteSpace(entry.CustomName)
                    ? baseUnitName
                    : entry.CustomName.Trim();

                var skills = ResolveSavedSkills(effectiveSourceFactionId, entry);
                var equipment = ResolveSavedEquipment(effectiveSourceFactionId, entry);
                var (rangedWeapons, meleeWeapons) = ResolveSavedWeapons(effectiveSourceFactionId, entry);
                var characteristics = ResolveSavedCharacteristics(effectiveSourceFactionId, entry);
                var isIrregular = HasOrderKeyword(characteristics, "Irregular");
                var isImpetuous = HasOrderKeyword(characteristics, "Impetuous");

                if (entry.IsLieutenant && captainStats.IsEnabled)
                {
                    rangedWeapons = AppendChoices(rangedWeapons,
                        captainWeaponChoices.Where(w => !CompanyProfileTextService.IsMeleeWeaponName(w)).ToList());
                    meleeWeapons = AppendChoices(meleeWeapons,
                        captainWeaponChoices.Where(CompanyProfileTextService.IsMeleeWeaponName).ToList());
                    skills = AppendChoices(skills, captainSkillChoices);
                    equipment = AppendChoices(equipment, captainEquipmentChoices);
                }

                var isTagFromTagCompany =
                    entry.UnitTypeCode?.Trim().Equals("TAG", StringComparison.OrdinalIgnoreCase) == true &&
                    effectiveSourceFactionId == TagCompanyFactionId;

                var item = new DeploymentUnitItem
                {
                    EntryIndex = entry.EntryIndex,
                    IsChecked = entry.IsLieutenant,
                    Name = displayName,
                    BaseUnitDisplayName = BuildUnitBaseDisplayName(baseUnitName),
                    Subtitle = entry.IsLieutenant ? "Lieutenant" : string.Empty,
                    IsLieutenant = entry.IsLieutenant,
                    IsIrregular = isIrregular,
                    IsImpetuous = isImpetuous,
                    IsTagFromTagCompany = isTagFromTagCompany,
                    CaptainIconPackagedPath = entry.IsLieutenant
                        ? "SVGCache/NonCBIcons/noun-captain-8115950.svg"
                        : string.Empty,
                    ExperienceIconPackagedPath = GetExperienceIconPackagedPath(entry.ExperiencePoints),
                    CachedLogoPath = _factionLogoCacheService?.TryGetCachedUnitLogoPath(logoSourceFactionId, logoSourceUnitId),
                    PackagedLogoPath = _factionLogoCacheService?.GetPackagedUnitLogoPath(logoSourceFactionId, logoSourceUnitId)
                        ?? $"SVGCache/units/{logoSourceFactionId}-{logoSourceUnitId}.svg",
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
                    VitalityHeader = InferVitalityHeader(entry.UnitTypeCode),
                    Equipment = SeasonDisplayUnitFormatter.ConvertExplicitDistances(equipment, _showUnitsInInches),
                    Skills = SeasonDisplayUnitFormatter.ConvertExplicitDistances(skills, _showUnitsInInches),
                    RangedWeapons = rangedWeapons,
                    MeleeWeapons = meleeWeapons
                };
                item.PropertyChanged += OnUnitCheckedChanged;
                DeploymentUnits.Add(item);
            }

            DeployedSlotCount = ComputeDeployedSlots();

            if (DeploymentUnits.Count > 0)
            {
                SelectUnit(DeploymentUnits[0]);
            }
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"PlayModePage failed to load company: {ex.Message}");
        }
    }

    private void SelectUnit(DeploymentUnitItem? item)
    {
        if (item is null) return;
        foreach (var unit in DeploymentUnits)
        {
            unit.IsSelected = ReferenceEquals(unit, item);
        }
        SelectedUnit = item;
    }

    private void OnUnitCheckedChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(DeploymentUnitItem.IsChecked)) return;
        if (sender is DeploymentUnitItem { IsLieutenant: true, IsChecked: false } lt)
        {
            lt.IsChecked = true;
            return;
        }
        if (sender is DeploymentUnitItem { IsChecked: true } item && ComputeDeployedSlots() > 6)
        {
            item.IsChecked = false;
            return;
        }
        DeployedSlotCount = ComputeDeployedSlots();
    }

    private int ComputeDeployedSlots() =>
        DeploymentUnits.Where(u => u.IsChecked).Sum(u => u.SlotCost);

    private bool ApplyGlobalDisplayUnitsPreference()
    {
        var showUnitsInInches = SeasonDisplayUnitFormatter.GetShowUnitsInInches(_appSettingsProvider);
        var changed = _showUnitsInInches != showUnitsInInches;
        _showUnitsInInches = showUnitsInInches;
        return changed;
    }

    // ── Characteristics resolution ─────────────────────────────────────────────

    private string ResolveSavedCharacteristics(int sourceFactionId, SavedCompanyEntry entry)
    {
        var names = ResolveCodeNames(sourceFactionId, entry.CurrentCharacteristicCodes, "chars");
        names.AddRange(entry.CustomCharacteristics ?? []);
        return JoinCodesOrDash(names);
    }

    private static bool HasOrderKeyword(string characteristics, string keyword) =>
        characteristics
            .Split(['\r', '\n', ','], StringSplitOptions.RemoveEmptyEntries)
            .Any(s => s.Trim().Equals(keyword, StringComparison.OrdinalIgnoreCase));

    private async void OnDeployClicked(object sender, EventArgs e)
    {
        var checkedUnits = DeploymentUnits.Where(u => u.IsChecked).ToList();
        if (checkedUnits.Count == 0)
        {
            await DisplayAlert("No Units Selected", "Select at least one unit to deploy.", "OK");
            return;
        }

        var encodedPath = Uri.EscapeDataString(_companyFilePath);
        var checkedIndices = string.Join(",", checkedUnits.Select(u => u.EntryIndex));
        var encodedIndices = Uri.EscapeDataString(checkedIndices);
        var eliteDeployment = IsEliteDeployment ? "1" : "0";
        await Shell.Current.GoToAsync(
            $"{nameof(GameModePage)}?companyFilePath={encodedPath}&deployedIndices={encodedIndices}&eliteDeployment={eliteDeployment}");
    }

    // ── Resolution helpers ────────────────────────────────────────────────────

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

    private (string Ranged, string Melee) ResolveSavedWeapons(int sourceFactionId, SavedCompanyEntry entry)
    {
        var currentWeapons = ResolveCodeNames(sourceFactionId, entry.CurrentWeaponCodes, "weapons")
            .Where(x => !string.IsNullOrWhiteSpace(x) && x.Trim() != "-")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        currentWeapons.AddRange((entry.CustomWeapons ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x) && x.Trim() != "-"));
        currentWeapons = currentWeapons.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        if (currentWeapons.Count == 0) return ("-", "-");

        var ranged = currentWeapons.Where(x => !CompanyProfileTextService.IsMeleeWeaponName(x)).ToList();
        var melee = currentWeapons.Where(CompanyProfileTextService.IsMeleeWeaponName).ToList();
        return (JoinCodesOrDash(ranged), JoinCodesOrDash(melee));
    }

    private List<string> ResolveCodeNames(
        int sourceFactionId,
        IEnumerable<CompanySavedCodeRef> codes,
        string sectionName)
    {
        var codeList = (codes ?? []).Where(x => x is not null && x.Id > 0).ToList();
        if (codeList.Count == 0) return [];

        var snapshot = _armyDataService?.GetFactionSnapshot(sourceFactionId);
        var lookup = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot?.FiltersJson, sectionName);
        var extrasLookup = CompanyUnitDetailsShared.BuildIdNameLookup(snapshot?.FiltersJson, "extras");
        var resolved = new List<string>();

        foreach (var code in codeList)
        {
            if (!lookup.TryGetValue(code.Id, out var name) || string.IsNullOrWhiteSpace(name))
                continue;

            var display = name.Trim();
            var extras = (code.Extra ?? [])
                .Distinct()
                .Select(extraId => extrasLookup.TryGetValue(extraId, out var extraName) ? extraName?.Trim() : null)
                .Where(n => !string.IsNullOrWhiteSpace(n))
                .Cast<string>()
                .ToList();

            if (extras.Count > 0)
                display = $"{display} ({string.Join(", ", extras)})";

            resolved.Add(display);
        }

        return resolved;
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

    private static int ResolveEffectiveSourceFactionId(SavedCompanyEntry entry)
    {
        if (entry.SourceFactionId == TagCompanyFactionId || entry.LogoSourceFactionId == TagCompanyFactionId)
            return TagCompanyFactionId;
        var baseName = entry.BaseUnitName ?? string.Empty;
        if (baseName.Contains("Repurposed Mining Equipment", StringComparison.OrdinalIgnoreCase) ||
            baseName.Contains("Turtlemek", StringComparison.OrdinalIgnoreCase))
            return TagCompanyFactionId;
        return entry.SourceFactionId;
    }

    private static IReadOnlyList<string> CollectCaptainChoices(params string[] choices)
    {
        return choices
            .Select(NormalizeCaptainChoice)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static string NormalizeCaptainChoice(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Trim() == "-") return string.Empty;
        return System.Text.RegularExpressions.Regex
            .Replace(value.Trim(), @"^\s*\([-+]?\d+\)\s*-\s*", string.Empty).Trim();
    }

    private static string AppendChoices(string? baseText, IReadOnlyList<string> additions)
    {
        if (additions.Count == 0) return baseText ?? "-";
        var lines = (baseText ?? string.Empty)
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Where(x => !string.IsNullOrWhiteSpace(x) && x != "-")
            .ToList();
        var existing = new HashSet<string>(lines, StringComparer.OrdinalIgnoreCase);
        foreach (var addition in additions)
        {
            if (existing.Add(addition)) lines.Add(addition);
        }
        return lines.Count == 0 ? "-" : string.Join(Environment.NewLine, lines);
    }

    private static string BuildUnitBaseDisplayName(string? baseUnitName)
    {
        if (string.IsNullOrWhiteSpace(baseUnitName)) return "Unit";
        var withoutParens = System.Text.RegularExpressions.Regex
            .Replace(baseUnitName, @"\s*\([^)]*\)\s*", " ").Trim();
        var collapsed = System.Text.RegularExpressions.Regex
            .Replace(withoutParens, @"\s{2,}", " ").Trim();
        return string.IsNullOrWhiteSpace(collapsed) ? "Unit" : collapsed;
    }

    private static string InferVitalityHeader(string? unitTypeCode)
    {
        var normalized = unitTypeCode?.Trim().ToUpperInvariant();
        return normalized is "TAG" or "REM" or "PERIPHERAL" ? "STR" : "VITA";
    }

    private static string GetExperienceIconPackagedPath(int experiencePoints)
    {
        var level = CompanyUnitExperienceRanks.GetRankLevel(experiencePoints);
        return level <= 0 ? string.Empty : $"SVGCache/NonCBIcons/Experience/noun-{level}-stars.svg";
    }
}

public sealed class DeploymentUnitItem : BaseViewModel, IViewerListItem
{
    // IViewerListItem
    public string Name { get; init; } = string.Empty;
    public string? CachedLogoPath { get; init; }
    public string? PackagedLogoPath { get; init; }
    public string? Subtitle { get; init; }
    public bool HasSubtitle => !string.IsNullOrWhiteSpace(Subtitle);

    // CompanyViewerUnitTileView bindings
    public int EntryIndex { get; init; }
    public string BaseUnitDisplayName { get; init; } = string.Empty;
    public bool IsLieutenant { get; init; }
    public bool IsIrregular { get; init; }
    public bool IsImpetuous { get; init; }
    public bool IsTagFromTagCompany { get; init; }
    public int SlotCost => IsTagFromTagCompany ? 2 : 1;
    public bool IsCheckboxEnabled => !IsLieutenant;
    public string CaptainIconPackagedPath { get; init; } = string.Empty;
    public string ExperienceIconPackagedPath { get; init; } = string.Empty;

    // Stats
    public string UnitMov { get; init; } = "-";
    public string UnitCc { get; init; } = "-";
    public string UnitBs { get; init; } = "-";
    public string UnitPh { get; init; } = "-";
    public string UnitWip { get; init; } = "-";
    public string UnitArm { get; init; } = "-";
    public string UnitBts { get; init; } = "-";
    public string UnitVitality { get; init; } = "-";
    public string UnitS { get; init; } = "-";
    public string VitalityHeader { get; init; } = "VITA";

    // Wound tracking (used by GameModePage)
    public int StartingVitality { get; init; } = 1;
    public bool HasNwi { get; init; }
    public bool HasRemotePresence { get; init; }
    public int MaxWounds => HasRemotePresence ? StartingVitality + 2 : StartingVitality + 1;

    private int _woundsReceived;
    public int WoundsReceived
    {
        get => _woundsReceived;
        set
        {
            var clamped = Math.Clamp(value, 0, MaxWounds);
            if (_woundsReceived == clamped) return;
            _woundsReceived = clamped;
            OnPropertyChanged();
            OnPropertyChanged(nameof(WoundStateKey));
        }
    }

    public string WoundStateKey
    {
        get
        {
            var w = _woundsReceived;
            var sv = StartingVitality;
            if (w == 0) return "Healthy";
            if (w < sv) return "Wounded";
            if (w == sv) return HasNwi ? "NwiDown" : "KnockedOut";
            if (w == sv + 1 && HasRemotePresence) return "KnockedOutBody2";
            return "Dead";
        }
    }

    // Resolved display text
    public string Equipment { get; init; } = "-";
    public string Skills { get; init; } = "-";
    public string RangedWeapons { get; init; } = "-";
    public string MeleeWeapons { get; init; } = "-";

    // Deployment checkbox
    private bool _isChecked;
    public bool IsChecked
    {
        get => _isChecked;
        set
        {
            if (_isChecked == value) return;
            _isChecked = value;
            OnPropertyChanged();
        }
    }

    // Tile selection highlight
    private bool _isSelected;
    public bool IsSelected
    {
        get => _isSelected;
        set
        {
            if (_isSelected == value) return;
            _isSelected = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TileStroke));
            OnPropertyChanged(nameof(TileStrokeThickness));
        }
    }

    public Color TileStroke => IsSelected ? Color.FromArgb("#22C55E") : Color.FromArgb("#374151");
    public double TileStrokeThickness => IsSelected ? 2.0 : 1.0;
}
