using System.Collections.ObjectModel;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Windows.Input;
using InfinityMercsApp.Domain.Utilities;
using InfinityMercsApp.Infrastructure.Providers;
using InfinityMercsApp.Services;
using InfinityMercsApp.ViewModels;
using InfinityMercsApp.Views.Controls;
using InfinityMercsApp.Views.Templates.NewCompany;
using Microsoft.Maui.Devices;
using SkiaSharp;
using SkiaSharp.Views.Maui;
using SkiaSharp.Views.Maui.Controls;
using Svg.Skia;
using ArmyFactionRecord = InfinityMercsApp.Domain.Models.Army.Faction;
using ArmyResumeRecord = InfinityMercsApp.Domain.Models.Army.Resume;
using ArmySpecopsEquipRecord = InfinityMercsApp.Domain.Models.Army.SpecopsEquipment;
using ArmySpecopsSkillRecord = InfinityMercsApp.Domain.Models.Army.SpecopsSkill;
using ArmySpecopsUnitRecord = InfinityMercsApp.Domain.Models.Army.SpecopsUnit;
using ArmySpecopsWeaponRecord = InfinityMercsApp.Domain.Models.Army.SpecopsWeapon;
using ArmyUnitRecord = InfinityMercsApp.Domain.Models.Army.Unit;
using FactionRecord = InfinityMercsApp.Domain.Models.Metadata.Faction;
using MercsArmyListEntry = InfinityMercsApp.Domain.Models.Army.MercsArmyListEntry;
using InfinityMercsApp.Views.Templates.UICommon;

namespace InfinityMercsApp.Views.StandardCompany;

public partial class StandardCompanySelectionPage : CompanySelectionPageBase, IUnitDisplayIconState, IUnitDisplayStatState
{
    private readonly record struct ExtraDefinition(string Name, string Type);
    private sealed class TeamAggregate
    {
        public TeamAggregate(string name)
        {
            Name = name;
        }

        public string Name { get; }
        public int Duo { get; private set; }
        public int Haris { get; private set; }
        public int Core { get; private set; }
        public Dictionary<string, (int Min, int Max, string? Slug, bool MinAsterisk)> UnitLimits { get; } = new(StringComparer.OrdinalIgnoreCase);

        public void AddCounts(int duo, int haris, int core)
        {
            Duo += duo;
            Haris += haris;
            Core += core;
        }

        public void MergeUnitLimit(string unitName, int min, int max, string? slug, bool minAsterisk)
        {
            if (UnitLimits.TryGetValue(unitName, out var existing))
            {
                UnitLimits[unitName] = (
                    Math.Min(existing.Min, min),
                    Math.Max(existing.Max, max),
                    string.IsNullOrWhiteSpace(existing.Slug) ? slug : existing.Slug,
                    existing.MinAsterisk || minAsterisk);
                return;
            }

            UnitLimits[unitName] = (min, max, slug, minAsterisk);
        }
    }

    private const double UnitNameHeadingMaxFontSize = 24d;
    private const double UnitNameHeadingMinFontSize = 11d;
    private const double UnitNameHeadingFontStep = 0.5d;
    private const int CharacterCategoryId = 10;

    private readonly ArmySourceSelectionMode _mode;
    private readonly IArmyDataService _armyDataService;
    private readonly ISpecOpsProvider _specOpsProvider;
    private readonly FactionLogoCacheService? _factionLogoCacheService;
    private readonly IAppSettingsProvider? _appSettingsProvider;
    private readonly StandardCompanyProfileCoordinator _profileCoordinator;
    private readonly FactionSlotSelectionState<ArmyFactionSelectionItem> _factionSelectionState = new();
    private SKPicture? _filterIconPicture;
    private string _companyName = "Company Name";
    private readonly Command _startCompanyCommand;
    private bool _showCompanyNameValidationError;
    private Color _companyNameBorderColor = Color.FromArgb("#6B7280");
    private int _activeSlotIndex;
    private bool _loaded;
    private bool _lieutenantOnlyUnits;
    private bool _showFireteams;
    private bool _isFactionSelectionActive = true;
    private string _pageHeading = string.Empty;
    private ArmyUnitSelectionItem? _selectedUnit;
    private string _profilesStatus = "Select a unit.";
    private bool _summaryHighlightLieutenant;
    private UnitFilterCriteria _activeUnitFilter = UnitFilterCriteria.None;
    private UnitFilterPopupView? _activeUnitFilterPopup;
    private UnitFilterPopupOptions? _preparedUnitFilterPopupOptions;

    public StandardCompanySelectionPage(
        ArmySourceSelectionMode mode,
        IMetadataProvider? metadataProvider,
        IFactionProvider? factionProvider,
        ISpecOpsProvider specOpsProvider,
        ICohesiveCompanyFactionQueryProvider cohesiveCompanyFactionQueryProvider,
        FactionLogoCacheService? factionLogoCacheService,
        IAppSettingsProvider? appSettingsProvider,
        IArmyDataService armyDataService)
        : base(mode, metadataProvider, factionProvider, specOpsProvider, cohesiveCompanyFactionQueryProvider, factionLogoCacheService, appSettingsProvider)
    {
        InitializeComponent();
        FactionSlotSelectorView.LeftSlotTapped += (_, _) => SetActiveSlot(0);
        FactionSlotSelectorView.RightSlotTapped += (_, _) =>
        {
            if (ShowRightSelectionBox)
            {
                SetActiveSlot(1);
            }
        };
        _mode = Mode;
        Title = _mode == ArmySourceSelectionMode.VanillaFactions
            ? "Choose your faction:"
            : "Choose your sectorials";
        PageHeading = _mode == ArmySourceSelectionMode.VanillaFactions
            ? "Choose your faction:"
            : "Choose your sectorials";

        _armyDataService = armyDataService;
        _specOpsProvider = SpecOpsProvider;
        _factionLogoCacheService = FactionLogoCacheService;
        _appSettingsProvider = AppSettingsProvider;
        _profileCoordinator = new StandardCompanyProfileCoordinator();

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
        AddProfileToMercsCompanyCommand = new Command<ViewerProfileItem>(AddProfileToMercsCompany);
        RemoveMercsCompanyEntryCommand = new Command<MercsCompanyEntry>(RemoveMercsCompanyEntry);
        SelectMercsCompanyEntryCommand = new Command<MercsCompanyEntry>(entry => _ = SelectMercsCompanyEntryAsync(entry));
        SelectTeamAllowedProfileCommand = new Command<ArmyTeamUnitLimitItem>(OnTeamAllowedProfileSelected);
        _startCompanyCommand = new Command(async () => await StartCompanyAsync(), () => IsCompanyValid);
        StartCompanyCommand = _startCompanyCommand;

        BindingContext = this;
        SetActiveSlot(0);
        RefreshSummaryFormatted();
        _ = LoadHeaderIconsAsync();
    }

    public ObservableCollection<ArmyFactionSelectionItem> Factions { get; } = [];
    public ObservableCollection<ArmyUnitSelectionItem> Units { get; } = [];
    public ObservableCollection<ArmyTeamListItem> TeamEntries { get; } = [];
    public ObservableCollection<ViewerProfileItem> Profiles { get; } = [];
    public ObservableCollection<MercsCompanyEntry> MercsCompanyEntries { get; } = [];

    public ICommand SelectFactionCommand { get; }
    public ICommand SelectUnitCommand { get; }
    public ICommand AddProfileToMercsCompanyCommand { get; }
    public ICommand RemoveMercsCompanyEntryCommand { get; }
    public ICommand SelectMercsCompanyEntryCommand { get; }
    public ICommand SelectTeamAllowedProfileCommand { get; }
    public ICommand StartCompanyCommand { get; }

    public bool ShowRightSelectionBox => _mode == ArmySourceSelectionMode.Sectorials;
    public string PageHeading
    {
        get => _pageHeading;
        private set
        {
            if (_pageHeading == value)
            {
                return;
            }

            _pageHeading = value;
            OnPropertyChanged();
        }
    }

    public string SelectedStartSeasonPoints
    {
        get => SeasonStartPointsView.SelectedStartSeasonPoints;
        set
        {
            if (SeasonStartPointsView.SelectedStartSeasonPoints == value)
            {
                return;
            }

            SeasonStartPointsView.SelectedStartSeasonPoints = value;
            OnPropertyChanged();
            UpdateSeasonValidationState();
            ApplyLieutenantVisualStates();
            _ = ApplyUnitVisibilityFiltersAsync();
        }
    }

    public string SeasonPointsCapText
    {
        get => SeasonStartPointsView.SeasonPointsCapText;
        set
        {
            if (SeasonStartPointsView.SeasonPointsCapText == value)
            {
                return;
            }

            SeasonStartPointsView.SeasonPointsCapText = value;
            OnPropertyChanged();
            UpdateSeasonValidationState();
            ApplyLieutenantVisualStates();
            _ = ApplyUnitVisibilityFiltersAsync();
        }
    }

    public string CompanyName
    {
        get => _companyName;
        set
        {
            if (_companyName == value)
            {
                return;
            }

            _companyName = value;
            OnPropertyChanged();
            if (_showCompanyNameValidationError && IsCompanyNameValid(value))
            {
                SetCompanyNameValidationError(false);
            }
        }
    }

    public bool IsCompanyValid
    {
        get => SeasonStartPointsView.IsCompanyValid;
        private set
        {
            if (SeasonStartPointsView.IsCompanyValid == value)
            {
                return;
            }

            SeasonStartPointsView.IsCompanyValid = value;
            OnPropertyChanged();
            _startCompanyCommand.ChangeCanExecute();
        }
    }

    public bool ShowCompanyNameValidationError
    {
        get => _showCompanyNameValidationError;
        private set
        {
            if (_showCompanyNameValidationError == value)
            {
                return;
            }

            _showCompanyNameValidationError = value;
            OnPropertyChanged();
        }
    }

    public Color CompanyNameBorderColor
    {
        get => _companyNameBorderColor;
        private set
        {
            if (_companyNameBorderColor == value)
            {
                return;
            }

            _companyNameBorderColor = value;
            OnPropertyChanged();
        }
    }
    public bool IsFactionSelectionActive
    {
        get => _isFactionSelectionActive;
        set
        {
            if (_isFactionSelectionActive == value)
            {
                return;
            }

            _isFactionSelectionActive = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(IsUnitSelectionActive));
        }
    }

    public bool IsUnitSelectionActive => !_isFactionSelectionActive;

    public string UnitNameHeading
    {
        get => UnitDisplayConfigurationsView.UnitNameHeading;
        private set
        {
            if (UnitDisplayConfigurationsView.UnitNameHeading == value)
            {
                return;
            }

            UnitDisplayConfigurationsView.UnitNameHeading = value;
            UpdateUnitNameHeadingFontSize();
        }
    }
    public double UnitNameHeadingFontSize
    {
        get => UnitDisplayConfigurationsView.UnitNameHeadingFontSize;
        private set
        {
            if (Math.Abs(UnitDisplayConfigurationsView.UnitNameHeadingFontSize - value) < 0.01d)
            {
                return;
            }

            UnitDisplayConfigurationsView.UnitNameHeadingFontSize = value;
        }
    }
    public Color UnitHeaderPrimaryColor
    {
        get => UnitDisplayConfigurationsView.UnitHeaderPrimaryColor;
        private set
        {
            if (UnitDisplayConfigurationsView.UnitHeaderPrimaryColor == value)
            {
                return;
            }

            UnitDisplayConfigurationsView.UnitHeaderPrimaryColor = value;
        }
    }
    public Color UnitHeaderSecondaryColor
    {
        get => UnitDisplayConfigurationsView.UnitHeaderSecondaryColor;
        private set
        {
            if (UnitDisplayConfigurationsView.UnitHeaderSecondaryColor == value)
            {
                return;
            }

            UnitDisplayConfigurationsView.UnitHeaderSecondaryColor = value;
        }
    }
    public Color UnitHeaderPrimaryTextColor
    {
        get => UnitDisplayConfigurationsView.UnitHeaderPrimaryTextColor;
        private set
        {
            if (UnitDisplayConfigurationsView.UnitHeaderPrimaryTextColor == value)
            {
                return;
            }

            UnitDisplayConfigurationsView.UnitHeaderPrimaryTextColor = value;
        }
    }
    public Color UnitHeaderSecondaryTextColor
    {
        get => UnitDisplayConfigurationsView.UnitHeaderSecondaryTextColor;
        private set
        {
            if (UnitDisplayConfigurationsView.UnitHeaderSecondaryTextColor == value)
            {
                return;
            }

            UnitDisplayConfigurationsView.UnitHeaderSecondaryTextColor = value;
        }
    }
    public string UnitMov { get => UnitDisplayConfigurationsView.UnitMov; private set => UnitDisplayConfigurationsView.UnitMov = value; }
    public string UnitCc { get => UnitDisplayConfigurationsView.UnitCc; private set => UnitDisplayConfigurationsView.UnitCc = value; }
    public string UnitBs { get => UnitDisplayConfigurationsView.UnitBs; private set => UnitDisplayConfigurationsView.UnitBs = value; }
    public string UnitPh { get => UnitDisplayConfigurationsView.UnitPh; private set => UnitDisplayConfigurationsView.UnitPh = value; }
    public string UnitWip { get => UnitDisplayConfigurationsView.UnitWip; private set => UnitDisplayConfigurationsView.UnitWip = value; }
    public string UnitArm { get => UnitDisplayConfigurationsView.UnitArm; private set => UnitDisplayConfigurationsView.UnitArm = value; }
    public string UnitBts { get => UnitDisplayConfigurationsView.UnitBts; private set => UnitDisplayConfigurationsView.UnitBts = value; }
    public string UnitVitalityHeader { get => UnitDisplayConfigurationsView.UnitVitalityHeader; private set => UnitDisplayConfigurationsView.UnitVitalityHeader = value; }
    public string UnitVitality { get => UnitDisplayConfigurationsView.UnitVitality; private set => UnitDisplayConfigurationsView.UnitVitality = value; }
    public string UnitS { get => UnitDisplayConfigurationsView.UnitS; private set => UnitDisplayConfigurationsView.UnitS = value; }
    public string UnitAva { get => UnitDisplayConfigurationsView.UnitAva; private set => UnitDisplayConfigurationsView.UnitAva = value; }
    public bool HasPeripheralStatBlock { get => UnitDisplayConfigurationsView.HasPeripheralStatBlock; private set => UnitDisplayConfigurationsView.HasPeripheralStatBlock = value; }
    public string PeripheralNameHeading { get => UnitDisplayConfigurationsView.PeripheralNameHeading; private set => UnitDisplayConfigurationsView.PeripheralNameHeading = value; }
    public string PeripheralMov { get => UnitDisplayConfigurationsView.PeripheralMov; private set => UnitDisplayConfigurationsView.PeripheralMov = value; }
    public string PeripheralCc { get => UnitDisplayConfigurationsView.PeripheralCc; private set => UnitDisplayConfigurationsView.PeripheralCc = value; }
    public string PeripheralBs { get => UnitDisplayConfigurationsView.PeripheralBs; private set => UnitDisplayConfigurationsView.PeripheralBs = value; }
    public string PeripheralPh { get => UnitDisplayConfigurationsView.PeripheralPh; private set => UnitDisplayConfigurationsView.PeripheralPh = value; }
    public string PeripheralWip { get => UnitDisplayConfigurationsView.PeripheralWip; private set => UnitDisplayConfigurationsView.PeripheralWip = value; }
    public string PeripheralArm { get => UnitDisplayConfigurationsView.PeripheralArm; private set => UnitDisplayConfigurationsView.PeripheralArm = value; }
    public string PeripheralBts { get => UnitDisplayConfigurationsView.PeripheralBts; private set => UnitDisplayConfigurationsView.PeripheralBts = value; }
    public string PeripheralVitalityHeader { get => UnitDisplayConfigurationsView.PeripheralVitalityHeader; private set => UnitDisplayConfigurationsView.PeripheralVitalityHeader = value; }
    public string PeripheralVitality { get => UnitDisplayConfigurationsView.PeripheralVitality; private set => UnitDisplayConfigurationsView.PeripheralVitality = value; }
    public string PeripheralS { get => UnitDisplayConfigurationsView.PeripheralS; private set => UnitDisplayConfigurationsView.PeripheralS = value; }
    public string PeripheralAva { get => UnitDisplayConfigurationsView.PeripheralAva; private set => UnitDisplayConfigurationsView.PeripheralAva = value; }
    public string PeripheralEquipment
    {
        get => UnitDisplayConfigurationsView.PeripheralEquipment;
        private set
        {
            if (UnitDisplayConfigurationsView.PeripheralEquipment == value)
            {
                return;
            }

            UnitDisplayConfigurationsView.PeripheralEquipment = value;
        }
    }
    public string PeripheralSkills
    {
        get => UnitDisplayConfigurationsView.PeripheralSkills;
        private set
        {
            if (UnitDisplayConfigurationsView.PeripheralSkills == value)
            {
                return;
            }

            UnitDisplayConfigurationsView.PeripheralSkills = value;
        }
    }
    public string EquipmentSummary { get => UnitDisplayConfigurationsView.EquipmentSummary; private set { if (UnitDisplayConfigurationsView.EquipmentSummary != value) { UnitDisplayConfigurationsView.EquipmentSummary = value; } } }
    public string SpecialSkillsSummary { get => UnitDisplayConfigurationsView.SpecialSkillsSummary; private set { if (UnitDisplayConfigurationsView.SpecialSkillsSummary != value) { UnitDisplayConfigurationsView.SpecialSkillsSummary = value; } } }
    public string ProfilesStatus { get => _profilesStatus; private set { if (_profilesStatus != value) { _profilesStatus = value; OnPropertyChanged(); } } }
    public FormattedString EquipmentSummaryFormatted { get => UnitDisplayConfigurationsView.EquipmentSummaryFormatted; private set => UnitDisplayConfigurationsView.EquipmentSummaryFormatted = value; }
    public FormattedString SpecialSkillsSummaryFormatted { get => UnitDisplayConfigurationsView.SpecialSkillsSummaryFormatted; private set => UnitDisplayConfigurationsView.SpecialSkillsSummaryFormatted = value; }
    public FormattedString PeripheralEquipmentFormatted { get => UnitDisplayConfigurationsView.PeripheralEquipmentFormatted; private set => UnitDisplayConfigurationsView.PeripheralEquipmentFormatted = value; }
    public FormattedString PeripheralSkillsFormatted { get => UnitDisplayConfigurationsView.PeripheralSkillsFormatted; private set => UnitDisplayConfigurationsView.PeripheralSkillsFormatted = value; }
    public bool HasPeripheralEquipment => UnitDisplayConfigurationsView.HasPeripheralEquipment;
    public bool HasPeripheralSkills => UnitDisplayConfigurationsView.HasPeripheralSkills;
    public bool HasAnyTopHeaderIcons => ShowRegularOrderIcon || ShowIrregularOrderIcon || ShowImpetuousIcon || ShowTacticalAwarenessIcon;
    public bool HasAnyBottomHeaderIcons => ShowCubeIcon || ShowCube2Icon || ShowHackableIcon;
    public bool HasAnyHeaderIcons => HasAnyTopHeaderIcons || HasAnyBottomHeaderIcons;

    public bool ShowRegularOrderIcon
    {
        get => UnitDisplayConfigurationsView.ShowRegularOrderIcon;
        private set
        {
            if (UnitDisplayConfigurationsView.ShowRegularOrderIcon == value)
            {
                return;
            }

            UnitDisplayConfigurationsView.ShowRegularOrderIcon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAnyTopHeaderIcons));
            OnPropertyChanged(nameof(HasAnyHeaderIcons));
            UnitDisplayConfigurationsView.InvalidateHeaderIconsCanvas();
        }
    }

    public bool ShowIrregularOrderIcon
    {
        get => UnitDisplayConfigurationsView.ShowIrregularOrderIcon;
        private set
        {
            if (UnitDisplayConfigurationsView.ShowIrregularOrderIcon == value)
            {
                return;
            }

            UnitDisplayConfigurationsView.ShowIrregularOrderIcon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAnyTopHeaderIcons));
            OnPropertyChanged(nameof(HasAnyHeaderIcons));
            UnitDisplayConfigurationsView.InvalidateHeaderIconsCanvas();
        }
    }

    public bool ShowImpetuousIcon
    {
        get => UnitDisplayConfigurationsView.ShowImpetuousIcon;
        private set
        {
            if (UnitDisplayConfigurationsView.ShowImpetuousIcon == value)
            {
                return;
            }

            UnitDisplayConfigurationsView.ShowImpetuousIcon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAnyTopHeaderIcons));
            OnPropertyChanged(nameof(HasAnyHeaderIcons));
            UnitDisplayConfigurationsView.InvalidateHeaderIconsCanvas();
        }
    }

    public bool ShowTacticalAwarenessIcon
    {
        get => UnitDisplayConfigurationsView.ShowTacticalAwarenessIcon;
        private set
        {
            if (UnitDisplayConfigurationsView.ShowTacticalAwarenessIcon == value)
            {
                return;
            }

            UnitDisplayConfigurationsView.ShowTacticalAwarenessIcon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAnyTopHeaderIcons));
            OnPropertyChanged(nameof(HasAnyHeaderIcons));
            UnitDisplayConfigurationsView.InvalidateHeaderIconsCanvas();
        }
    }

    public bool ShowCubeIcon
    {
        get => UnitDisplayConfigurationsView.ShowCubeIcon;
        private set
        {
            if (UnitDisplayConfigurationsView.ShowCubeIcon == value)
            {
                return;
            }

            UnitDisplayConfigurationsView.ShowCubeIcon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAnyBottomHeaderIcons));
            OnPropertyChanged(nameof(HasAnyHeaderIcons));
            UnitDisplayConfigurationsView.InvalidateHeaderIconsCanvas();
        }
    }

    public bool ShowCube2Icon
    {
        get => UnitDisplayConfigurationsView.ShowCube2Icon;
        private set
        {
            if (UnitDisplayConfigurationsView.ShowCube2Icon == value)
            {
                return;
            }

            UnitDisplayConfigurationsView.ShowCube2Icon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAnyBottomHeaderIcons));
            OnPropertyChanged(nameof(HasAnyHeaderIcons));
            UnitDisplayConfigurationsView.InvalidateHeaderIconsCanvas();
        }
    }

    public bool ShowHackableIcon
    {
        get => UnitDisplayConfigurationsView.ShowHackableIcon;
        private set
        {
            if (UnitDisplayConfigurationsView.ShowHackableIcon == value)
            {
                return;
            }

            UnitDisplayConfigurationsView.ShowHackableIcon = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasAnyBottomHeaderIcons));
            OnPropertyChanged(nameof(HasAnyHeaderIcons));
            UnitDisplayConfigurationsView.InvalidateHeaderIconsCanvas();
        }
    }

    private bool ShowUnitsInInches
    {
        get => UnitDisplayConfigurationsView.ShowUnitsInInches;
        set => UnitDisplayConfigurationsView.ShowUnitsInInches = value;
    }

    private int? UnitMoveFirstCm
    {
        get => UnitDisplayConfigurationsView.UnitMoveFirstCm;
        set => UnitDisplayConfigurationsView.UnitMoveFirstCm = value;
    }

    private int? UnitMoveSecondCm
    {
        get => UnitDisplayConfigurationsView.UnitMoveSecondCm;
        set => UnitDisplayConfigurationsView.UnitMoveSecondCm = value;
    }

    private int? PeripheralMoveFirstCm
    {
        get => UnitDisplayConfigurationsView.PeripheralMoveFirstCm;
        set => UnitDisplayConfigurationsView.PeripheralMoveFirstCm = value;
    }

    private int? PeripheralMoveSecondCm
    {
        get => UnitDisplayConfigurationsView.PeripheralMoveSecondCm;
        set => UnitDisplayConfigurationsView.PeripheralMoveSecondCm = value;
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
            ApplyLieutenantVisualStates();
            _ = ApplyUnitVisibilityFiltersAsync();
        }
    }

    public bool TeamsView
    {
        get => _showFireteams;
        set
        {
            if (_showFireteams == value)
            {
                return;
            }

            _showFireteams = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ShowUnitsList));
            OnPropertyChanged(nameof(ShowTeamsList));
        }
    }

    public bool ShowUnitsList => !TeamsView;
    public bool ShowTeamsList => TeamsView;

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

    protected override Task LoadFactionsAsync()
    {
        return LoadFactionsAsync(CancellationToken.None);
    }

    protected override Task LoadUnitsForActiveSlotAsync()
    {
        return LoadUnitsForActiveSlotAsync(CancellationToken.None);
    }

    protected override Task LoadSelectedUnitDetailsAsync()
    {
        return LoadSelectedUnitDetailsAsync(CancellationToken.None);
    }

    private async Task LoadFactionsAsync(CancellationToken cancellationToken = default)
    {
        try
        {
            var factions = _armyDataService
                .GetMetadataFactions(includeDiscontinued: true, cancellationToken)
                .ToList();

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

            // Hide Non-Aligned Armies from this selector.
            filtered = filtered.Where(x => !IsNonAlignedArmyName(x.Name));

            // If both variants exist, keep only the non-all-caps "Contracted Back-Up".
            filtered = CollapseContractedBackUpVariants(filtered);

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

    private static IEnumerable<FactionRecord> CollapseContractedBackUpVariants(IEnumerable<FactionRecord> factions)
    {
        var list = factions.ToList();
        var contracted = list
            .Where(x => IsContractedBackUpName(x.Name))
            .ToList();

        if (contracted.Count <= 1)
        {
            return list;
        }

        var preferred = contracted.FirstOrDefault(x => !LooksAllCaps(x.Name))
            ?? contracted.First();

        return list.Where(x => !IsContractedBackUpName(x.Name) || x.Id == preferred.Id);
    }

    private static bool IsNonAlignedArmyName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        return name.Trim().Equals("Non-Aligned Armies", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsContractedBackUpName(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return false;
        }

        var normalized = Regex.Replace(name.Trim(), @"[\s\-]+", " ").ToLowerInvariant();
        return normalized == "contracted back up";
    }

    private static bool LooksAllCaps(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return false;
        }

        var letters = value.Where(char.IsLetter).ToArray();
        if (letters.Length == 0)
        {
            return false;
        }

        return letters.All(char.IsUpper);
    }

    private void SetSelectedFaction(ArmyFactionSelectionItem item)
    {
        if (_factionSelectionState.SelectedFaction == item)
        {
            AssignSelectedFactionToActiveSlot(item);
            return;
        }

        if (_factionSelectionState.SelectedFaction is not null)
        {
            _factionSelectionState.SelectedFaction.IsSelected = false;
        }

        _factionSelectionState.SelectedFaction = item;
        _factionSelectionState.SelectedFaction.IsSelected = true;
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
            factionChanged = _factionSelectionState.LeftSlotFaction?.Id != item.Id;
            _factionSelectionState.LeftSlotFaction = item;
            FactionSlotSelectorView.LeftSlotText = item.Name;
            _ = LoadSlotIconAsync(0, item.CachedLogoPath, item.PackagedLogoPath);
        }
        else
        {
            factionChanged = _factionSelectionState.RightSlotFaction?.Id != item.Id;
            _factionSelectionState.RightSlotFaction = item;
            FactionSlotSelectorView.RightSlotText = item.Name;
            _ = LoadSlotIconAsync(1, item.CachedLogoPath, item.PackagedLogoPath);
        }

        AutoSelectEmptySlot();
        if (factionChanged)
        {
            ResetMercsCompany();
            _ = LoadUnitsForActiveSlotAsync();
        }
    }

    private void ResetMercsCompany()
    {
        if (MercsCompanyEntries.Count == 0)
        {
            UpdateMercsCompanyTotal();
            return;
        }

        MercsCompanyEntries.Clear();
        UpdateMercsCompanyTotal();
    }

    private bool IsDuplicateSelectionForActiveSlot(ArmyFactionSelectionItem item)
    {
        if (!ShowRightSelectionBox)
        {
            return false;
        }

        if (_activeSlotIndex == 0)
        {
            return _factionSelectionState.RightSlotFaction is not null
                && _factionSelectionState.RightSlotFaction.Id == item.Id
                && (_factionSelectionState.LeftSlotFaction is null || _factionSelectionState.LeftSlotFaction.Id != item.Id);
        }

        return _factionSelectionState.LeftSlotFaction is not null
            && _factionSelectionState.LeftSlotFaction.Id == item.Id
            && (_factionSelectionState.RightSlotFaction is null || _factionSelectionState.RightSlotFaction.Id != item.Id);
    }

    private void AutoSelectEmptySlot()
    {
        if (!ShowRightSelectionBox)
        {
            SetActiveSlot(0);
            return;
        }

        var leftEmpty = _factionSelectionState.LeftSlotFaction is null;
        var rightEmpty = _factionSelectionState.RightSlotFaction is null;

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
        FactionSlotSelectorView.ApplyActiveSlotBorders(_activeSlotIndex);
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

    private void AddProfileToMercsCompany(ViewerProfileItem? profile)
    {
        if (profile is null || _selectedUnit is null)
        {
            return;
        }

        // Block add for configurations currently marked unavailable (points, lieutenant, AVA, etc).
        if (!profile.IsVisible || profile.IsLieutenantBlocked)
        {
            return;
        }

        var combinedEquipment = CompanyProfileTextService.MergeCommonAndUnique(UnitDisplayConfigurationsView.SelectedUnitCommonEquipment, profile.UniqueEquipment);
        var combinedSkills = CompanyProfileTextService.MergeCommonAndUnique(UnitDisplayConfigurationsView.SelectedUnitCommonSkills, profile.UniqueSkills);
        var combinedEquipmentText = CompanyProfileTextService.JoinOrDash(combinedEquipment);
        var combinedSkillsText = CompanyProfileTextService.JoinOrDash(combinedSkills);
        var currentUnitMove = FormatMoveValue(UnitMoveFirstCm, UnitMoveSecondCm);
        var statline = $"MOV {UnitMov} | CC {UnitCc} | BS {UnitBs} | PH {UnitPh} | WIP {UnitWip} | ARM {UnitArm} | BTS {UnitBts} | {UnitVitalityHeader} {UnitVitality} | S {UnitS}";
        var peripheralStats = BuildMercsCompanyPeripheralStats(profile);
        var entry = new MercsCompanyEntry
        {
            Name = profile.Name,
            NameFormatted = profile.NameFormatted ?? CompanyProfileTextService.BuildNameFormatted(profile.Name),
            Subtitle = statline,
            UnitTypeCode = ExtractUnitTypeCode(_selectedUnit.Subtitle),
            CostDisplay = $"C {profile.Cost}",
            CostValue = ParseCostValue(profile.Cost),
            IsLieutenant = profile.IsLieutenant,
            ProfileKey = profile.ProfileKey,
            SourceUnitId = _selectedUnit.Id,
            SourceFactionId = _selectedUnit.SourceFactionId,
            CachedLogoPath = _selectedUnit.CachedLogoPath,
            PackagedLogoPath = _selectedUnit.PackagedLogoPath,
            SavedEquipment = combinedEquipmentText,
            SavedSkills = combinedSkillsText,
            SavedRangedWeapons = profile.RangedWeapons,
            SavedCcWeapons = profile.MeleeWeapons,
            ExperiencePoints = 0,
            EquipmentLineFormatted = CompanyProfileTextService.BuildMercsCompanyLineFormatted("Equipment", combinedEquipmentText, Color.FromArgb("#06B6D4")),
            HasEquipmentLine = combinedEquipment.Count > 0,
            SkillsLineFormatted = CompanyProfileTextService.BuildMercsCompanyLineFormatted("Skills", combinedSkillsText, Color.FromArgb("#F59E0B")),
            HasSkillsLine = combinedSkills.Count > 0,
            RangedLineFormatted = CompanyProfileTextService.BuildMercsCompanyLineFormatted("Ranged Weapons", profile.RangedWeapons, Color.FromArgb("#EF4444")),
            CcLineFormatted = CompanyProfileTextService.BuildMercsCompanyLineFormatted("CC Weapons", profile.MeleeWeapons, Color.FromArgb("#22C55E")),
            HasPeripheralStatBlock = peripheralStats is not null,
            PeripheralNameHeading = peripheralStats?.NameHeading ?? string.Empty,
            PeripheralMov = peripheralStats is null ? "-" : FormatMoveValue(peripheralStats.MoveFirstCm, peripheralStats.MoveSecondCm),
            PeripheralCc = peripheralStats?.Cc ?? "-",
            PeripheralBs = peripheralStats?.Bs ?? "-",
            PeripheralPh = peripheralStats?.Ph ?? "-",
            PeripheralWip = peripheralStats?.Wip ?? "-",
            PeripheralArm = peripheralStats?.Arm ?? "-",
            PeripheralBts = peripheralStats?.Bts ?? "-",
            PeripheralVitalityHeader = peripheralStats?.VitalityHeader ?? "VITA",
            PeripheralVitality = peripheralStats?.Vitality ?? "-",
            PeripheralS = peripheralStats?.S ?? "-",
            PeripheralAva = peripheralStats?.Ava ?? "-",
            SavedPeripheralEquipment = peripheralStats?.Equipment ?? "-",
            SavedPeripheralSkills = peripheralStats?.Skills ?? "-",
            PeripheralEquipmentLineFormatted = CompanyProfileTextService.BuildMercsCompanyLineFormatted("Equipment", peripheralStats?.Equipment, Color.FromArgb("#06B6D4")),
            HasPeripheralEquipmentLine = peripheralStats is not null && !string.IsNullOrWhiteSpace(peripheralStats.Equipment) && peripheralStats.Equipment != "-",
            PeripheralSkillsLineFormatted = CompanyProfileTextService.BuildMercsCompanyLineFormatted("Skills", peripheralStats?.Skills, Color.FromArgb("#F59E0B")),
            HasPeripheralSkillsLine = peripheralStats is not null && !string.IsNullOrWhiteSpace(peripheralStats.Skills) && peripheralStats.Skills != "-",
            UnitMoveFirstCm = UnitMoveFirstCm,
            UnitMoveSecondCm = UnitMoveSecondCm,
            UnitMoveDisplay = currentUnitMove,
            PeripheralMoveFirstCm = peripheralStats?.MoveFirstCm,
            PeripheralMoveSecondCm = peripheralStats?.MoveSecondCm
        };

        if (entry.IsLieutenant)
        {
            MercsCompanyEntries.Insert(0, entry);
        }
        else
        {
            MercsCompanyEntries.Add(entry);
        }

        UpdateMercsCompanyTotal();
        ApplyLieutenantVisualStates();
        _ = ApplyUnitVisibilityFiltersAsync();
    }

    private PeripheralMercsCompanyStats? BuildMercsCompanyPeripheralStats(ViewerProfileItem profile)
    {
        var peripheralName = ExtractFirstPeripheralName(profile.Peripherals);
        if (string.IsNullOrWhiteSpace(peripheralName) || string.IsNullOrWhiteSpace(UnitDisplayConfigurationsView.SelectedUnitProfileGroupsJson))
        {
            return null;
        }

        try
        {
            using var doc = JsonDocument.Parse(UnitDisplayConfigurationsView.SelectedUnitProfileGroupsJson);
            if (!TryFindPeripheralStatElement(doc.RootElement, peripheralName, out var peripheralProfile))
            {
                return null;
            }

            return BuildPeripheralStatBlock(peripheralName, peripheralProfile, UnitDisplayConfigurationsView.SelectedUnitFiltersJson);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage BuildMercsCompanyPeripheralStats failed: {ex.Message}");
            return null;
        }
    }

    private void RemoveMercsCompanyEntry(MercsCompanyEntry? entry)
    {
        if (entry is null)
        {
            return;
        }

        MercsCompanyEntries.Remove(entry);
        UpdateMercsCompanyTotal();
        ApplyLieutenantVisualStates();
        _ = ApplyUnitVisibilityFiltersAsync();
    }

    private async Task SelectMercsCompanyEntryAsync(MercsCompanyEntry? entry, CancellationToken cancellationToken = default)
    {
        if (entry is null)
        {
            return;
        }

        try
        {
            // Prefer existing unit item (even if hidden), otherwise build a temporary item
            // so details can load regardless of current list visibility filters.
            var unitItem = Units.FirstOrDefault(x =>
                x.Id == entry.SourceUnitId &&
                x.SourceFactionId == entry.SourceFactionId);

            if (unitItem is null)
            {
                var unitRecord = _armyDataService.GetUnit(entry.SourceFactionId, entry.SourceUnitId, cancellationToken);
                var unitName = !string.IsNullOrWhiteSpace(unitRecord?.Name)
                    ? unitRecord.Name
                    : entry.Name;

                unitItem = new ArmyUnitSelectionItem
                {
                    Id = entry.SourceUnitId,
                    SourceFactionId = entry.SourceFactionId,
                    Name = unitName,
                    CachedLogoPath = entry.CachedLogoPath,
                    PackagedLogoPath = entry.PackagedLogoPath,
                    Subtitle = null,
                    IsVisible = false
                };
            }

            SetSelectedUnit(unitItem);
            // Force-refresh details/configurations even if the selected unit instance
            // did not change (SetSelectedUnit can short-circuit on same instance).
            await LoadSelectedUnitDetailsAsync(cancellationToken);
            IsFactionSelectionActive = false;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage SelectMercsCompanyEntryAsync failed: {ex.Message}");
        }
    }

    private void ApplyLieutenantVisualStates()
    {
        var hasActiveLieutenant = MercsCompanyEntries.Any(x => x.IsLieutenant);
        var pointsLimit = int.TryParse(SelectedStartSeasonPoints, out var parsedLimit) ? parsedLimit : 0;
        var currentPoints = int.TryParse(SeasonPointsCapText, out var parsedPoints) ? parsedPoints : 0;
        var pointsRemaining = pointsLimit - currentPoints;
        var avaLimit = ParseAvaLimit(UnitAva);
        var selectedUnitCountInCompany = _selectedUnit is null
            ? 0
            : MercsCompanyEntries.Count(x =>
                x.SourceUnitId == _selectedUnit.Id &&
                x.SourceFactionId == _selectedUnit.SourceFactionId);
        var avaReached = avaLimit.HasValue && selectedUnitCountInCompany >= avaLimit.Value;
        var visibleProfiles = 0;

        foreach (var profile in Profiles)
        {
            var profileCost = ParseCostValue(profile.Cost);
            var overRemainingPoints = profileCost > pointsRemaining;
            var belowMinFilterPoints = _activeUnitFilter.MinPoints.HasValue && profileCost < _activeUnitFilter.MinPoints.Value;
            var aboveMaxFilterPoints = _activeUnitFilter.MaxPoints.HasValue && profileCost > _activeUnitFilter.MaxPoints.Value;
            var lieutenantFilteredOut = LieutenantOnlyUnits && !profile.IsLieutenant;

            profile.IsVisible = !lieutenantFilteredOut &&
                                !overRemainingPoints &&
                                !belowMinFilterPoints &&
                                !aboveMaxFilterPoints;
            profile.IsLieutenantBlocked =
                (hasActiveLieutenant && profile.IsLieutenant) ||
                overRemainingPoints ||
                avaReached;

            if (profile.IsVisible)
            {
                visibleProfiles++;
            }
        }

        ProfilesStatus = visibleProfiles == 0
            ? "No configurations found for this unit."
            : $"{visibleProfiles} configurations loaded.";

        UpdatePeripheralStatBlockFromVisibleProfiles();
        UpdateSeasonValidationState();
    }

    private static int? ParseAvaLimit(string? ava)
    {
        return CompanySelectionSharedUtilities.ParseAvaLimit(ava);
    }

    private async Task ApplyUnitVisibilityFiltersAsync(CancellationToken cancellationToken = default)
    {
        if (Units.Count == 0)
        {
            return;
        }

        try
        {
            var pointsLimit = int.TryParse(SelectedStartSeasonPoints, out var parsedLimit) ? parsedLimit : 0;
            var currentPoints = int.TryParse(SeasonPointsCapText, out var parsedPoints) ? parsedPoints : 0;
            var pointsRemaining = pointsLimit - currentPoints;

            var factions = GetUnitSourceFactions();
            var skillsLookupByFaction = new Dictionary<int, IReadOnlyDictionary<int, string>>();
            var typeLookupByFaction = new Dictionary<int, IReadOnlyDictionary<int, string>>();
            var charsLookupByFaction = new Dictionary<int, IReadOnlyDictionary<int, string>>();
            var equipLookupByFaction = new Dictionary<int, IReadOnlyDictionary<int, string>>();
            var weaponsLookupByFaction = new Dictionary<int, IReadOnlyDictionary<int, string>>();
            var ammoLookupByFaction = new Dictionary<int, IReadOnlyDictionary<int, string>>();
            var specopsByFaction = new Dictionary<int, Dictionary<int, ArmySpecopsUnitRecord>>();
            foreach (var faction in factions)
            {
                var snapshot = _armyDataService.GetFactionSnapshot(faction.Id, cancellationToken);
                skillsLookupByFaction[faction.Id] = BuildIdNameLookup(snapshot?.FiltersJson, "skills");
                typeLookupByFaction[faction.Id] = BuildIdNameLookup(snapshot?.FiltersJson, "type");
                charsLookupByFaction[faction.Id] = BuildIdNameLookup(snapshot?.FiltersJson, "chars");
                equipLookupByFaction[faction.Id] = BuildIdNameLookup(snapshot?.FiltersJson, "equip");
                weaponsLookupByFaction[faction.Id] = BuildIdNameLookup(snapshot?.FiltersJson, "weapons");
                ammoLookupByFaction[faction.Id] = BuildIdNameLookup(snapshot?.FiltersJson, "ammunition");
                var specopsUnits = await _specOpsProvider.GetSpecopsUnitsByFactionAsync(faction.Id, cancellationToken);
                specopsByFaction[faction.Id] = specopsUnits
                    .GroupBy(x => x.UnitId)
                    .ToDictionary(x => x.Key, x => x.First());
            }

            foreach (var unit in Units)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!skillsLookupByFaction.TryGetValue(unit.SourceFactionId, out var skillsLookup))
                {
                    unit.IsVisible = false;
                    continue;
                }
                typeLookupByFaction.TryGetValue(unit.SourceFactionId, out var typeLookup);
                charsLookupByFaction.TryGetValue(unit.SourceFactionId, out var charsLookup);
                equipLookupByFaction.TryGetValue(unit.SourceFactionId, out var equipLookup);
                weaponsLookupByFaction.TryGetValue(unit.SourceFactionId, out var weaponsLookup);
                ammoLookupByFaction.TryGetValue(unit.SourceFactionId, out var ammoLookup);

                if (!MatchesClassificationFilter(unit, typeLookup ?? new Dictionary<int, string>()))
                {
                    unit.IsVisible = false;
                    continue;
                }

                var unitRecord = _armyDataService.GetUnit(unit.SourceFactionId, unit.Id, cancellationToken);
                var profileGroupsJson = unitRecord?.ProfileGroupsJson;
                if (specopsByFaction.TryGetValue(unit.SourceFactionId, out var specopsUnitsById) &&
                    specopsUnitsById.TryGetValue(unit.Id, out var specopsUnit))
                {
                    if (unit.IsSpecOps || string.IsNullOrWhiteSpace(profileGroupsJson))
                    {
                        profileGroupsJson = specopsUnit.ProfileGroupsJson;
                    }
                }

                unit.IsVisible = StandardCompanyUnitFilterService.UnitHasVisibleOptionWithFilter(
                    profileGroupsJson,
                    skillsLookup,
                    charsLookup ?? new Dictionary<int, string>(),
                    equipLookup ?? new Dictionary<int, string>(),
                    weaponsLookup ?? new Dictionary<int, string>(),
                    ammoLookup ?? new Dictionary<int, string>(),
                    _activeUnitFilter,
                    requireLieutenant: LieutenantOnlyUnits && !unit.IsSpecOps,
                    requireZeroSwc: true,
                    maxCost: pointsRemaining);
            }

            if (_selectedUnit is not null && !_selectedUnit.IsVisible)
            {
                _selectedUnit.IsSelected = false;
                _selectedUnit = null;
                ResetUnitDetails();
            }
            else if (_selectedUnit is not null)
            {
                // Recompute configuration visibility for the selected unit after filter changes.
                ApplyLieutenantVisualStates();
            }

            StandardCompanyTeamService.RefreshTeamEntryVisibility(TeamEntries, Units);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"ArmyFactionSelectionPage ApplyUnitVisibilityFiltersAsync failed: {ex.Message}");
        }
    }



    private void OnTeamAllowedProfileSelected(ArmyTeamUnitLimitItem? teamItem)
    {
        if (teamItem is null)
        {
            Console.Error.WriteLine("ArmyFactionSelectionPage OnTeamAllowedProfileTappedFromView: no team item binding context.");
            return;
        }

        ArmyUnitSelectionItem? resolved = null;
        if (teamItem.ResolvedUnitId.HasValue && teamItem.ResolvedSourceFactionId.HasValue)
        {
            resolved = Units.FirstOrDefault(x =>
                x.Id == teamItem.ResolvedUnitId.Value &&
                x.SourceFactionId == teamItem.ResolvedSourceFactionId.Value);
        }

        resolved ??= Units.FirstOrDefault(x =>
            !string.IsNullOrWhiteSpace(teamItem.Slug) &&
            !string.IsNullOrWhiteSpace(x.Slug) &&
            string.Equals(x.Slug.Trim(), teamItem.Slug.Trim(), StringComparison.OrdinalIgnoreCase));

        resolved ??= Units.FirstOrDefault(x =>
            string.Equals(x.Name, teamItem.Name, StringComparison.OrdinalIgnoreCase));

        if (resolved is null)
        {
            Console.Error.WriteLine(
                $"ArmyFactionSelectionPage OnTeamAllowedProfileTappedFromView: unable to resolve unit for team entry '{teamItem.Name}'.");
            return;
        }

        SetSelectedUnit(resolved);
    }


}

